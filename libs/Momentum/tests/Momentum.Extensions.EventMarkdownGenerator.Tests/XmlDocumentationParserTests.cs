// Copyright (c) Momentum .NET. All rights reserved.

using Momentum.Extensions.EventMarkdownGenerator.Services;
using Shouldly;
using System.Reflection;
using Xunit;

namespace Momentum.Extensions.EventMarkdownGenerator.Tests;

public class XmlDocumentationParserTests
{
    [Fact]
    public async Task LoadDocumentationAsync_ShouldParseParameterDocumentation()
    {
        // Arrange
        var parser = new XmlDocumentationParser();
        var xmlPath = "/home/vgmello/shared/repos/momentum-sample/libs/Momentum/tests/TestEvents/bin/Debug/net9.0/TestEvents.xml";

        // Act
        var result = await parser.LoadMultipleDocumentationAsync([xmlPath]);

        // Assert
        result.ShouldBeTrue();

        // Load the actual CashierCreated type
        var assembly =
            Assembly.LoadFrom(
                "/home/vgmello/shared/repos/momentum-sample/libs/Momentum/tests/TestEvents/bin/Debug/net9.0/TestEvents.dll");
        var cashierCreatedType = assembly.GetType("AppDomain.Cashiers.Contracts.IntegrationEvents.CashierCreated");
        cashierCreatedType.ShouldNotBeNull();

        // Get documentation for the event
        var documentation = parser.GetEventDocumentation(cashierCreatedType!);

        // Verify summary is parsed
        documentation.Summary.ShouldContain("Published when a new cashier is successfully created");

        // Verify parameter descriptions are parsed
        documentation.PropertyDescriptions.ShouldContainKey("TenantId");
        documentation.PropertyDescriptions["TenantId"].ShouldBe("Identifier of the tenant that owns the cashier");

        documentation.PropertyDescriptions.ShouldContainKey("PartitionKeyTest");
        documentation.PropertyDescriptions["PartitionKeyTest"].ShouldBe("Additional partition key for message routing");

        documentation.PropertyDescriptions.ShouldContainKey("Cashier");
        documentation.PropertyDescriptions["Cashier"].ShouldBe("Complete cashier object containing all cashier data and configuration");
    }
}
