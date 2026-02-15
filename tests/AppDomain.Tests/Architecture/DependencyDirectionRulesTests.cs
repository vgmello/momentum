// Copyright (c) OrgName. All rights reserved.

namespace AppDomain.Tests.Architecture;
public class DependencyDirectionRulesTests : ArchitectureTestBase
{
    [Fact]
    public void ApiLayer_ShouldNotDependOnBackOffice()
    {
        var result = GetAppDomainTypes()
            .That().ResideInNamespace("AppDomain.Api")
            .Should().NotHaveDependencyOn("AppDomain.BackOffice")
            .GetResult();

        result.IsSuccessful.ShouldBeTrue(
            $"API layer should not depend on BackOffice: {string.Join(", ", result.FailingTypeNames ?? [])}");
    }

    [Fact]
    public void CoreDomain_ShouldNotDependOnApiOrBackOffice()
    {
        var result = GetAppDomainTypes()
            .That().ResideInNamespace("AppDomain")
            .And().DoNotResideInNamespace("AppDomain.Api")
            .And().DoNotResideInNamespace("AppDomain.BackOffice")
            .Should().NotHaveDependencyOnAny("AppDomain.Api", "AppDomain.BackOffice")
            .GetResult();

        result.IsSuccessful.ShouldBeTrue(
            $"Core domain should not depend on API or BackOffice layers: {string.Join(", ", result.FailingTypeNames ?? [])}");
    }

    [Fact]
    public void Contracts_ShouldNotDependOnImplementation()
    {
        var result = GetAppDomainTypes()
            .That().ResideInNamespaceEndingWith(".Contracts")
            .Should().NotHaveDependencyOnAny("AppDomain.Api", "AppDomain.BackOffice")
            .And().NotHaveDependencyOn("LinqToDB") // Contracts shouldn't depend on ORM
            .GetResult();

        result.IsSuccessful.ShouldBeTrue(
            $"Contracts should not depend on implementation details: {string.Join(", ", result.FailingTypeNames ?? [])}");
    }
}
