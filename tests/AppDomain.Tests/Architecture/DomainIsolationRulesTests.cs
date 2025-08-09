<!--#if (includeSample)-->
// Copyright (c) ABCDEG. All rights reserved.

namespace AppDomain.Tests.Architecture;

#pragma warning disable CS8602

public class DomainIsolationRulesTests : ArchitectureTestBase
{
    [Fact]
    public void Domains_ShouldNotDirectlyReferencEachOthersInternals()
    {
        var domainPrefixes = new[] { "AppDomain.Cashiers", "AppDomain.Invoices" };

        foreach (var domain in domainPrefixes)
        {
            var otherDomains = domainPrefixes.Where(d => d != domain).ToArray();

            var result = GetAppDomainTypes()
                .That().ResideInNamespace(domain)
                .And().DoNotResideInNamespaceEndingWith(".Contracts") // Contracts can be shared
                .Should().NotHaveDependencyOnAny(otherDomains.SelectMany(d => new[]
                {
                    $"{d}.Commands",
                    $"{d}.Queries",
                    $"{d}.Data"
                }).ToArray())
                .GetResult();

            result.IsSuccessful.ShouldBeTrue(
                $"Domain {domain} should not directly reference other domains' internals: {string.Join(", ", result.FailingTypeNames ?? [])}");
        }
    }
}
<!--#endif-->
