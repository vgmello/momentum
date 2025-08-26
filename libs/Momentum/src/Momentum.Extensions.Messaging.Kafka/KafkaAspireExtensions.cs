// Copyright (c) Momentum .NET. All rights reserved.

using Confluent.Kafka;
using Microsoft.Extensions.Configuration;

namespace Momentum.Extensions.Messaging.Kafka;

/// <summary>
///     Extensions to integrate Wolverine Kafka transport with Aspire Kafka configuration.
/// </summary>
public static class KafkaAspireExtensions
{
    /// <summary>
    ///     Applies Aspire producer configuration to Wolverine's producer config.
    /// </summary>
    public static void ApplyAspireProducerConfig(IConfiguration configuration, string serviceName, ProducerConfig producerConfig)
    {
        ApplyConfig(configuration, serviceName, "Producer", producerConfig);
    }

    /// <summary>
    ///     Applies Aspire consumer configuration to Wolverine's consumer config.
    /// </summary>
    public static void ApplyAspireConsumerConfig(IConfiguration configuration, string serviceName, ConsumerConfig consumerConfig)
    {
        ApplyConfig(configuration, serviceName, "Consumer", consumerConfig);
    }

    /// <summary>
    ///     Applies Aspire client configuration to Wolverine's client config.
    /// </summary>
    public static void ApplyAspireClientConfig(IConfiguration configuration, string serviceName, ClientConfig clientConfig)
    {
        ApplyConfig(configuration, serviceName, string.Empty, clientConfig);
        ApplySecuritySettings(configuration, serviceName, clientConfig);
    }

    private static void ApplySecuritySettings(IConfiguration configuration, string serviceName, ClientConfig clientConfig)
    {
        var securitySection = configuration.GetSection($"Aspire:Confluent:Kafka:{serviceName}:Security");

        if (!securitySection.Exists())
        {
            securitySection = configuration.GetSection("Aspire:Confluent:Kafka:Security");
        }

        if (securitySection.Exists())
        {
            if (securitySection["Protocol"] is { } protocol &&
                Enum.TryParse<SecurityProtocol>(protocol, ignoreCase: true, out var securityProtocol))
            {
                clientConfig.SecurityProtocol = securityProtocol;
            }

            if (securitySection["SaslMechanism"] is { } saslMechanism &&
                Enum.TryParse<SaslMechanism>(saslMechanism, ignoreCase: true, out var mechanism))
            {
                clientConfig.SaslMechanism = mechanism;
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

    private static void ApplyConfig(IConfiguration configuration, string serviceName, string? configType, ClientConfig clientConfig)
    {
        var configTypeSuffix = string.IsNullOrWhiteSpace(configType) ? string.Empty : $":{configType}";

        var configSection = configuration.GetSection($"Aspire:Confluent:Kafka:{serviceName}{configTypeSuffix}");

        if (configSection.Exists())
        {
            BindKafkaConfiguration(configSection, clientConfig);
        }

        var generalConfigSection = configuration.GetSection($"Aspire:Confluent:Kafka{configTypeSuffix}");

        if (generalConfigSection.Exists())
        {
            BindKafkaConfiguration(generalConfigSection, clientConfig);
        }

        var commonSection = configuration.GetSection($"Aspire:Confluent:Kafka:{serviceName}");

        if (commonSection.Exists())
        {
            BindKafkaConfiguration(commonSection, clientConfig);
        }
    }

    private static void BindKafkaConfiguration(IConfigurationSection section, ClientConfig config)
    {
        section.Bind(config);
    }
}
