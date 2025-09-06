// Copyright (c) OrgName. All rights reserved.

namespace AppDomain.Tests.Architecture;

#pragma warning disable CS8602

public class ValidationRulesTests : ArchitectureTestBase
{
    [Fact]
    public void Commands_ShouldHaveValidators()
    {
        var commandTypes = GetAppDomainTypes()
            .That().ResideInNamespaceEndingWith(".Commands")
            .And().HaveNameEndingWith("Command")
            .GetTypes();

        var commandsWithoutValidators = commandTypes
            .Where(cmdType => !HasCorrespondingValidator(cmdType))
            .Select(type => type.FullName)
            .ToList();

        commandsWithoutValidators.ShouldBeEmpty(
            $"Commands should have corresponding validators: {string.Join(", ", commandsWithoutValidators)}");
    }

    private static bool HasCorrespondingValidator(Type commandType)
    {
        var expectedValidatorName = commandType.Name.Replace("Command", "Validator");

        return commandType.Assembly.GetTypes()
            .Any(t => t.Name == expectedValidatorName && t.BaseType?.Name.Contains("AbstractValidator") == true);
    }
}
