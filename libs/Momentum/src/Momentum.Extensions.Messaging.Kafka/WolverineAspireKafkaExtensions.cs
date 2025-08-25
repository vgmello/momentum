// Copyright (c) Momentum .NET. All rights reserved.

using Confluent.Kafka;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Wolverine;
using Wolverine.Kafka;
using Wolverine.Runtime;

namespace Momentum.Extensions.Messaging.Kafka;

/// <summary>
/// Extensions to integrate Wolverine Kafka transport with Aspire Kafka configuration.
/// </summary>
public static class WolverineAspireKafkaExtensions
{
    /// <summary>
    /// Configure Wolverine to use Kafka with Aspire's configuration management.
    /// </summary>
    /// <param name="options">Wolverine options</param>
    /// <param name="builder">Host application builder for Aspire integration</param>
    /// <param name="connectionName">Connection name for Kafka configuration</param>
    /// <param name="configure">Additional Wolverine Kafka configuration</param>
    /// <returns>Kafka transport expression for further configuration</returns>
    public static KafkaTransportExpression UseKafkaWithAspire(
        this WolverineOptions options,
        IHostApplicationBuilder builder,
        string connectionName = KafkaMessagingExtensions.DefaultConnectionName,
        Action<KafkaTransportExpression>? configure = null)
    {
        // Register Aspire Kafka components
        builder.AddKafkaProducer<string, byte[]>(connectionName);
        builder.AddKafkaConsumer<string, byte[]>(connectionName);

        // Get the configuration
        var configuration = builder.Configuration;
        var connectionString = GetKafkaConnectionString(configuration, connectionName);

        var kafkaExpression = options.UseKafka(connectionString);

        // Configure producers with Aspire settings
        kafkaExpression.ConfigureProducers(producerConfig =>
        {
            ApplyAspireProducerConfig(configuration, connectionName, producerConfig);
        });

        // Configure consumers with Aspire settings
        kafkaExpression.ConfigureConsumers(consumerConfig =>
        {
            ApplyAspireConsumerConfig(configuration, connectionName, consumerConfig);
        });

        // Apply general Kafka client configuration
        kafkaExpression.ConfigureClient(clientConfig =>
        {
            ApplyAspireClientConfig(configuration, connectionName, clientConfig);
        });

        // Register Wolverine Kafka health check
        RegisterWolverineKafkaHealthCheck(options.Services, connectionName);

        // Apply any additional custom configuration
        configure?.Invoke(kafkaExpression);

        return kafkaExpression;
    }

    /// <summary>
    /// Gets the Kafka connection string from various Aspire configuration sources.
    /// </summary>
    private static string GetKafkaConnectionString(IConfiguration configuration, string connectionName)
    {
        // Try connection strings first
        var connectionString = configuration.GetConnectionString(connectionName);
        
        if (!string.IsNullOrEmpty(connectionString))
            return connectionString;

        // Try Aspire-specific bootstrap servers configuration
        var bootstrapServers = configuration[$"Aspire:Confluent:Kafka:{connectionName}:BootstrapServers"];
        if (!string.IsNullOrEmpty(bootstrapServers))
            return bootstrapServers;

        // Try alternative connection string format
        var altConnectionString = configuration[$"ConnectionStrings__{connectionName}"];
        if (!string.IsNullOrEmpty(altConnectionString))
            return altConnectionString;

        // Fall back to localhost for development
        return "localhost:9092";
    }

    /// <summary>
    /// Applies Aspire producer configuration to Wolverine's producer config.
    /// </summary>
    private static void ApplyAspireProducerConfig(
        IConfiguration configuration,
        string connectionName,
        ProducerConfig producerConfig)
    {
        // Apply producer-specific configuration
        var producerSection = configuration.GetSection($"Aspire:Confluent:Kafka:{connectionName}:Producer");
        if (producerSection.Exists())
        {
            BindKafkaConfiguration(producerSection, producerConfig);
        }

        // Apply general producer configuration
        var generalProducerSection = configuration.GetSection($"Aspire:Confluent:Kafka:Producer");
        if (generalProducerSection.Exists())
        {
            BindKafkaConfiguration(generalProducerSection, producerConfig);
        }

        // Apply common client configuration
        ApplyCommonClientConfig(configuration, connectionName, producerConfig);
    }

    /// <summary>
    /// Applies Aspire consumer configuration to Wolverine's consumer config.
    /// </summary>
    private static void ApplyAspireConsumerConfig(
        IConfiguration configuration,
        string connectionName,
        ConsumerConfig consumerConfig)
    {
        // Apply consumer-specific configuration
        var consumerSection = configuration.GetSection($"Aspire:Confluent:Kafka:{connectionName}:Consumer");
        if (consumerSection.Exists())
        {
            BindKafkaConfiguration(consumerSection, consumerConfig);
        }

        // Apply general consumer configuration
        var generalConsumerSection = configuration.GetSection($"Aspire:Confluent:Kafka:Consumer");
        if (generalConsumerSection.Exists())
        {
            BindKafkaConfiguration(generalConsumerSection, consumerConfig);
        }

        // Apply common client configuration
        ApplyCommonClientConfig(configuration, connectionName, consumerConfig);
    }

    /// <summary>
    /// Applies Aspire client configuration to Wolverine's client config.
    /// </summary>
    private static void ApplyAspireClientConfig(
        IConfiguration configuration,
        string connectionName,
        ClientConfig clientConfig)
    {
        // Apply connection-specific configuration
        var clientSection = configuration.GetSection($"Aspire:Confluent:Kafka:{connectionName}");
        if (clientSection.Exists())
        {
            BindKafkaConfiguration(clientSection, clientConfig);
        }

        // Apply general client configuration
        var generalSection = configuration.GetSection($"Aspire:Confluent:Kafka");
        if (generalSection.Exists())
        {
            BindKafkaConfiguration(generalSection, clientConfig);
        }

        // Apply security settings
        ApplySecuritySettings(configuration, connectionName, clientConfig);
    }

    /// <summary>
    /// Applies common client configuration to both producer and consumer configs.
    /// </summary>
    private static void ApplyCommonClientConfig(
        IConfiguration configuration,
        string connectionName,
        ClientConfig clientConfig)
    {
        var commonSection = configuration.GetSection($"Aspire:Confluent:Kafka:{connectionName}");
        if (commonSection.Exists())
        {
            BindKafkaConfiguration(commonSection, clientConfig);
        }
    }

    /// <summary>
    /// Applies security settings from Aspire configuration.
    /// </summary>
    private static void ApplySecuritySettings(
        IConfiguration configuration,
        string connectionName,
        ClientConfig clientConfig)
    {
        var securitySection = configuration.GetSection($"Aspire:Confluent:Kafka:{connectionName}:Security");
        if (!securitySection.Exists())
        {
            securitySection = configuration.GetSection($"Aspire:Confluent:Kafka:Security");
        }

        if (securitySection.Exists())
        {
            // Apply SASL settings
            if (securitySection["Protocol"] is { } protocol)
            {
                if (Enum.TryParse<SecurityProtocol>(protocol, ignoreCase: true, out var securityProtocol))
                {
                    clientConfig.SecurityProtocol = securityProtocol;
                }
            }

            if (securitySection["SaslMechanism"] is { } saslMechanism)
            {
                if (Enum.TryParse<SaslMechanism>(saslMechanism, ignoreCase: true, out var mechanism))
                {
                    clientConfig.SaslMechanism = mechanism;
                }
            }

            if (securitySection["SaslUsername"] is { } username)
                clientConfig.SaslUsername = username;
            if (securitySection["SaslPassword"] is { } password)
                clientConfig.SaslPassword = password;

            if (securitySection["SslCaLocation"] is { } caLocation)
                clientConfig.SslCaLocation = caLocation;

            if (securitySection["SslCertificateLocation"] is { } certLocation)
                clientConfig.SslCertificateLocation = certLocation;
            if (securitySection["SslKeyLocation"] is { } keyLocation)
                clientConfig.SslKeyLocation = keyLocation;
        }
    }

    /// <summary>
    /// Binds configuration section to Kafka configuration object using reflection.
    /// </summary>
    private static void BindKafkaConfiguration(IConfigurationSection section, object config)
    {
        try
        {
            section.Bind(config);
        }
        catch
        {
            // Fallback to manual property mapping for complex cases
            foreach (var kvp in section.AsEnumerable(makePathsRelative: true))
            {
                if (string.IsNullOrEmpty(kvp.Key) || string.IsNullOrEmpty(kvp.Value))
                    continue;

                var property = config.GetType().GetProperty(kvp.Key);
                if (property?.CanWrite == true)
                {
                    try
                    {
                        var convertedValue = Convert.ChangeType(kvp.Value, property.PropertyType);
                        property.SetValue(config, convertedValue);
                    }
                    catch
                    {
                        // Ignore conversion errors for unsupported properties
                    }
                }
            }
        }
    }

    /// <summary>
    /// Registers a health check for Wolverine Kafka endpoints.
    /// </summary>
    private static void RegisterWolverineKafkaHealthCheck(IServiceCollection services, string connectionName)
    {
        services.AddHealthChecks()
            .Add(new HealthCheckRegistration(
                $"wolverine_kafka_{connectionName}",
                sp => new WolverineKafkaHealthCheck(sp.GetService<IWolverineRuntime>()),
                HealthStatus.Degraded,
                tags: ["kafka", "wolverine", "messaging", connectionName]));
    }
}