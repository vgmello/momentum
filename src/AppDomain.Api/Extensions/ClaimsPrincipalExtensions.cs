// Copyright (c) ABCDEG. All rights reserved.

using System.Security.Claims;

namespace AppDomain.Api.Extensions;

public static class ClaimsPrincipalExtensions
{
    // Temporary fake tenant ID until we have proper authentication
    private static readonly Guid FakeTenantId = Guid.Parse("12345678-0000-0000-0000-000000000000");

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