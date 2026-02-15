// Copyright (c) Momentum .NET. All rights reserved.

using Momentum.ServiceDefaults.Api.FrontendIntegration;

namespace Momentum.ServiceDefaults.Api.Tests.FrontendIntegration;

public class CorsSettingsTests
{
    [Fact]
    public void Defaults_ShouldHaveExpectedValues()
    {
        var settings = new CorsSettings();

        settings.AllowedOrigins.ShouldBeEmpty();
        settings.AllowedMethods.ShouldBe(["GET", "POST", "PUT", "PATCH", "DELETE"]);
        settings.AllowedHeaders.ShouldBe(["Content-Type", "Authorization"]);
        settings.AllowCredentials.ShouldBeFalse();
    }
}
