// Copyright (c) Momentum .NET. All rights reserved.

using Momentum.Extensions.EventMarkdownGenerator.Services;
using Shouldly;
using System.Reflection;
using Xunit;

namespace Momentum.Extensions.EventMarkdownGenerator.Tests;

public class XmlParsingDebugTests
{
    [Fact(Skip = "Debug test - only run when debugging XML parsing issues")]
    public async Task DebugXmlParsing_ShouldShowWhatIsParsed()
    {
        // Arrange
        var parser = new XmlDocumentationParser();
        var xmlPath = "../TestEvents/bin/Debug/net10.0/TestEvents.xml";
        var assemblyPath = "../TestEvents/bin/Debug/net10.0/TestEvents.dll";

        // Act
        var result = await parser.LoadMultipleDocumentationAsync([xmlPath]);
        result.ShouldBeTrue("XML file should load successfully");

        var assembly = Assembly.LoadFrom(assemblyPath);
        var cashierCreatedType = assembly.GetType("AppDomain.Cashiers.Contracts.IntegrationEvents.CashierCreated");
        cashierCreatedType.ShouldNotBeNull();

        var documentation = parser.GetEventDocumentation(cashierCreatedType!);

        // Debug output - let's see what we actually get
        Console.WriteLine($"Summary: {documentation.Summary}");
        Console.WriteLine($"Remarks: {documentation.Remarks}");
        Console.WriteLine($"Property descriptions count: {documentation.PropertyDescriptions.Count}");

        foreach (var prop in documentation.PropertyDescriptions)
        {
            Console.WriteLine($"  {prop.Key}: {prop.Value}");
        }

        Console.WriteLine($"Full Remarks:\n{documentation.Remarks}");

        // Basic assertions
        documentation.Summary.ShouldNotBeNullOrEmpty();
        documentation.Summary.ShouldContain("Published when a new cashier is successfully created");

        // This will help us see why property descriptions aren't working
        var properties = cashierCreatedType.GetProperties();
        Console.WriteLine($"\nType properties ({properties.Length}):");

        foreach (var prop in properties)
        {
            Console.WriteLine($"  {prop.Name} - {prop.PropertyType.Name}");
        }

        // Check what's in the property descriptions
        documentation.PropertyDescriptions.ShouldContainKey("TenantId");
        documentation.PropertyDescriptions.ShouldContainKey("PartitionKeyTest");
        documentation.PropertyDescriptions.ShouldContainKey("Cashier");

        // The main issue we're investigating
        foreach (var expectedProp in new[] { "TenantId", "PartitionKeyTest", "Cashier" })
        {
            if (documentation.PropertyDescriptions.ContainsKey(expectedProp))
            {
                var desc = documentation.PropertyDescriptions[expectedProp];
                Console.WriteLine($"Property {expectedProp}: '{desc}'");

                // This should NOT be "No description available" if XML parsing works
                if (desc == "No description available")
                {
                    Console.WriteLine($"  ❌ Property {expectedProp} has no description - XML parsing failed");
                }
                else
                {
                    Console.WriteLine($"  ✅ Property {expectedProp} has description - XML parsing worked");
                }
            }
        }
    }

    [Fact(Skip = "Debug test - only run when debugging XML content issues")]
    public void ValidateXmlFileContent_ShouldVerifyXmlStructure()
    {
        // Let's verify the XML file contains what we expect
        var xmlPath = "/home/vgmello/shared/repos/momentum/libs/Momentum/tests/TestEvents/bin/Debug/net10.0/TestEvents.xml";
        File.Exists(xmlPath).ShouldBeTrue("XML documentation file should exist");

        var xmlContent = File.ReadAllText(xmlPath);
        Console.WriteLine("XML Content preview:");
        Console.WriteLine(xmlContent.Substring(0, Math.Min(1000, xmlContent.Length)));

        // Verify expected property documentation exists
        xmlContent.ShouldContain("P:AppDomain.Cashiers.Contracts.IntegrationEvents.CashierCreated.TenantId");
        xmlContent.ShouldContain("P:AppDomain.Cashiers.Contracts.IntegrationEvents.CashierCreated.PartitionKeyTest");
        xmlContent.ShouldContain("P:AppDomain.Cashiers.Contracts.IntegrationEvents.CashierCreated.Cashier");

        // Verify parameter documentation exists
        xmlContent.ShouldContain("M:AppDomain.Cashiers.Contracts.IntegrationEvents.CashierCreated.#ctor");
        xmlContent.ShouldContain("param name=\"TenantId\"");
        xmlContent.ShouldContain("param name=\"PartitionKeyTest\"");
        xmlContent.ShouldContain("param name=\"Cashier\"");

        // Verify descriptions
        xmlContent.ShouldContain("Identifier of the tenant that owns the cashier");
        xmlContent.ShouldContain("Additional partition key for message routing");
        xmlContent.ShouldContain("Complete cashier object containing all cashier data and configuration");
    }
}
