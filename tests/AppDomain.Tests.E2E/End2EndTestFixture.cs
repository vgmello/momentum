// Copyright (c) OrgName. All rights reserved.

using AppDomain.Tests.E2E.OpenApi.Generated;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.CommandLine;
using Microsoft.Extensions.DependencyInjection;

namespace AppDomain.Tests.E2E;

/// <summary>
///     Test fixture (only initialized once)
/// </summary>
public sealed class End2EndTestFixture : IDisposable
{
    private readonly ServiceProvider _serviceProvider;

    public TestSettings TestSettings { get; }

    public IAppDomainApiClient ApiClient { get; }

    public End2EndTestFixture()
    {
        var configuration = BuildConfiguration();

        TestSettings = configuration.GetSection(nameof(TestSettings)).Get<TestSettings>() ?? new TestSettings();

        var services = new ServiceCollection();
        services.ConfigureRefitClients(
            new Uri(TestSettings.ApiUrl),
            builder => builder.ConfigureHttpClient(c =>
                c.Timeout = TimeSpan.FromSeconds(TestSettings.TimeoutSeconds)));

        _serviceProvider = services.BuildServiceProvider();
        ApiClient = _serviceProvider.GetRequiredService<IAppDomainApiClient>();

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
        _serviceProvider.Dispose();
    }
}
