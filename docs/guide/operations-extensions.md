---
title: Seamless service operations
description: Standardize and streamline your service development with robust extensions for messaging, database access, and observability.
---

# Seamless service operations

Standardize and streamline your service development with our comprehensive suite of extensions. These tools provide robust, out-of-the-box solutions for common operational concerns like messaging, database access, and observability, so you can focus on writing business logic instead of boilerplate code.

This guide explores how to leverage these extensions to build scalable, maintainable, and observable services with minimal setup.

## Messaging contracts

The messaging contracts define a set of interfaces and attributes that establish a clear and consistent messaging pattern across your services. By using these contracts, you ensure that your commands, queries, and events are easily discoverable and interoperable.

### Core interfaces

The messaging system is built around three core interfaces: `ICommand<TResult>`, `IQuery<TResult>`, and `IIntegrationEvent`. These interfaces act as markers, allowing the messaging infrastructure to automatically identify and route messages.

-   `ICommand<TResult>`: Represents an action that modifies system state and returns a result.
-   `IQuery<TResult>`: Represents a read-only request to retrieve data.
-   `IIntegrationEvent`: Represents a notification of a state change that can be published to other services.

[!code-csharp[](../../libs/Operations/src/Operations.Extensions.Abstractions/Messaging/ICommand.cs)]

[!code-csharp[](../../libs/Operations/src/Operations.Extensions.Abstractions/Messaging/IQuery.cs)]

[!code-csharp[](../../libs/Operations/src/Operations.Extensions.Abstractions/Messaging/IIntegrationEvent.cs)]

### Topic and domain attributes

You can control how your integration events are routed by using the `[EventTopic]` and `[DefaultDomain]` attributes. These attributes determine the topic name and domain for your events in the message broker.

-   `[EventTopic]`: Specifies the topic name for an individual integration event. You can also provide an optional domain to override the assembly-level default.
-   `[DefaultDomain]`: Sets a default domain for all integration events within an assembly. This is useful for grouping related events under a common namespace.

[!code-csharp[](../../libs/Operations/src/Operations.Extensions.Abstractions/Messaging/EventTopicAttribute.cs)]

[!code-csharp[](../../libs/Operations/src/Operations.Extensions.Abstractions/Messaging/DefaultDomainAttribute.cs)]

### Partition key management

The `[PartitionKey]` attribute allows you to designate a property on your integration event as the partition key. The message broker uses this key to ensure that related messages are processed in the correct order.

[!code-csharp[](../../libs/Operations/src/Operations.Extensions.Abstractions/Messaging/PartitionKeyAttribute.cs)]

## Database contracts

The database contracts simplify data access by providing a standardized way to define and execute database commands. These contracts are designed to work seamlessly with our source-generated command handlers, which automate the creation of Dapper-based data access logic.

### The DbCommand attribute

The `[DbCommand]` attribute is the cornerstone of our database contracts. Apply it to a class to define a database command and trigger the source generation of `ToDbParams()` methods and command handlers. You can specify a stored procedure, raw SQL query, or a function, and the source generator will create the corresponding handler logic.

[!code-csharp[](../../libs/Operations/src/Operations.Extensions.Abstractions/Dapper/DbCommandAttribute.cs)]

### Column mapping

When your C# property names don't align with your database column names, you can use the `[Column]` attribute to specify the correct mapping. This is particularly useful for maintaining clean code while working with legacy database schemas.

[!code-csharp[](../../libs/Operations/src/Operations.Extensions.Abstractions/Dapper/ColumnAttribute.cs)]

### Custom parameter providers

For complex scenarios where you need full control over parameter creation, you can implement the `IDbParamsProvider` interface. This allows you to define a custom `ToDbParams()` method, giving you the flexibility to handle any parameter mapping requirements.

[!code-csharp[](../../libs/Operations/src/Operations.Extensions.Abstractions/Dapper/IDbParamsProvider.cs)]

## Dapper extensions

The Dapper extensions provide a set of convenience methods that simplify the execution of stored procedures. These extensions are built on top of Dapper and are designed to work with the `DbDataSource` class, which is the recommended way to manage database connections in modern .NET applications.

### Executing stored procedures

The `SpExecute` method allows you to execute a stored procedure that returns the number of affected rows. This is useful for `INSERT`, `UPDATE`, and `DELETE` operations.

[!code-csharp[](../../libs/Operations/src/Operations.Extensions/Dapper/DbDataSourceExtensions.cs?highlight=13-19)]

### Querying data

The `SpQuery<TResult>` method allows you to query data using a stored procedure that returns a collection of `TResult`. This is useful for `SELECT` operations.

[!code-csharp[](../../libs/Operations/src/Operations.Extensions/Dapper/DbDataSourceExtensions.cs?highlight=21-28)]

## Event documentation generator

The event documentation generator is a command-line tool that automatically creates Markdown documentation for your integration events. It uses reflection to discover events in your assemblies and XML documentation comments to generate rich, informative documentation.

### Usage

To use the generator, you need to provide the path to the assembly you want to analyze, the path to the XML documentation file, and the output directory for the generated documentation.

```bash
dotnet run --project Operations.Extensions.EventDocGenerator -- \
    --assembly "path/to/your/assembly.dll" \
    --xml-docs "path/to/your/assembly.xml" \
    --output "docs/events"
```

### Generated output

The tool generates a Markdown file containing detailed information about each event, including its properties, summary, remarks, and example usage. It also generates a JSON sidebar file that can be used to create a navigation menu for your documentation.

## DbCommand source generator

The `DbCommand` source generator is a powerful tool that automates the creation of database command handlers and parameter providers. By decorating your command classes with the `[DbCommand]` attribute, you can trigger the generation of highly optimized data access code.

### How it works

The source generator scans your codebase for types marked with the `[DbCommand]` attribute. For each command, it generates:

-   A `ToDbParams()` extension method that maps the command's properties to database parameters.
-   A Wolverine command handler that executes the database command using Dapper.

This approach eliminates the need to write repetitive data access code, allowing you to focus on your business logic.

### Usage

To use the source generator, simply define a command class that implements `ICommand<TResult>` or `IQuery<TResult>` and decorate it with the `[DbCommand]` attribute. You can specify a stored procedure, raw SQL query, or a function, and the generator will create the corresponding handler.

[!code-csharp[](../../libs/Operations/src/Operations.Extensions.SourceGenerators/DbCommand/DbCommandSourceGenerator.cs?highlight=25-36)]

### Analyzers

The source generator includes a set of analyzers that help you avoid common mistakes. These analyzers will issue warnings or errors if you misuse the `[DbCommand]` attribute, such as:

-   Using the `NonQuery` property with a command that returns a result.
-   Forgetting to implement `ICommand<TResult>` or `IQuery<TResult>` on a command class.
-   Specifying mutually exclusive properties in the `[DbCommand]` attribute.

[!code-csharp[](../../libs/Operations/src/Operations.Extensions.SourceGenerators/DbCommand/DbCommandAnalyzers.cs?highlight=7-32)]

## Service defaults

The service defaults provide a one-line setup for essential services, including logging, telemetry, messaging, and health checks. By calling `AddServiceDefaults()`, you can quickly configure your application with a robust set of operational tools.

### What's included

The `AddServiceDefaults()` method configures the following services:

-   **Structured logging**: Sets up Serilog for structured, queryable logs.
-   **OpenTelemetry**: Configures logging, metrics, and distributed tracing for comprehensive observability.
-   **Wolverine messaging**: Initializes the Wolverine messaging framework with sensible defaults.
-   **Health checks**: Adds health check endpoints for monitoring your service's status.
-   **FluentValidation**: Discovers and registers all validators from your domain assemblies.

[!code-csharp[](../../libs/Operations/src/Operations.ServiceDefaults/ServiceDefaultsExtensions.cs?highlight=43-64)]

### Health checks

The health check setup provides multiple endpoints for different monitoring scenarios:

-   `/status`: A lightweight liveness probe that returns the last known health status.
-   `/health/internal`: A container-only readiness probe with simplified output.
-   `/health`: A public, authorized endpoint with detailed health information.

[!code-csharp[](../../libs/Operations/src/Operations.ServiceDefaults/HealthChecks/HealthCheckSetupExtensions.cs?highlight=30-78)]

### Logging

The logging setup uses Serilog to provide structured, queryable logs. It's configured to work with OpenTelemetry, so your logs are automatically correlated with your traces and metrics.

[!code-csharp[](../../libs/Operations/src/Operations.ServiceDefaults/Logging/LoggingSetupExtensions.cs?highlight=18-25)]

### OpenTelemetry

The OpenTelemetry setup provides a comprehensive observability solution, including:

-   Distributed tracing with W3C Trace Context and Baggage propagation.
-   Metrics collection for ASP.NET Core, HTTP clients, and the .NET runtime.
-   An OTLP exporter for sending telemetry data to your observability backend.

[!code-csharp[](../../libs/Operations/src/Operations.ServiceDefaults/OpenTelemetry/OpenTelemetrySetupExtensions.cs?highlight=35-104)]

### Wolverine messaging

The Wolverine setup configures the Wolverine messaging framework with a robust set of features, including:

-   PostgreSQL-backed persistence for reliable messaging.
-   Kafka integration for high-throughput event streaming.
-   Middleware for exception handling, validation, and performance monitoring.

[!code-csharp[](../../libs/Operations/src/Operations.ServiceDefaults/Messaging/Wolverine/WolverineSetupExtensions.cs?highlight=27-50)]

### Kafka integration

The Kafka integration automatically discovers your integration events and configures Wolverine to publish and subscribe to the correct topics. It also handles topic naming, partitioning, and CloudEvents mapping.

[!code-csharp[](../../libs/Operations/src/Operations.ServiceDefaults/Messaging/Kafka/KafkaIntegrationEventsExtensions.cs?highlight=30-54)]

## API extensions

The API extensions streamline the process of building and configuring your web APIs. They provide a set of defaults for common API concerns, such as routing, serialization, and documentation.

### API service defaults

The `AddApiServiceDefaults()` method configures a range of services to help you build robust and maintainable APIs:

-   **Controllers and routing**: Sets up MVC controllers with kebab-case route transformation.
-   **Problem details**: Enables standardized error responses for consistent error handling.
-   **OpenAPI**: Configures OpenAPI with XML documentation support for rich, interactive API documentation.
-   **gRPC**: Adds gRPC services with reflection for easy service discovery.

[!code-csharp[](../../libs/Operations/src/Operations.ServiceDefaults.Api/ApiExtensions.cs?highlight=23-53)]

### API configuration defaults

The `ConfigureApiUsingDefaults()` method applies a set of default middleware and endpoints to your application:

-   **Request logging**: Enables HTTP request and response logging for debugging and auditing.
-   **Authentication and authorization**: Sets up authentication and authorization middleware.
-   **gRPC-Web**: Enables gRPC-Web for browser-based gRPC communication.
-   **OpenAPI and Scalar**: Exposes OpenAPI and Scalar UI endpoints in development environments.

[!code-csharp[](../../libs/Operations/src/Operations.ServiceDefaults.Api/ApiExtensions.cs?highlight=77-116)]

### Result type

The `Result<T>` type provides a standardized way to return values from your API endpoints. It's a discriminated union that can represent either a successful result or a list of validation failures. This allows you to communicate validation errors without throwing exceptions, which can be expensive and obscure your control flow.

[!code-csharp[](../../libs/Operations/src/Operations.Extensions/Result.cs?highlight=1-26)]

## See also

-   [Generate event documentation](./eventdoc-generator.md)
-   [Source-generate database commands](./dbcommand-source-generator.md)
-   [Configure service defaults](./service-defaults.md)
