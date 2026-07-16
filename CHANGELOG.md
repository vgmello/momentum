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

### Fixed

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
