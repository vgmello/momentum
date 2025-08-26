// Copyright (c) Momentum .NET. All rights reserved.

using FluentValidation;
using JasperFx;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Momentum.ServiceDefaults.Logging;
using Momentum.ServiceDefaults.Messaging;
using Momentum.ServiceDefaults.OpenTelemetry;
using Serilog;
using System.Reflection;

namespace Momentum.ServiceDefaults;

/// <summary>
///     Provides extension methods for configuring service defaults in the application.
/// </summary>
public static class ServiceDefaultsExtensions
{
    private static Assembly? _entryAssembly;

    /// <summary>
    ///     Gets or sets the entry assembly for the application.
    /// </summary>
    /// <value>
    ///     The entry assembly used for discovering domain assemblies and validators.
    ///     If not explicitly set, it defaults to the application's entry assembly.
    /// </value>
    /// <exception cref="InvalidOperationException">
    ///     Thrown when attempting to get the entry assembly, and it cannot be determined.
    /// </exception>
    public static Assembly EntryAssembly
    {
        get => _entryAssembly ??= GetEntryAssembly();
        set => _entryAssembly = value;
    }

    /// <summary>
    ///     Adds common service defaults to the application builder for production-ready microservices.
    /// </summary>
    /// <param name="builder">The web application builder to configure.</param>
    /// <returns>The configured host application builder for method chaining.</returns>
    /// <remarks>
    ///     <!--@include: @code/service-configuration/service-defaults-detailed.md -->
    /// </remarks>
    /// <example>
    ///     <!--@include: @code/examples/service-defaults-examples.md -->
    /// </example>
    public static IHostApplicationBuilder AddServiceDefaults(this WebApplicationBuilder builder)
    {
        builder.UseInitializationLogger();

        builder.WebHost.UseKestrelHttpsConfiguration();

        builder.AddLogging();
        builder.AddOpenTelemetry();
        builder.AddServiceBus();
        builder.AddValidators();

        builder.Services.AddHealthChecks();
        builder.Services.AddServiceDiscovery();

        builder.Services.ConfigureHttpClientDefaults(http =>
        {
            // Resilience by default
            http.AddStandardResilienceHandler();

            // Service discovery by default
            http.AddServiceDiscovery();
        });

        return builder;
    }

    /// <summary>
    ///     Adds FluentValidation validators from the entry assembly and all domain assemblies.
    /// </summary>
    /// <param name="builder">The web application builder to configure.</param>
    /// <remarks>
    ///     This method automatically discovers and registers FluentValidation validators from:
    ///     <list type="bullet">
    ///         <item>The entry assembly (your main application assembly)</item>
    ///         <item>All assemblies marked with <see cref="DomainAssemblyAttribute" /></item>
    ///     </list>
    ///     Validators are registered as scoped services and integrated with Wolverine's
    ///     message handling pipeline for automatic validation of commands and queries.
    /// 
    ///     This approach supports the Domain-Driven Design pattern where validation
    ///     rules are defined close to the domain entities and business logic.
    /// </remarks>
    /// <example>
    ///     See <see cref="AddServiceDefaults" /> for examples.
    /// </example>
    public static void AddValidators(this WebApplicationBuilder builder)
    {
        builder.Services.AddValidatorsFromAssembly(EntryAssembly);

        foreach (var assembly in DomainAssemblyAttribute.GetDomainAssemblies())
            builder.Services.AddValidatorsFromAssembly(assembly);
    }

    /// <summary>
    ///     Runs the web application with proper initialization, error handling, and command-line tool support.
    /// </summary>
    /// <param name="app">The web application to run.</param>
    /// <param name="args">Command line arguments passed to the application.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    /// <remarks>
    ///     This method provides robust application lifecycle management including:
    ///     <list type="bullet">
    ///         <item>Bootstrap logger initialization for early-stage logging</item>
    ///         <item>Wolverine CLI command detection and execution for DevOps tasks</item>
    ///         <item>Graceful exception handling with structured logging</item>
    ///         <item>Proper log flushing on application shutdown</item>
    ///         <item>Support for containerized environments and orchestrators</item>
    ///     </list>
    /// 
    ///     <para>
    ///         <strong>Supported Wolverine Commands:</strong>
    ///     </para>
    ///     <list type="table">
    ///         <item>
    ///             <term>check-env</term>
    ///             <description>Validates messaging infrastructure connectivity</description>
    ///         </item>
    ///         <item>
    ///             <term>codegen</term>
    ///             <description>Generates Wolverine handler code for optimization</description>
    ///         </item>
    ///         <item>
    ///             <term>db-apply, db-assert</term>
    ///             <description>Database schema migration and validation</description>
    ///         </item>
    ///         <item>
    ///             <term>storage</term>
    ///             <description>Message persistence storage management</description>
    ///         </item>
    ///     </list>
    /// </remarks>
    /// <example>
    ///     See <see cref="AddServiceDefaults" /> for examples.
    /// </example>
    public static async Task RunAsync(this WebApplication app, string[] args)
    {
        try
        {
            if (args.Length > 0 && WolverineCommands.Contains(args[0]))
            {
                await app.RunJasperFxCommands(args);

                return;
            }

            await app.RunAsync();
        }
        catch (Exception e)
        {
            Log.Fatal(e, "Application terminated unexpectedly");
        }
        finally
        {
            await Log.CloseAndFlushAsync();
        }
    }

    private static readonly HashSet<string> WolverineCommands =
    [
        "check-env",
        "codegen",
        "db-apply",
        "db-assert",
        "db-dump",
        "db-patch",
        "describe",
        "help",
        "resources",
        "storage"
    ];

    private static Assembly GetEntryAssembly()
    {
        return Assembly.GetEntryAssembly() ??
               throw new InvalidOperationException(
                   "Unable to identify entry assembly. Please provide an assembly via the ServiceDefaultsExtensions.EntryAssembly property.");
    }
}
