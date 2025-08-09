// Copyright (c) ABCDEG. All rights reserved.

using DotNet.Testcontainers.Containers;

namespace AppDomain.Tests.Integration._Internal.Extensions;

public static class DatabaseContainerExtensions
{
    public static string GetDbConnectionString(this IDatabaseContainer dbContainer, string dbName)
    {
        return $"Host={dbContainer.Hostname};" +
               $"Port={dbContainer.GetMappedPublicPort(5432)};" +
               $"Database={dbName};" +
               $"Username=postgres;Password=postgres";
    }
}
