// Copyright (c) Momentum .NET. All rights reserved.

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Momentum.ServiceDefaults.Api.FrontendIntegration;

/// <summary>
///     Provides extension methods for configuring frontend integration features
///     such as CORS and security headers.
/// </summary>
public static class FrontendIntegrationExtensions
{
    internal const string CorsPolicyName = "FrontendIntegrationPolicy";

    /// <summary>
    ///     Adds CORS and security header services for frontend integration.
    /// </summary>
    /// <param name="builder">The web application builder to configure.</param>
    /// <returns>The configured host application builder for method chaining.</returns>
    public static IHostApplicationBuilder AddFrontendIntegration(this WebApplicationBuilder builder)
    {
        builder.AddCorsFromConfiguration();
        builder.Services.Configure<SecurityHeaderSettings>(
            builder.Configuration.GetSection("SecurityHeaders"));

        builder.Services.AddResponseCompression(options =>
        {
            options.EnableForHttps = true;
            options.Providers.Add<Microsoft.AspNetCore.ResponseCompression.BrotliCompressionProvider>();
            options.Providers.Add<Microsoft.AspNetCore.ResponseCompression.GzipCompressionProvider>();
        });

        builder.Services.Configure<Microsoft.AspNetCore.ResponseCompression.BrotliCompressionProviderOptions>(options =>
        {
            options.Level = System.IO.Compression.CompressionLevel.Fastest;
        });

        return builder;
    }

    /// <summary>
    ///     Applies CORS and security header middleware for frontend integration.
    /// </summary>
    /// <param name="app">The web application to configure.</param>
    /// <returns>The configured web application for method chaining.</returns>
    public static WebApplication UseFrontendIntegration(this WebApplication app)
    {
        if (!app.Environment.IsDevelopment())
        {
            app.UseHttpsRedirection();
        }

        app.UseResponseCompression();
        app.UseCors(CorsPolicyName);
        app.UseSecurityHeaders();

        return app;
    }

    /// <summary>
    ///     Adds CORS services configured from the "Cors" configuration section.
    /// </summary>
    /// <param name="builder">The web application builder to configure.</param>
    /// <returns>The configured web application builder for method chaining.</returns>
    public static WebApplicationBuilder AddCorsFromConfiguration(this WebApplicationBuilder builder)
    {
        var corsSettings = builder.Configuration
            .GetSection("Cors")
            .Get<CorsSettings>() ?? new CorsSettings();

        if (corsSettings.AllowCredentials && corsSettings.AllowedOrigins.Length == 0)
        {
            throw new InvalidOperationException(
                "CORS: AllowCredentials requires explicit AllowedOrigins. Wildcard origins are not permitted with credentials.");
        }

        builder.Services.AddCors(options =>
        {
            options.AddPolicy(CorsPolicyName, policy =>
            {
                policy.WithOrigins(corsSettings.AllowedOrigins)
                    .WithMethods(corsSettings.AllowedMethods)
                    .WithHeaders(corsSettings.AllowedHeaders);

                if (corsSettings.AllowCredentials)
                {
                    policy.AllowCredentials();
                }
            });
        });

        return builder;
    }

    /// <summary>
    ///     Adds security headers middleware that sets X-Frame-Options, X-Content-Type-Options,
    ///     Referrer-Policy, and Content-Security-Policy response headers.
    /// </summary>
    /// <param name="app">The web application to configure.</param>
    /// <returns>The configured web application for method chaining.</returns>
    public static WebApplication UseSecurityHeaders(this WebApplication app)
    {
        var settings = app.Services.GetRequiredService<IOptions<SecurityHeaderSettings>>().Value;

        app.Use(async (context, next) =>
        {
            var headers = context.Response.Headers;
            headers.XFrameOptions = settings.XFrameOptions;
            headers.XContentTypeOptions = settings.XContentTypeOptions;
            headers["Referrer-Policy"] = settings.ReferrerPolicy;
            headers.ContentSecurityPolicy = settings.ContentSecurityPolicy;

            await next();
        });

        return app;
    }
}
