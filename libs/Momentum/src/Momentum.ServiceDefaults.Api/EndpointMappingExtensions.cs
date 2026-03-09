// Copyright (c) Momentum .NET. All rights reserved.

using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace Momentum.ServiceDefaults.Api;

/// <summary>
///     Provides extension methods for automatic endpoint discovery and registration.
/// </summary>
[ExcludeFromCodeCoverage]
public static class EndpointMappingExtensions
{
    private static readonly Type EndpointMapperType = typeof(IEndpointDefinition);

    /// <summary>
    ///     Discovers and maps all endpoints from classes implementing <see cref="IEndpointDefinition" />
    ///     in the specified assembly.
    /// </summary>
    /// <param name="routeBuilder">The endpoint route builder to register endpoints with.</param>
    /// <param name="assembly">
    ///     The assembly to scan for endpoint mappers. Defaults to the entry assembly.
    /// </param>
    public static void MapEndpoints(this IEndpointRouteBuilder routeBuilder, Assembly? assembly = null)
    {
        assembly ??= Assembly.GetEntryAssembly()
                     ?? throw new InvalidOperationException(
                         "Unable to identify entry assembly for endpoint discovery. " +
                         "Specify the assembly explicitly.");

        var endpointMapperTypes = assembly.GetTypes()
            .Where(type => type is { IsClass: true, IsAbstract: false } && type.GetInterfaces().Contains(EndpointMapperType))
            .ToList();

        if (endpointMapperTypes.Count == 0)
        {
            var logger = routeBuilder.ServiceProvider.GetService<ILoggerFactory>()?.CreateLogger("EndpointMapping");
            logger?.LogWarning("No IEndpointDefinition implementations found in assembly {AssemblyName}", assembly.GetName().Name);
            return;
        }

        foreach (var mapperType in endpointMapperTypes)
        {
            var method = mapperType.GetMethod(nameof(IEndpointDefinition.MapEndpoints), BindingFlags.Public | BindingFlags.Static, [typeof(IEndpointRouteBuilder)]);
            method?.Invoke(null, [routeBuilder]);
        }
    }
}
