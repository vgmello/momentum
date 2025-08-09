// Copyright (c) ABCDEG. All rights reserved.

using Microsoft.AspNetCore.Http;
using System.Net.Sockets;

namespace AppDomain.AppHost.Extensions;

public static class LaunchProfileExtensions
{
    private const string KestrelEndpointsPrefix = "KESTREL__ENDPOINTS__";

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


    private sealed record KestrelLaunchSettingsEndpoint
    {
        public BindingAddress? Url { get; set; }

        public string? Protocols { get; set; }
    }
}