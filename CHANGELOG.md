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

### Fixed

- **EventMarkdownGenerator**: property descriptions whose XML doc summary wrapped onto multiple lines
  previously spilled out of their table cell as an orphaned line.
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
