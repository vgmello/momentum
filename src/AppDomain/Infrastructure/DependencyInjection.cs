// Copyright (c) OrgName. All rights reserved.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

//#if (USE_DB)
using AppDomain.Infrastructure.Messaging;
using Dapper;
using LinqToDB.DataProvider.PostgreSQL;
using LinqToDB.Extensions.DependencyInjection;
using LinqToDB.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Momentum.Extensions.Data.LinqToDb;
using Wolverine.Runtime.Handlers;

//#endif

namespace AppDomain.Infrastructure;

/// <summary>
///     Provides extension methods for configuring AppDomain infrastructure services.
/// </summary>
[ExcludeFromCodeCoverage]
public static class DependencyInjection
{
    /// <summary>
    ///     Registers AppDomain core services including database context and data mapping.
    /// </summary>
    /// <param name="builder">The host application builder to configure.</param>
    /// <returns>The configured host application builder for method chaining.</returns>
    public static IHostApplicationBuilder AddAppDomainServices(this IHostApplicationBuilder builder)
    {
        //#if (USE_PGSQL)
        DefaultTypeMap.MatchNamesWithUnderscores = true;
        builder.AddNpgsqlDataSource("AppDomainDb");

        var connectionString = builder.Configuration.GetConnectionString("AppDomainDb")
            ?? throw new InvalidOperationException("Connection string 'AppDomainDb' is not configured.");

        builder.Services.AddLinqToDBContext<AppDomainDb>((svcProvider, options) =>
        {
            options = options
                .UseMappingSchema(schema => schema.AddMetadataReader(new SnakeCaseNamingConventionMetadataReader()))
                .UseDefaultLogging(svcProvider);

            // Inside a command pipeline, attach to the transaction opened by
            // TransactionalOutboxMiddleware so business writes commit atomically with the outbox.
            return TransactionalOutbox.CurrentTransaction is { } transaction
                ? options.UseTransaction(PostgreSQLTools.GetDataProvider(PostgreSQLVersion.v15), transaction)
                : options.UsePostgreSQL(connectionString);
        });

        builder.Services.AddSingleton<IWolverineExtension, AppDomainDbWolverineExtension>();
        //#endif

        return builder;
    }
}

//#if (USE_PGSQL)
file sealed class AppDomainDbWolverineExtension : IWolverineExtension
{
    public void Configure(WolverineOptions options)
    {
        // AddLinqToDBContext registers DataOptions<AppDomainDb> behind an opaque lambda factory that
        // Wolverine's codegen can't see through, so it needs the service-location allow-list under
        // Wolverine 6's default ServiceLocationPolicy.NotAllowed (see WolverineSetupExtensions.cs).
        options.CodeGeneration.AlwaysUseServiceLocationFor<DataOptions<AppDomainDb>>();

        options.Policies.AddMiddleware<TransactionalOutboxMiddleware>(chain => IsCommandChain(chain));
    }

    private static bool IsCommandChain(HandlerChain chain) =>
        chain.MessageType.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ICommand<>));
}
//#endif
