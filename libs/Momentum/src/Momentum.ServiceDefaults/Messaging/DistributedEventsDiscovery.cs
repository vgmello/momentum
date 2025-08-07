// Copyright (c) Momentum .NET. All rights reserved.

using System.Reflection;
using Wolverine.Runtime.Handlers;

namespace Momentum.ServiceDefaults.Messaging;

public static class DistributedEventsDiscovery
{
    private const string IntegrationEventsNamespace = ".IntegrationEvents";

    private const string Async = "Async";

    private static readonly HashSet<string> HandlerMethodNames = new(StringComparer.OrdinalIgnoreCase)
    {
        HandlerChain.Handle,
        HandlerChain.Handle + Async,
        HandlerChain.Handles,
        HandlerChain.Handles + Async,
        HandlerChain.Consume,
        HandlerChain.Consume + Async,
        HandlerChain.Consumes,
        HandlerChain.Consumes + Async
    };

    /// <summary>
    ///     Discovers and retrieves types that represent integration event types
    ///     within the application's domain assemblies.
    /// </summary>
    /// <remarks>This only applies to "local" domain assemblies</remarks>
    public static IEnumerable<Type> GetIntegrationEventTypes()
    {
        Assembly[] appAssemblies = [.. DomainAssemblyAttribute.GetDomainAssemblies(), ServiceDefaultsExtensions.EntryAssembly];

        var domainPrefixes = appAssemblies
            .Select(a => a.GetName().Name)
            .Where(assemblyName => assemblyName is not null)
            .Select(assemblyName =>
            {
                var mainNamespaceIndex = assemblyName!.IndexOf('.');

                return mainNamespaceIndex >= 0 ? assemblyName[..mainNamespaceIndex] : assemblyName;
            })
            .ToHashSet();

        var domainAssemblies = AppDomain.CurrentDomain.GetAssemblies()
            .Where(assembly =>
            {
                var name = assembly.GetName().Name;

                return name is not null && domainPrefixes.Any(prefix => name.StartsWith(prefix));
            })
            .ToArray();

        return domainAssemblies.SelectMany(a => a.GetTypes()).Where(IsIntegrationEventType);
    }

    /// <summary>
    ///     Discovers and retrieves types that represent integration event types,
    ///     focusing specifically on those that have associated handlers.
    /// </summary>
    /// <remarks>
    ///     This method identifies distributed events by analyzing handler method parameters
    ///     and ensures that only events with corresponding handlers are included.
    /// </remarks>
    public static IEnumerable<Type> GetIntegrationEventTypesWithHandlers()
    {
        var handlerMethods = GetHandlerMethods();

        var integrationEvents = handlerMethods.SelectMany(method =>
            method.GetParameters().Select(parameter => parameter.ParameterType).Where(IsIntegrationEventType)).ToHashSet();

        return integrationEvents;
    }

    // TODO: Use source generation in the future for this
    private static IEnumerable<MethodInfo> GetHandlerMethods()
    {
        Assembly[] handlerAssemblies = [.. DomainAssemblyAttribute.GetDomainAssemblies(), ServiceDefaultsExtensions.EntryAssembly];

        var candidateHandlers = handlerAssemblies
            .SelectMany(assembly => assembly.GetTypes())
            .Where(type => type is { IsClass: true })
            .SelectMany(type => type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static));

        return candidateHandlers.Where(IsHandlerMethod);
    }

    private static bool IsHandlerMethod(MethodInfo method) => HandlerMethodNames.Contains(method.Name) && method.GetParameters().Length > 0;

    private static bool IsIntegrationEventType(Type messageType) => messageType.Namespace?.EndsWith(IntegrationEventsNamespace) == true;
}
