// Copyright (c) ORG_NAME. All rights reserved.

namespace AppDomain.Api.Core.Extensions;

/// <summary>
///     Extensions for gRPC-specific operations and conversions.
/// </summary>
public static class GrpcExtensions
{
    /// <summary>
    ///     Safely parses a string to GUID, throwing an appropriate RpcException if parsing fails.
    /// </summary>
    /// <param name="value">The string value to parse as a GUID.</param>
    /// <param name="error">The full error message to use if parsing fails.</param>
    /// <returns>The parsed GUID if successful.</returns>
    /// <exception cref="RpcException">Thrown with StatusCode.InvalidArgument if the GUID format is invalid.</exception>
    public static Guid ToGuidSafe(this string value, string error)
    {
        if (!Guid.TryParse(value, out var guid))
            throw new RpcException(new Status(StatusCode.InvalidArgument, error));
            
        return guid;
    }
}