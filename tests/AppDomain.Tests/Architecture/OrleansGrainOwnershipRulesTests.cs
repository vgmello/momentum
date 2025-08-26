// Copyright (c) ORG_NAME. All rights reserved.

namespace AppDomain.Tests.Architecture;

#pragma warning disable CS8602

/// <summary>
///     Architecture tests to enforce grain ownership boundaries in Orleans.
///     Only the domain that owns a grain should be able to call it directly.
///     These tests automatically discover domains and their grains.
/// </summary>
public class OrleansGrainOwnershipRulesTests : ArchitectureTestBase
{
    [Fact]
    public void DomainsWithGrains_ShouldNotCallOtherDomainsGrains()
    {
        var domainActorNamespaces = DomainDiscovery.GetDomainActorNamespaces().ToList();
        var violations = new List<string>();

        foreach (var actorNamespace in domainActorNamespaces)
        {
            var domainName = DomainDiscovery.ExtractDomainName(actorNamespace);
            var domainNamespace = $"AppDomain.{domainName}";
            var otherActorNamespaces = domainActorNamespaces.Where(ns => ns != actorNamespace).ToArray();

            foreach (var otherActorNamespace in otherActorNamespaces)
            {
                var result = GetAppDomainTypes()
                    .That().ResideInNamespace(domainNamespace)
                    .Should().NotHaveDependencyOn(otherActorNamespace)
                    .GetResult();

                if (!result.IsSuccessful)
                {
                    var otherDomainName = DomainDiscovery.ExtractDomainName(otherActorNamespace);
                    violations.Add(
                        $"{domainName} domain should not call {otherDomainName} grains: {string.Join(", ", result.FailingTypeNames ?? [])}");
                }
            }
        }

        violations.ShouldBeEmpty(
            $"Domains should not directly call other domains' grains. Use integration events instead:\n{string.Join("\n", violations)}");
    }

    [Fact]
    public void GrainInterfaces_ShouldOnlyBeAccessedByOwningDomain()
    {
        var domainActorNamespaces = DomainDiscovery.GetDomainActorNamespaces().ToList();
        var violations = new List<string>();

        foreach (var actorNamespace in domainActorNamespaces)
        {
            var domainName = DomainDiscovery.ExtractDomainName(actorNamespace);
            var allowedNamespaces = new[]
            {
                $"AppDomain.{domainName}",
                $"AppDomain.BackOffice.Orleans.{domainName}"
            };

            var grainInterfaceTypes = GetAppDomainTypes()
                .That().ResideInNamespace(actorNamespace)
                .And().ImplementInterface(typeof(IGrain))
                .GetTypes();

            foreach (var grainInterface in grainInterfaceTypes)
            {
                var result = GetAppDomainTypes()
                    .That().HaveDependencyOn(grainInterface.FullName!)
                    .And().DoNotResideInNamespace("OrleansCodeGen") // Exclude Orleans generated code
                    .Should().ResideInNamespaceMatching(string.Join("|", allowedNamespaces.Select(ns => $"^{ns.Replace(".", "\\.")}")))
                    .GetResult();

                if (!result.IsSuccessful)
                {
                    violations.Add(
                        $"Grain interface {grainInterface.Name} in {domainName} domain should only be accessed by its owning domain ({string.Join(", ", allowedNamespaces)}): {string.Join(", ", result.FailingTypeNames ?? [])}");
                }
            }
        }

        violations.ShouldBeEmpty(
            $"Grain interfaces should only be accessed by their owning domains:\n{string.Join("\n", violations)}");
    }

    [Fact]
    public void OnlyOwningDomain_ShouldCallItsGrains()
    {
        var domainActorNamespaces = DomainDiscovery.GetDomainActorNamespaces().ToList();
        var violations = new List<string>();

        foreach (var actorNamespace in domainActorNamespaces)
        {
            var domainName = DomainDiscovery.ExtractDomainName(actorNamespace);
            var allowedNamespaces = new[]
            {
                $"AppDomain.{domainName}",
                $"AppDomain.BackOffice.Orleans.{domainName}"
            };

            var result = GetAppDomainTypes()
                .That().HaveDependencyOn(actorNamespace)
                .And().DoNotResideInNamespace("OrleansCodeGen") // Exclude Orleans generated code
                .Should().ResideInNamespaceMatching(string.Join("|", allowedNamespaces.Select(ns => $"^{ns.Replace(".", "\\.")}")))
                .GetResult();

            if (!result.IsSuccessful)
            {
                violations.Add(
                    $"Only {domainName} domain and its Orleans implementation should call {domainName} grains: {string.Join(", ", result.FailingTypeNames ?? [])}");
            }
        }

        violations.ShouldBeEmpty(
            $"Only owning domains should call their grains:\n{string.Join("\n", violations)}");
    }

    [Fact]
    public void CrossDomainCommunication_ShouldUseIntegrationEvents()
    {
        var domainNames = DomainDiscovery.GetDomainNames().ToList();
        var violations = new List<string>();

        foreach (var domainName in domainNames)
        {
            var domainNamespace = $"AppDomain.{domainName}";
            var otherDomainsActorNamespaces = domainNames
                .Where(d => d != domainName)
                .Select(d => $"AppDomain.{d}.Actors")
                .ToArray();

            foreach (var otherActorNamespace in otherDomainsActorNamespaces)
            {
                var result = GetAppDomainTypes()
                    .That().ResideInNamespace(domainNamespace)
                    .Should().NotHaveDependencyOn(otherActorNamespace)
                    .GetResult();

                if (!result.IsSuccessful)
                {
                    violations.AddRange(result.FailingTypeNames ?? []);
                }
            }
        }

        violations.ShouldBeEmpty(
            $"Cross-domain communication should use integration events, not direct grain calls. Violations: {string.Join(", ", violations)}");
    }

    [Fact]
    public void DiscoveredDomains_ShouldFollowExpectedConventions()
    {
        var domainActorNamespaces = DomainDiscovery.GetDomainActorNamespaces().ToList();
        var domainNames = DomainDiscovery.GetDomainNames().ToList();

        // Ensure we found some domains
        domainNames.ShouldNotBeEmpty("Should discover at least one domain with grains");

        // Ensure all discovered actor namespaces follow the expected pattern
        foreach (var actorNamespace in domainActorNamespaces)
        {
            actorNamespace.ShouldMatch(@"^AppDomain\.\w+\.Actors$",
                $"Actor namespace {actorNamespace} should follow pattern 'AppDomain.DomainName.Actors'");

            // Ensure the corresponding domain namespace exists
            var domainName = DomainDiscovery.ExtractDomainName(actorNamespace);
            var domainNamespace = $"AppDomain.{domainName}";

            var domainTypes = GetAppDomainTypes()
                .That().ResideInNamespace(domainNamespace)
                .GetTypes();

            domainTypes.ShouldNotBeEmpty($"Domain namespace {domainNamespace} should contain types");
        }

        // Log discovered domains for verification
        Console.WriteLine($"Discovered domains with grains: {string.Join(", ", domainNames)}");
    }
}
