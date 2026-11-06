// Copyright (c) Momentum .NET. All rights reserved.

using Momentum.Extensions.EventMarkdownGenerator.Services;
using Shouldly;
using Xunit;

namespace Momentum.Extensions.EventMarkdownGenerator.Tests;

/// <summary>
///     Built-in scalar types (DateOnly, TimeOnly, Uri, Half, Int128, UInt128 and their nullable forms) must render
///     as plain properties: not complex, no schema page, no Reference Schemas entry.
/// </summary>
public class ScalarTypeSchemaTests
{
    private readonly PayloadSizeCalculator _calculator = PayloadSizeCalculator.Create("json");

    [Theory]
    [InlineData(typeof(DateOnly))]
    [InlineData(typeof(DateOnly?))]
    [InlineData(typeof(TimeOnly))]
    [InlineData(typeof(TimeOnly?))]
    [InlineData(typeof(Uri))]
    [InlineData(typeof(Half))]
    [InlineData(typeof(Half?))]
    [InlineData(typeof(Int128))]
    [InlineData(typeof(Int128?))]
    [InlineData(typeof(UInt128))]
    [InlineData(typeof(UInt128?))]
    public void ScalarType_ShouldBePrimitiveAndNotComplex(Type type)
    {
        TypeUtils.IsPrimitiveType(type).ShouldBeTrue();
        TypeUtils.IsComplexType(type).ShouldBeFalse();
    }

    [Fact]
    public void EventWithScalarProperties_ShouldHaveNoComplexPropertiesOrSchemas()
    {
        var (properties, _) = EventPropertyMetadataBuilder.Build(typeof(ScalarPropertiesEvent), null, _calculator, "PartitionKey");

        // Collections are always flagged complex by the builder; their element type decides whether a schema is emitted
        properties.Where(p => !TypeUtils.IsCollectionType(p.PropertyType)).ShouldAllBe(p => !p.IsComplexType);
        TypeUtils.CollectComplexTypesFromProperties(properties).ShouldBeEmpty();
    }

    [Fact]
    public void NestedTypeWithScalarProperties_ShouldNotCollectSystemSchemas()
    {
        var (properties, _) = EventPropertyMetadataBuilder.Build(typeof(NestedScalarEvent), null, _calculator, "PartitionKey");

        var schemaTypes = TypeUtils.CollectComplexTypesFromProperties(properties);
        foreach (var type in schemaTypes.ToList())
        {
            TypeUtils.CollectNestedComplexTypes(type, schemaTypes);
        }

        schemaTypes.ShouldHaveSingleItem().ShouldBe(typeof(ScalarHolder));
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Major Code Smell", "S1144:Unused private types or members should be removed")]
    private record ScalarPropertiesEvent(
        DateOnly Date,
        DateOnly? OptionalDate,
        TimeOnly Time,
        TimeOnly? OptionalTime,
        Uri Link,
        Uri? OptionalLink,
        Half Ratio,
        Half? OptionalRatio,
        Int128 Big,
        Int128? OptionalBig,
        UInt128 BigUnsigned,
        UInt128? OptionalBigUnsigned,
        List<DateOnly> Dates,
        TimeOnly[] Times);

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Major Code Smell", "S1144:Unused private types or members should be removed")]
    private record NestedScalarEvent(ScalarHolder Holder);

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Major Code Smell", "S1144:Unused private types or members should be removed")]
    private record ScalarHolder(DateOnly? ClosedLocalDate, TimeOnly OpensAt, List<DateOnly> Holidays);
}
