// Copyright (c) Momentum .NET. All rights reserved.

using Microsoft.AspNetCore.Routing;

namespace Momentum.ServiceDefaults.Api;

/// <summary>
///     Marker interface for automatic endpoint discovery and registration.
///     Implement this interface on endpoint classes to have them automatically
///     mapped by <see cref="ApiExtensions.ConfigureApiUsingDefaults" />.
/// </summary>
/// <remarks>
///     <para>
///         Implementing classes must define a <c>static void MapEndpoints(IEndpointRouteBuilder routes)</c>
///         method that registers the endpoints.
///     </para>
///     <example>
///         <code>
///         public class CustomerEndpoints : IEndpointDefinition
///         {
///             public static void MapEndpoints(IEndpointRouteBuilder routes)
///             {
///                 var group = routes.MapGroup("customers").WithTags("Customers");
///                 group.MapGet("/", GetCustomers);
///                 group.MapPost("/", CreateCustomer);
///             }
///         }
///         </code>
///     </example>
/// </remarks>
public interface IEndpointDefinition
{
    /// <summary>
    ///     Maps the endpoints for this feature to the specified route builder.
    /// </summary>
    /// <param name="routes">The endpoint route builder to register endpoints with.</param>
    static abstract void MapEndpoints(IEndpointRouteBuilder routes);
}
