// Copyright (c) Momentum .NET. All rights reserved.

using FluentValidation;
using Momentum.ServiceDefaults.Messaging.Middlewares;

namespace Momentum.Extensions.Tests.ServiceDefaults;

public class FluentValidationExecutorTests
{
    private record TestMessage(string Name, int Age);

    private class TestValidator : AbstractValidator<TestMessage>
    {
        public TestValidator()
        {
            RuleFor(x => x.Name).NotEmpty();
            RuleFor(x => x.Age).GreaterThan(0);
        }
    }

    private class AlwaysPassValidator : AbstractValidator<TestMessage>;

    // --- ExecuteOne ---

    [Fact]
    public async Task ExecuteOne_WithValidMessage_ShouldReturnEmptyList()
    {
        var validator = new TestValidator();
        var message = new TestMessage("Alice", 30);

        var failures = await FluentValidationExecutor.ExecuteOne(validator, message);

        failures.ShouldBeEmpty();
    }

    [Fact]
    public async Task ExecuteOne_WithInvalidMessage_ShouldReturnFailures()
    {
        var validator = new TestValidator();
        var message = new TestMessage("", -1);

        var failures = await FluentValidationExecutor.ExecuteOne(validator, message);

        failures.ShouldNotBeEmpty();
        failures.Count.ShouldBe(2);
    }

    // --- ExecuteMany ---

    [Fact]
    public async Task ExecuteMany_WithValidMessage_ShouldReturnEmptyList()
    {
        var validators = new IValidator<TestMessage>[] { new TestValidator() };
        var message = new TestMessage("Alice", 30);

        var failures = await FluentValidationExecutor.ExecuteMany(validators, message);

        failures.ShouldBeEmpty();
    }

    [Fact]
    public async Task ExecuteMany_WithInvalidMessage_ShouldReturnAggregatedFailures()
    {
        var validators = new IValidator<TestMessage>[] { new TestValidator(), new TestValidator() };
        var message = new TestMessage("", -1);

        var failures = await FluentValidationExecutor.ExecuteMany(validators, message);

        failures.Count.ShouldBe(4);
    }

    [Fact]
    public async Task ExecuteMany_WithEmptyValidators_ShouldReturnEmptyList()
    {
        var validators = Array.Empty<IValidator<TestMessage>>();
        var message = new TestMessage("Alice", 30);

        var failures = await FluentValidationExecutor.ExecuteMany(validators, message);

        failures.ShouldBeEmpty();
    }

    [Fact]
    public async Task ExecuteMany_WithMixedResults_ShouldReturnOnlyFailures()
    {
        var validators = new IValidator<TestMessage>[] { new AlwaysPassValidator(), new TestValidator() };
        var message = new TestMessage("", -1);

        var failures = await FluentValidationExecutor.ExecuteMany(validators, message);

        failures.Count.ShouldBe(2);
    }
}
