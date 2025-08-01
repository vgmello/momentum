// Copyright (c) ABCDEG. All rights reserved.

using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Operations.ServiceDefaults.Api.EndpointFilters;

/// <summary>
///     Endpoint filter that restricts access to localhost/loopback addresses only.
/// </summary>
/// <remarks>
///     This filter is typically used for sensitive endpoints like health checks or
///     internal diagnostics that should only be accessible from the local machine.
///     Remote requests are rejected with a 401 Unauthorized response.
/// </remarks>
public partial class LocalhostEndpointFilter(ILogger logger) : IEndpointFilter
{
    /// <summary>
    ///     Validates that the request originates from a loopback address.
    /// </summary>
    /// <param name="context">The endpoint filter invocation context.</param>
    /// <param name="next">The next filter in the pipeline.</param>
    /// <returns>
    ///     The result from the next filter if the request is from localhost,
    ///     or an Unauthorized result if the request is from a remote address.
    /// </returns>
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var remoteIp = context.HttpContext.Connection.RemoteIpAddress;

        if (remoteIp is null || !IPAddress.IsLoopback(remoteIp))
        {
            LogRemoteRequestForLocalEndpoint(logger, remoteIp);

            return Results.Unauthorized();
        }

        return await next(context);
    }

    [LoggerMessage(
        EventId = 1,
        Level = LogLevel.Debug,
        Message = "Remote request received for a local-only endpoint, returning unauthorized. IP address: {RemoteIpAddress}")]
    private static partial void LogRemoteRequestForLocalEndpoint(ILogger logger, IPAddress? remoteIpAddress);
}
