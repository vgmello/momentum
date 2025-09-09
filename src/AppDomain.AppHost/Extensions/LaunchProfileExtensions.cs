// Copyright (c) OrgName. All rights reserved.

using Microsoft.AspNetCore.Http;
using System.Net.Sockets;

namespace AppDomain.AppHost.Extensions;

/// <summary>
///     Provides extension methods for integrating launch profile endpoint configurations with .NET Aspire orchestration.
/// </summary>
public static class LaunchProfileExtensions
{
    private const string KestrelEndpointsPrefix = "KESTREL__ENDPOINTS__";

    /// <summary>
    ///     Configures Kestrel endpoints from launch profile settings for a project resource in Aspire orchestration.
    /// </summary>
    /// <param name="builder">The project resource builder to configure.</param>
    /// <returns>The configured project resource builder.</returns>
    /// <remarks>
    ///     This method extracts Kestrel endpoint configurations from launch profile environment variables,
    ///     maps them to Aspire endpoint annotations, and sets up the appropriate Kestrel configuration
    ///     environment variables for the project.
    /// </remarks>
    public static IResourceBuilder<ProjectResource> WithKestrelLaunchProfileEndpoints(this IResourceBuilder<ProjectResource> builder)
    {
        builder.WithEnvironment(ctx =>
        {
            var launchProfileEndpoints = ExtractLaunchProfileEndpoints(ctx);

            ctx.Resource.TryGetEndpoints(out var endpointAnnotations);

            var configuredEndpoints = endpointAnnotations?.ToList() ?? [];

            foreach (var (endpointName, endpointInfo) in launchProfileEndpoints)
            {
                var configuredEndpoint = configuredEndpoints.FirstOrDefault(e => e.Port is not null && e.Port == endpointInfo.Url?.Port);

                if (configuredEndpoint is not null)
                {
                    configuredEndpoint.Name = endpointName;

                    if (endpointInfo.Protocols is not null)
                    {
                        configuredEndpoint.Transport = endpointInfo.Protocols;
                    }
                }
                else
                {
                    ctx.Resource.Annotations.Add(new EndpointAnnotation(ProtocolType.Tcp)
                    {
                        Name = endpointName,
                        Port = endpointInfo.Url?.Port,
                        Transport = endpointInfo.Protocols ?? "http",
                        IsProxied = true
                    });
                }
            }

            var endpointReferences = builder.Resource.GetEndpoints().ToList();
            endpointReferences.ForEach(e =>
            {
                var endpointExpression = new ReferenceExpressionBuilder();
                endpointExpression.Append($"{e.Scheme}://{e.Host}:{e.Property(EndpointProperty.TargetPort)}");

                var endpointConfigPrefix = KestrelEndpointsPrefix + e.EndpointName;

                ctx.EnvironmentVariables[$"{endpointConfigPrefix}__URL"] = endpointExpression.Build();

                if (launchProfileEndpoints.TryGetValue(e.EndpointName, out var launchSettingsEndpoint) &&
                    launchSettingsEndpoint.Protocols is not null)
                {
                    ctx.EnvironmentVariables[$"{endpointConfigPrefix}__PROTOCOLS"] = launchSettingsEndpoint.Protocols;
                }
            });

            // Note: This is a workaround to ensure that the dotnet does not load launch profile env variables again
            ctx.EnvironmentVariables["DOTNET_LAUNCH_PROFILE"] = "none";
        });

        return builder;
    }

    /// <summary>
    ///     Extracts Kestrel endpoint configurations from launch profile environment variables.
    /// </summary>
    /// <param name="envContext">The environment callback context containing environment variables.</param>
    /// <returns>A dictionary mapping endpoint names to their configuration settings.</returns>
    private static Dictionary<string, KestrelLaunchSettingsEndpoint> ExtractLaunchProfileEndpoints(EnvironmentCallbackContext envContext)
    {
        var launchProfileEndpoints = new Dictionary<string, KestrelLaunchSettingsEndpoint>();

        var kestrelEndpointConfig = envContext.EnvironmentVariables
            .Where(kv => kv.Key.StartsWith(KestrelEndpointsPrefix, StringComparison.OrdinalIgnoreCase));

        foreach (var (key, value) in kestrelEndpointConfig)
        {
            var endpointNameEndIndex = key.IndexOf("__", KestrelEndpointsPrefix.Length, StringComparison.InvariantCulture);

            if (endpointNameEndIndex == -1)
                continue;

            var endpointName = key[KestrelEndpointsPrefix.Length..endpointNameEndIndex].ToLowerInvariant();
            var valueName = key[(endpointNameEndIndex + 2)..].ToLowerInvariant();

            if (!launchProfileEndpoints.TryGetValue(endpointName, out var endpointInfo))
            {
                endpointInfo = new KestrelLaunchSettingsEndpoint();
                launchProfileEndpoints[endpointName] = endpointInfo;
            }

            switch (valueName)
            {
                case "url":
                    endpointInfo.Url = value.ToString() is { } urlString ? BindingAddress.Parse(urlString) : null;

                    break;
                case "protocols":
                    endpointInfo.Protocols = value.ToString();

                    break;
            }

            envContext.EnvironmentVariables.Remove(key);
        }

        return launchProfileEndpoints;
    }


    /// <summary>
    ///     Represents a Kestrel endpoint configuration from launch settings.
    /// </summary>
    private sealed record KestrelLaunchSettingsEndpoint
    {
        /// <summary>
        ///     Gets or sets the binding address for the endpoint.
        /// </summary>
        public BindingAddress? Url { get; set; }

        /// <summary>
        ///     Gets or sets the protocols supported by the endpoint (e.g., "http", "https", "http2").
        /// </summary>
        public string? Protocols { get; set; }
    }
}
