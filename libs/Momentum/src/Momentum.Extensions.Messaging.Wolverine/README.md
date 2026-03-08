# Momentum.Extensions.Messaging.Wolverine

Wolverine messaging integration for the Momentum platform providing production-ready CQRS message handling with PostgreSQL persistence, FluentValidation, and OpenTelemetry support.

## Usage

```csharp
builder.AddServiceBus(bus => bus.UseWolverine());
```
