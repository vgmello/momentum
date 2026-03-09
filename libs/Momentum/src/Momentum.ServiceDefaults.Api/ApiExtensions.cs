// Copyright (c) Momentum .NET. All rights reserved.

using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using Grpc.AspNetCore.Server;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Momentum.ServiceDefaults.HealthChecks;
using OpenTelemetry.Trace;
using Scalar.AspNetCore;

namespace Momentum.ServiceDefaults.Api;

/// <summary>
///     Internal marker used to communicate the auth configuration decision
///     from <see cref="ApiExtensions.AddApiServiceDefaults" /> to
///     <see cref="ApiExtensions.ConfigureApiUsingDefaults" />.
/// </summary>
[ExcludeFromCodeCoverage]
internal sealed record ApiAuthConfiguration(bool RequireAuth);

/// <summary>
///     Provides extension methods for configuring API services with sensible defaults.
/// </summary>
[ExcludeFromCodeCoverage]
public static class ApiExtensions
{
    /// <summary>
    ///     Adds default API services to the application builder, including core service defaults (excluding OpenAPI).
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
    ///         This method calls <see cref="ServiceDefaultsExtensions.AddServiceDefaults" /> internally,
    ///         so there is no need to call it separately for API projects.
    ///     </para>
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
        builder.AddServiceDefaults();

        builder.Services.AddSingleton(new ApiAuthConfiguration(requireAuth));

        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddProblemDetails();
        builder.Services.AddExceptionHandler<ProblemDetailsExceptionHandler>();

        // NOTE: OpenAPI is NOT configured here.
        // Call AddOpenApi() directly in your Program.cs for XML documentation support.
        // The .NET 10 source generator intercepts AddOpenApi() calls and generates
        // XML comment transformers for the calling project's documentation.

        if (builder.Configuration.GetValue("HttpLogging:Enabled", false))
        {
            builder.Services.AddHttpLogging();
        }

        // Add output caching for OpenAPI document caching
        builder.Services.AddOutputCache(options =>
        {
            options.AddPolicy("OpenApi", policy => policy.Expire(TimeSpan.FromMinutes(10)));
        });

        builder.Services.AddGrpc();
        builder.Services.Configure<GrpcServiceOptions>(builder.Configuration.GetSection("Grpc"));
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

        builder.WebHost.UseKestrelHttpsConfiguration();
        builder.WebHost.ConfigureKestrel(serverOptions =>
        {
            serverOptions.AddServerHeader = false;
        });
        builder.Services.Configure<KestrelServerOptions>(builder.Configuration.GetSection("Kestrel"));

        builder.AddRateLimiting();

        builder.Services.ConfigureHttpJsonOptions(options =>
        {
            builder.Configuration.GetSection("JsonOptions").Bind(options.SerializerOptions);
            options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
        });

        builder.Services.AddApiVersioning(options =>
        {
            options.DefaultApiVersion = new Asp.Versioning.ApiVersion(1, 0);
            options.AssumeDefaultVersionWhenUnspecified = true;
            options.ReportApiVersions = true;
            options.ApiVersionReader = new Asp.Versioning.UrlSegmentApiVersionReader();
        });

        return builder;
    }

    /// <summary>
    ///     Adds rate limiting services based on the <c>RateLimiting</c> configuration section.
    /// </summary>
    /// <param name="builder">The web application builder to configure.</param>
    /// <returns>The configured host application builder for method chaining.</returns>
    /// <remarks>
    ///     <para>
    ///         Rate limiting is enabled when <c>RateLimiting:Enabled</c> is <c>true</c> in configuration.
    ///         Supports a fixed window limiter configured via the <c>RateLimiting:Fixed</c> section.
    ///     </para>
    ///     <para>
    ///         This method is called automatically by <see cref="AddApiServiceDefaults" /> but can also
    ///         be called independently for non-API hosts that need rate limiting.
    ///     </para>
    /// </remarks>
    public static IHostApplicationBuilder AddRateLimiting(this WebApplicationBuilder builder)
    {
        var rateLimitingSection = builder.Configuration.GetSection("RateLimiting");
        if (rateLimitingSection.GetValue("Enabled", false))
        {
            builder.Services.AddRateLimiter(options =>
            {
                options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

                var fixedSection = rateLimitingSection.GetSection("Fixed");
                if (fixedSection.Exists())
                {
                    options.AddFixedWindowLimiter("fixed", limiterOptions =>
                    {
                        fixedSection.Bind(limiterOptions);
                    });
                }
            });
        }

        return builder;
    }

    /// <summary>
    ///     Configures the web application with default API middleware, endpoints, and health checks.
    /// </summary>
    /// <param name="app">The web application to configure.</param>
    /// <returns>The configured web application for method chaining.</returns>
    /// <remarks>
    ///     <para>
    ///         This method configures the full API middleware pipeline and maps all endpoints:
    ///     </para>
    ///     <list type="bullet">
    ///         <item>HTTP logging, routing, rate limiting, auth middleware</item>
    ///         <item>gRPC-Web, HSTS, exception handling, output caching</item>
    ///         <item>OpenAPI and Scalar documentation (development only)</item>
    ///         <item>Auto-discovered gRPC services via <see cref="GrpcRegistrationExtensions.MapGrpcServices" /></item>
    ///         <item>Auto-discovered REST endpoints via <see cref="EndpointMappingExtensions.MapEndpoints" />
    ///             from classes implementing <see cref="IEndpointDefinition" /></item>
    ///         <item>Default health check endpoints via
    ///             <see cref="HealthCheckSetupExtensions.MapDefaultHealthCheckEndpoints" /></item>
    ///     </list>
    ///     <!--@include: @code/api/api-extensions-detailed.md#application-configuration -->
    /// </remarks>
    public static WebApplication ConfigureApiUsingDefaults(this WebApplication app)
    {
        if (app.Configuration.GetValue("HttpLogging:Enabled", false))
        {
            app.UseHttpLogging();
        }

        app.UseRouting();

        if (app.Configuration.GetValue("RateLimiting:Enabled", false))
        {
            app.UseRateLimiter();
        }

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
        app.MapEndpoints();
        app.MapDefaultHealthCheckEndpoints();

        return app;
    }
}
