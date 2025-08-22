// Copyright (c) Momentum .NET. All rights reserved.

using System.Text.Json;
using System.Text;

namespace Momentum.Extensions.EventMarkdownGenerator.Tests;

/// <summary>
///     Helper class to programmatically create test scenarios for the integration tests
/// </summary>
public class TestScenarioBuilder
{
    private readonly string _scenarioName;
    private readonly string _basePath;
    private readonly StringBuilder _xmlBuilder;
    private readonly List<ExpectedFile> _expectedFiles = [];
    private TestScenarioConfig _config = new();

    public TestScenarioBuilder(string scenarioName, string basePath = "IntegrationTestScenarios")
    {
        _scenarioName = scenarioName;
        _basePath = basePath;
        _xmlBuilder = new StringBuilder();
        _xmlBuilder.AppendLine("<?xml version=\"1.0\"?>");
        _xmlBuilder.AppendLine("<doc>");
        _xmlBuilder.AppendLine("    <assembly>");
        _xmlBuilder.AppendLine("        <name>TestEvents</name>");
        _xmlBuilder.AppendLine("    </assembly>");
        _xmlBuilder.AppendLine("    <members>");
    }

    public TestScenarioBuilder WithEvent(
        string eventType,
        string summary,
        string? remarks = null,
        Dictionary<string, string>? parameters = null)
    {
        _xmlBuilder.AppendLine($"        <member name=\"T:{eventType}\">");
        _xmlBuilder.AppendLine($"            <summary>{summary}</summary>");

        if (!string.IsNullOrEmpty(remarks))
        {
            _xmlBuilder.AppendLine("            <remarks>");
            _xmlBuilder.AppendLine($"            {remarks}");
            _xmlBuilder.AppendLine("            </remarks>");
        }

        _xmlBuilder.AppendLine("        </member>");

        // Add constructor documentation if parameters provided
        if (parameters != null && parameters.Any())
        {
            var parameterTypes = string.Join(",", parameters.Keys.Select(GetSystemType));
            _xmlBuilder.AppendLine($"        <member name=\"M:{eventType}.#ctor({parameterTypes})\">");

            foreach (var (paramName, paramDesc) in parameters)
            {
                _xmlBuilder.AppendLine($"            <param name=\"{paramName}\">{paramDesc}</param>");
            }

            _xmlBuilder.AppendLine("        </member>");
        }

        return this;
    }

    public TestScenarioBuilder WithTypeDocumentation(
        string typeName,
        string summary,
        Dictionary<string, string>? properties = null)
    {
        _xmlBuilder.AppendLine($"        <member name=\"T:{typeName}\">");
        _xmlBuilder.AppendLine($"            <summary>{summary}</summary>");
        _xmlBuilder.AppendLine("        </member>");

        if (properties != null)
        {
            foreach (var (propName, propDesc) in properties)
            {
                _xmlBuilder.AppendLine($"        <member name=\"P:{typeName}.{propName}\">");
                _xmlBuilder.AppendLine($"            <summary>{propDesc}</summary>");
                _xmlBuilder.AppendLine("        </member>");
            }
        }

        return this;
    }

    public TestScenarioBuilder WithExpectedEventMarkdown(string fileName, string content)
    {
        _expectedFiles.Add(new ExpectedFile
        {
            FileName = fileName,
            Content = content,
            IsSchemaFile = false
        });

        return this;
    }

    public TestScenarioBuilder WithExpectedSchema(string fileName, string content)
    {
        _expectedFiles.Add(new ExpectedFile
        {
            FileName = fileName,
            Content = content,
            IsSchemaFile = true
        });

        return this;
    }

    public TestScenarioBuilder WithConfig(TestScenarioConfig config)
    {
        _config = config;

        return this;
    }

    public TestScenarioBuilder WithConfig(
        bool strictFileMatching = true,
        bool generateSchemas = true,
        bool generateSidebar = true,
        List<string>? ignoreFiles = null,
        Dictionary<string, string>? customAssertions = null)
    {
        _config = new TestScenarioConfig
        {
            StrictFileMatching = strictFileMatching,
            GenerateSchemas = generateSchemas,
            GenerateSidebar = generateSidebar,
            IgnoreFiles = ignoreFiles ?? [],
            CustomAssertions = customAssertions ?? new Dictionary<string, string>()
        };

        return this;
    }

    public async Task<string> BuildAsync()
    {
        _xmlBuilder.AppendLine("    </members>");
        _xmlBuilder.AppendLine("</doc>");

        var scenarioPath = Path.Combine(_basePath, _scenarioName);
        Directory.CreateDirectory(scenarioPath);

        var expectedPath = Path.Combine(scenarioPath, "expected");
        Directory.CreateDirectory(expectedPath);

        // Write input XML
        var inputXmlPath = Path.Combine(scenarioPath, "input.xml");
        await File.WriteAllTextAsync(inputXmlPath, _xmlBuilder.ToString());

        // Write expected files
        foreach (var expectedFile in _expectedFiles)
        {
            var filePath = expectedFile.IsSchemaFile && !expectedFile.FileName.StartsWith("schemas/")
                ? Path.Combine(expectedPath, "schemas", expectedFile.FileName)
                : Path.Combine(expectedPath, expectedFile.FileName);

            Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
            await File.WriteAllTextAsync(filePath, expectedFile.Content);
        }

        // Write config
        var configPath = Path.Combine(scenarioPath, "config.json");
        var configJson = JsonSerializer.Serialize(_config, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(configPath, configJson);

        return scenarioPath;
    }

    private static string GetSystemType(string parameter)
    {
        return parameter.ToLowerInvariant() switch
        {
            "tenantid" or "orderid" or "customerid" or "productid" => "System.Guid",
            "email" or "name" or "ordernumber" or "transactionid" or "productname" => "System.String",
            "amount" or "unitprice" or "totalprice" or "totalamount" => "System.Decimal",
            "quantity" => "System.Int32",
            "registrationdate" or "completedat" => "System.DateTime",
            "customer" => "AppDomain.Orders.Contracts.Models.Customer",
            "items" => "System.Collections.Generic.List{AppDomain.Orders.Contracts.Models.OrderItem}",
            "app_domainaddress" => "AppDomain.Orders.Contracts.Models.Address",
            _ => "System.String"
        };
    }
}

/// <summary>
///     Factory methods for creating common test scenarios
/// </summary>
public static class TestScenarioFactory
{
    public static TestScenarioBuilder CreateBasicEvent(
        string scenarioName,
        string eventType,
        string summary,
        Dictionary<string, string> parameters)
    {
        var builder = new TestScenarioBuilder(scenarioName)
            .WithEvent(eventType, summary, parameters: parameters)
            .WithConfig(generateSchemas: false, generateSidebar: false);

        return builder;
    }

    public static TestScenarioBuilder CreateComplexEventWithSchemas(
        string scenarioName,
        string eventType,
        string summary,
        string remarks,
        Dictionary<string, string> parameters,
        List<(string typeName, string summary, Dictionary<string, string> properties)> schemas)
    {
        var builder = new TestScenarioBuilder(scenarioName)
            .WithEvent(eventType, summary, remarks, parameters);

        foreach (var (typeName, typeSummary, properties) in schemas)
        {
            builder.WithTypeDocumentation(typeName, typeSummary, properties);
        }

        return builder.WithConfig(generateSchemas: true, generateSidebar: true);
    }

    public static TestScenarioBuilder CreateObsoleteEvent(
        string scenarioName,
        string eventType,
        string summary,
        string migrationNotes,
        Dictionary<string, string> parameters)
    {
        var remarks = $@"
            ## Migration Notes

            {migrationNotes}";

        return new TestScenarioBuilder(scenarioName)
            .WithEvent(eventType, summary, remarks, parameters)
            .WithConfig(generateSchemas: false, generateSidebar: false);
    }

    public static TestScenarioBuilder CreateMultiplePartitionKeyEvent(
        string scenarioName,
        string eventType,
        string summary,
        Dictionary<string, string> parameters)
    {
        return new TestScenarioBuilder(scenarioName)
            .WithEvent(eventType, summary, parameters: parameters)
            .WithConfig(generateSchemas: false, generateSidebar: false,
                customAssertions: new Dictionary<string, string>
                {
                    [$"{GetFileNameFromEventType(eventType)}.md"] = "Should contain multiple partition keys"
                });
    }

    private static string GetFileNameFromEventType(string eventType)
    {
        var eventTypeParts = eventType.Split('.');
        var eventName = eventTypeParts[eventTypeParts.Length - 1];

        return string.Concat(eventName.Select((x, i) =>
            i > 0 && char.IsUpper(x) && char.IsLower(eventName[i - 1])
                ? "-" + char.ToLowerInvariant(x)
                : char.ToLowerInvariant(x).ToString()));
    }
}
