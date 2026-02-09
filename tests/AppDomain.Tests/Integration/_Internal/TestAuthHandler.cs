// Copyright (c) OrgName. All rights reserved.

//#if (INCLUDE_API)
using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace AppDomain.Tests.Integration._Internal;

/// <summary>
///     A minimal authentication handler for integration testing.
///     Authenticates requests that carry "Bearer {ValidToken}" and rejects everything else.
///     <para>
///         Replace this with your real authentication scheme tests (JWT, OAuth, etc.)
///         once authentication is configured in the application.
///     </para>
/// </summary>
internal sealed class TestAuthHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string SchemeName = "TestScheme";
    public const string ValidToken = "valid-test-token";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.ContainsKey("Authorization"))
            return Task.FromResult(AuthenticateResult.NoResult());

        var header = Request.Headers.Authorization.ToString();

        if (header != $"Bearer {ValidToken}")
            return Task.FromResult(AuthenticateResult.Fail("Invalid token"));

        var claims = new[]
        {
            new Claim(ClaimTypes.Name, "test-user"),
            new Claim("tenant_id", "12345678-0000-0000-0000-000000000000")
        };

        var identity = new ClaimsIdentity(claims, SchemeName);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, SchemeName);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
//#endif
