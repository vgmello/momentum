// Copyright (c) Momentum .NET. All rights reserved.

namespace Momentum.ServiceDefaults.OpenTelemetry;

/// <summary>
///     Configuration options for OpenTelemetry instrumentation.
/// </summary>
[ExcludeFromCodeCoverage]
public class OpenTelemetryOptions
{
    /// <summary>
    ///     The configuration section name for OpenTelemetry options.
    /// </summary>
    public const string SectionName = "OpenTelemetry";

    /// <summary>
    ///     Sampling rate for traces (0.0 to 1.0). When not set, defaults to 1.0 in development and 0.1 in production.
    /// </summary>
    public double? SamplingRate { get; set; }
}
