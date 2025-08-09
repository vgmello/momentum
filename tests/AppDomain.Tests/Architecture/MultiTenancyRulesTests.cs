<!--#if (includeSample)-->
// Copyright (c) ABCDEG. All rights reserved.

namespace AppDomain.Tests.Architecture;

#pragma warning disable CS8602

public class MultiTenancyRulesTests : ArchitectureTestBase
{
    [Fact]
    public void Commands_ShouldIncludeTenantId_ForDomainCommands()
    {
        var commandTypes = GetAppDomainTypes()
            .That().ResideInNamespaceEndingWith(".Commands")
            .And().HaveNameEndingWith("Command")
            .GetTypes();

        var commandsWithoutTenantId = commandTypes
            .Where(type => !HasTenantIdProperty(type) && RequiresTenantId(type))
            .Select(type => type.FullName)
            .ToList();

        commandsWithoutTenantId.ShouldBeEmpty(
            $"Domain commands should include TenantId property: {string.Join(", ", commandsWithoutTenantId)}");
    }

    private static bool HasTenantIdProperty(Type type)
    {
        return type.GetProperties().Any(p => p.Name == "TenantId" && p.PropertyType == typeof(Guid));
    }

    private static bool RequiresTenantId(Type type)
    {
        // Some commands like CreateInvoiceCommand might not require direct TenantId
        // as they can be scoped at the handler level or through other means
        var commandsNotRequiringTenantId = new[]
        {
            "CreateInvoiceCommand", "CancelInvoiceCommand", "MarkInvoiceAsPaidCommand"
        };

        return !commandsNotRequiringTenantId.Contains(type.Name);
    }
}
<!--#endif-->
