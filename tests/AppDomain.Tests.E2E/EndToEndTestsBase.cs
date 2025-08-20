// Copyright (c) ORG_NAME. All rights reserved.

namespace AppDomain.Tests.E2E;

/// <summary>
/// Base class for E2E tests that provides HTTP client and API availability checks
/// </summary>
public abstract class EndToEndTestsBase : IDisposable
{
    private readonly HttpClient _httpClient;

    protected readonly TestSettings TestSettings;

    protected EndToEndTestsBase()
    {
        var configuration = BuildConfiguration();

        TestSettings = new TestSettings();
        configuration.GetSection("TestSettings").Bind(TestSettings);

        _httpClient = new HttpClient
        {
            BaseAddress = new Uri(TestSettings.ApiUrl),
            Timeout = TimeSpan.FromSeconds(TestSettings.TimeoutSeconds)
        };
    }

    private static IConfiguration BuildConfiguration()
    {
        var environment = GetTestEnvironment();

        var builder = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
            .AddJsonFile($"appsettings.{environment}.json", optional: true, reloadOnChange: false)
            .AddEnvironmentVariables("E2E_")
            .AddCommandLine(Environment.GetCommandLineArgs());

        return builder.Build();
    }

    private void LogTestConfiguration()
    {
        Console.WriteLine($"=== E2E Test Configuration ===");
        Console.WriteLine($"Environment: {TestSettings.Environment}");
        Console.WriteLine($"API Base URL: {TestSettings.ApiUrl}");
        Console.WriteLine($"Timeout: {TestSettings.TimeoutSeconds}s");
        Console.WriteLine($"Max Retries: {TestSettings.MaxRetries}");
        Console.WriteLine($"================================");
    }

    protected HttpClient HttpClient => _httpClient;

    protected async Task<T?> GetJsonAsync<T>(string endpoint)
    {
        var response = await _httpClient.GetAsync(endpoint);
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<T>(content, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
    }

    protected async Task<HttpResponseMessage> PostJsonAsync<T>(string endpoint, T data)
    {
        var json = JsonSerializer.Serialize(data, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        var content = new StringContent(json, Encoding.UTF8, "application/json");
        return await _httpClient.PostAsync(endpoint, content);
    }

    protected async Task<HttpResponseMessage> PutJsonAsync<T>(string endpoint, T data)
    {
        var json = JsonSerializer.Serialize(data, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        var content = new StringContent(json, Encoding.UTF8, "application/json");
        return await _httpClient.PutAsync(endpoint, content);
    }

    public void Dispose()
    {
        _httpClient?.Dispose();
        GC.SuppressFinalize(this);
    }
}
