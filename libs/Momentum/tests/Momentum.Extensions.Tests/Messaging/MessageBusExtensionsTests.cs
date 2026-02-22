// Copyright (c) Momentum .NET. All rights reserved.

using Momentum.Extensions.Abstractions.Messaging;
using Momentum.Extensions.Messaging;
using NSubstitute;
using Wolverine;

namespace Momentum.Extensions.Tests.Messaging;

public class MessageBusExtensionsTests
{
    private record TestCommandResult(string Value);

    private record TestCommand(string Input) : ICommand<TestCommandResult>;

    private record TestQueryResult(int Count);

    private record TestQuery(string Filter) : IQuery<TestQueryResult>;

    [Fact]
    public async Task InvokeCommandAsync_ShouldDelegateToMessageBus()
    {
        // Arrange
        var bus = Substitute.For<IMessageBus>();
        var command = new TestCommand("input");
        var expected = new TestCommandResult("result");
        var cancellationToken = TestContext.Current.CancellationToken;

        bus.InvokeAsync<TestCommandResult>(Arg.Any<object>(), Arg.Any<CancellationToken>())
            .Returns(expected);

        // Act
        var result = await bus.InvokeCommandAsync(command, cancellationToken);

        // Assert
        result.ShouldBe(expected);
        await bus.Received(1).InvokeAsync<TestCommandResult>(command, cancellationToken);
    }

    [Fact]
    public async Task InvokeQueryAsync_ShouldDelegateToMessageBus()
    {
        // Arrange
        var bus = Substitute.For<IMessageBus>();
        var query = new TestQuery("filter");
        var expected = new TestQueryResult(42);
        var cancellationToken = TestContext.Current.CancellationToken;

        bus.InvokeAsync<TestQueryResult>(Arg.Any<object>(), Arg.Any<CancellationToken>())
            .Returns(expected);

        // Act
        var result = await bus.InvokeQueryAsync(query, cancellationToken);

        // Assert
        result.ShouldBe(expected);
        await bus.Received(1).InvokeAsync<TestQueryResult>(query, cancellationToken);
    }

    [Fact]
    public async Task InvokeCommandAsync_WithCancellationToken_ShouldPassItThrough()
    {
        // Arrange
        var bus = Substitute.For<IMessageBus>();
        var command = new TestCommand("input");
        var expected = new TestCommandResult("cancelled");
        using var cts = new CancellationTokenSource();
        var token = cts.Token;

        bus.InvokeAsync<TestCommandResult>(Arg.Any<object>(), token)
            .Returns(expected);

        // Act
        var result = await bus.InvokeCommandAsync(command, token);

        // Assert
        result.ShouldBe(expected);
        await bus.Received(1).InvokeAsync<TestCommandResult>(command, token);
    }

    [Fact]
    public async Task InvokeQueryAsync_WithCancellationToken_ShouldPassItThrough()
    {
        // Arrange
        var bus = Substitute.For<IMessageBus>();
        var query = new TestQuery("filter");
        var expected = new TestQueryResult(99);
        using var cts = new CancellationTokenSource();
        var token = cts.Token;

        bus.InvokeAsync<TestQueryResult>(Arg.Any<object>(), token)
            .Returns(expected);

        // Act
        var result = await bus.InvokeQueryAsync(query, token);

        // Assert
        result.ShouldBe(expected);
        await bus.Received(1).InvokeAsync<TestQueryResult>(query, token);
    }
}
