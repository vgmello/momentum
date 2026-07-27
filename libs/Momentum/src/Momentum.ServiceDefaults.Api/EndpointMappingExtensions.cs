// Copyright (c) Momentum .NET. All rights reserved.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace Momentum.ServiceDefaults.Api;

[ExcludeFromCodeCoverage]
public static class EndpointMappingExtensions
{
    private static readonly Type EndpointDefinitionType = typeof(IEndpointDefinition);

    /// <summary>
    ///     Discovers classes implementing <see cref="IEndpointDefinition" /> in the specified assembly
    ///     and maps their endpoints under a shared parent group.
    /// </summary>
    /// <param name="routeBuilder">The endpoint route builder to register endpoints with.</param>
    /// <param name="assembly">
    ///     The assembly to scan for endpoint definitions. Defaults to the entry assembly.
    /// </param>
    /// <param name="predicate">
    ///     Optional filter selecting which endpoint definition types this call maps. When <c>null</c>,
    ///     all discovered definitions are mapped. Call this method multiple times with complementary
    ///     predicates to give different endpoint sets different conventions — e.g. one call with
    ///     <c>.RequireAuthorization()</c> and another without. Predicates should partition the types so
    ///     none is mapped twice (which would produce duplicate routes).
    /// </param>
    /// <returns>
    ///     The parent <see cref="RouteGroupBuilder" /> the matched feature groups are nested under, so
    ///     conventions applied to it fan out to every endpoint mapped by this call — e.g.
    ///     <c>app.MapEndpoints(asm, IsAdminEndpoint).RequireAuthorization()</c>.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    ///     Thrown when no assembly is provided and the entry assembly cannot be determined.
    /// </exception>
    public static RouteGroupBuilder MapEndpoints(
        this IEndpointRouteBuilder routeBuilder, Assembly? assembly = null, Func<Type, bool>? predicate = null)
    {
        assembly ??= Assembly.GetEntryAssembly()
                     ?? throw new InvalidOperationException(
                         "Unable to identify entry assembly for endpoint discovery. Specify the assembly explicitly.");

        // Parent group with an empty prefix: it adds no path segment, but carries conventions applied
        // to the returned builder down to every nested feature group.
        var parentGroup = routeBuilder.MapGroup(string.Empty);

        // Order by full name so endpoint (and generated OpenAPI) registration is deterministic —
        // Type ordering from GetTypes() is not guaranteed.
        var endpointDefinitionTypes = assembly.GetTypes()
            .Where(type => type is { IsClass: true, IsAbstract: false } && type.GetInterfaces().Contains(EndpointDefinitionType))
            .Where(type => predicate is null || predicate(type))
            .OrderBy(type => type.FullName, StringComparer.Ordinal)
            .ToList();

        if (endpointDefinitionTypes.Count == 0)
        {
            // A predicate matching nothing is a deliberate caller filter, not a misconfiguration —
            // only log when the assembly genuinely has no endpoint definitions.
            if (predicate is null)
            {
                var logger = routeBuilder.ServiceProvider.GetService<ILoggerFactory>()?.CreateLogger("EndpointMapping");
                logger?.LogInformation("No IEndpointDefinition implementations found in assembly {AssemblyName}", assembly.GetName().Name);
            }

            return parentGroup;
        }

        foreach (var definitionType in endpointDefinitionTypes)
        {
            var method = definitionType.GetMethod(
                nameof(IEndpointDefinition.MapEndpoints), BindingFlags.Public | BindingFlags.Static, [typeof(IEndpointRouteBuilder)]);

            method?.Invoke(null, [parentGroup]);
        }

        return parentGroup;
    }
}
