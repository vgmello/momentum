// Copyright (c) OrgName. All rights reserved.

using AppDomain.Tests.E2E.OpenApi.Generated;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.CommandLine;

namespace AppDomain.Tests.E2E;

/// <summary>
///     Test fixture (only initialized once)
/// </summary>
public sealed class End2EndTestFixture : IDisposable
{
    public TestSettings TestSettings { get; }

    public HttpClient HttpClient { get; }

    public AppDomainApiClient ApiClient { get; }

    public End2EndTestFixture()
    {
        var configuration = BuildConfiguration();

        TestSettings = configuration.GetSection(nameof(TestSettings)).Get<TestSettings>() ?? new TestSettings();

        HttpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(TestSettings.TimeoutSeconds)
        };

        ApiClient = new AppDomainApiClient(TestSettings.ApiUrl, HttpClient);

        LogTestConfiguration();
    }

    private static IConfiguration BuildConfiguration()
    {
        var args = Environment.GetCommandLineArgs();

        var cliArgs = new CommandLineConfigurationProvider(args);
        cliArgs.Load();

        if (!cliArgs.TryGet("config", out var config))
        {
            config = "dev";
        }

        return new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json")
            .AddJsonFile($"appsettings.{config}.json", optional: true)
            .AddCommandLine(args)
            .Build();
    }

    private void LogTestConfiguration()
    {
        Console.WriteLine("=== E2E Test Configuration ===");
        Console.WriteLine($"Environment: {TestSettings.Environment}");
        Console.WriteLine($"API Base URL: {TestSettings.ApiUrl}");
        Console.WriteLine($"Timeout: {TestSettings.TimeoutSeconds}s");
        Console.WriteLine($"Max Retries: {TestSettings.MaxRetries}");
        Console.WriteLine("================================");
    }

    public void Dispose()
    {
        HttpClient.Dispose();
    }
}
