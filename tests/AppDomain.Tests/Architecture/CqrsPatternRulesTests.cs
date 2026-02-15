// Copyright (c) OrgName. All rights reserved.

namespace AppDomain.Tests.Architecture;
public class CqrsPatternRulesTests : ArchitectureTestBase
{
    [Fact]
    public void Commands_ShouldImplementGenericICommand()
    {
        var commandTypes = GetAppDomainTypes()
            .That().ResideInNamespaceEndingWith(".Commands")
            .And().AreClasses()
            .And().HaveNameEndingWith("Command")
            .GetTypes();

        var commandsNotImplementingICommand = commandTypes
            .Where(type => !ImplementsGenericInterface(type, "ICommand"))
            .Select(type => type.FullName)
            .ToList();

        commandsNotImplementingICommand.ShouldBeEmpty(
            $"All commands should implement ICommand<T> interface: {string.Join(", ", commandsNotImplementingICommand)}");
    }

    [Fact]
    public void Queries_ShouldImplementGenericIQuery()
    {
        var queryTypes = GetAppDomainTypes()
            .That().ResideInNamespaceEndingWith(".Queries")
            .And().AreClasses()
            .And().HaveNameEndingWith("Query")
            .GetTypes();

        var queriesNotImplementingIQuery = queryTypes
            .Where(type => !ImplementsGenericInterface(type, "IQuery"))
            .Select(type => type.FullName)
            .ToList();

        queriesNotImplementingIQuery.ShouldBeEmpty(
            $"All queries should implement IQuery<T> interface: {string.Join(", ", queriesNotImplementingIQuery)}");
    }

    private static bool ImplementsGenericInterface(Type type, string interfaceBaseName)
    {
        return type.GetInterfaces()
            .Any(i => i.IsGenericType && i.GetGenericTypeDefinition().Name.StartsWith(interfaceBaseName));
    }
}
