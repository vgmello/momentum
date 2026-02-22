// Copyright (c) OrgName. All rights reserved.

namespace AppDomain.AppHost.Extensions;

/// <summary>
///     Provides extension methods for gRPC endpoint configuration in .NET Aspire orchestration.
/// </summary>
[ExcludeFromCodeCoverage]
public static class GrpcExtensions
{
    /// <summary>
    ///     Gets the gRPC endpoint reference from a resource builder.
    /// </summary>
    /// <typeparam name="T">The resource type that implements <see cref="IResourceWithEndpoints" />.</typeparam>
    /// <param name="builder">The resource builder containing endpoints.</param>
    /// <returns>
    ///     The gRPC endpoint reference if found, otherwise returns the first available endpoint.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="builder" /> is null.</exception>
    public static EndpointReference GetGrpcEndpoint<T>(this IResourceBuilder<T> builder) where T : IResourceWithEndpoints
    {
        ArgumentNullException.ThrowIfNull(builder);

        var endpoints = builder.Resource.GetEndpoints().ToList();

        return endpoints.FirstOrDefault(e => e.EndpointName == "grpc") ?? endpoints[0];
    }
}
