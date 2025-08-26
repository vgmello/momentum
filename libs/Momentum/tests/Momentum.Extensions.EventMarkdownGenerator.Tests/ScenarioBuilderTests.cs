// Copyright (c) Momentum .NET. All rights reserved.

using Shouldly;
using System.Text.Json;
using Xunit;

namespace Momentum.Extensions.EventMarkdownGenerator.Tests;

public class ScenarioBuilderTests
{
    private readonly string _testScenariosPath = Path.Combine(Path.GetTempPath(), "test-scenarios");

    [Fact]
    public async Task TestScenarioBuilder_ShouldCreateValidScenarioStructure()
    {
        // Arrange
        var scenarioName = "builder-test-scenario";
        var scenarioPath = Path.Combine(_testScenariosPath, scenarioName);

        // Clean up any existing test data
        if (Directory.Exists(scenarioPath))
            Directory.Delete(scenarioPath, true);

        try
        {
            // Act
            var builder = new TestScenarioBuilder(scenarioName, _testScenariosPath)
                .WithEvent("Test.Events.UserCreated",
                    "Published when a user is created",
                    "## When It's Triggered\n\nWhen user creation completes",
                    new Dictionary<string, string>
                    {
                        { "tenantId", "The tenant identifier" },
                        { "userId", "The user identifier" },
                        { "email", "The user email address" }
                    })
                .WithExpectedEventMarkdown("user-created.md", "# UserCreated\n\nSample content")
                .WithConfig(strictFileMatching: true, generateSchemas: false);

            var createdPath = await builder.BuildAsync();

            // Assert
            createdPath.ShouldBe(scenarioPath);
            Directory.Exists(scenarioPath).ShouldBeTrue();

            // Verify input.xml exists and is valid
            var inputXmlPath = Path.Combine(scenarioPath, "input.xml");
            File.Exists(inputXmlPath).ShouldBeTrue();

            var xmlContent = await File.ReadAllTextAsync(inputXmlPath, TestContext.Current.CancellationToken);
            xmlContent.ShouldContain("Test.Events.UserCreated");
            xmlContent.ShouldContain("Published when a user is created");
            xmlContent.ShouldContain("The tenant identifier");

            // Verify expected folder and files
            var expectedPath = Path.Combine(scenarioPath, "expected");
            Directory.Exists(expectedPath).ShouldBeTrue();

            var expectedFilePath = Path.Combine(expectedPath, "user-created.md");
            File.Exists(expectedFilePath).ShouldBeTrue();

            var expectedContent = await File.ReadAllTextAsync(expectedFilePath, TestContext.Current.CancellationToken);
            expectedContent.ShouldBe("# UserCreated\n\nSample content");

            // Verify config.json
            var configPath = Path.Combine(scenarioPath, "config.json");
            File.Exists(configPath).ShouldBeTrue();

            var configContent = await File.ReadAllTextAsync(configPath, TestContext.Current.CancellationToken);
            var config = JsonSerializer.Deserialize<TestScenarioConfig>(configContent);
            config.ShouldNotBeNull();
            config.StrictFileMatching.ShouldBeTrue();
            config.GenerateSchemas.ShouldBeFalse();
        }
        finally
        {
            // Clean up
            if (Directory.Exists(scenarioPath))
                Directory.Delete(scenarioPath, true);
        }
    }

    [Fact]
    public async Task TestScenarioFactory_ShouldCreateBasicEventScenario()
    {
        // Arrange
        var scenarioName = "factory-basic-test";
        var scenarioPath = Path.Combine(_testScenariosPath, scenarioName);

        if (Directory.Exists(scenarioPath))
            Directory.Delete(scenarioPath, true);

        try
        {
            // Act
            var builder = TestScenarioFactory.CreateBasicEvent(
                scenarioName,
                "AppDomain.Events.PaymentReceived",
                "Published when payment is received",
                new Dictionary<string, string>
                {
                    { "tenantId", "Tenant identifier" },
                    { "amount", "Payment amount" }
                });

            builder.WithExpectedEventMarkdown("payment-received.md", "# PaymentReceived\n\nBasic payment event");

            var createdPath = await builder.BuildAsync();

            // Assert
            Directory.Exists(createdPath).ShouldBeTrue();

            var configPath = Path.Combine(createdPath, "config.json");
            var configContent = await File.ReadAllTextAsync(configPath, TestContext.Current.CancellationToken);
            var config = JsonSerializer.Deserialize<TestScenarioConfig>(configContent);

            config.ShouldNotBeNull();
            config.GenerateSchemas.ShouldBeFalse();
            config.GenerateSidebar.ShouldBeFalse();
        }
        finally
        {
            if (Directory.Exists(scenarioPath))
                Directory.Delete(scenarioPath, true);
        }
    }

    [Fact]
    public async Task TestScenarioFactory_ShouldCreateComplexEventWithSchemas()
    {
        // Arrange
        var scenarioName = "factory-complex-test";
        var scenarioPath = Path.Combine(_testScenariosPath, scenarioName);

        if (Directory.Exists(scenarioPath))
            Directory.Delete(scenarioPath, true);

        try
        {
            // Act
            var schemas = new List<(string, string, Dictionary<string, string>)>
            {
                ("AppDomain.Models.Customer", "Customer information", new Dictionary<string, string>
                {
                    { "Id", "Customer identifier" },
                    { "Name", "Customer name" }
                })
            };

            var builder = TestScenarioFactory.CreateComplexEventWithSchemas(
                scenarioName,
                "AppDomain.Events.OrderCreated",
                "Published when order is created",
                "## When It's Triggered\n\nWhen order creation completes",
                new Dictionary<string, string>
                {
                    { "tenantId", "Tenant identifier" },
                    { "customer", "Customer information" }
                },
                schemas);

            builder.WithExpectedEventMarkdown("order-created.md", "# OrderCreated\n\nOrder event with customer schema")
                .WithExpectedSchema("AppDomain.Models.Customer.md", "# Customer\n\nCustomer schema");

            var createdPath = await builder.BuildAsync();

            // Assert
            Directory.Exists(createdPath).ShouldBeTrue();

            var expectedSchemasPath = Path.Combine(createdPath, "expected", "schemas");
            Directory.Exists(expectedSchemasPath).ShouldBeTrue();

            var schemaFilePath = Path.Combine(expectedSchemasPath, "AppDomain.Models.Customer.md");
            File.Exists(schemaFilePath).ShouldBeTrue();

            var configPath = Path.Combine(createdPath, "config.json");
            var configContent = await File.ReadAllTextAsync(configPath, TestContext.Current.CancellationToken);
            var config = JsonSerializer.Deserialize<TestScenarioConfig>(configContent);

            config.ShouldNotBeNull();
            config.GenerateSchemas.ShouldBeTrue();
            config.GenerateSidebar.ShouldBeTrue();
        }
        finally
        {
            if (Directory.Exists(scenarioPath))
                Directory.Delete(scenarioPath, true);
        }
    }
}
