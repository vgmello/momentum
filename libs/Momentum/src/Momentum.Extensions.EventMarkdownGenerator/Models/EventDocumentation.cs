// Copyright (c) Momentum .NET. All rights reserved.

namespace Momentum.Extensions.EventMarkdownGenerator.Models;

public record EventDocumentation
{
    public required string Summary { get; init; }

    public string? Remarks { get; init; }

    public string? Example { get; init; }

    public Dictionary<string, string> PropertyDescriptions { get; init; } = [];

    public string GetDescription() => !string.IsNullOrEmpty(Summary) ? Summary : "No description available";
}

public record EventWithDocumentation
{
    public required EventMetadata Metadata { get; init; }

    public required EventDocumentation Documentation { get; init; }

    /// <summary>
    ///     The output subfolder this event's markdown belongs under: <c>domain_events</c> for events marked
    ///     <c>Internal</c> on their topic attribute, <c>integration_events</c> otherwise. Reuses
    ///     <see cref="EventMetadata.IsInternal"/> (rather than inspecting the namespace for an
    ///     IntegrationEvents/DomainEvents segment) so placement always matches the "Domain Events" sidebar
    ///     grouping <see cref="Momentum.Extensions.EventMarkdownGenerator.Services.JsonSidebarGenerator"/>
    ///     already produces from the same flag. Lives
    ///     here rather than on <see cref="EventMetadata"/> since it's a generation-pipeline output-layout
    ///     decision, not a property of the event itself.
    /// </summary>
    public string GetEventKindFolder() => Metadata.IsInternal ? "domain_events" : "integration_events";
}
