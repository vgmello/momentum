// Copyright (c) ABCDEG. All rights reserved.

namespace AppDomain.AppHost.Extensions;

public static class LiquibaseExtensions
{
    public static IResourceBuilder<ContainerResource> AddLiquibaseMigrations(
        this IDistributedApplicationBuilder builder,
        IResourceBuilder<IResourceWithConnectionString> dbServerResource,
        IResourceBuilder<ParameterResource> dbPassword)
    {
        return builder
            .AddContainer("liquibase", "liquibase/liquibase:4.32-alpine")
            .WithBindMount("../../infra/AppDomain.Database/Liquibase", "/liquibase/changelog")
            .WithEnvironment("LIQUIBASE_COMMAND_USERNAME", "postgres")
            .WithEnvironment("LIQUIBASE_COMMAND_PASSWORD", dbPassword)
            .WithEnvironment("LIQUIBASE_COMMAND_CHANGELOG_FILE", "changelog.xml")
            .WithEnvironment("LIQUIBASE_SEARCH_PATH", "/liquibase/changelog")
            .WaitFor(dbServerResource)
            .WithReference(dbServerResource)
            .WithEntrypoint("/bin/sh")
            .WithArgs("-c",
                """
                liquibase --url=jdbc:postgresql://AppDomain-db:5432/service_bus update --changelog-file=service_bus/changelog.xml && \
                liquibase --url=jdbc:postgresql://AppDomain-db:5432/AppDomain update --changelog-file=AppDomain/changelog.xml
                """);
    }
}