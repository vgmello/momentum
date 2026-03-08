// Copyright (c) Momentum .NET. All rights reserved.

using Microsoft.Extensions.Hosting;

namespace Momentum.ServiceDefaults.Messaging;

/// <summary>
///     Builder for configuring the service bus messaging infrastructure.
///     Messaging providers register themselves as extension methods on this class.
/// </summary>
public class ServiceBusBuilder(IHostApplicationBuilder hostBuilder)
{
    /// <summary>
    ///     Gets the host application builder for accessing configuration and services.
    /// </summary>
    public IHostApplicationBuilder HostBuilder { get; } = hostBuilder;
}
