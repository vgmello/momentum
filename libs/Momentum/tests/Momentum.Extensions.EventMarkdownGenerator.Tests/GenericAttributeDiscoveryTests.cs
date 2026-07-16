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
        public string Version { get; init; } = "v1";
        public bool Internal { get; init; }
        public string? Topic { get; init; }
        public string? EventName { get; init; }
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
        conventionEvent.Topic.ShouldBe("reservation-created");
        conventionEvent.FullyQualifiedTopicName.ShouldBe("reservations.public.reservation-created.v1");

        // Ask #2: partition keys discovered via the custom attribute, ordered by Order.
        conventionEvent.PartitionKeys.Count.ShouldBe(2);
        conventionEvent.PartitionKeys[0].Name.ShouldBe("TenantId");
        conventionEvent.PartitionKeys[0].Order.ShouldBe(0);
        conventionEvent.PartitionKeys[1].Name.ShouldBe("ReservationId");
        conventionEvent.PartitionKeys[1].Order.ShouldBe(1);
    }

    // Custom attribute exercising properties the generator has no dedicated EventMetadata field for
    // (Subdomain), plus an explicitly empty Topic to verify the kebab-case fallback still applies.
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class CustomAttribute : Attribute
    {
        public string Domain { get; init; } = string.Empty;
        public string Subdomain { get; init; } = string.Empty;
        public string Topic { get; init; } = string.Empty;
    }

    [Custom(Domain = "billing", Subdomain = "invoicing", Topic = "")]
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

        // Empty Topic falls back to the kebab-cased CLR type name, same as a missing Topic would.
        customEvent.Topic.ShouldBe("custom-attribute-event");

        // Every property of the attribute is captured, including ones with no dedicated EventMetadata
        // field (Subdomain) — not just the well-known ones (Domain).
        customEvent.AttributeProperties.Count.ShouldBe(3);
        customEvent.AttributeProperties["Domain"].ShouldBe("billing");
        customEvent.AttributeProperties["Subdomain"].ShouldBe("invoicing");
        customEvent.AttributeProperties["Topic"].ShouldBe(string.Empty);
    }

    [ConventionEventTopic(Domain = "widgets", Topic = "widget-created")]
    public record WidgetCreated(Guid WidgetId);

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
        widgetCreated.Entity.ShouldBe("widget");

        // ConventionEvent's name matches no known suffix, so Entity stays empty rather than guessing.
        var conventionEvent = events.Single(e => e.FullTypeName == typeof(ConventionEvent).FullName);
        conventionEvent.Entity.ShouldBe(string.Empty);
    }
}
