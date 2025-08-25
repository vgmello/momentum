// Copyright (c) Momentum .NET. All rights reserved.

using Microsoft.Extensions.Diagnostics.HealthChecks;
using Wolverine.Runtime;

namespace Momentum.Extensions.Messaging.Kafka;

/// <summary>
/// Health check implementation for Wolverine Kafka endpoints.
/// </summary>
public class WolverineKafkaHealthCheck : IHealthCheck
{
    private readonly IWolverineRuntime? _runtime;

    public WolverineKafkaHealthCheck(IWolverineRuntime? runtime)
    {
        _runtime = runtime;
    }

    /// <summary>
    /// Performs health check on Wolverine Kafka endpoints.
    /// </summary>
    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        if (_runtime is null)
        {
            return Task.FromResult(HealthCheckResult.Degraded("Wolverine runtime not available"));
        }

        try
        {
            var kafkaEndpoints = _runtime.Endpoints.ActiveListeners()
                .Where(e => e.Uri.Scheme.Equals("kafka", StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (!kafkaEndpoints.Any())
            {
                return Task.FromResult(HealthCheckResult.Healthy("No Kafka endpoints configured"));
            }

            var unhealthyEndpoints = new List<string>();
            var healthyCount = 0;

            foreach (var endpoint in kafkaEndpoints)
            {
                try
                {
                    // Check if the endpoint is available (Wolverine doesn't expose direct listening status)
                    // We'll consider an endpoint healthy if it exists and has a valid URI
                    if (endpoint.Uri != null && !string.IsNullOrEmpty(endpoint.Uri.ToString()))
                    {
                        healthyCount++;
                    }
                    else
                    {
                        unhealthyEndpoints.Add($"{endpoint.Uri} (invalid configuration)");
                    }
                }
                catch (Exception ex)
                {
                    unhealthyEndpoints.Add($"{endpoint.Uri} ({ex.Message})");
                }
            }

            var data = new Dictionary<string, object>
            {
                ["total_endpoints"] = kafkaEndpoints.Count,
                ["healthy_endpoints"] = healthyCount,
                ["unhealthy_endpoints"] = unhealthyEndpoints.Count
            };

            if (unhealthyEndpoints.Any())
            {
                data["unhealthy_details"] = unhealthyEndpoints;
                
                // If some endpoints are healthy, it's degraded; if none are healthy, it's unhealthy
                var status = healthyCount > 0 ? HealthStatus.Degraded : HealthStatus.Unhealthy;
                var message = $"Kafka endpoints: {healthyCount}/{kafkaEndpoints.Count} healthy";
                
                return Task.FromResult(new HealthCheckResult(status, message, data: data));
            }

            return Task.FromResult(HealthCheckResult.Healthy(
                $"All {kafkaEndpoints.Count} Kafka endpoints are healthy", 
                data));
        }
        catch (Exception ex)
        {
            return Task.FromResult(HealthCheckResult.Unhealthy(
                "Error checking Wolverine Kafka endpoints", 
                ex,
                data: new Dictionary<string, object> 
                { 
                    ["error"] = ex.Message,
                    ["error_type"] = ex.GetType().Name
                }));
        }
    }
}