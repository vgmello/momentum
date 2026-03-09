# Momentum.Extensions.Messaging

Messaging abstractions and setup infrastructure for the Momentum platform.

## Features

- **Service Bus Configuration** - `AddServiceBus()` extensibility point for plugging in messaging providers
- **Event Discovery** - Automatic integration event and handler discovery via assembly scanning
- **Distributed Tracing** - CloudEvents trace parent attribute support
- **CLI Command Handling** - Framework for messaging CLI commands (e.g., codegen, db-apply)
- **Service Bus Options** - Shared messaging configuration with validation

## Usage

```csharp
// Configure messaging with a provider
builder.AddServiceBus(bus => bus.UseWolverine());
```

## Dependencies

- `Momentum.Extensions` - Core utilities and shared types
