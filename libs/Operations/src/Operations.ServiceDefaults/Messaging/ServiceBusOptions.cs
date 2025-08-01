// Copyright (c) ABCDEG. All rights reserved.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Operations.Extensions.Abstractions.Extensions;

namespace Operations.ServiceDefaults.Messaging;

public class ServiceBusOptions
{
    public static string SectionName => "ServiceBus";

    public string Domain { get; set; } = GetDomainName();

    public string PublicServiceName { get; set; } = string.Empty;

    public Uri ServiceUrn { get; private set; } = null!;

    public CloudEventsSettings CloudEvents { get; set; } = new();

    public static string GetServiceName(string appName) => appName.ToLowerInvariant().Replace('.', '-');

    private static string GetDomainName()
    {
        //TODO: make this better, potentially extract an assembly attribute/csproj config
        var assemblyName = ServiceDefaultsExtensions.EntryAssembly.FullName!;
        var mainNamespaceIndex = assemblyName.IndexOf('.');

        return mainNamespaceIndex >= 0 ? assemblyName[..mainNamespaceIndex] : assemblyName;
    }

    public class Configurator(ILogger<Configurator> logger, IHostEnvironment env, IConfiguration config)
        : IPostConfigureOptions<ServiceBusOptions>
    {
        public void PostConfigure(string? name, ServiceBusOptions options)
        {
            if (options.PublicServiceName.Length == 0)
                options.PublicServiceName = GetServiceName(env.ApplicationName);

            options.ServiceUrn = new Uri($"/{options.Domain.ToSnakeCase()}/{GetServiceName(options.PublicServiceName)}", UriKind.Relative);

            var connectionString = config.GetConnectionString(SectionName);

            if (string.IsNullOrWhiteSpace(connectionString))
            {
                logger.LogWarning("ConnectionStrings:ServiceBus is not set. " +
                                  "Transactional Inbox/Outbox and Message Persistence features disabled");
            }
        }
    }
}
