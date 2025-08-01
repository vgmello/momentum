// Copyright (c) ABCDEG. All rights reserved.

using System.Reflection;
using System.Diagnostics.CodeAnalysis;
using Grpc.Core;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace Operations.ServiceDefaults.Api;

/// <summary>
///     Provides extension methods for automatic gRPC service registration.
/// </summary>
[ExcludeFromCodeCoverage]
public static class GrpcRegistrationExtensions
{
    private const BindingFlags STATIC_METHODS = BindingFlags.Public | BindingFlags.Static;

    private static readonly MethodInfo GrpcMapServiceMethod = typeof(GrpcEndpointRouteBuilderExtensions)
        .GetMethod(nameof(GrpcEndpointRouteBuilderExtensions.MapGrpcService), STATIC_METHODS)!;

    /// <summary>
    ///     Maps all gRPC services found in the entry assembly to endpoints.
    /// </summary>
    /// <param name="routeBuilder">The endpoint route builder to register services with.</param>
    /// <remarks>
    ///     This method uses reflection to discover all gRPC service implementations
    ///     in the entry assembly and automatically registers them as endpoints.
    ///     Services are identified by inheriting from a base class with the
    ///     <see cref="BindServiceMethodAttribute" />.
    /// </remarks>
    public static void MapGrpcServices(this IEndpointRouteBuilder routeBuilder)
    {
        routeBuilder.MapGrpcServices(Assembly.GetEntryAssembly()!);
    }

    /// <summary>
    ///     Maps all gRPC services found in the specified type's assembly to endpoints.
    /// </summary>
    /// <param name="routeBuilder">The endpoint route builder to register services with.</param>
    /// <param name="assemblyMarker">A type whose assembly will be scanned for gRPC services.</param>
    /// <remarks>
    ///     This overload allows specifying a different assembly than the entry assembly
    ///     by providing a marker type from the target assembly.
    /// </remarks>
    public static void MapGrpcServices(this IEndpointRouteBuilder routeBuilder, Type assemblyMarker)
    {
        routeBuilder.MapGrpcServices(assemblyMarker.Assembly);
    }

    private static void MapGrpcServices(this IEndpointRouteBuilder routeBuilder, Assembly assembly)
    {
        var grpcServiceTypes = assembly.GetTypes()
            .Where(type => type is { IsClass: true, IsAbstract: false, IsInterface: false, IsGenericType: false } && IsGrpcService(type));

        foreach (var grpcServiceType in grpcServiceTypes)
        {
            MapGrpcService(grpcServiceType, routeBuilder);
        }
    }

    private static void MapGrpcService(Type grpcServiceType, IEndpointRouteBuilder routeBuilder)
    {
        var typedGrpcMapServiceMethod = GrpcMapServiceMethod.MakeGenericMethod(grpcServiceType);
        typedGrpcMapServiceMethod.Invoke(null, [routeBuilder]);
    }

    private static bool IsGrpcService(Type type)
    {
        var grpcServiceAttribute = type.BaseType?.GetCustomAttribute<BindServiceMethodAttribute>();

        return grpcServiceAttribute is not null;
    }
}
