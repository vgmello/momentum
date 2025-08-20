// Copyright (c) ORG_NAME. All rights reserved.

namespace AppDomain.Tests.E2E.Configuration;

public class TestSettings
{
    public string ApiUrl { get; set; } = "http://localhost:8101";

    public int TimeoutSeconds { get; set; } = 30;

    public int MaxRetries { get; set; } = 3;
}
