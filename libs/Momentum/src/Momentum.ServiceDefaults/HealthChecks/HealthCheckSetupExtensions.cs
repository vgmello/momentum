// Copyright (c) Momentum .NET. All rights reserved.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Momentum.ServiceDefaults.Api.EndpointFilters;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Momentum.ServiceDefaults.HealthChecks;

[ExcludeFromCodeCoverage]
public static partial class HealthCheckSetupExtensions
{
    private const string HealthCheckLogName = "HealthChecks";

    private static readonly JsonSerializerOptions JsonSerializerOptions = new()
    {
        WriteIndented = false,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    /// <summary>
    ///     Configures default health check endpoints for the application.
    /// </summary>
    /// <remarks>
    ///     <!--@include: @code/health/health-check-setup-detailed.md#default-endpoints -->
    /// </remarks>
    public static WebApplication MapDefaultHealthCheckEndpoints(this WebApplication app)
    {
        var healthCheckStore = app.Services.GetRequiredService<HealthCheckStatusStore>();

        var logger = GetHealthCheckLogger(app.Services);

        // liveness probe
        app.MapGet("/status", () =>
            {
                var statusCode = healthCheckStore.LastHealthStatus is not HealthStatus.Unhealthy
                    ? StatusCodes.Status200OK
                    : StatusCodes.Status503ServiceUnavailable;

                return Results.Text(healthCheckStore.LastHealthStatus.ToString(), statusCode: statusCode);
            })
            .ExcludeFromDescription();

        // container-only health check probe
        var isDevelopment = app.Environment.IsDevelopment();
        app.MapHealthChecks("/health/internal",
                new HealthCheckOptions
                {
                    ResponseWriter = (ctx, report) =>
                        ProcessHealthCheckResult(ctx, logger, healthCheckStore, report, outputResult: isDevelopment)
                })
            .RequireHost("localhost")
            .AddEndpointFilter(new LocalhostEndpointFilter(logger));

        // public health check probe
        app.MapHealthChecks("/health",
                new HealthCheckOptions
                {
                    ResponseWriter = (ctx, report) =>
                        ProcessHealthCheckResult(ctx, logger, healthCheckStore, report, outputResult: true)
                })
            .RequireAuthorization();

        return app;
    }

    private static Task ProcessHealthCheckResult(
        HttpContext httpContext,
        ILogger logger,
        HealthCheckStatusStore healthCheckStore,
        HealthReport report,
        bool outputResult)
    {
        LogHealthCheckResponse(logger, report);

        healthCheckStore.LastHealthStatus = report.Status;

        httpContext.Response.StatusCode = report.Status == HealthStatus.Unhealthy ?
            StatusCodes.Status503ServiceUnavailable : StatusCodes.Status200OK;

        return outputResult
            ? WriteReportObject(httpContext, report)
            : httpContext.Response.WriteAsync(report.Status.ToString());
    }

    private static void LogHealthCheckResponse(ILogger logger, HealthReport report)
    {
        if (report.Status is HealthStatus.Healthy)
        {
            LogSuccessfulHealthCheck(logger, report);

            return;
        }

        var logLevel = report.Status == HealthStatus.Unhealthy ? LogLevel.Error : LogLevel.Warning;

        var failedHealthReport = report.Entries.Select(e =>
            new { e.Key, e.Value.Status, e.Value.Duration, Error = e.Value.Exception?.Message });

        LogFailedHealthCheck(logger, logLevel, failedHealthReport);
    }

    private static Task WriteReportObject(HttpContext context, HealthReport report)
    {
        var response = new
        {
            Status = report.Status.ToString(),
            Duration = report.TotalDuration,
            Info = report.Entries
                .Select(e =>
                    new
                    {
                        e.Key,
                        e.Value.Description,
                        e.Value.Duration,
                        Status = Enum.GetName(e.Value.Status),
                        Error = e.Value.Exception?.Message,
                        e.Value.Data
                    })
                .ToList()
        };

        return context.Response.WriteAsJsonAsync(response, options: JsonSerializerOptions);
    }

    private static ILogger GetHealthCheckLogger(IServiceProvider provider)
    {
        var loggerFactory = provider.GetRequiredService<ILoggerFactory>();

        return loggerFactory.CreateLogger(HealthCheckLogName);
    }

    [LoggerMessage(EventId = 1, Level = LogLevel.Debug, Message = "Health check response: {@HealthReport}")]
    private static partial void LogSuccessfulHealthCheck(ILogger logger, HealthReport healthReport);

    [LoggerMessage(EventId = 2, Message = "Health check failed: {FailedHealthReport}")]
    private static partial void LogFailedHealthCheck(ILogger logger, LogLevel level, object failedHealthReport);
}
