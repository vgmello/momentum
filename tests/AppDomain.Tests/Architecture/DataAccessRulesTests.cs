// Copyright (c) OrgName. All rights reserved.

using AppDomain.Core.Data;
using NetArchTest.Rules;

namespace AppDomain.Tests.Architecture;

#pragma warning disable CS8602

public class DataAccessRulesTests : ArchitectureTestBase
{
    [Fact]
    public void DataClasses_ShouldOnlyBeUsedByDomainClasses_ExceptCoreDataContext()
    {
        var assemblies = new[]
        {
            typeof(IAppDomainAssembly).Assembly
#if INCLUDE_API
            , typeof(Api.DependencyInjection).Assembly
#endif
        };

        var dataNamespaces = assemblies
            .SelectMany(a => a.GetTypes())
            .Select(t => t.Namespace)
            .Where(ns => ns is not null && (ns.Contains(".Data.") || ns.EndsWith(".Data")))
            .Select(ns => ns![..ns.IndexOf(".Data", StringComparison.Ordinal)])
            .Distinct()
            .ToList();

        foreach (var prefix in dataNamespaces.Where(ns => !ns.EndsWith(".Core")))
        {
            var result = Types
                .InAssemblies(assemblies)
                .That().HaveDependencyOn($"{prefix}.Data")
                .And().DoNotResideInNamespace($"{prefix}.Core.Data") // Allow Core data context to reference domain entities
                .And().DoNotHaveName("AppDomainDb") // Allow AppDomainDb to reference all domain data
                .And().DoNotResideInNamespace("Internal")
#if (INCLUDE_ORLEANS)
                .And().DoNotResideInNamespace("OrleansCodeGen")
#endif
                .Should().ResideInNamespace(prefix)
                .GetResult();

            var error =
                $"The following types depend on {prefix}.Data but don't reside in {prefix} namespace (Core data context and AppDomainDb excluded): " +
                $"{string.Join(", ", result.FailingTypeNames ?? [])}";

            result.IsSuccessful.ShouldBeTrue(error);
        }
    }

    [Fact]
    public void Entities_ShouldInheritFromDbEntity()
    {
        var result = GetAppDomainTypes()
            .That().ResideInNamespace("AppDomain")
            .And().ResideInNamespaceEndingWith(".Data.Entities")
            .And().AreClasses()
            .And().DoNotHaveName("DbEntity")
            .Should().Inherit(typeof(DbEntity))
            .GetResult();

        result.IsSuccessful.ShouldBeTrue(
            $"All entities must inherit from DbEntity: {string.Join(", ", result.FailingTypeNames ?? [])}");
    }

    [Fact]
    public void AppDomainDb_ShouldOnlyBeUsedByCommandAndQueryHandlers()
    {
        var typesUsingAppDomainDb = GetAppDomainTypes()
            .That().HaveDependencyOn("AppDomainDb")
            .And().DoNotResideInNamespaceEndingWith(".Commands")
            .And().DoNotResideInNamespaceEndingWith(".Queries")
            .And().DoNotHaveName("AppDomainDb")
            .GetTypes()
            .Select(t => t.FullName)
            .ToList();

        typesUsingAppDomainDb.ShouldBeEmpty(
            $"AppDomainDb should only be used by command and query handlers: {string.Join(", ", typesUsingAppDomainDb)}");
    }
}
