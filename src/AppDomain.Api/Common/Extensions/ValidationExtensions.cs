// Copyright (c) OrgName. All rights reserved.

using FluentValidation.Results;

namespace AppDomain.Api.Common.Extensions;

/// <summary>
///     Extensions for validation operations.
/// </summary>
public static class ValidationExtensions
{
    /// <summary>
    ///     Gets the tenant ID from the HTTP context.
    ///     This is a placeholder implementation - in real scenarios, this would extract from JWT, headers, etc.
    /// </summary>
    /// <param name="context">The HTTP context to extract the tenant ID from.</param>
    /// <returns>The tenant ID. Currently returns a placeholder value until proper tenant resolution is implemented.</returns>
    /// <remarks>
    ///     This is a placeholder implementation. In production scenarios, this would extract the tenant ID
    ///     from JWT claims, request headers, route parameters, or other authentication mechanisms.
    /// </remarks>
    public static Guid GetTenantId(this HttpContext context)
    {
        // Delegate to the ClaimsPrincipal-based implementation for consistency
        return context.User.GetTenantId();
    }

    public static bool IsConcurrencyConflict(this IEnumerable<ValidationFailure> errors)
    {
        return errors.Any(e => e.PropertyName == "Version" &&
                               e.ErrorMessage.Contains("modified by another user"));
    }
}
