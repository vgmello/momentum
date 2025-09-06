// Copyright (c) OrgName. All rights reserved.

using System.Security.Claims;

namespace AppDomain.Api.Core.Extensions;

/// <summary>
///     Provides extension methods for <see cref="ClaimsPrincipal" /> to extract tenant information.
/// </summary>
public static class ClaimsPrincipalExtensions
{
    // Temporary fake tenant ID until we have proper authentication
    private static readonly Guid FakeTenantId = Guid.Parse("12345678-0000-0000-0000-000000000000");

    /// <summary>
    ///     Gets the tenant ID from the claims principal.
    /// </summary>
    /// <param name="principal">The claims principal to extract the tenant ID from.</param>
    /// <returns>The tenant ID. Currently returns a placeholder value until proper authentication is implemented.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="principal" /> is null.</exception>
    public static Guid GetTenantId(this ClaimsPrincipal principal)
    {
        if (principal == null)
        {
            throw new ArgumentNullException(nameof(principal));
        }

        // TODO: Replace with actual tenant claim when authentication is implemented
        // For now, return the fake tenant ID

        return FakeTenantId;
    }
}
