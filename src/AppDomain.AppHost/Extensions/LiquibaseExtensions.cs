// Copyright (c) OrgName. All rights reserved.

namespace AppDomain.AppHost.Extensions;

/// <summary>
///     Provides extension methods for configuring Liquibase database migrations in .NET Aspire orchestration.
/// </summary>
public static class LiquibaseExtensions
{
    /// <summary>
    ///     Adds a Liquibase container resource for running database migrations in Aspire orchestration.
    /// </summary>
    /// <param name="builder">The distributed application builder.</param>
    /// <param name="dbServerResource">The database server resource that migrations will target.</param>
    /// <param name="dbPassword">The database password parameter resource.</param>
    /// <returns>A container resource builder configured to run Liquibase migrations.</returns>
    /// <remarks>
    ///     This method configures a Liquibase container that will run migrations against the postgres database,
    ///     creating the service_bus and app_domain databases and their schemas using the master changelog.
    ///     The container waits for the database server to be ready before executing migrations.
    /// </remarks>
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
                "liquibase --url=jdbc:postgresql://app-domain-db:5432/postgres update");
    }
}
