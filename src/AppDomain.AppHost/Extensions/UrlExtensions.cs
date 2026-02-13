// Copyright (c) OrgName. All rights reserved.

using Microsoft.Extensions.Logging;

namespace AppDomain.AppHost.Extensions;

/// <summary>
/// Represents a parsed endpoint specification with scheme and optional port.
/// </summary>
internal record EndpointSpec(string Scheme, int? Port)
{
    /// <summary>
    /// Gets a value indicating whether this specification is scheme-only (no port specified).
    /// </summary>
    public bool IsSchemeOnly => Port == null;
}

public static class UrlExtensions
{
    /// <summary>
    /// Configures endpoint URLs with enhanced port-specific filtering and comprehensive error handling.
    /// </summary>
    /// <typeparam name="T">The resource type.</typeparam>
    /// <param name="builder">The resource builder.</param>
    /// <param name="endpoints">Pipe-separated list of endpoint specifications (e.g., "http", "https:8443", "http:8080|https").</param>
    /// <param name="displayText">The display text for the endpoint URL.</param>
    /// <param name="url">The URL path (default: "/").</param>
    /// <param name="logger">Optional logger for warning and error messages.</param>
    /// <returns>The resource builder for method chaining.</returns>
    public static IResourceBuilder<T> WithEndpointUrl<T>(this IResourceBuilder<T> builder,
        string endpoints, string displayText, string url = "/", ILogger? logger = null) where T : IResource
    {
        if (builder == null)
            throw new ArgumentNullException(nameof(builder));

        if (string.IsNullOrWhiteSpace(endpoints))
        {
            logger?.LogWarning("Empty or null endpoints specification provided to WithEndpointUrl, no endpoint URLs will be configured");

            return builder;
        }

        var endpointsList = endpoints.Split("|");
        logger?.LogDebug("Processing {EndpointCount} endpoint specifications: {Endpoints}", endpointsList.Length, endpoints);

        // Parse endpoint specifications to support port-specific matching
        var endpointSpecs = new List<EndpointSpec>();
        var invalidEndpoints = new List<string>();

        foreach (var endpointString in endpointsList)
        {
            var trimmedEndpoint = endpointString.Trim();

            if (string.IsNullOrEmpty(trimmedEndpoint))
            {
                logger?.LogWarning("Skipping empty endpoint specification in '{Endpoints}'", endpoints);

                continue;
            }

            try
            {
                var spec = ParseEndpointSpec(trimmedEndpoint, logger);
                endpointSpecs.Add(spec);
                logger?.LogDebug("Parsed endpoint specification: {Scheme}:{Port}", spec.Scheme, spec.Port?.ToString() ?? "any");
            }
            catch (ArgumentException ex)
            {
                invalidEndpoints.Add(trimmedEndpoint);
                logger?.LogWarning(ex, "Invalid endpoint specification '{EndpointString}' will be skipped", trimmedEndpoint);
            }
        }

        if (endpointSpecs.Count == 0)
        {
            logger?.LogWarning("No valid endpoint specifications found in '{Endpoints}', no endpoint URLs will be configured", endpoints);

            return builder;
        }

        if (invalidEndpoints.Count > 0)
        {
            logger?.LogInformation(
                "Successfully parsed {ValidCount} out of {TotalCount} endpoint specifications. Invalid: [{InvalidEndpoints}]",
                endpointSpecs.Count, endpointsList.Length, string.Join(", ", invalidEndpoints));
        }

        builder.WithUrls(context =>
        {
            try
            {
                // Find the first URL that matches any of our endpoint specifications
                var urlForEndpoint = context.Urls.FirstOrDefault(u =>
                    endpointSpecs.Any(spec => MatchesEndpointSpec(spec, u)));

                if (urlForEndpoint is not null)
                {
                    urlForEndpoint.Url = url;
                    urlForEndpoint.DisplayText = displayText;
                    logger?.LogDebug(
                        "Applied endpoint URL configuration: DisplayText='{DisplayText}', Url='{Url}' to endpoint '{EndpointName}'",
                        displayText, url, GetEndpointName(urlForEndpoint));
                }
                else
                {
                    logger?.LogWarning(
                        "No matching endpoints found for specifications '{Endpoints}'. Available endpoints: [{AvailableEndpoints}]",
                        endpoints, string.Join(", ", context.Urls.Select(u => GetEndpointName(u) ?? "unknown")));
                }
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "Error occurred while applying endpoint URL configuration for '{Endpoints}'", endpoints);
                // Don't rethrow - allow the application to continue with other endpoints
            }
        });

        return builder;
    }

    /// <summary>
    /// Helper method to safely extract endpoint name from URL context using reflection.
    /// </summary>
    private static string? GetEndpointName<T>(T urlContext) where T : class
    {
        try
        {
            var endpointProperty = typeof(T).GetProperty("Endpoint");
            var endpoint = endpointProperty?.GetValue(urlContext);

            if (endpoint == null) return null;

            var endpointNameProperty = endpoint.GetType().GetProperty("EndpointName");

            return endpointNameProperty?.GetValue(endpoint) as string;
        }
        catch (Exception)
        {
            return null;
        }
    }

    /// <summary>
    /// Determines if a URL context matches the given EndpointSpec.
    /// Supports both scheme-only and scheme-with-port matching.
    /// </summary>
    /// <param name="spec">The EndpointSpec to match against.</param>
    /// <param name="urlContext">The URL context containing endpoint and URL information.</param>
    /// <returns>True if the URL context matches the specification, false otherwise.</returns>
    internal static bool MatchesEndpointSpec<T>(EndpointSpec spec, T? urlContext) where T : class
    {
        if (urlContext == null)
        {
            return false;
        }

        // Use reflection to get the endpoint name and URL information
        var endpointProperty = typeof(T).GetProperty("Endpoint");
        var endpoint = endpointProperty?.GetValue(urlContext);

        if (endpoint == null)
        {
            return false;
        }

        var endpointNameProperty = endpoint.GetType().GetProperty("EndpointName");
        var endpointName = endpointNameProperty?.GetValue(endpoint) as string;

        if (string.IsNullOrEmpty(endpointName))
        {
            return false;
        }

        // Check if the scheme matches (case-insensitive)
        if (!string.Equals(spec.Scheme, endpointName, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        // If the spec is scheme-only, we have a match
        if (spec.IsSchemeOnly)
        {
            return true;
        }

        // For port-specific matching, we need to check the URL or port information
        // Try to get port information from the URL context
        var urlProperty = typeof(T).GetProperty("Url");
        var url = urlProperty?.GetValue(urlContext) as string;

        if (!string.IsNullOrEmpty(url) && Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            // Check if the port matches
            var actualPort = uri.Port;

            return spec.Port == actualPort;
        }

        // If we can't determine the port, fall back to scheme-only matching
        return false;
    }

    /// <summary>
    /// Parses an endpoint specification string into an EndpointSpec object.
    /// Supports formats like "http", "https", "http:8080", "https:8443".
    /// </summary>
    /// <param name="endpointString">The endpoint string to parse.</param>
    /// <param name="logger">Optional logger for warning messages.</param>
    /// <returns>An EndpointSpec representing the parsed endpoint specification.</returns>
    internal static EndpointSpec ParseEndpointSpec(string endpointString, ILogger? logger = null)
    {
        if (string.IsNullOrWhiteSpace(endpointString))
        {
            throw new ArgumentException("Endpoint string cannot be null or whitespace.", nameof(endpointString));
        }

        var trimmedEndpoint = endpointString.Trim();

        // Check if the endpoint contains a port specification (scheme:port format)
        var colonIndex = trimmedEndpoint.IndexOf(':');

        if (colonIndex == -1)
        {
            // No port specified, return scheme-only specification
            return new EndpointSpec(trimmedEndpoint.ToLowerInvariant(), null);
        }

        var scheme = trimmedEndpoint[..colonIndex].Trim().ToLowerInvariant();
        var portString = trimmedEndpoint[(colonIndex + 1)..].Trim();

        if (string.IsNullOrEmpty(scheme))
        {
            throw new ArgumentException($"Invalid endpoint specification '{endpointString}': scheme cannot be empty.",
                nameof(endpointString));
        }

        if (string.IsNullOrEmpty(portString))
        {
            // Colon present but no port specified, treat as scheme-only
            logger?.LogWarning("Endpoint specification '{EndpointString}' has colon but no port, treating as scheme-only", endpointString);

            return new EndpointSpec(scheme, null);
        }

        // Try to parse the port number
        if (int.TryParse(portString, out var port))
        {
            if (port <= 0 || port > 65535)
            {
                logger?.LogWarning("Invalid port number {Port} in endpoint specification '{EndpointString}', treating as scheme-only", port,
                    endpointString);

                return new EndpointSpec(scheme, null);
            }

            return new EndpointSpec(scheme, port);
        }

        // Port parsing failed, log warning and fall back to scheme-only
        logger?.LogWarning("Invalid port specification '{PortString}' in endpoint '{EndpointString}', treating as scheme-only", portString,
            endpointString);

        return new EndpointSpec(scheme, null);
    }
}
