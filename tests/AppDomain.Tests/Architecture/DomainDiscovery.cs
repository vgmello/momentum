// Copyright (c) ORG_NAME. All rights reserved.

using NetArchTest.Rules;
using Orleans;

namespace AppDomain.Tests.Architecture;

/// <summary>
///     Helper class for discovering domains and their components in the codebase.
///     Provides centralized logic for auto-discovering domains to make architecture tests generic.
/// </summary>
public static class DomainDiscovery
{
    /// <summary>
    ///     Gets all types from the AppDomain assemblies for architecture testing.
    /// </summary>
    private static Types GetAppDomainTypes() => Types
        .InAssemblies([typeof(IAppDomainAssembly).Assembly, typeof(Api.DependencyInjection).Assembly]);

    /// <summary>
    ///     Discovers all domain namespaces by looking for Commands, Queries, or Data folders.
    /// </summary>
    public static IEnumerable<string> GetAllDomains()
    {
        var allTypes = GetAppDomainTypes().GetTypes();

        return allTypes
            .Where(t => t.Namespace != null &&
                        t.Namespace.StartsWith("AppDomain.") &&
                        !t.Namespace.StartsWith("AppDomain.BackOffice") &&
                        !t.Namespace.StartsWith("AppDomain.Api") &&
                        !t.Namespace.StartsWith("AppDomain.Contracts") &&
                        !t.Namespace.StartsWith("AppDomain.Tests") &&
                        !t.Namespace.StartsWith("AppDomain.AppHost") &&
                        (t.Namespace.Contains(".Commands") ||
                         t.Namespace.Contains(".Queries") ||
                         t.Namespace.Contains(".Data") ||
                         t.Namespace.Contains(".Actors")))
            .Select(t =>
            {
                // Extract domain name (e.g., "AppDomain.Invoices.Commands" -> "AppDomain.Invoices")
                var parts = t.Namespace!.Split('.');

                return parts.Length >= 2 ? $"{parts[0]}.{parts[1]}" : null;
            })
            .Where(ns => ns != null)
            .Distinct()
            .ToList()!;
    }

    /// <summary>
    ///     Gets domain names without the AppDomain prefix (e.g., "Invoices", "Cashiers").
    /// </summary>
    public static IEnumerable<string> GetDomainNames()
    {
        return GetAllDomains()
            .Select(d => d.Replace("AppDomain.", ""))
            .Distinct();
    }

    /// <summary>
    ///     Discovers all domains that have Orleans grains by looking for *.Actors namespaces.
    /// </summary>
    public static IEnumerable<string> GetDomainsWithGrains()
    {
        return GetAppDomainTypes()
            .GetTypes()
            .Where(t => t.Namespace?.Contains(".Actors") == true &&
                        t.Namespace.StartsWith("AppDomain.") &&
                        !t.Namespace.StartsWith("AppDomain.BackOffice.Orleans") &&
                        t.GetInterfaces().Any(i => typeof(IGrain).IsAssignableFrom(i)))
            .Select(t => t.Namespace!)
            .Distinct()
            .ToList();
    }

    /// <summary>
    ///     Gets all domain actor namespaces (e.g., "AppDomain.Invoices.Actors").
    /// </summary>
    public static IEnumerable<string> GetDomainActorNamespaces()
    {
        return GetDomainsWithGrains();
    }

    /// <summary>
    ///     Extracts domain name from a namespace (e.g., "AppDomain.Invoices.Actors" -> "Invoices").
    /// </summary>
    public static string ExtractDomainName(string namespaceName)
    {
        var parts = namespaceName.Split('.');

        if (parts.Length >= 2 && parts[0] == "AppDomain")
        {
            return parts[1];
        }

        throw new ArgumentException($"Invalid namespace format: {namespaceName}");
    }

    /// <summary>
    ///     Checks if a type belongs to a domain (not infrastructure or API).
    /// </summary>
    public static bool IsDomainType(Type type)
    {
        return type.Namespace != null &&
               type.Namespace.StartsWith("AppDomain.") &&
               !type.Namespace.StartsWith("AppDomain.BackOffice") &&
               !type.Namespace.StartsWith("AppDomain.Api") &&
               !type.Namespace.StartsWith("AppDomain.Contracts") &&
               !type.Namespace.StartsWith("AppDomain.Tests") &&
               !type.Namespace.StartsWith("AppDomain.AppHost");
    }

    /// <summary>
    ///     Gets all command types across all domains.
    /// </summary>
    public static IEnumerable<Type> GetAllCommandTypes()
    {
        return GetAppDomainTypes()
            .That().ResideInNamespaceEndingWith(".Commands")
            .And().AreClasses()
            .And().HaveNameEndingWith("Command")
            .GetTypes();
    }

    /// <summary>
    ///     Gets all query types across all domains.
    /// </summary>
    public static IEnumerable<Type> GetAllQueryTypes()
    {
        return GetAppDomainTypes()
            .That().ResideInNamespaceEndingWith(".Queries")
            .And().AreClasses()
            .And().HaveNameEndingWith("Query")
            .GetTypes();
    }

    /// <summary>
    ///     Gets all integration event types.
    /// </summary>
    public static IEnumerable<Type> GetAllIntegrationEventTypes()
    {
        return GetAppDomainTypes()
            .That().ResideInNamespaceEndingWith(".IntegrationEvents")
            .And().AreClasses()
            .GetTypes();
    }

    /// <summary>
    ///     Gets all domain event types.
    /// </summary>
    public static IEnumerable<Type> GetAllDomainEventTypes()
    {
        return GetAppDomainTypes()
            .That().ResideInNamespaceEndingWith(".DomainEvents")
            .And().AreClasses()
            .GetTypes();
    }
}
