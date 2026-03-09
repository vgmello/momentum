// Copyright (c) Momentum .NET. All rights reserved.

using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
//#if (!LIBS_INCLUDES_API)
using Momentum.Extensions;
//#endif
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
    ///     in assemblies registered via <see cref="DomainAssemblyAttribute" />.
    /// </summary>
    /// <param name="routeBuilder">The endpoint route builder to register endpoints with.</param>
    /// <param name="assemblies">
    ///     The assemblies to scan for endpoint mappers. When not specified, scans the entry assembly
    ///     and all assemblies registered via <see cref="DomainAssemblyAttribute" />.
    ///     Falls back to scanning all loaded assemblies that reference <see cref="IEndpointDefinition" />
    ///     when the entry assembly is not available (e.g. during build-time OpenAPI generation).
    /// </param>
    public static void MapEndpoints(this IEndpointRouteBuilder routeBuilder, params Assembly[] assemblies)
    {
        var assembliesToScan = assemblies.Length > 0
            ? assemblies
            : DiscoverEndpointAssemblies();

        var endpointMapperTypes = assembliesToScan
            .SelectMany(a =>
            {
                try { return a.GetTypes(); }
                catch (ReflectionTypeLoadException e) { return e.Types.OfType<Type>(); }
            })
            .Where(type => type is { IsClass: true, IsAbstract: false } && type.GetInterfaces().Contains(EndpointMapperType))
            .ToList();

        if (endpointMapperTypes.Count == 0)
        {
            var logger = routeBuilder.ServiceProvider.GetService<ILoggerFactory>()?.CreateLogger("EndpointMapping");
            var scannedNames = string.Join(", ", assembliesToScan.Select(a => a.GetName().Name));
            logger?.LogWarning("No IEndpointDefinition implementations found in assemblies: {Assemblies}", scannedNames);
            return;
        }

        foreach (var mapperType in endpointMapperTypes)
        {
            var method = mapperType.GetMethod(nameof(IEndpointDefinition.MapEndpoints), BindingFlags.Public | BindingFlags.Static, [typeof(IEndpointRouteBuilder)]);
            method?.Invoke(null, [routeBuilder]);
        }
    }

    private static Assembly[] DiscoverEndpointAssemblies()
    {
        // Try using the DomainAssembly marker attribute for explicit assembly registration.
        // Only use this path when the entry assembly actually has the attribute, which
        // confirms it's the real API project (not a build-time tool like dotnet-getdocument).
        var entryAssembly = Assembly.GetEntryAssembly();

        if (entryAssembly?.GetCustomAttributes<DomainAssemblyAttribute>().Any() == true)
        {
            var domainAssemblies = DomainAssemblyAttribute.GetDomainAssemblies(entryAssembly);
            return [entryAssembly, .. domainAssemblies];
        }

        // Fallback: scan all loaded assemblies that reference IEndpointDefinition.
        // This handles build-time scenarios (e.g. OpenAPI document generation) where
        // the entry assembly is the doc generator tool, not the API project.
        var definingAssembly = EndpointMapperType.Assembly;

        return AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => !a.IsDynamic && a.GetReferencedAssemblies().Any(r => r.FullName == definingAssembly.FullName))
            .ToArray();
    }
}
