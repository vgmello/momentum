// Copyright (c) OrgName. All rights reserved.

namespace AppDomain.Tests.E2E;

public class TestSettings
{
    public string Environment { get; set; } = "Dev";

    public string ApiUrl { get; set; } = "http://localhost:8101";

    public int TimeoutSeconds { get; set; } = 30;

    public int MaxRetries { get; set; } = 3;
}
