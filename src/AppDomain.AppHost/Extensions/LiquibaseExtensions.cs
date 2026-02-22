// Copyright (c) OrgName. All rights reserved.

namespace AppDomain.AppHost.Extensions;

/// <summary>
///     Provides extension methods for configuring Liquibase database migrations in .NET Aspire orchestration.
/// </summary>
[ExcludeFromCodeCoverage]
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
    ///     This method configures a Liquibase container that will run migrations for both the
    ///     service_bus and app_domain databases using changelog files from the mounted volume.
    ///     The container waits for the database server to be ready before executing migrations.
    /// </remarks>
    public static IResourceBuilder<ContainerResource> AddLiquibaseMigrations(
        this IDistributedApplicationBuilder builder,
        IResourceBuilder<IResourceWithConnectionString> dbServerResource,
        IResourceBuilder<ParameterResource> dbPassword)
    {
        return builder
            .AddContainer("liquibase", "liquibase/liquibase:4.33-alpine")
            .WithBindMount("../../infra/AppDomain.Database/Liquibase", "/liquibase/changelog")
            .WithEnvironment("LIQUIBASE_COMMAND_USERNAME", "postgres")
            .WithEnvironment("LIQUIBASE_COMMAND_PASSWORD", dbPassword)
            .WithEnvironment("LIQUIBASE_SEARCH_PATH", "/liquibase/changelog")
            .WaitFor(dbServerResource)
            .WithReference(dbServerResource)
            .WithEntrypoint("/bin/sh")
            .WithArgs("-c",
                """
                liquibase update --url=jdbc:postgresql://app-domain-db:5432/postgres --changelog-file=postgres/changelog.xml --contexts=aspire && \
                liquibase update --url=jdbc:postgresql://app-domain-db:5432/service_bus --changelog-file=service_bus/changelog.xml && \
                liquibase update --url=jdbc:postgresql://app-domain-db:5432/app_domain --changelog-file=app_domain/changelog.xml && \
                echo 'Database migrations completed successfully!'
                """);
    }
}
