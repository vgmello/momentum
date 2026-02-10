// Copyright (c) OrgName. All rights reserved.

//#if (INCLUDE_API)
using AppDomain.Tests.Integration._Internal;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.TestHost;
using Momentum.ServiceDefaults.Api;
using Momentum.ServiceDefaults.Api.OpenApi.Extensions;
using System.Net;
using System.Net.Http.Headers;

namespace AppDomain.Tests.Integration.Authorization;

/// <summary>
///     Tests that the API authorization middleware is correctly configured.
///     These tests are lightweight — no database or messaging containers required.
///     <para>
///         Customize these tests once you plug in your real authentication scheme
///         (JWT Bearer, OAuth, etc.) to verify your security configuration end-to-end.
///     </para>
/// </summary>
[Trait("Type", "Integration")]
public class ApiAuthorizationTests : IAsyncDisposable
{
    private WebApplication? _app;

    private async Task<HttpClient> CreateTestServer(bool requireAuth, string environment = "Development")
    {
        var builder = WebApplication.CreateSlimBuilder();
        builder.Environment.EnvironmentName = environment;
        builder.WebHost.UseTestServer();

        builder.AddApiServiceDefaults(requireAuth: requireAuth);
        builder.Services.AddOpenApi(options => options.ConfigureOpenApiDefaults());

        if (requireAuth)
        {
            // Wire up a test authentication scheme so the auth middleware can
            // challenge and authenticate requests. Replace TestAuthHandler with
            // your real scheme configuration (e.g. AddJwtBearer) for production tests.
            builder.Services
                .AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = TestAuthHandler.SchemeName;
                    options.DefaultChallengeScheme = TestAuthHandler.SchemeName;
                })
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(
                    TestAuthHandler.SchemeName, null);
        }

        _app = builder.Build();

        _app.ConfigureApiUsingDefaults();
        _app.MapGet("/test", () => "ok");
        _app.MapGet("/test-anonymous", () => "ok").AllowAnonymous();

        await _app.StartAsync();
        return _app.GetTestClient();
    }

    // ── Unauthenticated requests are rejected ────────────────────────

    [Fact]
    public async Task RequireAuth_NoHeader_Returns401()
    {
        using var client = await CreateTestServer(requireAuth: true);

        var response = await client.GetAsync("/test", TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task RequireAuth_InvalidToken_Returns401()
    {
        using var client = await CreateTestServer(requireAuth: true);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "bad-token");

        var response = await client.GetAsync("/test", TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task RequireAuth_UnmatchedRoute_Returns401_NotRevealing404()
    {
        // The fallback authorization policy applies to all requests, including
        // unmatched routes. This prevents information leakage about which
        // endpoints exist to unauthenticated callers.
        using var client = await CreateTestServer(requireAuth: true);

        var response = await client.GetAsync("/does-not-exist", TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    // ── Authenticated requests succeed ───────────────────────────────

    [Fact]
    public async Task RequireAuth_ValidToken_Returns200()
    {
        using var client = await CreateTestServer(requireAuth: true);
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", TestAuthHandler.ValidToken);

        var response = await client.GetAsync("/test", TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        body.ShouldBe("ok");
    }

    // ── AllowAnonymous endpoints bypass the fallback policy ──────────

    [Fact]
    public async Task RequireAuth_AllowAnonymousEndpoint_Returns200WithoutAuth()
    {
        using var client = await CreateTestServer(requireAuth: true);

        var response = await client.GetAsync("/test-anonymous", TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    // ── Development tooling endpoints remain accessible ──────────────

    [Fact]
    public async Task RequireAuth_OpenApiEndpoint_IsAnonymous()
    {
        using var client = await CreateTestServer(requireAuth: true, environment: "Development");

        var response = await client.GetAsync("/openapi/v1.json", TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.ShouldBe("application/json");
    }

    [Fact]
    public async Task RequireAuth_ScalarEndpoint_IsAnonymous()
    {
        using var client = await CreateTestServer(requireAuth: true, environment: "Development");

        var response = await client.GetAsync("/scalar/v1", TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    // ── requireAuth: false — no auth enforcement ─────────────────────

    [Fact]
    public async Task NoAuth_Endpoint_IsAccessibleWithoutCredentials()
    {
        using var client = await CreateTestServer(requireAuth: false);

        var response = await client.GetAsync("/test", TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        body.ShouldBe("ok");
    }

    [Fact]
    public async Task NoAuth_NoWwwAuthenticateHeader()
    {
        using var client = await CreateTestServer(requireAuth: false);

        var response = await client.GetAsync("/test", TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Headers.WwwAuthenticate.ShouldBeEmpty();
    }

    // ── Production environment — dev endpoints are not exposed ───────

    [Fact]
    public async Task Production_DevEndpoints_AreNotExposed()
    {
        // In production, OpenAPI and Scalar are not mapped. With requireAuth: true,
        // the fallback policy returns 401 for unmatched routes (not 404), hiding
        // endpoint existence from unauthenticated callers.
        using var client = await CreateTestServer(requireAuth: true, environment: "Production");

        var openApiResponse = await client.GetAsync("/openapi/v1.json", TestContext.Current.CancellationToken);
        openApiResponse.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);

        var scalarResponse = await client.GetAsync("/scalar/v1", TestContext.Current.CancellationToken);
        scalarResponse.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Production_NoAuth_DevEndpoints_Return404()
    {
        // Without auth, unmatched routes correctly return 404.
        using var client = await CreateTestServer(requireAuth: false, environment: "Production");

        var openApiResponse = await client.GetAsync("/openapi/v1.json", TestContext.Current.CancellationToken);
        openApiResponse.StatusCode.ShouldBe(HttpStatusCode.NotFound);

        var scalarResponse = await client.GetAsync("/scalar/v1", TestContext.Current.CancellationToken);
        scalarResponse.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    public async ValueTask DisposeAsync()
    {
        if (_app is not null)
        {
            await _app.StopAsync();
            await _app.DisposeAsync();
        }
    }
}
//#endif
