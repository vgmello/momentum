// Copyright (c) Momentum .NET. All rights reserved.

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;

namespace Momentum.ServiceDefaults.Logging;

public static class LoggingSetupExtensions
{
    /// <summary>
    ///     Sets up a bootstrap logger for use during the two-stage initialization of the app host.
    ///     This ensures that any exceptions or log events during host setup are captured and reported.
    ///     See: https://github.com/serilog/serilog-aspnetcore?tab=readme-ov-file#two-stage-initialization
    /// </summary>
    public static void UseInitializationLogger(this WebApplicationBuilder builder)
    {
        Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(builder.Configuration)
            .Enrich.FromLogContext()
            .WriteTo.Console()
            .WriteTo.OpenTelemetry()
            .CreateBootstrapLogger();
    }

    public static IHostApplicationBuilder AddLogging(this IHostApplicationBuilder builder)
    {
        builder.Logging.ClearProviders();
        builder.Services.AddSerilog((services, loggerConfig) => ConfigureLogger(loggerConfig, builder.Configuration, services));

        return builder;
    }

    public static void ConfigureLogger(
        LoggerConfiguration loggerConfiguration, IConfiguration configuration, IServiceProvider services) =>
        loggerConfiguration
            .ReadFrom.Configuration(configuration)
            .ReadFrom.Services(services)
            .WriteTo.OpenTelemetry()
            .Enrich.FromLogContext();
}
