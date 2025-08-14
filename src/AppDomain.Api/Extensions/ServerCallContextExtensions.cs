// Copyright (c) ORG_NAME. All rights reserved.

namespace AppDomain.Api.Extensions;

public static class ServerCallContextExtensions
{
    /// <summary>
    ///     Gets the tenant ID from the gRPC server call context by extracting it from the HTTP context user claims.
    /// </summary>
    /// <param name="context">The gRPC server call context</param>
    /// <returns>The tenant ID, or Guid.Empty if not found</returns>
    public static Guid GetTenantId(this ServerCallContext context) => context.GetHttpContext().User.GetTenantId();
}