// Copyright (c) ABCDEG. All rights reserved.

namespace AppDomain.Tests.Architecture;

/// <summary>
/// Architecture tests for CQRS pattern compliance in the AppDomain domain.
/// </summary>
[PublicAPI]
public class CqrsPatternRulesTests : ArchitectureTestBase
{
    [Fact]
    public void Commands_ShouldEndWithCommand()
    {
        var result = DomainTypes()
            .That()
            .ResideInNamespace("AppDomain.*.Commands")
            .And()
            .AreClasses()
            .And()
            .ImplementInterface(typeof(Momentum.Extensions.Abstractions.Messaging.ICommand<>))
            .Should()
            .HaveNameEndingWith("Command")
            .GetResult();

        result.IsSuccessful.Should().BeTrue();
    }

    [Fact]
    public void Queries_ShouldEndWithQuery()
    {
        var result = DomainTypes()
            .That()
            .ResideInNamespace("AppDomain.*.Queries")
            .And()
            .AreClasses()
            .And()
            .ImplementInterface(typeof(Momentum.Extensions.Abstractions.Messaging.IQuery<>))
            .Should()
            .HaveNameEndingWith("Query")
            .GetResult();

        result.IsSuccessful.Should().BeTrue();
    }

    [Fact]
    public void CommandHandlers_ShouldEndWithCommandHandler()
    {
        var result = DomainTypes()
            .That()
            .ResideInNamespace("AppDomain.*.Commands")
            .And()
            .AreClasses()
            .And()
            .HaveNameEndingWith("Handler")
            .Should()
            .HaveNameEndingWith("CommandHandler")
            .GetResult();

        result.IsSuccessful.Should().BeTrue();
    }

    [Fact]
    public void QueryHandlers_ShouldEndWithQueryHandler()
    {
        var result = DomainTypes()
            .That()
            .ResideInNamespace("AppDomain.*.Queries")
            .And()
            .AreClasses()
            .And()
            .HaveNameEndingWith("Handler")
            .Should()
            .HaveNameEndingWith("QueryHandler")
            .GetResult();

        result.IsSuccessful.Should().BeTrue();
    }

    [Fact]
    public void IntegrationEvents_ShouldEndWithCorrectSuffix()
    {
        var result = DomainTypes()
            .That()
            .ResideInNamespace("AppDomain.*.Contracts.IntegrationEvents")
            .And()
            .AreClasses()
            .Should()
            .HaveNameEndingWith("Created")
            .Or()
            .HaveNameEndingWith("Updated")
            .Or()
            .HaveNameEndingWith("Deleted")
            .Or()
            .HaveNameEndingWith("Event")
            .GetResult();

        result.IsSuccessful.Should().BeTrue();
    }
}