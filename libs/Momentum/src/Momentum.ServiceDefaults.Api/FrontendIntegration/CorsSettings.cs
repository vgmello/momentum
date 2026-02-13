// Copyright (c) Momentum .NET. All rights reserved.

namespace Momentum.ServiceDefaults.Api.FrontendIntegration;

/// <summary>
///     Configuration settings for CORS policy, bound from the "Cors" configuration section.
/// </summary>
public sealed class CorsSettings
{
    public string[] AllowedOrigins { get; set; } = [];
    public string[] AllowedMethods { get; set; } = ["GET", "POST", "PUT", "DELETE"];
    public string[] AllowedHeaders { get; set; } = ["Content-Type", "Authorization"];
    public bool AllowCredentials { get; set; }
}
