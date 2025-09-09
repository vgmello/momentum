// Copyright (c) OrgName. All rights reserved.

namespace AppDomain.Tests.Integration._Internal.Extensions;

public static class ServiceCollectionExtensions
{
    public static void RemoveServices<TService>(this IServiceCollection services)
    {
        var registeredServices = services.Where(d => d.ServiceType == typeof(TService)).ToList();

        foreach (var registeredService in registeredServices)
            services.Remove(registeredService);
    }
}
