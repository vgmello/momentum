# Changelog

All notable changes to this project are documented in this file, grouped by date.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/).

## [2026-07-15]

### Added

- **EventMarkdownGenerator**: `Topic` and `FullyQualifiedTopicName` are now exposed separately on `EventMetadata`
  and `EventViewModel`. `Topic` is the plain topic / event hub name; `FullyQualifiedTopicName` is the composed
  `{env}.{domain}.{visibility}.{topic}.{version}` convention string.
- **EventMarkdownGenerator**: `--partition-key-attribute` option to discover partition keys by attribute name
  (or name prefix), mirroring `--event-attribute`. Partition key discovery and its `Order` are now resolved
  generically via reflection instead of a hardcoded `PartitionKeyAttribute` type.
- **EventMarkdownGenerator**: event attributes may expose an optional `EventName` property to override the
  documented event name (falls back to the CLR type name when absent).
- **Docs**: new VitePress guide page for the Event Documentation Generator
  (`libs/Momentum/docs/guide/messaging/event-documentation.md`), covering MSBuild/CLI usage, template
  customization, and generated output structure.

### Changed

- **EventMarkdownGenerator**: `EventMetadata.TopicName` / `EventViewModel.TopicName` replaced by
  `FullyQualifiedTopicName`. The default `event.liquid` template now renders both `Topic` and
  `Fully Qualified Topic`.
- **EventMarkdownGenerator**: multi-line XML doc property descriptions are flattened to a single line
  (whitespace collapsed) so they no longer break the generated markdown payload table.

### Added

- **EventMarkdownGenerator**: `EventMetadata.EventTypeName` exposes the event's CLR type name
  separately from `EventName` (which may be overridden via the topic attribute's `EventName`
  property). Rendered in generated docs as "Type Name".
- **EventMarkdownGenerator**: `EventMetadata.AttributeProperties` captures every public property of
  the discovered topic attribute (via reflection) into a name/value dictionary, so custom attribute
  properties the generator has no dedicated field for still surface in generated docs, under
  "Attribute Properties".

### Changed

- **EventMarkdownGenerator**: extracted `EventMetadataBuilder` from `AssemblyEventDiscovery` so
  assembly/type scanning stays independent from reflecting a single event type and its topic
  attribute into `EventMetadata`.
- **EventMarkdownGenerator**: extracted `EventPropertyMetadataBuilder` from `EventMetadataBuilder` so
  reflecting an event type's own properties/partition keys stays independent from resolving its topic
  attribute and computed fields (topic, domain, fully-qualified topic name, etc).
- **EventMarkdownGenerator**: moved generic reflection helpers (`FindAttributeByName`,
  `GetPropertyValue<T>`, `MapConstructorParametersToProperties`) that carry no event-metadata-specific
  meaning from `EventMetadataBuilder` into `TypeUtils`. Also removed a private kebab-case fallback
  that duplicated the `Momentum.Extensions.Abstractions.Extensions.ToKebabCase()` extension already
  imported in the same file.

### Tests

- **EventMarkdownGenerator**: added an `all-properties-showcase` scenario (real `TestEvents` fixture
  and XML doc input, checked into `IntegrationTestScenarios/`) that exercises every renderable
  `EventMetadata` property at once — obsolete marker, explicit domain/topic/version, internal
  visibility, multiple partition keys, a complex nested property, a collection of a complex type,
  and real Summary/Remarks/Example/param documentation — as a durable, reviewable example alongside
  the existing marker-value completeness unit test.
- **EventMarkdownGenerator**: `ScenarioBasedIntegrationTests` now supports a per-scenario
  `templates/event.liquid` override (falling back to the default embedded template when absent).
  Used by `all-properties-showcase`, whose override renders every single `EventViewModel` field
  verbatim — including every entry of `Properties`, `PartitionKeys`, and `AttributeProperties` — as a
  raw, exhaustive dump distinct from the polished default rendering.

### Added

- **EventMarkdownGenerator**: `EventMetadata.Entity` is now computed once at metadata-build time
  (moved off `EventViewModelFactory`). When the topic attribute isn't generic (no `TEntity` to
  reflect), it falls back to stripping a common event-verb suffix (`Created`, `Updated`, `Deleted`,
  `Completed`, `Processed`, etc.) off the event type's own name, e.g. `WidgetCreated` → `widget`.
- **EventMarkdownGenerator**: `EventMetadata.EventNameKebab` exposes a kebab-cased form of
  `EventName` (respecting the same `EventName` attribute override) for topic-adjacent/URL-safe uses,
  without kebab-casing the human-facing `EventName`/heading itself. Rendered in generated docs as
  "Event Slug".

### Changed

- **EventMarkdownGenerator**: `AssemblyEventDiscovery.DiscoverEvents` now returns
  `IEnumerable<EventWithDocumentation>` (metadata paired with its XML documentation) instead of bare
  `EventMetadata`, doing the `xmlParser.GetEventDocumentation(...)` lookup internally. Removes the
  repeated `events.Select(m => new EventWithDocumentation { ... })` boilerplate every caller
  (`GenerateCommand`, tests) previously had to write itself.
- **EventMarkdownGenerator**: removed `EventMetadata.EventType` — the raw CLR `Type` was only ever
  used to re-look-up XML documentation or extract `Entity`, both of which now happen once during
  metadata construction instead of being deferred to callers.
- **EventMarkdownGenerator**: `FullyQualifiedTopicName` no longer includes a leading `{env}.`
  placeholder segment. It was never substituted by the doc generator (a design-time tool with no
  concept of a deployment environment), so it only ever showed up as a literal, unresolved token in
  generated docs. Now composed as `{domain}.{visibility}.{topic}.{version}`.

### Fixed

- **EventMarkdownGenerator**: `FullyQualifiedTopicName` used the assembly-level default domain
  instead of the event's actually-resolved domain (explicit attribute `Domain` override, then
  namespace-derived, then the default) — so an event could render `Domain: comprehensive-domain` in
  one field while its fully-qualified topic string still showed the unrelated default domain segment.
  Both fields now agree.
- **EventMarkdownGenerator**: the domain segment of `FullyQualifiedTopicName` was only
  lowercased (`ToLowerInvariant()`), not kebab-cased, so a multi-word domain like `TestEvents`
  produced the glued `testevents` instead of `test-events`. Now uses `ToKebabCase()`, consistent
  with how `Topic` and `Entity` are derived.
- **EventMarkdownGenerator**: property descriptions whose XML doc summary wrapped onto multiple lines
  previously spilled out of their table cell as an orphaned line.
- **EventMarkdownGenerator**: `EventMetadata.Domain` was computed but never mapped onto the Liquid
  view model, so it never appeared in generated docs. Now rendered as "Domain".
- **EventMarkdownGenerator**: the `TopicAttribute` field on `EventMetadata` could point at a
  different attribute instance than the one topic/domain/version were actually parsed from (its own
  lookup used `GetCustomAttributes<Attribute>().FirstOrDefault()`, which matches _any_ attribute, not
  specifically the topic one). It now reuses the single correctly-matched instance throughout.
- **DummyInvoiceGenerator** (sample BackOffice service): resolved a DI lifetime crash on startup —
  the singleton `BackgroundService` was constructor-injecting the scoped `Wolverine.IMessageBus`
  directly, which fails ASP.NET Core's DI validation. It now resolves `IMessageBus` from a new
  `IServiceScopeFactory`-created scope on each publish iteration.
- **AppDomain.BackOffice.Orleans**: fixed a startup crash where resolving `GrainDirectory:Default`
  wrote an incomplete provider section (`ServiceKey` with no `ProviderType`) into `IConfiguration`,
  which Orleans' own configuration-driven provider discovery then tried and failed to auto-register.
  Grain directory service-name resolution is now read-only, since it's configured purely through the
  fluent `AddAzureTableGrainDirectory` call.
- **AppDomain.BackOffice.Orleans**: fixed Azure Table clustering failing with "No credentials
  specified" under Docker Compose — the `Clustering:ServiceKey` / `GrainStorage:Default:ServiceKey`
  values must be written back into `IConfiguration` for Orleans to bind its declarative provider to
  the correct keyed Azure client, which a prior fix attempt had inadvertently removed.
- **compose.yml**: Azurite now starts with `--skipApiVersionCheck` — the Azure SDK clients request
  the latest Azure Storage service API version, which is routinely ahead of what Azurite has added
  support for, breaking local Orleans clustering/grain-storage/grain-directory calls out of the box.

### Security

- **docs-dotnet.ts**: the `docfx metadata` build step now resolves the `docfx` executable from the
  fixed `.NET` global-tools directory instead of an unpinned `PATH` lookup, closing a SonarCloud
  security hotspot (`typescript:S4036`).
