// Copyright (c) Momentum .NET. All rights reserved.

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenTelemetry.Trace;
using Scalar.AspNetCore;

namespace Momentum.ServiceDefaults.Api;

/// <summary>
///     Internal marker used to communicate the auth configuration decision
///     from <see cref="ApiExtensions.AddApiServiceDefaults" /> to
///     <see cref="ApiExtensions.ConfigureApiUsingDefaults" />.
/// </summary>
internal sealed record ApiAuthConfiguration(bool RequireAuth);

/// <summary>
///     Provides extension methods for configuring API services with sensible defaults.
/// </summary>
public static class ApiExtensions
{
    /// <summary>
    ///     Adds default API services to the application builder (excluding OpenAPI).
    /// </summary>
    /// <param name="builder">The web application builder to configure.</param>
    /// <param name="requireAuth">
    ///     When <c>true</c> (default), registers authentication and authorization services
    ///     with a fallback policy that requires all endpoints (minimal API and gRPC) to be
    ///     authenticated. Individual endpoints can opt out with <c>[AllowAnonymous]</c> or
    ///     <c>.AllowAnonymous()</c>. Development-only endpoints (OpenAPI, Scalar, gRPC reflection)
    ///     are automatically marked as anonymous.
    ///     When <c>false</c>, no authentication or authorization services are registered.
    /// </param>
    /// <returns>The configured host application builder for method chaining.</returns>
    /// <remarks>
    ///     <para>
    ///         <strong>Important:</strong> This method does not configure OpenAPI.
    ///         For XML documentation support with OpenAPI, call <c>AddOpenApi()</c> directly
    ///         in your API project's <c>Program.cs</c>. This allows the .NET 10 source generator
    ///         to intercept the call and generate XML comment transformers.
    ///     </para>
    ///     <example>
    ///         <code>
    ///         // In Program.cs:
    ///         builder.AddApiServiceDefaults(requireAuth: false);
    ///         builder.Services.AddOpenApi(options =&gt; options.ConfigureOpenApiDefaults());
    ///         </code>
    ///     </example>
    ///     <!--@include: @code/api/api-extensions-detailed.md#service-configuration -->
    /// </remarks>
    public static IHostApplicationBuilder AddApiServiceDefaults(this WebApplicationBuilder builder, bool requireAuth = true)
    {
        builder.Services.AddSingleton(new ApiAuthConfiguration(requireAuth));

        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddProblemDetails();

        // NOTE: OpenAPI is NOT configured here.
        // Call AddOpenApi() directly in your Program.cs for XML documentation support.
        // The .NET 10 source generator intercepts AddOpenApi() calls and generates
        // XML comment transformers for the calling project's documentation.

        builder.Services.AddHttpLogging();

        // Add output caching for OpenAPI document caching
        builder.Services.AddOutputCache(options =>
        {
            options.AddPolicy("OpenApi", policy => policy.Expire(TimeSpan.FromMinutes(10)));
        });

        builder.Services.AddGrpc(options =>
        {
            options.EnableDetailedErrors = builder.Environment.IsDevelopment();
        });
        builder.Services.AddGrpcReflection();

        builder.Services.AddOpenTelemetry()
            .WithTracing(tracing => tracing.AddGrpcCoreInstrumentation());

        if (requireAuth)
        {
            builder.Services.AddAuthentication();
            builder.Services.AddAuthorizationBuilder()
                .SetFallbackPolicy(new AuthorizationPolicyBuilder()
                    .RequireAuthenticatedUser()
                    .Build());
        }

        builder.WebHost.ConfigureKestrel(serverOptions =>
        {
            serverOptions.AddServerHeader = false;
        });

        return builder;
    }

    /// <summary>
    ///     Configures the web application with default API middleware and endpoints.
    /// </summary>
    /// <param name="app">The web application to configure.</param>
    /// <returns>The configured web application for method chaining.</returns>
    /// <remarks>
    ///     <!--@include: @code/api/api-extensions-detailed.md#application-configuration -->
    /// </remarks>
    public static WebApplication ConfigureApiUsingDefaults(this WebApplication app)
    {
        app.UseHttpLogging();
        app.UseRouting();

        var requireAuth = app.Services.GetService<ApiAuthConfiguration>()?.RequireAuth == true;

        if (requireAuth)
        {
            app.UseAuthentication();
            app.UseAuthorization();
        }

        app.UseGrpcWeb(new GrpcWebOptions { DefaultEnabled = true });

        if (!app.Environment.IsDevelopment())
        {
            app.UseHsts();
            app.UseExceptionHandler();
        }

        app.UseOutputCache();

        if (app.Environment.IsDevelopment())
        {
            // Development tooling endpoints are always anonymous so they remain
            // accessible when the fallback authorization policy is active.
            app.MapOpenApi().CacheOutput("OpenApi").AllowAnonymous();
            app.MapScalarApiReference(options => options.WithTitle($"{app.Environment.ApplicationName} OpenAPI"))
                .AllowAnonymous();
            app.MapGrpcReflectionService().AllowAnonymous();
        }

        app.MapGrpcServices();

        return app;
    }
}
