// Copyright (c) ABCDEG. All rights reserved.

namespace AppDomain.AppHost.Extensions;

public static class GrpcExtensions
{
    public static EndpointReference GetGrpcEndpoint<T>(this IResourceBuilder<T> builder) where T : IResourceWithEndpoints
    {
        ArgumentNullException.ThrowIfNull(builder);

        var endpoints = builder.Resource.GetEndpoints().ToList();

        return endpoints.FirstOrDefault(e => e.EndpointName == "grpc") ?? endpoints[0];
    }
}