// Copyright (c) OrgName. All rights reserved.

using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Configurations;
using DotNet.Testcontainers.Containers;
using DotNet.Testcontainers.Networks;

namespace AppDomain.Tests.Integration._Internal.Containers;

public class LiquibaseMigrationContainer : IAsyncDisposable
{
    private readonly IContainer _liquibaseContainer;

    public LiquibaseMigrationContainer(string dbContainerName, INetwork containerNetwork)
    {
        var dbServerSanitized = dbContainerName.Trim('/');
        // Get the directory where the test assembly is located, then navigate up to workspace root
        var assemblyLocation = Path.GetDirectoryName(typeof(LiquibaseMigrationContainer).Assembly.Location)!;
        var baseDirectory = Path.GetFullPath(Path.Combine(assemblyLocation, "../../../../../"));
        var liquibasePath = Path.Combine(baseDirectory, "infra/AppDomain.Database/Liquibase");

        var containerBuilder = new ContainerBuilder()
            .WithImage("liquibase/liquibase:4.33-alpine")
            .WithNetwork(containerNetwork);

        // Copy Liquibase files into the container instead of bind mounting
        // This is necessary for Docker-in-Docker scenarios where bind mounts don't work
        foreach (var file in Directory.GetFiles(liquibasePath, "*", SearchOption.AllDirectories))
        {
            var relativePath = Path.GetRelativePath(liquibasePath, file);
            var targetPath = $"/liquibase/changelog/{relativePath.Replace(Path.DirectorySeparatorChar, '/')}";
            containerBuilder = containerBuilder.WithResourceMapping(file, targetPath);
        }

        _liquibaseContainer = containerBuilder
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
                    .UntilMessageIsLogged("Migration Complete", opt => opt
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
                throw new InvalidOperationException($"Liquibase migration failed. Unable to retrieve logs.", e);
            }

            throw new InvalidOperationException($"Liquibase migration failed. Logs: {logs}", e);
        }
    }

    public async ValueTask DisposeAsync() => await _liquibaseContainer.DisposeAsync();
}
