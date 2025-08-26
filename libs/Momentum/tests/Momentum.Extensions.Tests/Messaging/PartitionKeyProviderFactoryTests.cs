// Copyright (c) Momentum .NET. All rights reserved.

using Momentum.Extensions.Abstractions.Messaging;
using Momentum.Extensions.Messaging.Kafka;

namespace Momentum.Extensions.Tests.Messaging;

public class PartitionKeyProviderFactoryTests
{
    [Fact]
    public void GetPartitionKeyFunction_WhenNoPartitionKeyAttributes_ReturnsNull()
    {
        // Act
        var func = PartitionKeyProviderFactory.GetPartitionKeyFunction<NoPartitionKeyEvent>();

        // Assert
        func.ShouldBeNull();
    }

    [Fact]
    public void GetPartitionKeyFunction_WithSinglePartitionKey_ReturnsCorrectKey()
    {
        // Arrange
        var eventObj = new SinglePartitionKeyEvent("test-id", "other-data");

        // Act
        var func = PartitionKeyProviderFactory.GetPartitionKeyFunction<SinglePartitionKeyEvent>();
        var result = func!(eventObj);

        // Assert
        result.ShouldBe("test-id");
    }

    [Fact]
    public void GetPartitionKeyFunction_WithMultiplePartitionKeys_ConcatenatesWithPipeDelimiter()
    {
        // Arrange
        var eventObj = new MultiplePartitionKeyEvent("key1", "key2", "non-partition-data");

        // Act
        var func = PartitionKeyProviderFactory.GetPartitionKeyFunction<MultiplePartitionKeyEvent>();
        var result = func!(eventObj);

        // Assert
        result.ShouldBe("key1|key2");
    }

    [Fact]
    public void GetPartitionKeyFunction_WithOrderedPartitionKeys_RespectsOrder()
    {
        // Arrange
        var eventObj = new OrderedPartitionKeyEvent("second", "first", "third");

        // Act
        var func = PartitionKeyProviderFactory.GetPartitionKeyFunction<OrderedPartitionKeyEvent>();
        var result = func!(eventObj);

        // Assert
        result.ShouldBe("first|second|third");
    }

    [Fact]
    public void GetPartitionKeyFunction_WithNullValues_HandlesNullCorrectly()
    {
        // Arrange
        var eventObj = new NullablePartitionKeyEvent(null, "key2");

        // Act
        var func = PartitionKeyProviderFactory.GetPartitionKeyFunction<NullablePartitionKeyEvent>();
        var result = func!(eventObj);

        // Assert
        result.ShouldBe("|key2");
    }

    [Fact]
    public void GetPartitionKeyFunction_WithDifferentPropertyTypes_ConvertsToString()
    {
        // Arrange
        var eventObj = new DifferentTypesPartitionKeyEvent(Guid.Parse("123e4567-e89b-12d3-a456-426614174000"), 42);

        // Act
        var func = PartitionKeyProviderFactory.GetPartitionKeyFunction<DifferentTypesPartitionKeyEvent>();
        var result = func!(eventObj);

        // Assert
        result.ShouldBe("123e4567-e89b-12d3-a456-426614174000|42");
    }

    [Fact]
    public void GetPartitionKeyFunction_WithDelimiterInValue_HandlesCorrectly()
    {
        // Arrange
        var eventObj = new DelimiterInValueEvent("key|with|pipes", "normal-key");

        // Act
        var func = PartitionKeyProviderFactory.GetPartitionKeyFunction<DelimiterInValueEvent>();
        var result = func!(eventObj);

        // Assert
        result.ShouldBe("key|with|pipes|normal-key");
    }

    // Test event types
    public record NoPartitionKeyEvent(string Id, string Data);

    public record SinglePartitionKeyEvent([PartitionKey] string Id, string Data);

    public record MultiplePartitionKeyEvent(
        [PartitionKey] string Key1,
        [PartitionKey] string Key2,
        string Data);

    public record OrderedPartitionKeyEvent(
        [PartitionKey(Order = 1)] string Second,
        [PartitionKey(Order = 0)] string First,
        [PartitionKey(Order = 2)] string Third);

    public record NullablePartitionKeyEvent(
        [PartitionKey] string? Key1,
        [PartitionKey] string Key2);

    public record DifferentTypesPartitionKeyEvent(
        [PartitionKey] Guid Id,
        [PartitionKey] int Number);

    public record DelimiterInValueEvent(
        [PartitionKey] string KeyWithDelimiter,
        [PartitionKey] string NormalKey);
}
