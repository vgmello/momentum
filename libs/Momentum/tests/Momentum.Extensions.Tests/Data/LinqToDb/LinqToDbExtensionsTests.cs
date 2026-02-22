// Copyright (c) Momentum .NET. All rights reserved.

using LinqToDB;
using LinqToDB.Mapping;
using Momentum.Extensions.Data.LinqToDb;

namespace Momentum.Extensions.Tests.Data.LinqToDb;

public class LinqToDbExtensionsTests
{
    [Fact]
    public void UseMappingSchema_ShouldInvokeConfigAction()
    {
        // Arrange
        var options = new DataOptions();
        var configCalled = false;

        // Act
        options.UseMappingSchema(_ =>
        {
            configCalled = true;
        });

        // Assert
        configCalled.ShouldBeTrue();
    }

    [Fact]
    public void UseMappingSchema_ShouldReturnDataOptions()
    {
        // Arrange
        var options = new DataOptions();

        // Act
        var result = options.UseMappingSchema(_ => { });

        // Assert
        result.ShouldNotBeNull();
        result.ShouldBeOfType<DataOptions>();
    }

    [Fact]
    public void UseMappingSchema_ShouldPassMappingSchemaToConfigAction()
    {
        // Arrange
        var options = new DataOptions();
        MappingSchema? capturedSchema = null;

        // Act
        options.UseMappingSchema(schema =>
        {
            capturedSchema = schema;
        });

        // Assert
        capturedSchema.ShouldNotBeNull();
        capturedSchema.ShouldBeOfType<MappingSchema>();
    }

    [Fact]
    public void UseMappingSchema_ShouldApplyConfigurationToSchema()
    {
        // Arrange
        var options = new DataOptions();

        // Act
        var result = options.UseMappingSchema(schema =>
        {
            schema.SetConverter<string, int>(s => int.Parse(s));
        });

        // Assert
        result.ShouldNotBeNull();
    }
}
