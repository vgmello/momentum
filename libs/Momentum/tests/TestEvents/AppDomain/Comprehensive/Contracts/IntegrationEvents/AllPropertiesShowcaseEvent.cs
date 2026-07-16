// Copyright (c) Momentum .NET. All rights reserved.

using Momentum.Extensions.Abstractions.Messaging;

namespace TestEvents.AppDomain.Comprehensive.Contracts.IntegrationEvents;

/// <summary>
///     Exercises every EventMetadata property in a single event: explicit domain/topic/version,
///     internal visibility, an obsolete marker, multiple partition keys, a complex nested property,
///     and a collection of a complex type.
/// </summary>
/// <param name="TenantId">Identifier of the tenant that owns this record</param>
/// <param name="SequenceNumber">Secondary partition key used for ordering within a tenant</param>
/// <param name="PrimaryLocation">The primary complex-type property, rendered with its own reference schema</param>
/// <param name="RelatedLocations">A collection of complex-type entries, rendered with an element reference schema</param>
/// <remarks>
///     ## When It's Triggered
///
///     This event exists purely as a documentation-generator test fixture, showcasing every
///     renderable field of the generated markdown at once.
/// </remarks>
/// <example>
///     <code>
///     await bus.PublishAsync(new AllPropertiesShowcaseEvent(tenantId, 1, primaryLocation, relatedLocations));
///     </code>
/// </example>
[Obsolete("Superseded by a hypothetical future event; kept only as a documentation-generator test fixture.")]
[EventTopic<ShowcaseLocation>(domain: "comprehensive-domain", topic: "comprehensive-topic", version: "v9", Internal = true)]
public sealed record AllPropertiesShowcaseEvent(
    [PartitionKey(Order = 0)] Guid TenantId,
    [PartitionKey(Order = 1)] int SequenceNumber,
    ShowcaseLocation PrimaryLocation,
    List<ShowcaseLocation> RelatedLocations
);

/// <summary>
///     A complex nested type referenced by <see cref="AllPropertiesShowcaseEvent"/>, both directly
///     and inside a collection, so both reference-schema rendering paths are exercised.
/// </summary>
public record ShowcaseLocation
{
    /// <summary>Unique identifier for the location</summary>
    public required Guid Id { get; init; }

    /// <summary>Display name of the location</summary>
    public required string Name { get; init; }
}
