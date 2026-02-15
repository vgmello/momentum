// Copyright (c) OrgName. All rights reserved.

namespace AppDomain.Tests.Architecture;
public class IntegrationEventsRulesTests : ArchitectureTestBase
{
    [Fact]
    public void IntegrationEvents_ShouldFollowNamingConvention()
    {
        var result = GetAppDomainTypes()
            .That().ResideInNamespaceEndingWith(".IntegrationEvents")
            .And().AreClasses()
            .Should().HaveNameEndingWith("ed") // Created, Updated, Deleted, etc.
            .Or().HaveNameEndingWith("Event")
            .Or().HaveNameEndingWith("Paid") // Allow Paid as valid ending
            .GetResult();

        result.IsSuccessful.ShouldBeTrue(
            $"Integration events should follow naming convention (past tense or end with 'Event'): {string.Join(", ", result.FailingTypeNames ?? [])}");
    }

    [Fact]
    public void IntegrationEvents_ShouldBeInContractsNamespace()
    {
        var result = GetAppDomainTypes()
            .That().HaveNameEndingWith("Created")
            .Or().HaveNameEndingWith("Updated")
            .Or().HaveNameEndingWith("Deleted")
            .Or().HaveNameEndingWith("Event")
            .Should().ResideInNamespaceEndingWith(".Contracts.IntegrationEvents")
            .Or().ResideInNamespaceEndingWith(".Contracts.DomainEvents")
            .GetResult();

        result.IsSuccessful.ShouldBeTrue(
            $"Integration events should be in Contracts namespace: {string.Join(", ", result.FailingTypeNames ?? [])}");
    }
}
