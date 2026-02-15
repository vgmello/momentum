// Copyright (c) OrgName. All rights reserved.

using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Configurations;
using DotNet.Testcontainers.Containers;
using DotNet.Testcontainers.Networks;
using Testcontainers.PostgreSql;

namespace AppDomain.Tests.Integration._Internal.Containers;

public class LiquibaseMigrationContainer : IAsyncDisposable
{
    private readonly IContainer _liquibaseContainer;

    public LiquibaseMigrationContainer(string dbContainerName, INetwork containerNetwork)
    {
        var dbServerSanitized = dbContainerName.Trim('/');
        var baseDirectory = FindSolutionRoot();

        _liquibaseContainer = new ContainerBuilder("liquibase/liquibase:4.33-alpine")
            .WithNetwork(containerNetwork)
            .WithBindMount($"{baseDirectory}infra/AppDomain.Database/Liquibase", "/liquibase/changelog")
            .WithEnvironment("LIQUIBASE_SEARCH_PATH", "/liquibase/changelog")
            .WithEnvironment("LIQUIBASE_COMMAND_USERNAME", PostgreSqlBuilder.DefaultUsername)
            .WithEnvironment("LIQUIBASE_COMMAND_PASSWORD", PostgreSqlBuilder.DefaultPassword)
            .WithEntrypoint("/bin/sh")
            .WithCommand("-c", $"""
                                liquibase update --url=jdbc:postgresql://{dbServerSanitized}:5432/postgres --changelog-file=postgres/changelog.xml && \
                                liquibase update --url=jdbc:postgresql://{dbServerSanitized}:5432/service_bus --changelog-file=service_bus/changelog.xml && \
                                liquibase update --url=jdbc:postgresql://{dbServerSanitized}:5432/app_domain --changelog-file=app_domain/changelog.xml && \
                                echo 'Database migrations completed successfully!'
                                """)
            .WithWaitStrategy(
                Wait.ForUnixContainer()
                    .UntilMessageIsLogged("Database migrations completed successfully!", opt => opt
                        .WithMode(WaitStrategyMode.OneShot)
                        .WithTimeout(TimeSpan.FromMinutes(1)))).Build();
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
                throw new InvalidOperationException("Liquibase migration failed. Unable to retrieve logs.", e);
            }

            throw new InvalidOperationException($"Liquibase migration failed. Logs: {logs}", e);
        }
    }

    public async ValueTask DisposeAsync()
    {
        GC.SuppressFinalize(this);
        await _liquibaseContainer.DisposeAsync();
    }

    private static string FindSolutionRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            if (directory.GetFiles("*.slnx").Length > 0 || directory.GetFiles("*.sln").Length > 0)
                return directory.FullName + Path.DirectorySeparatorChar;

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Could not find solution root directory from " + AppContext.BaseDirectory);
    }
}
