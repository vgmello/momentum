// Copyright (c) Momentum .NET. All rights reserved.

using Microsoft.Extensions.Hosting;

namespace Momentum.Extensions.Messaging.Kafka;

public static class EnvExtensions
{
    public static string GetEnvNameShort(this IHostEnvironment env)
    {
        var envLower = env.EnvironmentName.ToLowerInvariant();

        if (envLower.Length < 5)
        {
            return envLower;
        }

        return envLower switch
        {
            "development" => "dev",
            _ => envLower[..4]
        };
    }
}
