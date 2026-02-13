// Copyright (c) Momentum .NET. All rights reserved.

namespace Momentum.ServiceDefaults.OpenTelemetry;

/// <summary>
///     Configuration options for OpenTelemetry instrumentation.
/// </summary>
public class OpenTelemetryOptions
{
    /// <summary>
    ///     The configuration section name for OpenTelemetry options.
    /// </summary>
    public const string SectionName = "OpenTelemetry";

    /// <summary>
    ///     Sampling rate for production traces (0.0 to 1.0). Default: 0.1 (10%).
    /// </summary>
    public double ProductionSamplingRate { get; set; } = 0.1;
}
