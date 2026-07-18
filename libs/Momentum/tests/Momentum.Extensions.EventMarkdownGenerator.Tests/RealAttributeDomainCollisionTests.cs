// Copyright (c) Momentum .NET. All rights reserved.

using Momentum.Extensions.Abstractions.Messaging;
using Momentum.Extensions.EventMarkdownGenerator.Services;
using Shouldly;
using System.Reflection;
using Xunit;

namespace Momentum.Extensions.EventMarkdownGenerator.Tests;

/// <summary>
///     Same domain/topic dedupe scenario as GenericAttributeDiscoveryTests' Order* cases, but through the
///     real, compiled <see cref="EventTopicAttribute{TEntity}"/> — not the test-local mock convention
///     attribute — to prove the collapse also fires for actual template consumers.
///
///     These fixtures live in THIS test project (not TestEvents) deliberately: TestEvents is scanned
///     wholesale by several other tests (sidebar generation, reference-format comparisons) that assert
///     exact event/domain counts, so adding a real [EventTopic&lt;T&gt;] fixture there — especially one with
///     its own distinct "orders" domain — would silently inflate those counts. Discovery here is filtered
///     to this one type, so it can't affect them.
/// </summary>
public class RealAttributeDomainCollisionTests
{
    /// <summary>Minimal entity backing the real-attribute domain-collision fixture below.</summary>
    public record Order
    {
        public required Guid Id { get; init; }
    }

    // No explicit topic: EventTopicAttribute<Order> auto-derives "orders" from the entity name. Domain is
    // explicitly "orders" too, with no subdomain, so domain and auto-derived topic collide.
    [EventTopic<Order>(domain: "orders")]
    public sealed record OrderPlaced([PartitionKey] Guid OrderId);

    [Fact]
    public void DiscoverEvents_RealEventTopicAttributeWithDomainMatchingEntity_CollapsesDuplicateSegment()
    {
        var assembly = Assembly.GetExecutingAssembly();

        var events = AssemblyEventDiscovery.DiscoverEvents(
            assembly,
            xmlParser: null,
            PayloadSizeCalculator.Create("json")).Select(e => e.Metadata).ToList();

        var orderPlaced = events.Single(e => e.FullTypeName == typeof(OrderPlaced).FullName);

        orderPlaced.Entity.ShouldBe("Order");
        orderPlaced.Topic.ShouldBe("orders");
        orderPlaced.Domain.ShouldBe("orders");
        orderPlaced.Subdomain.ShouldBeNull();

        // Without the dedupe this would be "orders.orders.v1".
        orderPlaced.FullyQualifiedTopicName.ShouldBe("orders.v1");
    }
}
