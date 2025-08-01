// Copyright (c) ABCDEG. All rights reserved.

using FluentValidation;
using JasperFx;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Operations.ServiceDefaults.Logging;
using Operations.ServiceDefaults.Messaging.Wolverine;
using Operations.ServiceDefaults.OpenTelemetry;
using Serilog;
using System.Reflection;

namespace Operations.ServiceDefaults;

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
    ///     Adds common service defaults to the application builder.
    /// </summary>
    /// <param name="builder">The web application builder to configure.</param>
    /// <returns>The configured host application builder for method chaining.</returns>
    /// <remarks>
    ///     This method configures the following:
    ///     <list type="bullet">
    ///         <item>HTTPS configuration for Kestrel</item>
    ///         <item>Structured logging with Serilog</item>
    ///         <item>OpenTelemetry for observability</item>
    ///         <item>Wolverine messaging framework</item>
    ///         <item>FluentValidation validators from domain assemblies</item>
    ///         <item>Health checks</item>
    ///         <item>Service discovery</item>
    ///         <item>HTTP client resilience with standard retry policies</item>
    ///     </list>
    /// </remarks>
    public static IHostApplicationBuilder AddServiceDefaults(this WebApplicationBuilder builder)
    {
        builder.WebHost.UseKestrelHttpsConfiguration();

        builder.AddLogging();
        builder.AddOpenTelemetry();
        builder.AddWolverine();
        builder.AddValidators();

        builder.Services.AddHealthChecks();
        builder.Services.AddServiceDiscovery();

        builder.Services.ConfigureHttpClientDefaults(http =>
        {
            // Turn on resilience by default
            http.AddStandardResilienceHandler();
        });

        return builder;
    }

    /// <summary>
    ///     Adds FluentValidation validators from the entry assembly and all domain assemblies.
    /// </summary>
    /// <param name="builder">The web application builder to configure.</param>
    /// <remarks>
    ///     This method scans for validators in:
    ///     <list type="bullet">
    ///         <item>The entry assembly</item>
    ///         <item>All assemblies marked with <see cref="DomainAssemblyAttribute" /></item>
    ///     </list>
    /// </remarks>
    public static void AddValidators(this WebApplicationBuilder builder)
    {
        builder.Services.AddValidatorsFromAssembly(EntryAssembly);

        foreach (var assembly in DomainAssemblyAttribute.GetDomainAssemblies())
            builder.Services.AddValidatorsFromAssembly(assembly);
    }

    /// <summary>
    ///     Runs the web application with proper initialization and error handling.
    /// </summary>
    /// <param name="app">The web application to run.</param>
    /// <param name="args">Command line arguments.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    /// <remarks>
    ///     This method:
    ///     <list type="bullet">
    ///         <item>Initializes the logging system</item>
    ///         <item>Handles Wolverine command-line operations if specified</item>
    ///         <item>Runs the application with proper exception handling</item>
    ///         <item>Ensures logs are flushed on application shutdown</item>
    ///     </list>
    ///     Supported Wolverine commands include: check-env, codegen, db-apply, db-assert,
    ///     db-dump, db-patch, describe, help, resources, and storage.
    /// </remarks>
    public static async Task RunAsync(this WebApplication app, string[] args)
    {
        app.UseInitializationLogger();

        try
        {
            if (args.Length > 0 && WolverineCommands.Contains(args[0]))
            {
                await app.RunJasperFxCommands(args);
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
                   "Unable to identify entry assembly. Please provide an assembly via the Extensions.AssemblyMarker property.");
    }
}
