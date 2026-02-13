// Copyright (c) Momentum .NET. All rights reserved.

namespace Momentum.ServiceDefaults.Api.FrontendIntegration;

/// <summary>
///     Configuration settings for security response headers, bound from the "SecurityHeaders" configuration section.
/// </summary>
public sealed class SecurityHeaderSettings
{
    public string XFrameOptions { get; set; } = "DENY";
    public string XContentTypeOptions { get; set; } = "nosniff";
    public string ReferrerPolicy { get; set; } = "strict-origin-when-cross-origin";
    public string ContentSecurityPolicy { get; set; } = "default-src 'self'";
}
