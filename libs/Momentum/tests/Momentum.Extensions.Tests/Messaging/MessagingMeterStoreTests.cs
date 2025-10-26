// Copyright (c) Momentum .NET. All rights reserved.

using System.Diagnostics.Metrics;
using Momentum.ServiceDefaults.Messaging.Telemetry;

namespace Momentum.Extensions.Tests.Messaging;

public class MessagingMeterStoreTests
{
    private readonly MessagingMeterStore _store = new(new Meter("TestMeter"));

    [Fact]
    public void GetOrCreateMetrics_WithSameMessageType_ShouldReturnSameInstance()
    {
        // Arrange
        const string messageType = "TestMessage";

        // Act
        var metrics1 = _store.GetOrCreateMetrics(messageType);
        var metrics2 = _store.GetOrCreateMetrics(messageType);

        // Assert
        metrics1.ShouldBeSameAs(metrics2);
    }

    [Fact]
    public void GetOrCreateMetrics_WithDifferentMessageTypes_ShouldReturnDifferentInstances()
    {
        // Arrange
        const string messageType1 = "TestMessage1";
        const string messageType2 = "TestMessage2";

        // Act
        var metrics1 = _store.GetOrCreateMetrics(messageType1);
        var metrics2 = _store.GetOrCreateMetrics(messageType2);

        // Assert
        metrics1.ShouldNotBeSameAs(metrics2);
    }

    [Fact]
    public void MessagingMeterKey_ShouldHaveCorrectValue()
    {
        // Assert
        MessagingMeterStore.MessagingMeterKey.ShouldBe("App.Messaging.Meter");
    }

    [Theory]
    [InlineData("test_command_handler", "test")]
    [InlineData("test_query_handler", "test")]
    [InlineData("prefix_query_handler_suffix", "prefix_suffix")]
    [InlineData("prefix_command_handler_suffix", "prefix_suffix")]
    [InlineData("test_query_handler_db_query", "test_db")]
    [InlineData("test_command_handler_db_command", "test_db")]
    [InlineData("some_query_handler_with_command", "some_with")]
    [InlineData("some_command_handler_with_query", "some_with")]
    [InlineData("My.Commands.CreateUserCommand", "my.commands.create_user")]
    [InlineData("My.Queries.GetUserQuery", "my.queries.get_user")]
    [InlineData("My.Handlers.CreateUserCommandHandler", "my.handlers.create_user")]
    [InlineData("My.Handlers.GetUserQueryHandler", "my.handlers.get_user")]
    [InlineData("My.Handlers.GetUserQueryHandler_DbQuery", "my.handlers.get_user_db")]
    [InlineData("test_command", "test")]
    [InlineData("test_query", "test")]
    [InlineData("no_special_suffix", "no_special_suffix")]
    [InlineData("", "")]
    public void GetOrCreateMetrics_WithHandlerPatterns_ShouldRemoveHandlerPatterns(string input, string expectedMetric)
    {
        // Act
        var metric = _store.GetOrCreateMetrics(input);

        metric.ShouldNotBeNull();
        metric.MetricName.ShouldBe(expectedMetric);
    }
}
