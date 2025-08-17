// Copyright (c) ORG_NAME. All rights reserved.

using FluentValidation.Results;

namespace AppDomain.Api.Extensions;

/// <summary>
///     Extensions for validation operations.
/// </summary>
public static class ValidationExtensions
{
    /// <summary>
    ///     Gets the tenant ID from the HTTP context.
    ///     This is a placeholder implementation - in real scenarios, this would extract from JWT, headers, etc.
    /// </summary>
    /// <param name="context">The HTTP context</param>
    /// <returns>The tenant ID</returns>
    public static Guid GetTenantId(this HttpContext context)
    {
        // Placeholder implementation - replace with actual tenant resolution logic
        // This could come from JWT claims, headers, route parameters, etc.
        return Guid.Parse("00000000-0000-0000-0000-000000000001");
    }

    public static bool IsConcurrencyConflict(this IEnumerable<ValidationFailure> errors)
    {
        return errors.Any(e => e.PropertyName == "Version" &&
                               e.ErrorMessage.Contains("modified by another user"));
    }
}
