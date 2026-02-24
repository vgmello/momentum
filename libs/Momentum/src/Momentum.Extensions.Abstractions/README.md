# Momentum.Extensions.Abstractions

Core abstractions and interfaces for the Momentum platform. This foundational package defines contracts, attributes, and marker interfaces used across all Momentum libraries for CQRS messaging, database command generation, and distributed event handling.

## Overview

The `Momentum.Extensions.Abstractions` package provides the foundational contracts that enable the Momentum platform's CQRS architecture, source-generated database commands, and distributed event streaming. As a dependency-free abstraction layer targeting .NET Standard 2.1, it defines the interfaces and attributes that other Momentum libraries (source generators, Kafka integration, service defaults) build upon.

## Installation

```bash
dotnet add package Momentum.Extensions.Abstractions
```

## Key Features

- **CQRS Messaging Contracts**: `ICommand<T>` and `IQuery<T>` marker interfaces for Wolverine-based message handling
- **Database Command Generation**: `[DbCommand]` attribute for source-generated Dapper handlers (stored procedures, SQL queries, functions)
- **Distributed Event Abstractions**: `IDistributedEvent`, `[EventTopic]`, and `[PartitionKey]` for Kafka event streaming with CloudEvents
- **String Utilities**: Snake case, kebab case conversion, and English pluralization
- **Dependency-Free**: No external dependencies; targets .NET Standard 2.1

## CQRS Messaging

### Commands

Define commands that modify state. Handlers are discovered by convention via Wolverine.

```csharp
// A command that returns a result
public record CreateCashierCommand(Guid TenantId, string Name, string Email)
    : ICommand<Result<Cashier>>;

// A command with no meaningful return value
public record DeleteCashierCommand(Guid TenantId, Guid CashierId)
    : ICommand<Result<bool>>;
```

### Queries

Define read-only data retrieval operations.

```csharp
// A query returning a single entity
public record GetCashierQuery(Guid TenantId, Guid CashierId)
    : IQuery<Result<Cashier>>;

// A query returning a collection
public record GetCashiersQuery(Guid TenantId, int Page, int Size)
    : IQuery<Result<IEnumerable<Cashier>>>;
```

## Database Command Generation

The `[DbCommand]` attribute marks records for source-generated Dapper database handlers via `Momentum.Extensions.SourceGenerators`.

### Stored Procedures

```csharp
[DbCommand(sp: "main.usp_get_cashier")]
public record GetCashierDbQuery(Guid TenantId, Guid CashierId)
    : IQuery<Cashier>;
```

### SQL Queries

```csharp
[DbCommand(sql: "SELECT * FROM main.cashiers WHERE tenant_id = @p_tenant_id")]
public record FindCashiersDbQuery(Guid TenantId)
    : IQuery<IEnumerable<Cashier>>;
```

### Database Functions

```csharp
[DbCommand(fn: "main.fn_get_invoice_total")]
public record GetInvoiceTotalDbQuery(Guid TenantId, Guid InvoiceId)
    : IQuery<decimal>;
```

### Parameter Control

Parameters are automatically converted to snake_case with `p_` prefix by default. Use `DbParamsCase` to control this:

```csharp
// Default: TenantId -> p_tenant_id
[DbCommand(sp: "main.usp_create_cashier")]
public record CreateCashierDbCommand(Guid TenantId, string Name) : ICommand<Cashier>;

// No conversion: parameters used as-is
[DbCommand(sp: "main.usp_create_cashier", paramsCase: DbParamsCase.None)]
public record CreateCashierDbCommand(Guid TenantId, string Name) : ICommand<Cashier>;

// Exclude a property from parameter generation
public record UpdateCashierDbCommand(
    Guid TenantId,
    string Name,
    [property: DbCommandIgnore] DateTime LocalTimestamp
) : ICommand<Cashier>;
```

### Custom Parameter Providers

For complex parameter mapping, implement `IDbParamsProvider`:

```csharp
public record BulkInsertCommand(List<CashierData> Items) : ICommand<int>, IDbParamsProvider
{
    public object ToDbParams() => new { items = JsonSerializer.Serialize(Items) };
}
```

## Distributed Events

### Event Topics

Mark records as distributed events with topic routing:

```csharp
// Explicit topic name
[EventTopic("app-domain.cashiers.cashier-created")]
public record CashierCreated(Guid TenantId, Guid CashierId, string Name);

// Auto-generated topic from entity type (e.g., "app-domain.cashiers.cashier-updated")
[EventTopic<Cashier>(suffix: "updated")]
public record CashierUpdated(Guid TenantId, Guid CashierId, string Name);
```

### Partition Keys

Control message ordering and routing in Kafka:

```csharp
[EventTopic("app-domain.invoices.invoice-created")]
public record InvoiceCreated(
    Guid TenantId,
    [property: PartitionKey] Guid InvoiceId,
    decimal Amount
);

// Composite partition keys with ordering
[EventTopic("app-domain.invoices.line-item-added")]
public record LineItemAdded(
    [property: PartitionKey(Order = 0)] Guid TenantId,
    [property: PartitionKey(Order = 1)] Guid InvoiceId,
    Guid LineItemId
);
```

### IDistributedEvent

Implement `IDistributedEvent` for custom partition key logic:

```csharp
[EventTopic("app-domain.cashiers.cashier-created")]
public record CashierCreated(Guid TenantId, Guid CashierId) : IDistributedEvent
{
    public string GetPartitionKey() => $"{TenantId}:{CashierId}";
}
```

### Default Domain

Set the default domain prefix for all events in an assembly:

```csharp
[assembly: DefaultDomain("app-domain")]
```

## String Extensions

Utility extensions for case conversion and pluralization:

```csharp
using Momentum.Extensions.Abstractions.Extensions;

"TenantId".ToSnakeCase();    // "tenant_id"
"CashierId".ToKebabCase();   // "cashier-id"
"cashier".Pluralize();        // "cashiers"
"person".Pluralize();         // "people"
"status".Pluralize();         // "statuses"
```

## Architecture

This package sits at the foundation of the Momentum library ecosystem:

```
Application Code
├── Momentum.Extensions                    (Result types, validation, data access)
├── Momentum.Extensions.SourceGenerators   (DbCommand code generation)
├── Momentum.Extensions.Messaging.Kafka    (CloudEvents, Kafka integration)
├── Momentum.ServiceDefaults               (Aspire, observability)
├── Momentum.ServiceDefaults.Api           (OpenAPI, gRPC)
└── Momentum.Extensions.Abstractions       ← Foundation (this package)
```

## Target Frameworks

- **.NET Standard 2.1**: Compatible with .NET Core 3.0+, .NET 5.0+

## Related Packages

- [Momentum.Extensions](../Momentum.Extensions/README.md) - Result types, validation, and data access
- [Momentum.Extensions.SourceGenerators](../Momentum.Extensions.SourceGenerators/README.md) - Compile-time DbCommand handler generation
- [Momentum.Extensions.Messaging.Kafka](../Momentum.Extensions.Messaging.Kafka/README.md) - Kafka integration with CloudEvents
- [Momentum.ServiceDefaults](../Momentum.ServiceDefaults/README.md) - Service configuration and observability

## License

This project is licensed under the MIT License. See the [LICENSE](https://github.com/vgmello/momentum/blob/main/LICENSE) file for details.
