// Copyright (c) OrgName. All rights reserved.

namespace AppDomain.Tests.Architecture;

#pragma warning disable CS8602

public class EntityContractSeparationRulesTests : ArchitectureTestBase
{
    [Fact]
    public void DbCommands_ShouldReturnEntityTypes_NotContractModels()
    {
        var commandTypes = GetAppDomainTypes()
            .That().HaveNameStartingWith("Db")
            .And().HaveNameEndingWith("Command")
            .GetTypes();

        var commandsReturningContracts = new List<string>();

        foreach (var commandType in commandTypes)
        {
            var interfaces = commandType.GetInterfaces();
            var commandInterface = interfaces.FirstOrDefault(i =>
                i.IsGenericType && i.GetGenericTypeDefinition().Name.StartsWith("ICommand"));

            if (commandInterface != null)
            {
                var returnType = commandInterface.GetGenericArguments()[0];

                // Check if return type is from Contracts namespace
                if (returnType.Namespace?.Contains(".Contracts.") == true)
                {
                    commandsReturningContracts.Add($"{commandType.FullName} returns {returnType.FullName}");
                }
            }
        }

        commandsReturningContracts.ShouldBeEmpty(
            $"DbCommands should return entity types, not contract models: {string.Join(", ", commandsReturningContracts)}");
    }

    [Fact]
    public void DbQueries_ShouldReturnEntityTypes_NotContractModels()
    {
        var queryTypes = GetAppDomainTypes()
            .That().HaveNameStartingWith("Db")
            .And().HaveNameEndingWith("Query")
            .GetTypes();

        var queriesReturningContracts = new List<string>();

        foreach (var queryType in queryTypes)
        {
            var interfaces = queryType.GetInterfaces();
            var queryInterface = interfaces.FirstOrDefault(i =>
                i.IsGenericType && i.GetGenericTypeDefinition().Name.StartsWith("IQuery"));

            if (queryInterface != null)
            {
                var returnType = queryInterface.GetGenericArguments()[0];

                // Handle IEnumerable<T> and similar generic types
                var typeToCheck = returnType;

                if (returnType.IsGenericType)
                {
                    var genericArgs = returnType.GetGenericArguments();

                    if (genericArgs.Length > 0)
                    {
                        typeToCheck = genericArgs[0];
                    }
                }

                // Check if return type is from Contracts namespace
                if (typeToCheck.Namespace?.Contains(".Contracts.") == true)
                {
                    queriesReturningContracts.Add($"{queryType.FullName} returns {returnType.FullName}");
                }
            }
        }

        queriesReturningContracts.ShouldBeEmpty(
            $"DbQueries should return entity types, not contract models: {string.Join(", ", queriesReturningContracts)}");
    }
}
