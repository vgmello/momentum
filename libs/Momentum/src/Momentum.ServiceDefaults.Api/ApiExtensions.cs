// Copyright (c) Momentum .NET. All rights reserved.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Momentum.ServiceDefaults.Api.OpenApi.Extensions;
using Scalar.AspNetCore;

namespace Momentum.ServiceDefaults.Api;

/// <summary>
///     Provides extension methods for configuring API services with sensible defaults.
/// </summary>
public static class ApiExtensions
{
    /// <summary>
    ///     Adds default API services to the application builder (excluding OpenAPI).
    /// </summary>
    /// <param name="builder">The web application builder to configure.</param>
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
    ///         builder.AddApiServiceDefaults();
    ///         builder.Services.AddOpenApi(options =&gt; options.ConfigureOpenApiDefaults());
    ///         </code>
    ///     </example>
    ///     <!--@include: @code/api/api-extensions-detailed.md#service-configuration -->
    /// </remarks>
    public static IHostApplicationBuilder AddApiServiceDefaults(this WebApplicationBuilder builder)
    {
        builder.Services.AddControllers(opt =>
        {
            opt.Conventions.Add(new RouteTokenTransformerConvention(new KebabCaseRoutesTransformer()));
        });

        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddProblemDetails();
        builder.Services.AddAutoProducesConvention();

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

        // Authentication and authorization services
        builder.Services.AddAuthentication();
        builder.Services.AddAuthorization();

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
    /// <param name="requireAuth">
    ///     Whether to require authorization for controller endpoints.
    ///     Defaults to <c>true</c>.
    /// </param>
    /// <returns>The configured web application for method chaining.</returns>
    /// <remarks>
    ///     <!--@include: @code/api/api-extensions-detailed.md#application-configuration -->
    /// </remarks>
    public static WebApplication ConfigureApiUsingDefaults(this WebApplication app, bool requireAuth = true)
    {
        app.UseHttpLogging();
        app.UseRouting();
        app.UseAuthentication();
        app.UseAuthorization();

        app.UseGrpcWeb(new GrpcWebOptions { DefaultEnabled = true });

        if (!app.Environment.IsDevelopment())
        {
            app.UseHsts();
            app.UseExceptionHandler();
        }

        app.UseOutputCache();

        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi().CacheOutput("OpenApi");
            app.MapScalarApiReference(options => options.WithTitle($"{app.Environment.ApplicationName} OpenAPI"));

            app.MapGrpcReflectionService();
        }

        var controllersEndpointBuilder = app.MapControllers();

        if (requireAuth)
            controllersEndpointBuilder.RequireAuthorization();

        app.MapGrpcServices();

        return app;
    }
}
