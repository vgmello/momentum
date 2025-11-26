// Copyright (c) Momentum .NET. All rights reserved.

namespace Momentum.Extensions.EventMarkdownGenerator.Services;

/// <summary>
///     Constants for namespace parsing used across event discovery and sidebar generation.
/// </summary>
internal static class NamespaceConstants
{
    /// <summary>
    ///     The "Contracts" namespace segment used to identify event contract assemblies.
    /// </summary>
    public const string Contracts = "Contracts";

    /// <summary>
    ///     The "IntegrationEvents" namespace segment for public events.
    /// </summary>
    public const string IntegrationEvents = "IntegrationEvents";

    /// <summary>
    ///     The "DomainEvents" namespace segment for internal domain events.
    /// </summary>
    public const string DomainEvents = "DomainEvents";

    /// <summary>
    ///     Namespace suffix for integration events (with leading dot).
    /// </summary>
    public const string IntegrationEventsNamespaceSuffix = ".IntegrationEvents";

    /// <summary>
    ///     Namespace suffix for domain events (with leading dot).
    /// </summary>
    public const string DomainEventsNamespaceSuffix = ".DomainEvents";
}
