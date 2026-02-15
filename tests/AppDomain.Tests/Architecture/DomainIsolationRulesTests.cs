// Copyright (c) OrgName. All rights reserved.

namespace AppDomain.Tests.Architecture;
public class DomainIsolationRulesTests : ArchitectureTestBase
{
    [Fact]
    public void Domains_ShouldNotDirectlyReferencEachOthersInternals()
    {
        var domainPrefixes = DomainDiscovery.GetAllDomains().ToList();

        // Ensure we discovered some domains
        domainPrefixes.ShouldNotBeEmpty("Should discover at least one domain with Commands, Queries, or Data");

        var violations = new List<string>();

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

            if (!result.IsSuccessful)
            {
                violations.Add(
                    $"Domain {domain} should not directly reference other domains' internals: {string.Join(", ", result.FailingTypeNames ?? [])}");
            }
        }

        violations.ShouldBeEmpty(
            $"Domains should maintain isolation from each other's internals:\n{string.Join("\n", violations)}");
    }

    [Fact]
    public void DiscoveredDomains_ShouldHaveExpectedStructure()
    {
        var domains = DomainDiscovery.GetAllDomains().ToList();

        Console.WriteLine($"Discovered domains: {string.Join(", ", domains)}");

        foreach (var domain in domains)
        {
            domain.ShouldMatch(@"^AppDomain\.\w+$",
                $"Domain {domain} should follow pattern 'AppDomain.DomainName'");
        }
    }
}
