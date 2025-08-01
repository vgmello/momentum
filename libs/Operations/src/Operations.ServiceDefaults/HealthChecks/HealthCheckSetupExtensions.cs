// Copyright (c) ABCDEG. All rights reserved.

using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Operations.ServiceDefaults.Api.EndpointFilters;

namespace Operations.ServiceDefaults.HealthChecks;

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
    ///     This method sets up the following health check endpoints:
    ///     <list type="table">
    ///         <item>
    ///             <term>
    ///                 <c>/status</c>
    ///             </term>
    ///             <description>
    ///                 A lightweight endpoint returning the status string of the last health check. Used for liveness probes.
    ///                 <br />
    ///                 Note: This endpoint does not actually execute any health checks
    ///             </description>
    ///         </item>
    ///         <item>
    ///             <term>
    ///                 <c>/health/internal</c>
    ///             </term>
    ///             <description>
    ///                 A container-only endpoint that returns simplified version health status information. Used for readiness
    ///                 probes in cloud environments.<br />
    ///                 Locally, this endpoint will return the same information as the <c>/health</c> endpoint.
    ///             </description>
    ///         </item>
    ///         <item>
    ///             <term>
    ///                 <c>/health</c>
    ///             </term>
    ///             <description>A public endpoint requiring authorization that returns detailed health status information.</description>
    ///         </item>
    ///     </list>
    /// </remarks>
    public static WebApplication MapDefaultHealthCheckEndpoints(this WebApplication app)
    {
        // If the HealthCheckStatusStore is registered in the app DI container, we will use it,
        // otherwise we will create a new one. This simplifies app registration in the majority of cases,
        // while allowing the app to query for health status if it ever needs to.
        var healthCheckStore = app.Services.GetService<HealthCheckStatusStore>() ?? new HealthCheckStatusStore();

        var logger = GetHealthCheckLogger(app.Services);

        // liveness probe
        app.MapGet("/status", () => healthCheckStore.LastHealthStatus.ToString())
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
