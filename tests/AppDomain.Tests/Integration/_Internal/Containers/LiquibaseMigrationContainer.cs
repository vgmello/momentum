// Copyright (c) OrgName. All rights reserved.

using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Configurations;
using DotNet.Testcontainers.Containers;
using DotNet.Testcontainers.Images;
using DotNet.Testcontainers.Networks;

namespace AppDomain.Tests.Integration._Internal.Containers;

public class LiquibaseMigrationContainer : IAsyncDisposable
{
    private readonly IContainer _liquibaseContainer;

    public LiquibaseMigrationContainer(string dbContainerName, INetwork containerNetwork)
    {
        var dbServerSanitized = dbContainerName.Trim('/');

        // Use pre-built custom Liquibase image with migration files baked in
        // This avoids Docker-in-Docker bind mount issues
        // The image must be built before running tests using:
        // docker build -f infra/AppDomain.Database/Dockerfile.liquibase -t appdomain-liquibase-test:latest infra/AppDomain.Database
        _liquibaseContainer = new ContainerBuilder()
            .WithImage("appdomain-liquibase-test:latest")
            .WithImagePullPolicy(PullPolicy.Never) // Use locally built image
            .WithNetwork(containerNetwork)
            .WithEnvironment("LIQUIBASE_COMMAND_USERNAME", "postgres")
            .WithEnvironment("LIQUIBASE_COMMAND_PASSWORD", "postgres")
            .WithEnvironment("LIQUIBASE_COMMAND_CHANGELOG_FILE", "changelog.xml")
            .WithEnvironment("LIQUIBASE_SEARCH_PATH", "/liquibase/changelog")
            .WithEntrypoint("/bin/sh")
            .WithCommand("-c", $"""
                                liquibase --url=jdbc:postgresql://{dbServerSanitized}:5432/postgres update --contexts @setup && \
                                liquibase --url=jdbc:postgresql://{dbServerSanitized}:5432/service_bus update --changelog-file=service_bus/changelog.xml && \
                                liquibase --url=jdbc:postgresql://{dbServerSanitized}:5432/app_domain update --changelog-file=app_domain/changelog.xml && \
                                echo Migration Complete
                                """)
            .WithWaitStrategy(
                Wait.ForUnixContainer()
                    .UntilMessageIsLogged("Migration Complete", opt => opt.WithTimeout(TimeSpan.FromMinutes(2))))
            .Build();
    }

    public async Task StartAsync()
    {
        try
        {
            await _liquibaseContainer.StartAsync();
            var result = await _liquibaseContainer.GetExitCodeAsync();

            if (result != 0)
            {
                var logs = await _liquibaseContainer.GetLogsAsync();

                throw new InvalidOperationException($"Liquibase migration failed with exit code {result}. Logs: {logs}");
            }
        }
        catch (Exception e) when (e is not InvalidOperationException)
        {
            (string Stdout, string Stderr)? logs;

            try
            {
                logs = await _liquibaseContainer.GetLogsAsync();
            }
            catch
            {
                throw new InvalidOperationException($"Liquibase migration failed. Unable to retrieve logs.", e);
            }

            throw new InvalidOperationException($"Liquibase migration failed. Logs: {logs}", e);
        }
    }

    public async ValueTask DisposeAsync()
    {
        await _liquibaseContainer.DisposeAsync();
        // Note: Testcontainers will automatically clean up the built image
    }
}
