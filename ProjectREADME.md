# AppDomain Microservices Solution

A comprehensive .NET 9 microservices solution implementing Domain-Driven Design principles with event-driven architecture. The solution
mirrors real-world business operations with separate front office (synchronous APIs) and back office (asynchronous event processing)
components.

## Overview

The AppDomain solution demonstrates modern enterprise architecture patterns including:

-   **Domain-Driven Design (DDD)** with clear bounded contexts
-   **Event-driven architecture** using Apache Kafka
-   **CQRS pattern** with separate command and query handling
-   **Microservices architecture** with proper service boundaries
<!--#if (INCLUDE_ORLEANS)-->
-   **Orleans-based stateful processing** for complex business workflows
<!--#endif-->
-   **Comprehensive testing strategy** with unit and integration tests

<!--#if (INCLUDE_SAMPLE)-->

### Key Business Domains

-   **Cashiers**: Management of cashier entities and their payment capabilities
-   **Invoices**: Invoice lifecycle management with state transitions and payment processing
<!--#endif-->

## Architecture

```mermaid
graph TB
    Client[Client Applications]
    <!--#if (INCLUDE_API)-->
    subgraph "Front Office (Synchronous)"
        API[AppDomain.Api<br/>REST & gRPC]
    end
    <!--#endif-->

    <!--#if (INCLUDE_BACK_OFFICE || INCLUDE_ORLEANS)-->
    subgraph "Back Office (Asynchronous)"
        <!--#if (INCLUDE_BACK_OFFICE)-->
        BackOffice[AppDomain.BackOffice<br/>Event Processing]
        <!--#endif-->
        <!--#if (INCLUDE_ORLEANS)-->
        Orleans[AppDomain.BackOffice.Orleans<br/>Stateful Processing]
        <!--#endif-->
    end
    <!--#endif-->

    subgraph "Core Domain"
        Domain[AppDomain<br/>Commands, Queries, Events]
        Contracts[AppDomain.Contracts<br/>Integration Events]
    end

    subgraph "Infrastructure"
        <!--#if (USE_DB)-->
        DB[(PostgreSQL<br/>Primary Database)]
        <!--#endif-->
        <!--#if (USE_KAFKA)-->
        Kafka[Apache Kafka<br/>Event Streaming]
        <!--#endif-->
        <!--#if (INCLUDE_ASPIRE)-->
        Aspire[.NET Aspire<br/>Orchestration]
        <!--#endif-->
    end

    <!--#if (INCLUDE_API)-->
    Client --> API
    API --> Domain
    <!--#endif-->
    <!--#if (INCLUDE_BACK_OFFICE)-->
    BackOffice --> Domain
    <!--#endif-->
    <!--#if (INCLUDE_ORLEANS)-->
    Orleans --> Domain
    <!--#endif-->
    Domain --> Contracts
    <!--#if (INCLUDE_API && USE_DB)-->
    API --> DB
    <!--#endif-->
    <!--#if (INCLUDE_BACK_OFFICE && USE_DB)-->
    BackOffice --> DB
    <!--#endif-->
    <!--#if (INCLUDE_ORLEANS && USE_DB)-->
    Orleans --> DB
    <!--#endif-->
    <!--#if (INCLUDE_BACK_OFFICE && USE_KAFKA)-->
    BackOffice --> Kafka
    <!--#endif-->
    <!--#if (INCLUDE_ORLEANS && USE_KAFKA)-->
    Orleans --> Kafka
    <!--#endif-->
    <!--#if (INCLUDE_API && USE_KAFKA)-->
    API --> Kafka
    <!--#endif-->
```

## Project Structure

<!-- prettier-ignore-start -->
<!--#if (INCLUDE_API)-->
- **AppDomain.Api**: REST and gRPC endpoints for synchronous operations
<!--#endif-->
<!--#if (INCLUDE_BACK_OFFICE)-->
- **AppDomain.BackOffice**: Background event processing and integration workflows
<!--#endif-->
<!--#if (INCLUDE_ORLEANS)-->
- **AppDomain.BackOffice.Orleans**: Stateful processing using Microsoft Orleans
<!--#endif-->
<!--#if (INCLUDE_ASPIRE)-->
- **AppDomain.AppHost**: .NET Aspire orchestration for local development
<!--#endif-->
- **AppDomain**: Core domain logic with commands, queries, and events
- **AppDomain.Contracts**: Shared contracts and integration events
<!--#if (db == "liquibase")-->
- **infra/AppDomain.Database**: Liquibase database migrations
<!--#endif-->
<!--#if (INCLUDE_DOCS)-->
- **docs**: VitePress documentation site with auto-generated event docs
<!--#endif-->
<!-- prettier-ignore-end -->

## Port Allocations

The solution uses the following port allocations (default base port: 8100):

<!-- prettier-ignore-start -->
| Service | HTTP Port | HTTPS Port | Description |
|---------|-----------|------------|-------------|
<!--#if (INCLUDE_API)-->
| API (HTTP) | 8101 | 8111 | REST API endpoints |
| API (gRPC) | 8102 | - | gRPC service endpoints |
<!--#endif-->
<!--#if (INCLUDE_BACK_OFFICE)-->
| BackOffice | 8103 | 8113 | Background processing service |
<!--#endif-->
<!--#if (INCLUDE_ORLEANS)-->
| Orleans | 8104 | 8114 | Orleans silo endpoints |
<!--#endif-->
<!--#if (INCLUDE_ASPIRE)-->
| Aspire Dashboard | 18100 | 18110 | .NET Aspire dashboard |
<!--#endif-->
<!--#if (INCLUDE_DOCS)-->
| Documentation | 8119 | - | VitePress documentation site |
<!--#endif-->
<!--#if (USE_DB)-->
| PostgreSQL | 54320 | - | Database server |
| pgAdmin | 54321 | - | Database management UI |
<!--#endif-->
<!--#if (USE_KAFKA)-->
| Kafka | 59092 | - | Kafka broker |
<!--#endif-->
<!-- prettier-ignore-end -->

## Prerequisites

<!-- prettier-ignore-start -->

-   **.NET 9 SDK** or later
-   **Docker Desktop** with Docker Compose
<!--#if (USE_PGSQL)-->
-   **PostgreSQL** (handled by Docker Compose)
    <!--#endif-->
    <!--#if (USE_KAFKA)-->
-   **Apache Kafka** (handled by Docker Compose)
<!--#endif-->
-   **Git** for version control
<!-- prettier-ignore-end -->

### Optional Tools

<!-- prettier-ignore-start -->
<!--#if (USE_PGSQL)-->

-   **pgAdmin** (included in Docker setup)
<!--#endif-->
-   **Visual Studio 2022** or **JetBrains Rider**
-   **Postman** or similar for API testing
<!-- prettier-ignore-end -->

## Getting Started

<!--#if (INCLUDE_ASPIRE)-->

### Option 1: Using .NET Aspire (Recommended)

The fastest way to run the complete solution with orchestration:

```bash
# Clone the repository
git clone <repository-url>
cd AppDomain

# Start the complete application stack
dotnet run --project src/AppDomain.AppHost
```

This will:

-   Start all services with proper orchestration
-   Launch the Aspire dashboard at https://localhost:18110
-   Set up service discovery and health monitoring
-   Configure distributed tracing with OpenTelemetry
<!--#endif-->

### Option 2: Using Docker Compose

Run specific service profiles:

```bash
<!--#if (INCLUDE_API)-->
# Run API services
docker compose --profile api up
<!--#endif-->
<!--#if (INCLUDE_BACK_OFFICE)-->
# Run BackOffice services
docker compose --profile backoffice up
<!--#endif-->
<!--#if (INCLUDE_ORLEANS)-->
# Run Orleans services
docker compose --profile orleans up
<!--#endif-->
# Run all services
docker compose up
```

<!--#if (db == "liquibase")-->

### Database Setup

The solution uses PostgreSQL with Liquibase migrations:

```bash
# Run database migrations
docker compose up AppDomain-db-migrations

# Reset database (warning: destroys all data)
docker compose down -v
docker compose up AppDomain-db AppDomain-db-migrations
```

#### Accessing the Database

-   **Connection String**: `Host=localhost;Port=54320;Database=app_domain;Username=postgres;Password=password@`
-   **pgAdmin**: http://localhost:54321 (admin@example.com / admin)
<!--#endif-->

## Development Workflow

### Building the Solution

```bash
# Build all projects
dotnet build

# Build specific project
dotnet build src/AppDomain.Api
```

### Running Tests

```bash
# Run all tests
dotnet test

# Run with coverage
dotnet test --collect:"XPlat Code Coverage"

# Run specific test project
dotnet test tests/AppDomain.Tests
```

### Adding New Features

<!-- prettier-ignore-start -->

1. **Define the Command/Query** in `src/AppDomain/[Domain]/Commands/` or `Queries/`
2. **Implement the Handler** using Wolverine's handler pattern
3. **Create Integration Events** in `src/AppDomain.Contracts/IntegrationEvents/`
 <!--#if (INCLUDE_API)-->
4. **Add API Endpoints** in `src/AppDomain.Api/[Domain]/Controller.cs`
    <!--#endif-->
    <!--#if (db == "liquibase")-->
5. **Create Database Migrations** in `infra/AppDomain.Database/Liquibase/`
 <!--#endif-->
6. **Write Tests** in `tests/AppDomain.Tests/`
 <!-- prettier-ignore-end -->

<!--#if (INCLUDE_API)-->

## API Documentation

### Accessing API Documentation

-   **Scalar UI**: http://localhost:8101/scalar
-   **OpenAPI Spec**: http://localhost:8101/openapi/v1.json
-   **gRPC Reflection**: Enabled on port 8102

### API Endpoints

<!--#if (INCLUDE_SAMPLE)-->

#### Cashiers API

| Method | Endpoint             | Description        |
| ------ | -------------------- | ------------------ |
| GET    | `/api/cashiers`      | List all cashiers  |
| GET    | `/api/cashiers/{id}` | Get cashier by ID  |
| POST   | `/api/cashiers`      | Create new cashier |
| PUT    | `/api/cashiers/{id}` | Update cashier     |
| DELETE | `/api/cashiers/{id}` | Delete cashier     |

#### Invoices API

| Method | Endpoint                 | Description          |
| ------ | ------------------------ | -------------------- |
| GET    | `/api/invoices`          | List all invoices    |
| GET    | `/api/invoices/{id}`     | Get invoice by ID    |
| POST   | `/api/invoices`          | Create new invoice   |
| POST   | `/api/invoices/{id}/pay` | Mark invoice as paid |

<!--#endif-->

### Authentication

The API supports multiple authentication schemes:

-   **API Key**: Pass via `X-API-Key` header
-   **JWT Bearer**: Standard OAuth 2.0 bearer tokens
-   **Basic Auth**: For development/testing only
<!--#endif-->

<!--#if (USE_KAFKA)-->

## Event-Driven Architecture

### Integration Events

The solution uses Apache Kafka for event streaming:

<!--#if (INCLUDE_SAMPLE)-->

| Event           | Producer   | Consumers           | Description                 |
| --------------- | ---------- | ------------------- | --------------------------- |
| CashierCreated  | API        | BackOffice          | New cashier registered      |
| CashierUpdated  | API        | BackOffice          | Cashier details modified    |
| InvoiceCreated  | API        | BackOffice, Orleans | New invoice generated       |
| InvoicePaid     | API        | BackOffice, Orleans | Payment received            |
| PaymentReceived | BackOffice | Orleans             | Payment processing complete |

<!--#endif-->

### Kafka Topics

Topics follow the naming convention: `app_domain.[domain].[event-type]`

Example: `app_domain.cashiers.created`, `app_domain.invoices.paid`

<!--#endif-->

## Configuration

### Environment Variables

| Variable               | Default     | Description         |
| ---------------------- | ----------- | ------------------- |
| ASPNETCORE_ENVIRONMENT | Development | Runtime environment |

<!--#if (USE_DB)-->

| ConnectionStrings\_\_AppDomainDb | - | PostgreSQL connection string |
| ConnectionStrings\_\_ServiceBus | - | Service bus database connection |

<!--#endif-->
<!--#if (USE_KAFKA)-->

| Kafka\_\_BootstrapServers | localhost:59092 | Kafka broker addresses |

<!--#endif-->
<!--#if (INCLUDE_ORLEANS)-->

| Orleans\_\_ClusterId | dev | Orleans cluster identifier |
| Orleans\_\_ServiceId | AppDomain | Orleans service identifier |

<!--#endif-->

### Application Settings

Configuration files follow the standard .NET pattern:

-   `appsettings.json` - Base configuration
-   `appsettings.Development.json` - Development overrides
-   `appsettings.Production.json` - Production settings

## Deployment

### Docker Build

```bash
<!--#if (INCLUDE_API)-->
# Build API image
docker build -f src/AppDomain.Api/Dockerfile -t appdomain-api .
<!--#endif-->
<!--#if (INCLUDE_BACK_OFFICE)-->
# Build BackOffice image
docker build -f src/AppDomain.BackOffice/Dockerfile -t appdomain-backoffice .
<!--#endif-->
<!--#if (INCLUDE_ORLEANS)-->
# Build Orleans image
docker build -f src/AppDomain.BackOffice.Orleans/Dockerfile -t appdomain-orleans .
<!--#endif-->
```

### Kubernetes Deployment

Example deployment manifests are available in `/k8s`:

```bash
# Deploy to Kubernetes
kubectl apply -f k8s/namespace.yaml
kubectl apply -f k8s/configmap.yaml
kubectl apply -f k8s/secrets.yaml
kubectl apply -f k8s/deployments/
kubectl apply -f k8s/services/
```

### Production Considerations

1. **Database**: Use managed PostgreSQL (Azure Database, AWS RDS, etc.)
2. **Messaging**: Use managed Kafka (Confluent Cloud, AWS MSK, etc.)
3. **Monitoring**: Configure OpenTelemetry exporters for your APM solution
4. **Secrets**: Use Key Vault or Secret Manager for sensitive configuration
5. **Scaling**: Configure horizontal pod autoscaling for services

## Monitoring

### Health Checks

All services expose health endpoints:

-   `/health/live` - Liveness probe
-   `/health/ready` - Readiness probe
-   `/health/internal` - Detailed health status

### Metrics

OpenTelemetry metrics are exposed at `/metrics` (Prometheus format)

### Distributed Tracing

<!--#if (INCLUDE_ASPIRE)-->

Traces are collected by the Aspire dashboard and can be exported to:

<!--#else-->

Traces can be exported to:

<!--#endif-->

-   Jaeger
-   Zipkin
-   Azure Application Insights
-   AWS X-Ray

## Troubleshooting

### Common Issues

#### Database Connection Failed

```bash
# Check PostgreSQL is running
docker ps | grep postgres

# Verify connection string
psql -h localhost -p 54320 -U postgres -d app_domain
```

<!--#if (USE_KAFKA)-->

#### Kafka Connection Issues

```bash
# Check Kafka is running
docker ps | grep kafka

# Test Kafka connectivity
docker exec -it <kafka-container> kafka-topics.sh --list --bootstrap-server localhost:9092
```

<!--#endif-->

<!--#if (INCLUDE_API)-->

#### API Not Responding

```bash
# Check service logs
docker logs appdomain-api

# Verify port binding
netstat -an | grep 8101
```

<!--#endif-->

#### Build Failures

```bash
# Clear NuGet cache
dotnet nuget locals all --clear

# Restore packages
dotnet restore --force
```

### Getting Help

1. Check the `/docs` folder for detailed documentation
2. Review architecture decision records in `/docs/arch/adr/`
3. Consult the API documentation at `/scalar`
4. Enable debug logging: `export Logging__LogLevel__Default=Debug`

---
