// Copyright (c) Momentum .NET. All rights reserved.

using Momentum.ServiceDefaults.Api.FrontendIntegration;

namespace Momentum.ServiceDefaults.Api.Tests.FrontendIntegration;

public class SecurityHeaderSettingsTests
{
    [Fact]
    public void Defaults_ShouldHaveExpectedValues()
    {
        var settings = new SecurityHeaderSettings();

        settings.XFrameOptions.ShouldBe("DENY");
        settings.XContentTypeOptions.ShouldBe("nosniff");
        settings.ReferrerPolicy.ShouldBe("strict-origin-when-cross-origin");
        settings.ContentSecurityPolicy.ShouldBe("default-src 'self'");
    }
}
