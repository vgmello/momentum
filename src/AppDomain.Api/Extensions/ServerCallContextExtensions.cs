// Copyright (c) ORG_NAME. All rights reserved.

using Grpc.Core;

namespace AppDomain.Api.Extensions;

/// <summary>
/// Provides extension methods for <see cref="ServerCallContext"/> to extract tenant information from gRPC calls.
/// </summary>
public static class ServerCallContextExtensions
{
    /// <summary>
    /// Gets the tenant ID from the gRPC server call context by extracting it from the HTTP context user claims.
    /// </summary>
    /// <param name="context">The gRPC server call context.</param>
    /// <returns>The tenant ID associated with the current user.</returns>
    public static Guid GetTenantId(this ServerCallContext context) => context.GetHttpContext().User.GetTenantId();
}
