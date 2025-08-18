// Copyright (c) Momentum .NET. All rights reserved.

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Wolverine;

namespace Momentum.Extensions.Messaging.Kafka;

public static class KafkaMessagingExtensions
{
    public static WebApplicationBuilder AddKafkaMessagingExtensions(this WebApplicationBuilder builder)
    {
        var kafkaConnectionString = builder.Configuration.GetConnectionString(KafkaEventsExtensions.ConnectionStringName);

        if (string.IsNullOrEmpty(kafkaConnectionString))
            throw new InvalidOperationException("Kafka connection string 'Messaging' not found in configuration.");

        builder.Services.AddSingleton<IWolverineExtension, KafkaEventsExtensions>();

        builder.Services
            .AddHealthChecks()
            .AddKafka(options =>
            {
                options.BootstrapServers = kafkaConnectionString;
            }, name: "kafka", tags: ["messaging", "kafka"]);

        return builder;
    }
}
