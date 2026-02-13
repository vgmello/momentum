# AppDomain.Contracts

Contracts (models, integration events, and optional gRPC protobufs) that define the public surface of the AppDomain. This package is designed to be shared by other services and clients so they can:

-   Consume and publish integration events consistently;
-   Reuse domain models for request/response DTOs;
-   Optionally generate gRPC clients/servers from the shipped .proto files.

## What’s included

<!-- prettier-ignore-start -->

-   Domain Models
    <!--#if (INCLUDE_SAMPLE) -->
    -   Cashiers: `Cashier`, `CashierPayment`, …
    -   Invoices: domain-facing DTOs where applicable
    <!--#endif -->
-   Integration Events
    <!--#if (INCLUDE_SAMPLE) -->
    -   Cashiers: `CashierCreated`, `CashierUpdated`, `CashierDeleted`
    -   Invoices: e.g. `InvoiceCancelled`
    <!--#endif -->
    <!--#if (INCLUDE_API) -->
-   gRPC Protobuf contracts (if AppDomain.Api is included at build time) - Shipped under the package path `Protos/` (e.g. `Invoices/Protos/...`)
    <!--#endif -->

<!-- prettier-ignore-end -->

All source files are linked from the AppDomain implementation, so this package stays in sync with the domain.

## Target framework

-   net10.0

## Installation

You can consume the contracts via NuGet (CI/CD published).

-   1. Add a package reference to `AppDomain.Contracts`.
        ```xml
        <ItemGroup>
          <PackageReference Include="AppDomain.Contracts" Version="x.y.z" />
        </ItemGroup>
        ```

Note: In Debug builds the package may be published as a prerelease (suffix `-pre`).

## Usage

### Integration events (consumer perspective)

Implement a handler that consumes events from this package. Handlers generally follow the pattern Task Handle(TEvent) and can use constructor injection for dependencies like ILogger or IMessageBus.

<!--#if (INCLUDE_SAMPLE) -->

```csharp
using AppDomain.Cashiers.Contracts.IntegrationEvents;
using Microsoft.Extensions.Logging;

public class CashierCreatedHandler(ILogger<CashierCreatedHandler> logger)
{
    public Task Handle(CashierCreated evt)
    {
        logger.LogInformation(
            "Handling CashierCreated: Tenant={TenantId}, CashierId={CashierId}, Name={Name}",
            evt.TenantId,
            evt.Cashier.CashierId,
            evt.Cashier.Name
        );

        // TODO: apply your side-effects (update read models, notify systems, etc.)
        return Task.CompletedTask;
    }
}
```

Notes:

-   Handler discovery and wiring depend on your messaging/bus setup. Many frameworks will auto-discover classes with a Handle(TEvent) method when registered in DI.
-   Events use attributes from Momentum.Extensions.Abstractions.Messaging (e.g., EventTopic<T>, PartitionKey) that your transport can leverage for routing.

<!--#else -->

```csharp
using AppDomain.Foo.Contracts.IntegrationEvents;
using Microsoft.Extensions.Logging;

public class FooCreatedHandler(ILogger<FooCreatedHandler> logger)
{
    public Task Handle(FooCreated evt)
    {
        logger.LogInformation(
            "Handling FooCreated: Tenant={TenantId}, FooId={FooId}, Name={Name}",
            evt.TenantId,
            evt.Foo.FooId,
            evt.Foo.Name
        );

        // TODO: apply your side-effects
        return Task.CompletedTask;
    }
}
```

<!--#endif -->

<!--#if (INCLUDE_API) -->

### Using the shipped protobufs (optional)

The AppDomain gRPC .protos are automatically included in your project once you add a reference to the AppDomain.Contracts package.
To build them, you will need to install the Grpc.Tools package.

Your project file should look similar to this:

```xml
<Project>
    ...
  <ItemGroup>
    <PackageReference Include="Grpc.Tools" Version="2.*" PrivateAssets="All" />
    <PackageReference Include="AppDomain.Contracts" Version="x.y.z" />
  </ItemGroup>
    ...
</Project>
```

<!--#endif -->

## Versioning

-   Follows semantic versioning (MAJOR.MINOR.PATCH).
-   Debug builds may append `-pre` suffix.
