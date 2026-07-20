# Changelog

All notable changes to this project are documented in this file, grouped by date.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/).

## [2026-07-20]

### Changed

- **Infra**: PostgreSQL bumped **17 → 18** across compose, the Aspire AppHost, and the Testcontainers
  integration fixture. PostgreSQL 18's official image moves the cluster from `/var/lib/postgresql/data`
  to `/var/lib/postgresql/{major}/docker`, so the compose volume now mounts the whole
  `/var/lib/postgresql` tree instead of `.../data` (this also enables future major upgrades with
  `pg_upgrade --link`).
  **Action required for local dev:** an existing `postgres_data` volume holds a PG17 cluster that PG18
  will not start against — run `docker compose down -v` before the first `up`. Local data is
  disposable; Liquibase recreates the schema.
- **Deps**: `WolverineFx`/`.Kafka`/`.Postgresql`/`.RuntimeCompilation` `6.20.0` → **6.21.0**. Headline
  is durable-inbox listener batching, which directly benefits the durable inbox enabled yesterday
  (upstream measured a 2,000 msg/s Kafka stream going from unbounded backlog to a steady 32ms delivery
  p50, +83% sustained durable throughput), plus a sender-batching flush fix and a faster/lower-allocation
  Kafka mapping hot path. Two upstream behavior changes to note: the per-message "success" log now
  defaults to `Debug` (was `Information`), and `wolverine-execution-time` became a floating-point
  histogram (same name/unit, different point type — check dashboards built on it).
- **Deps**: `CloudNative.CloudEvents.Kafka` `2.8.0` → **3.9.0** and `CloudNative.CloudEvents.SystemTextJson`
  `2.8.0` → **2.9.0**. This resolves a latent version mismatch: the 2.8.0 Kafka package declares
  `Confluent.Kafka 1.9.3` while `WolverineFx.Kafka` pulls `2.14.x`, so `CloudEventMapper` was compiled
  against a five-major-versions-old client API and unified upward at runtime. 3.9.0 targets
  `Confluent.Kafka 2.14.2`, matching Wolverine exactly. (The 2.x → 3.x jump is the Kafka package's own
  version line; core `CloudNative.CloudEvents` remains 2.9.0.)
- **Deps**: OpenTelemetry family → **1.17.0** (exporter, hosting, and the AspNetCore/Http/Runtime
  instrumentation packages, now all driven by one property); `Microsoft.Extensions.Http.Resilience`,
  `.ServiceDiscovery`, `.Diagnostics.Testing` → **10.8.0**; `Microsoft.NET.Test.Sdk` → **18.8.1**;
  `SonarAnalyzer.CSharp` → **10.29.0.143774**; `Microsoft.SourceLink.GitHub` → **10.0.301**;
  `Scalar.AspNetCore` → **2.16.15**; `Microsoft.CodeAnalysis.Analyzers` → **5.6.0** (the 5.0.0 pin was
  already being overridden transitively to 5.3.0).
- **Infra**: Kafka image `confluentinc/cp-kafka` `7.6.0` → **7.9.8** in compose and the integration
  fixture. 8.x was evaluated and rejected for now: Testcontainers 4.13's `KafkaBuilder` does not set
  `KAFKA_PROCESS_ROLES`, which the 8.x entrypoint requires, so every integration test fails to start
  the broker. Revisit when Testcontainers adds 8.x support.

### Fixed

- **Tests**: the integration fixture set `Aspire:Confluent:Kafka:Messaging:Consumer:Config:EnableAutoCommit=true`,
  the same setting removed from `appsettings` yesterday — it suppressed Wolverine's at-least-once offset
  management, so the tests were not exercising the delivery guarantee the template ships. Removed.

### Notes

- `Refitter.MSBuild` is **held at 2.0.0**: 2.1.0's generator throws
  `Method not found: System.Text.ValueStringBuilder.AsSpan()` against the current runtime and fails the
  E2E client generation at build time.
- Still outstanding (unchanged): `OpenTelemetry.Instrumentation.GrpcCore 1.0.0-beta.13` is abandoned
  upstream and tied to the end-of-life `Grpc.Core` library; it should be removed if the API only uses
  grpc-dotnet. `dpage/pgadmin4:latest` and `azurite:latest` still float their tags, and there is no
  `global.json` pinning the SDK.

## [2026-07-19]

### Changed

- **Messaging topology**: Wolverine message persistence (inbox/outbox) and the PostgreSQL queue
  transport now live in the **application database** (`app_domain`) instead of a separate
  `service_bus` database — the `ServiceBus` connection string now points at the application database
  everywhere (appsettings, compose, AppHost, integration tests). Wolverine does not support splitting
  the transport from the message store, and a separate database made the outbox non-atomic with
  business writes; co-locating them (per-service `svcbus_{service}` persistence schema + `svcbus_queues` schema alongside
  `main`) removes the crash window in which committed business data could lose its outgoing events.
  The `service_bus` database, its Liquibase changelog, and `liquibase.servicebus.properties` were
  removed; the `svcbus_queues` schema is now pre-created by the `app_domain` changelog. The AppHost passes
  the `ServiceBus` connection name via `WithReference(database, connectionName: "ServiceBus")`.
- **Messaging routing**: removed `ConventionalLocalRoutingIsAdditive()`. An integration event that a
  service both publishes and handles was previously processed twice — once via local routing at
  publish time and again when consumed back from its own Kafka subscription. With the default
  (non-additive) routing, such events go only to the external transport and are processed exactly
  once on consumption; messages without explicit external routes (commands, queries, local events)
  still route locally as before.
- **ServiceDefaults/Messaging**: Wolverine schema names now carry a `svcbus_` prefix so messaging
  infrastructure is clearly distinguishable from application schemas in the shared database — the
  per-service persistence schema is `svcbus_{service}` (e.g. `svcbus_appdomain_api`) and the
  PostgreSQL transport schema is `svcbus_queues` (was `queues`).
- **ServiceDefaults/Messaging**: `Envelope.GetMessageName(fullName: true)` caches the sanitized full type
  name per message type (previously four `string.Replace` allocations per processed message via the OTel
  and performance middlewares).

### Added

- **Template**: new `TransactionalOutboxMiddleware` (applied to all `ICommand<>` handler chains via
  the AppDomain Wolverine extension) makes command handling fully atomic: it opens a single
  transaction on the application database, exposes it ambiently (`TransactionalOutbox.CurrentTransaction`),
  `AppDomainDb` (LinqToDB) instances resolved during the message execution attach to that transaction,
  and Wolverine's outbox is enlisted in it (`MessageContext.EnlistInOutboxAsync` +
  `DatabaseEnvelopeTransaction`), so cascaded integration events are persisted to the outgoing
  envelope table in the same transaction as the business writes — one commit covers both, and
  failures roll back both. Nested command invocations (the DbCommand pattern) join the ambient
  transaction instead of opening their own.

- **ServiceDefaults/Messaging**: `ConfigureReliableMessaging` now installs a default failure policy —
  transient `NpgsqlException`s and `TimeoutException`s get two quick in-process retries (50ms/250ms),
  then two durable **scheduled retries** (5s/30s) that release the listener instead of blocking it (so
  a cooling-down message never stalls a Kafka partition), then the dead letter queue. Previously no
  retry rules existed anywhere, so any transient failure dead-lettered on first attempt despite the
  docs claiming retry support. Application failure rules (added via the `AddServiceBus` configure
  callback) still take precedence.
- **ServiceDefaults/Messaging**: reliable messaging now also enables the **durable inbox on all
  listening endpoints** (`UseDurableInboxOnAllListeners`) — incoming Kafka messages are persisted
  before processing, giving at-least-once delivery with duplicate detection by message id, and failed
  messages land in the replayable database dead-letter table.
- **Docs**: new dead-letter operations guide in `guide/messaging/wolverine.md` — inspect
  (`storage counts`), replay (`storage replay [--exception-type ...]`), and programmatic
  `IDeadLetterAdminService` usage — closing the "no DLQ recovery story" gap.

### Fixed

- **Kafka at-least-once delivery**: removed `EnableAutoCommit: true` / `AutoCommitIntervalMs` from the
  template's Kafka consumer configuration. Under Wolverine 6, an explicit `EnableAutoCommit=true`
  suppresses Wolverine's commit management (`KafkaOffsetCommitter.ResolveStrategy`) and falls back to
  librdkafka storing offsets **at consume time** — a crash during message processing lost the message
  (the failure mode JasperFx/wolverine#2114 was opened against). With the keys removed, Wolverine's
  default `CommitMode.StoreThenAutoFlush` applies: `EnableAutoOffsetStore=false`, offsets stored only
  after successful processing, and the commit watermark never advances past an in-flight message.
  `KafkaWolverineExtensions` now logs a startup warning if configuration reintroduces the unsafe
  combination.

- **ServiceDefaults/Messaging**: `AutoProvision`/`AutoBuildMessageStorageOnStartup` are now read from the
  `ServiceBus:Wolverine` section — the same path `appsettings.json` and the Kafka extension already use.
  Previously `WolverineSetupExtensions` read `ServiceBus:AutoProvision`, a key nothing sets, so
  `AutoBuildMessageStorageOnStartup` was forced to `None` in every environment.
- **ServiceDefaults/Messaging**: a `ServiceName` set explicitly in the `AddServiceBus` configure callback
  is no longer silently overwritten by the `ServiceBus:PublicServiceName`-derived default (which also
  feeds the Postgres persistence schema name).
- **ServiceDefaults/Messaging**: integration-event publisher discovery now unions the explicitly marked
  `[DomainAssembly]` assemblies (force-loaded via their type markers) into the loaded-assembly sweep, and
  prefix-matches on full namespace segments (`Name` or `Name.`) instead of raw `StartsWith`. Fixes events
  being missed when a referenced contracts assembly had not been lazily loaded yet, and accidental matches
  of unrelated assemblies sharing a name prefix.
- **Kafka**: the "Configured Kafka subscriptions" log line now reports the actual consumer group id from
  the Aspire consumer config instead of Wolverine's `ServiceName`, which is not the group id.
- **ServiceDefaults/Messaging**: FluentValidation middleware now flows the message `CancellationToken`
  into `ValidateAsync`, so async validators cancel with the request.
- **Docs**: `AddWolverineWithDefaults` XML remarks no longer overstate delivery guarantees — they now
  document that the outbox is only atomic with business data when both share the same database
  connection/transaction (not the case with the default separate `service_bus` database + LinqToDB
  topology).

## [2026-07-18]

### Changed

- **Deps**: bumped `WolverineFx`/`WolverineFx.Kafka`/`WolverineFx.Postgresql` from `5.39.x` to `6.20.0`
  (major). Two runtime-default changes needed code fixes, both in the generated project template:
    - Core `WolverineFx` no longer ships the Roslyn runtime compiler; `TypeLoadMode.Dynamic` (used locally
      when `CodegenEnabled: true`, see `appsettings.Local.json`) now needs the new `WolverineFx.RuntimeCompilation`
      package. Added it as an unconditional dependency of `Momentum.ServiceDefaults` (every host type uses
      Dynamic mode locally) and call `opts.UseRuntimeCompilation()` explicitly in `WolverineSetupExtensions`
      — the package's usual `[WolverineModule]` auto-registration relies on Wolverine's assembly-discovery
      scan, which this codebase already disables via `ExtensionDiscovery.ManualOnly`.
    - `WolverineOptions.ServiceLocationPolicy` now defaults to `NotAllowed` (was `AllowedButWarn`). LinqToDB's
      `AddLinqToDBContext<T>` registers `DataOptions<T>` behind an opaque lambda factory Wolverine's codegen
      can't statically resolve, which broke every command handler touching the database. Fixed with a scoped
      allow-list (`opts.CodeGeneration.AlwaysUseServiceLocationFor<DataOptions<AppDomainDb>>()`) registered via
      `IWolverineExtension` in the generated project's `DependencyInjection.cs`, rather than disabling the new
      policy repo-wide.
    - Verified: `dotnet new mmt --local` (default flags) restores, builds, and passes all 47 integration tests
      (Testcontainers Postgres + Liquibase); this repo's own `AppDomain.slnx` passes 350 unit + 209
      integration/arch tests including all 49 real Wolverine-handler integration tests.

### Fixed

- **EventMarkdownGenerator**: GitHub source links for generated events no longer guess wrong when a project
  folder's name itself contains a dot (e.g. `AppDomain.BackOffice` was being split into
  `AppDomain/BackOffice/...`) — the compiled assembly name is now used as the (unsplit) root segment. New
  optional `--source-root` CLI flag additionally verifies the guessed file exists before emitting a link,
  falling back to `#` otherwise (catches cases reflection can never resolve, e.g. a type colocated in a file
  named after something else entirely).

- **EventMarkdownGenerator**: `Pluralize()` no longer mangles past-tense/participle words (e.g.
  `"reservation-created"` → `"reservation-createds"`) — words ending in `-ed` are now left unchanged, except
  for a small set of genuine `-ed` nouns (`bed`, `seed`, `speed`, ...).
- **EventMarkdownGenerator**: topic fallback (when `Topic` isn't set) now derives from the pluralized entity
  name instead of the event name.
- **EventMarkdownGenerator**: 22 event-doc scenario baselines regenerated to drop the bad pluralization they
  had baked in (`payment-processeds` → `payment-processed`, etc); `docs/events/events-sidebar.json`
  duplicate entries removed as a byproduct of regeneration.

### Added

- **EventTopicAttribute**: new `CollapseTopicOnDomain` property (default `true`) — drops the topic segment
  from the fully-qualified topic name when it exactly duplicates the domain[.subdomain] path (e.g. domain
  `"orders"` + topic `"orders"` → `"orders.v1"` instead of `"orders.orders.v1"`); set to `false` to always
  keep both segments.

### Changed

- **EventMarkdownGenerator**: generated event docs now nest under `integration_events/`/`domain_events/`
  subfolders instead of sitting flat in `docs/events/`, classified by the same `IsInternal` flag the
  sidebar already groups "Domain Events" by (not by namespace text, which can disagree with it — e.g. an
  event under an `IntegrationEvents` namespace but marked `Internal = true`). Sidebar links, the VitePress
  fallback sidebar generator, and the static `integration_events.md` overview page all updated to match;
  real `docs/events/` regenerated onto the new layout.

## [2026-07-17]

### Changed

- **Deps**: bumped `NSubstitute` from `5.3.0` to `6.0.0` (major). Fixed three test files
  (`CreateCashierCommandHandlerTests`, `UpdateCashierCommandHandlerTests`, `CreateInvoiceCommandHandlerTests`)
  that cast `CallInfo`'s indexer directly (`(T)x[0]`), which no longer compiles under 6.0's nullable-annotated
  public API — switched to the null-safe `x.ArgAt<T>(0)` extension instead.

### Skipped

- **Deps**: `Refitter.MSBuild` `2.0.0` → `2.1.0` was left in place — 2.1.0 refactored the MSBuild task itself
  (upstream decoupled the CLI binary from the task) and fails code generation locally with
  `MissingMethodException: System.Text.ValueStringBuilder.AsSpan()`. Needs an upstream fix before retrying.

## [2026-07-16]

### Changed

- **Docs**: widened both VitePress sites (`docs/`, `libs/Momentum/docs/`) — layout frame now maxes out at
  1640px (`--vp-layout-max-width`) and the doc content column at 900px, up from the VitePress defaults.
- **Docs**: nav "Changelog" link now renders `CHANGELOG.md` inline as a docs page (`/changelog`) instead of
  linking out to GitHub.

### Fixed

- **Docs**: Mermaid diagrams now render with the registered ELK layout (`layout: "elk"` was never passed to
  `mermaid.render`, so the loader was registered but unused).
- **Docs**: fixed 115 Mermaid diagrams across both sites using the invalid arrow `-/->`, which failed to
  parse and silently dropped every affected diagram (most notably all of `docs/arch/*.md`).

### Changed

- **Docs**: Mermaid diagrams now render with the `neo` look (was unset, defaulting to `classic`).
- **Docs**: the architecture/system diagrams in `docs/arch/*.md` (`index.md`, `eda.md`, `events.md`,
  `background-processing.md`) now use a consistent C4-style palette — blue for containers, light blue for
  domain/component-level nodes, grey for external systems (Kafka, third-party services) — replacing the
  ad-hoc pastel `style` overrides that only existed on one of the four diagrams.
- **Deps**: bumped `mermaid` from `^11.10.1` to `^11.16.0` (latest) in both `docs/` and `libs/Momentum/docs/`.
- **Docs**: Mermaid subgraph/cluster backgrounds are now white instead of the default pale-yellow theme
  color (light theme only, via `themeVariables.clusterBkg`).

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
- **Docs**: synced the `event.liquid` variable table in the new VitePress guide page
  (`libs/Momentum/docs/guide/messaging/event-documentation.md`) with the fields and behavior
  described above — dropped the stale `{env}` prefix, and added `EventNameKebab`, `EventTypeName`,
  `Domain`, `Summary`, and `AttributeProperties`.

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
