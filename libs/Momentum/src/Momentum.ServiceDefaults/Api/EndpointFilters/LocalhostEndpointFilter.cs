// Copyright (c) Momentum .NET. All rights reserved.

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Net;

namespace Momentum.ServiceDefaults.Api.EndpointFilters;

/// <summary>
///     Endpoint filter that restricts access to localhost/loopback addresses only.
/// </summary>
/// <remarks>
///     This filter is typically used for sensitive endpoints like health checks or
///     internal diagnostics that should only be accessible from the local machine.
///     Remote requests are rejected with a 401 Unauthorized response.
///     <para>
///         Security considerations:
///         <list type="bullet">
///             <item>Rejects requests with forwarded headers (X-Forwarded-For) to prevent proxy bypass attacks</item>
///             <item>Validates both IPv4 and IPv6 loopback addresses</item>
///             <item>Null remote IP addresses are rejected for safety</item>
///         </list>
///     </para>
/// </remarks>
public partial class LocalhostEndpointFilter(ILogger logger) : IEndpointFilter
{
    private static readonly string[] ForwardedHeaders =
    [
        "X-Forwarded-For",
        "X-Real-IP",
        "Forwarded",
        "X-Original-Forwarded-For"
    ];

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
        var request = context.HttpContext.Request;

        // Check for proxy/forwarding headers - reject if present to prevent bypass attacks
        foreach (var header in ForwardedHeaders)
        {
            if (request.Headers.ContainsKey(header))
            {
                LogProxyHeaderDetected(logger, header);
                return Results.Unauthorized();
            }
        }

        var remoteIp = context.HttpContext.Connection.RemoteIpAddress;

        if (remoteIp is null || !IsLocalAddress(remoteIp))
        {
            LogRemoteRequestForLocalEndpoint(logger, remoteIp);
            return Results.Unauthorized();
        }

        return await next(context);
    }

    /// <summary>
    ///     Determines whether the specified IP address is a local/loopback address.
    /// </summary>
    /// <param name="address">The IP address to check.</param>
    /// <returns><c>true</c> if the address is a loopback address; otherwise, <c>false</c>.</returns>
    private static bool IsLocalAddress(IPAddress address)
    {
        // Handle IPv6 mapped IPv4 addresses (::ffff:127.0.0.1)
        if (address.IsIPv4MappedToIPv6)
        {
            address = address.MapToIPv4();
        }

        return IPAddress.IsLoopback(address);
    }

    [LoggerMessage(
        EventId = 1,
        Level = LogLevel.Debug,
        Message = "Remote request received for a local-only endpoint, returning unauthorized. IP address: {RemoteIpAddress}")]
    private static partial void LogRemoteRequestForLocalEndpoint(ILogger logger, IPAddress? remoteIpAddress);

    [LoggerMessage(
        EventId = 2,
        Level = LogLevel.Warning,
        Message = "Request with proxy header '{HeaderName}' detected for local-only endpoint, returning unauthorized")]
    private static partial void LogProxyHeaderDetected(ILogger logger, string headerName);
}
