// Copyright (c) ORG_NAME. All rights reserved.

namespace AppDomain.Api.Infrastructure.Extensions;

public static class OrleansExtensions
{
    public static IHostApplicationBuilder AddOrleansClient(this IHostApplicationBuilder builder)
    {
        var useLocalCluster = builder.Configuration.GetValue<bool>("Orleans:UseLocalhostClustering");
        var orleansConnectionString = builder.Configuration.GetConnectionString("OrleansClustering");

        if (!useLocalCluster && string.IsNullOrEmpty(orleansConnectionString))
            throw new InvalidOperationException("Orleans 'OrleansClustering' ConnectionString is missing");

        if (!useLocalCluster)
        {
            builder.AddKeyedAzureTableServiceClient("OrleansClustering");
        }

        builder.UseOrleansClient(clientBuilder =>
        {
            if (useLocalCluster)
            {
                clientBuilder.UseLocalhostClustering();
            }
        });

        SetupLazyClusterClients(builder);

        builder.Services
            .AddOpenTelemetry()
            .WithMetrics(opt => opt.AddMeter("Microsoft.Orleans"))
            .WithTracing(tracing =>
            {
                tracing.AddSource("Microsoft.Orleans.Runtime");
                tracing.AddSource("Microsoft.Orleans.Application");
            });

        return builder;
    }

    private static void SetupLazyClusterClients(IHostApplicationBuilder builder)
    {
        var clusterClientType = typeof(IClusterClient);

        var hostedClusterClientRegistrations = builder.Services
            .Where(r => r.ServiceType == typeof(IHostedService) && IsClusterClientService(r, clusterClientType) && !r.IsKeyedService)
            .ToList();

        foreach (var reg in hostedClusterClientRegistrations)
        {
            builder.Services.Remove(reg);
        }

        var primaryClusterClientRegistration = builder.Services.Last(r => r.ServiceType == clusterClientType && !r.IsKeyedService);
        var primaryKeyedClusterClient = CreateKeyedClusterClient(primaryClusterClientRegistration);

        var factory = LazyClusterClientFactory(primaryClusterClientRegistration);
        var newPrimaryClusterClient = new ServiceDescriptor(clusterClientType, factory, primaryKeyedClusterClient.Lifetime);

        builder.Services.Add(newPrimaryClusterClient);
        builder.Services.AddHostedService<LazyClusterClientManager>();
    }

    private static Func<IServiceProvider, object> LazyClusterClientFactory(ServiceDescriptor primaryClusterClientRegistration)
    {
        var initialized = false;
        var initializing = false;

        return provider =>
        {
            var primaryClusterClient = provider.GetRequiredKeyedService<IClusterClient>(primaryClusterClientRegistration);

            if (initialized)
                return primaryClusterClient;

            var lazyClusterClient = provider.GetRequiredService<LazyClusterClientManager>();

            if (initializing)
            {
                // This should the main ClusterClient initialization (first call)
                lazyClusterClient.StartDelayedAsync(primaryClusterClient);

                return primaryClusterClient;
            }

            initializing = true;

            // This call should trigger this same factory again, which it will be in the `initializing` state, returning the main client
            var clients = provider.GetServices<IClusterClient>().ToList();
            clients.ForEach(lazyClusterClient.StartDelayedAsync);

            initialized = true;

            return primaryClusterClient;
        };
    }

    private static ServiceDescriptor CreateKeyedClusterClient(ServiceDescriptor registration)
    {
        var serviceKey = registration;

        if (registration.ImplementationInstance is not null)
            return new ServiceDescriptor(typeof(IClusterClient), serviceKey, registration.ImplementationInstance!);

        if (registration.ImplementationFactory is not null)
        {
            return new ServiceDescriptor(typeof(IClusterClient), serviceKey, static (provider, key) =>
                ((ServiceDescriptor)key!).ImplementationFactory!.Invoke(provider), registration.Lifetime);
        }

        return new ServiceDescriptor(typeof(IClusterClient), serviceKey, registration.ImplementationType!);
    }

    private static bool IsClusterClientService(ServiceDescriptor serviceDescriptor, Type? clusterClientType)
    {
        static bool IsClusterClientFactory(Func<IServiceProvider, object?>? factory)
        {
            // I couldn't find a better way to check if the factory is a cluster client factory
            var implementationField = factory?.Target?.GetType().GetField("implementation");

            if (implementationField is null)
                return false;

            return (implementationField.GetValue(factory!.Target) as Type)?.IsAssignableTo(typeof(IClusterClient)) == true;
        }

        return IsClusterClientFactory(serviceDescriptor.ImplementationFactory) ||
               serviceDescriptor.ImplementationType?.IsAssignableTo(clusterClientType) == true;
    }

    private sealed class LazyClusterClientManager : IHostedService
    {
        private readonly HashSet<IHostedService> _startedClients = [];

        public void StartDelayedAsync(IClusterClient clusterClient)
        {
            if (clusterClient is not IHostedService hostedClient || _startedClients.Contains(hostedClient))
                return;

            hostedClient.StartAsync(CancellationToken.None).GetAwaiter().GetResult();

            _startedClients.Add(hostedClient);
        }

        public Task StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        public Task StopAsync(CancellationToken cancellationToken)
        {
            var stopTasks = _startedClients.Select(c => c.StopAsync(cancellationToken));

            return Task.WhenAll(stopTasks);
        }
    }
}
