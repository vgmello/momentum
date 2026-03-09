// Copyright (c) Momentum .NET. All rights reserved.

using System.Collections.Frozen;
using System.Reflection;

namespace Momentum.Extensions.Messaging;

public static class DistributedEventsDiscovery
{
    private const string IntegrationEventsNamespace = ".IntegrationEvents";

    private const string Async = "Async";

    private const string Handle = "Handle";
    private const string Handles = "Handles";
    private const string Consume = "Consume";
    private const string Consumes = "Consumes";

    private static readonly FrozenSet<string> HandlerMethodNames = new[]
    {
        Handle,
        Handle + Async,
        Handles,
        Handles + Async,
        Consume,
        Consume + Async,
        Consumes,
        Consumes + Async
    }.ToFrozenSet(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    ///     Discovers and retrieves types that represent integration event types
    ///     within the application's domain assemblies.
    /// </summary>
    /// <remarks>
    ///     <!--@include: @code/messaging/events-discovery-detailed.md#local-domain-discovery -->
    /// </remarks>
    public static IEnumerable<Type> GetIntegrationEventTypes()
    {
        Assembly[] appAssemblies = [.. DomainAssemblyAttribute.GetDomainAssemblies(), MomentumApp.EntryAssembly];

        var domainPrefixes = appAssemblies
            .Select(a => a.GetName().Name)
            .Where(assemblyName => assemblyName is not null)
            .Select(assemblyName =>
            {
                var mainNamespaceIndex = assemblyName!.IndexOf('.');

                return mainNamespaceIndex >= 0 ? assemblyName[..mainNamespaceIndex] : assemblyName;
            })
            .ToHashSet();
        //-:replacements:noEmit

        var domainAssemblies = AppDomain.CurrentDomain.GetAssemblies()
            .Where(assembly =>
            {
                var name = assembly.GetName().Name;

                return name is not null && domainPrefixes.Any(prefix => name.StartsWith(prefix));
            })
            .ToArray();
        //+:replacements:noEmit

        return domainAssemblies.SelectMany(a => a.GetTypes()).Where(IsIntegrationEventType);
    }

    /// <summary>
    ///     Discovers and retrieves types that represent integration event types,
    ///     focusing specifically on those that have associated handlers.
    /// </summary>
    /// <remarks>
    ///     <!--@include: @code/messaging/events-discovery-detailed.md#handler-associated-events -->
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
        Assembly[] handlerAssemblies = [.. DomainAssemblyAttribute.GetDomainAssemblies(), MomentumApp.EntryAssembly];

        var candidateHandlers = handlerAssemblies
            .SelectMany(assembly => assembly.GetTypes())
            .Where(type => type is { IsClass: true })
            .SelectMany(type => type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static));

        return candidateHandlers.Where(IsHandlerMethod);
    }

    private static bool IsHandlerMethod(MethodInfo method) => HandlerMethodNames.Contains(method.Name) && method.GetParameters().Length > 0;

    private static bool IsIntegrationEventType(Type messageType) => messageType.Namespace?.EndsWith(IntegrationEventsNamespace) == true;
}
