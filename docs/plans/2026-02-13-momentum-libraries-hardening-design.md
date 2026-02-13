# Momentum Libraries Hardening Design

## Overview

Hardening pass across Momentum libraries: ServiceDefaults, ServiceDefaults.Api, SourceGenerators, XmlDocs, and EventMarkdownGenerator. Addresses critical security, reliability, and observability gaps.

## Section 1: Momentum.ServiceDefaults

### 1.1 Connection string format validation (Critical)
- After null/whitespace check in `WolverineNpgsqlExtensions.cs`, validate via `NpgsqlConnectionStringBuilder` parse attempt
- Catches malformed strings at startup with clear error message

### 1.2 Configurable sampling rate (High)
- New `OpenTelemetryOptions` class with `ProductionSamplingRate` (default 0.1)
- Bound from `"OpenTelemetry"` config section
- Replace hard-coded constant with options value

### 1.3 Request size limits (High)
- Configure Kestrel `MaxRequestBodySize` via options pattern
- Bound from `"Kestrel:Limits"` config section

### 1.4 Health check cache expiry (High)
- Add `StatusCacheDuration` (TimeSpan) to health check configuration
- `/status` endpoint checks staleness before returning cached value
- Default: 30 seconds

### 1.5 Resilience configuration (High)
- Configure `AddStandardResilienceHandler()` with explicit timeout (30s), retry (3x exponential backoff, 2s base), circuit breaker (5 failures, 30s break)

### 1.6 HealthCheckStatusStore DI registration (Medium)
- `TryAddSingleton<HealthCheckStatusStore>()` in `AddServiceDefaults`
- Remove fallback `?? new` in health check setup

### 1.7 Configuration validation at startup (Medium)
- Add `ValidateOnStart()` for `ServiceBusOptions` and new options classes

### 1.8 OpenTelemetry resource attributes (Medium)
- Add `service.instance.id` (hostname) and `host.name` resource attributes

### 1.9 Console sink on bootstrap logger
- Add `.WriteTo.Console()` on the Serilog bootstrap logger so init logs are visible before OpenTelemetry

## Section 2: Momentum.ServiceDefaults.Api

### 2.1 Rate limiting (Critical)
- `AddRateLimiting()` with fixed window limiter
- Configurable via `"RateLimiting"` config section (window, permit limit, queue limit)
- Applied globally with per-endpoint override option

### 2.2 Custom exception handler (Critical)
- ProblemDetails-based exception handler mapping known exceptions to HTTP status codes
- Validation → 400, not found → 404, unauthorized → 401, default → 500

### 2.3 Security headers (Critical)
- Already implemented in `FrontendIntegrationExtensions.cs` - verify working

### 2.4 API versioning (High)
- URL segment versioning via `Asp.Versioning.Http` package
- Extension method `AddApiVersioningDefaults()` with default version 1.0

### 2.5 gRPC message size limits (High)
- `GrpcOptions` with `MaxReceiveMessageSize`/`MaxSendMessageSize`
- Bound from `"Grpc:Limits"` config section via IOptions pattern

### 2.6 gRPC silent failure warning (High)
- Log warning when `MapGrpcServices` finds no gRPC services in scanned assembly

### 2.7 JSON serialization (Medium)
- Configure `JsonSerializerDefaults.Web` with camelCase, enum as string, ignore null

### 2.8 OpenAPI security scheme (Medium)
- Bearer auth security scheme added to OpenAPI spec via document transformer

### 2.9 HTTP logging environment check (Medium)
- Only enable HTTP logging middleware in Development environment

### 2.10 Frontend integration features (moved from ApiExtensions)
- Response compression (Brotli + Gzip) added to `FrontendIntegrationExtensions.cs`
- HTTPS redirection added to `FrontendIntegrationExtensions.cs`
- HSTS already handled in `ConfigureApiUsingDefaults` for non-dev

### 2.11 Resilience pipeline configuration
- Same as 1.5 - configure the standard resilience handler with explicit settings

## Section 3: Source Generators

### 3.1 Primary constructor documentation (Critical)
- Add comment documenting assumption + diagnostic if multiple non-clone constructors

### 3.2 Exception handling in generator (High)
- Wrap `RegisterSourceOutput` in try-catch, report `MMT004` diagnostic on unexpected errors

### 3.3 SQL identifier validation (High)
- Regex: `^("?[a-zA-Z_]\w*"?|\[[a-zA-Z_]\w*\])(\.\s*("?[a-zA-Z_]\w*"?|\[[a-zA-Z_]\w*\]))*$`
- Supports plain, `[bracketed]`, `"quoted"`, and schema-qualified names
- Emit `MMT005` error diagnostic if invalid

### 3.4 NonQuery analyzer fix (Medium)
- Use `IsIntegralType()` extension instead of string comparison against `"Int32"`

### 3.5 `[GeneratedCode]` attribute (Medium)
- Add `[GeneratedCodeAttribute("Momentum.SourceGenerators", "1.0")]` to all generated types

### 3.6 `[DbCommandIgnore]` attribute (Medium)
- New attribute in Abstractions to exclude properties from parameter generation
- Filter in `DbCommandTypeInfo.SourceGen.cs` property collection

### 3.7 StringBuilder capacity (Low)
- Pre-allocate: 512 for handlers, 256 for params

### 3.8 XML documentation (Low)
- Add `/// <inheritdoc />` on generated handler methods

### 3.9 Edge case tests (High)
- Nested types, nullable parameters, multiple properties

## Section 4: XmlDocs & EventMarkdownGenerator

### 4.1 Assembly loading timeout (High)
- `CancellationTokenSource` with 30s timeout around assembly loading
- Cancel and report warning if exceeded

### 4.2 Async template loading (Medium)
- Change `File.ReadAllText()` to `File.ReadAllTextAsync()` in `GetTemplate`

### 4.3 Integration tests (Medium)
- Remove `Skip` from 2 skipped tests, fix underlying path resolution issues

### 4.4 CancellationToken in LoadDocumentationAsync (Low)
- Add `CancellationToken` parameter, pass through to file/XML operations

### 4.6 UTF-32 comment fix (Low)
- Rename comment from "UTF-32" to "UTF-8 worst-case" since JSON uses UTF-8 and 4 bytes/char is also UTF-8 worst case

## Skipped Items
- BFF template flag (existing FrontendIntegration implementation is sufficient)
- Sensitive data log filtering (deferred)
- Progress reporting for EventMarkdownGenerator (4.5)
