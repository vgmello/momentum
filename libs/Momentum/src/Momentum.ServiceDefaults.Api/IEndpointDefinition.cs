// Copyright (c) Momentum .NET. All rights reserved.

using Microsoft.AspNetCore.Routing;

namespace Momentum.ServiceDefaults.Api;

/// <summary>
///     Marks a class as a feature endpoint group that can be auto-discovered and mapped by
///     <see cref="EndpointMappingExtensions.MapEndpoints" />.
/// </summary>
public interface IEndpointDefinition
{
    /// <summary>
    ///     Maps the endpoints for this feature to the specified route builder.
    /// </summary>
    /// <param name="routes">The endpoint route builder to register endpoints with.</param>
    /// <returns>
    ///     The <see cref="RouteGroupBuilder" /> the endpoints were mapped under, so callers can apply
    ///     group-wide conventions (authorization, rate limiting, endpoint filters, etc.).
    /// </returns>
    static abstract RouteGroupBuilder MapEndpoints(IEndpointRouteBuilder routes);
}
