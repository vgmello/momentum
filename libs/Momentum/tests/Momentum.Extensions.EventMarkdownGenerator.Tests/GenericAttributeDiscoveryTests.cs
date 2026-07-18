// Copyright (c) Momentum .NET. All rights reserved.

using Momentum.Extensions.EventMarkdownGenerator.Services;
using Shouldly;
using System.Reflection;
using Xunit;

namespace Momentum.Extensions.EventMarkdownGenerator.Tests;

/// <summary>
///     Verifies that event, partition-key, and event-name discovery all resolve attributes by name
///     (configurable convention) rather than by a hardcoded compiled attribute type.
/// </summary>
public class GenericAttributeDiscoveryTests
{
    // Custom, non-Momentum attribute types that only match by naming convention.
    // Members are read via reflection by the generator, so they are public to avoid unused-private warnings.
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class ConventionEventTopicAttribute : Attribute
    {
        public string? Domain { get; init; }
        public string? Subdomain { get; init; }
        public string Version { get; init; } = "v1";
        public bool Internal { get; init; }
        public string? Topic { get; init; }
        public string? EventName { get; init; }
        public bool CollapseTopicOnDomain { get; init; } = true;
    }

    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter)]
    public sealed class ConventionPartitionKeyAttribute : Attribute
    {
        public int Order { get; init; }
    }

    [ConventionEventTopic(Domain = "reservations", Topic = "reservation-created", EventName = "ReservationBooked")]
    public record ConventionEvent(
        [property: ConventionPartitionKey(Order = 1)] Guid ReservationId,
        [property: ConventionPartitionKey(Order = 0)] Guid TenantId,
        string Notes);

    [Fact]
    public void DiscoverEvents_WithCustomAttributeNames_DiscoversPartitionKeysAndEventNameOverride()
    {
        var assembly = Assembly.GetExecutingAssembly();

        var events = AssemblyEventDiscovery.DiscoverEvents(
            assembly,
            xmlParser: null,
            PayloadSizeCalculator.Create("json"),
            attributeNamePrefix: nameof(ConventionEventTopicAttribute),
            partitionKeyAttributeNamePrefix: nameof(ConventionPartitionKeyAttribute)).Select(e => e.Metadata).ToList();

        var conventionEvent = events.Single(e => e.FullTypeName == typeof(ConventionEvent).FullName);

        // Ask #4: EventName override read from the attribute instead of the CLR type name.
        conventionEvent.EventName.ShouldBe("ReservationBooked");

        // Ask #3: Topic exposes the plain topic/hub name; the fully-qualified name keeps the composed convention.
        // Public events omit the visibility segment by default (emitPublicVisibility defaults to false).
        conventionEvent.Topic.ShouldBe("reservation-created");
        conventionEvent.FullyQualifiedTopicName.ShouldBe("reservations.reservation-created.v1");

        // Ask #2: partition keys discovered via the custom attribute, ordered by Order.
        conventionEvent.PartitionKeys.Count.ShouldBe(2);
        conventionEvent.PartitionKeys[0].Name.ShouldBe("TenantId");
        conventionEvent.PartitionKeys[0].Order.ShouldBe(0);
        conventionEvent.PartitionKeys[1].Name.ShouldBe("ReservationId");
        conventionEvent.PartitionKeys[1].Order.ShouldBe(1);
    }

    [ConventionEventTopic(Domain = "sales")]
    public record OrderCreated(Guid OrderId);

    [Fact]
    public void DiscoverEvents_WithNoTopic_DerivesTopicFromPluralizedEntityNotEventName()
    {
        var assembly = Assembly.GetExecutingAssembly();

        var events = AssemblyEventDiscovery.DiscoverEvents(
            assembly,
            xmlParser: null,
            PayloadSizeCalculator.Create("json"),
            attributeNamePrefix: nameof(ConventionEventTopicAttribute)).Select(e => e.Metadata).ToList();

        var orderCreated = events.Single(e => e.FullTypeName == typeof(OrderCreated).FullName);

        // No Topic set: the fallback derives from the pluralized, kebab-cased Entity ("Order" -> "orders"),
        // not the event name ("OrderCreated" -> "order-created") and not the raw CLR type name.
        orderCreated.Entity.ShouldBe("Order");
        orderCreated.Topic.ShouldBe("orders");
        orderCreated.FullyQualifiedTopicName.ShouldBe("sales.orders.v1");
    }

    [ConventionEventTopic(Domain = "orders")]
    public record OrderApproved(Guid OrderId);

    [Fact]
    public void DiscoverEvents_WithNoTopicAndDomainMatchingEntity_CollapsesDuplicateSegment()
    {
        var assembly = Assembly.GetExecutingAssembly();

        var events = AssemblyEventDiscovery.DiscoverEvents(
            assembly,
            xmlParser: null,
            PayloadSizeCalculator.Create("json"),
            attributeNamePrefix: nameof(ConventionEventTopicAttribute)).Select(e => e.Metadata).ToList();

        var orderApproved = events.Single(e => e.FullTypeName == typeof(OrderApproved).FullName);

        // Domain "orders" (no subdomain) and the auto-derived topic ("Order" -> "orders") both kebab to
        // the same string. Topic itself still reports "orders", but the fully-qualified name must not
        // repeat the segment ("orders.v1", not "orders.orders.v1").
        orderApproved.Entity.ShouldBe("Order");
        orderApproved.Topic.ShouldBe("orders");
        orderApproved.FullyQualifiedTopicName.ShouldBe("orders.v1");
    }

    [ConventionEventTopic(Domain = "orders", Topic = "orders")]
    public record OrderRefunded(Guid OrderId);

    [Fact]
    public void DiscoverEvents_WithExplicitTopicMatchingDomain_CollapsesByDefault()
    {
        var assembly = Assembly.GetExecutingAssembly();

        var events = AssemblyEventDiscovery.DiscoverEvents(
            assembly,
            xmlParser: null,
            PayloadSizeCalculator.Create("json"),
            attributeNamePrefix: nameof(ConventionEventTopicAttribute)).Select(e => e.Metadata).ToList();

        var orderRefunded = events.Single(e => e.FullTypeName == typeof(OrderRefunded).FullName);

        // CollapseTopicOnDomain defaults to true regardless of whether Topic was explicit or auto-derived
        // from the entity — here it's explicit ("orders") and still collapses because it matches the
        // domain path exactly.
        orderRefunded.Topic.ShouldBe("orders");
        orderRefunded.FullyQualifiedTopicName.ShouldBe("orders.v1");
    }

    [ConventionEventTopic(Domain = "orders", Topic = "orders", CollapseTopicOnDomain = false)]
    public record OrderIssued(Guid OrderId);

    [Fact]
    public void DiscoverEvents_WithCollapseTopicOnDomainFalse_KeepsDuplicateSegment()
    {
        var assembly = Assembly.GetExecutingAssembly();

        var events = AssemblyEventDiscovery.DiscoverEvents(
            assembly,
            xmlParser: null,
            PayloadSizeCalculator.Create("json"),
            attributeNamePrefix: nameof(ConventionEventTopicAttribute)).Select(e => e.Metadata).ToList();

        var orderIssued = events.Single(e => e.FullTypeName == typeof(OrderIssued).FullName);

        // Explicitly opting out (CollapseTopicOnDomain = false) keeps both segments even though topic and
        // domain are identical — the caller's choice always wins over the default.
        orderIssued.Topic.ShouldBe("orders");
        orderIssued.FullyQualifiedTopicName.ShouldBe("orders.orders.v1");
    }

    [ConventionEventTopic(Domain = "commerce", Subdomain = "orders")]
    public record OrderSubmitted(Guid OrderId);

    [Fact]
    public void DiscoverEvents_WithNoTopicAndSubdomainMatchingEntity_DoesNotCollapse()
    {
        var assembly = Assembly.GetExecutingAssembly();

        var events = AssemblyEventDiscovery.DiscoverEvents(
            assembly,
            xmlParser: null,
            PayloadSizeCalculator.Create("json"),
            attributeNamePrefix: nameof(ConventionEventTopicAttribute)).Select(e => e.Metadata).ToList();

        var orderSubmitted = events.Single(e => e.FullTypeName == typeof(OrderSubmitted).FullName);

        // The dedupe only collapses when the topic duplicates the FULL domain[.subdomain] path. Here the
        // subdomain alone ("orders") matches the auto-derived topic, but the domain ("commerce") doesn't,
        // so the full path "commerce.orders" != "orders" and the topic segment is NOT dropped — unlike
        // DiscoverEvents_WithNoTopicAndDomainMatchingEntity_CollapsesDuplicateSegment, where domain alone
        // (no subdomain) matches and the segment IS dropped.
        orderSubmitted.Entity.ShouldBe("Order");
        orderSubmitted.Topic.ShouldBe("orders");
        orderSubmitted.Subdomain.ShouldBe("orders");
        orderSubmitted.FullyQualifiedTopicName.ShouldBe("commerce.orders.orders.v1");
    }

    // Custom attribute exercising a property the generator has no dedicated EventMetadata field for
    // (Notes), plus an explicitly empty Topic to verify the entity-fallback still applies.
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class CustomAttribute : Attribute
    {
        public string Domain { get; init; } = string.Empty;
        public string Notes { get; init; } = string.Empty;
        public string Topic { get; init; } = string.Empty;
    }

    [Custom(Domain = "billing", Notes = "invoicing", Topic = "")]
    public record CustomAttributeEvent(Guid InvoiceId);

    [Fact]
    public void DiscoverEvents_WithCustomAttribute_PopulatesAttributePropertiesDictionaryAndFallsBackTopic()
    {
        var assembly = Assembly.GetExecutingAssembly();

        var events = AssemblyEventDiscovery.DiscoverEvents(
            assembly,
            xmlParser: null,
            PayloadSizeCalculator.Create("json"),
            attributeNamePrefix: nameof(CustomAttribute)).Select(e => e.Metadata).ToList();

        var customEvent = events.Single(e => e.FullTypeName == typeof(CustomAttributeEvent).FullName);

        // Empty Topic falls back to the pluralized, kebab-cased Entity, same as a missing Topic would.
        // CustomAttribute has no TEntity to reflect, so Entity is derived by stripping the trailing
        // "Event" word from the CLR type name ("CustomAttributeEvent" -> "CustomAttribute" -> "custom-attributes").
        customEvent.Topic.ShouldBe("custom-attributes");

        // Every property of the attribute is captured, including ones with no dedicated EventMetadata
        // field (Notes) — not just the well-known ones (Domain).
        customEvent.AttributeProperties.Count.ShouldBe(3);
        customEvent.AttributeProperties["Domain"].ShouldBe("billing");
        customEvent.AttributeProperties["Notes"].ShouldBe("invoicing");
        customEvent.AttributeProperties["Topic"].ShouldBe(string.Empty);
    }

    [ConventionEventTopic(Domain = "widgets", Topic = "widget-created")]
    public record WidgetCreated(Guid WidgetId);

    [ConventionEventTopic(Domain = "orders", Topic = "order-completed-event")]
    public record OrderCompletedEvent(Guid OrderId);

    [ConventionEventTopic(Domain = "gadgets", Topic = "gadget-event")]
    public record GadgetEvent(Guid GadgetId);

    [Fact]
    public void DiscoverEvents_WithNonGenericAttribute_InfersEntityFromEventNameSuffix()
    {
        var assembly = Assembly.GetExecutingAssembly();

        var events = AssemblyEventDiscovery.DiscoverEvents(
            assembly,
            xmlParser: null,
            PayloadSizeCalculator.Create("json"),
            attributeNamePrefix: nameof(ConventionEventTopicAttribute)).Select(e => e.Metadata).ToList();

        // ConventionEventTopicAttribute is non-generic (no TEntity), so Entity can only come from
        // stripping a known event-verb suffix off the type's own name.
        var widgetCreated = events.Single(e => e.FullTypeName == typeof(WidgetCreated).FullName);
        widgetCreated.Entity.ShouldBe("Widget");

        // A trailing "Event" word is trimmed BEFORE suffix matching, so the "Completed" suffix still
        // matches against "OrderCompleted" rather than failing against the untrimmed "OrderCompletedEvent".
        var orderCompletedEvent = events.Single(e => e.FullTypeName == typeof(OrderCompletedEvent).FullName);
        orderCompletedEvent.Entity.ShouldBe("Order");

        // "Event" trimmed, remainder ("Gadget") matches no known suffix, so Entity falls back to the
        // trimmed name rather than the untrimmed "GadgetEvent".
        var gadgetEvent = events.Single(e => e.FullTypeName == typeof(GadgetEvent).FullName);
        gadgetEvent.Entity.ShouldBe("Gadget");

        // ConventionEvent's name (after trimming the trailing "Event" word) matches no known suffix, so
        // Entity falls back to the trimmed event type name.
        var conventionEvent = events.Single(e => e.FullTypeName == typeof(ConventionEvent).FullName);
        conventionEvent.Entity.ShouldBe("Convention");
    }
}
