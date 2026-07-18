// Copyright (c) Momentum .NET. All rights reserved.

using Momentum.Extensions.EventMarkdownGenerator.Models;

namespace Momentum.Extensions.EventMarkdownGenerator.Extensions;

/// <summary>
///     Output-organization helpers for <see cref="EventWithDocumentation"/>. Kept off <see cref="EventMetadata"/>
///     itself, which stays a plain data carrier reused well beyond the markdown-generation pipeline (e.g. the
///     raw AttributeProperties reflection dump) — deciding which output subfolder an event belongs under is a
///     generation-pipeline concern, not a property of the event.
/// </summary>
public static class EventWithDocumentationExtensions
{
    /// <summary>
    ///     The output subfolder an event's markdown belongs under: <c>domain_events</c> for events marked
    ///     <c>Internal</c> on their topic attribute, <c>integration_events</c> otherwise. Reuses
    ///     <see cref="EventMetadata.IsInternal"/> (rather than inspecting the namespace for an
    ///     IntegrationEvents/DomainEvents segment) so placement always matches the "Domain Events" sidebar
    ///     grouping <see cref="Momentum.Extensions.EventMarkdownGenerator.Services.JsonSidebarGenerator"/>
    ///     already produces from the same flag.
    /// </summary>
    public static string GetEventKindFolder(this EventWithDocumentation eventWithDoc) =>
        eventWithDoc.Metadata.IsInternal ? "domain_events" : "integration_events";
}
