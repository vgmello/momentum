# AppDomain Microservices Solution

A comprehensive .NET 9 microservices solution implementing Domain-Driven Design principles with event-driven architecture. The solution mirrors real-world business operations with separate front office (synchronous APIs) and back office (asynchronous event processing) components.

## Overview

The AppDomain solution demonstrates modern enterprise architecture patterns including:

- **Domain-Driven Design (DDD)** with clear bounded contexts
- **Event-driven architecture** using Apache Kafka
- **CQRS pattern** with separate command and query handling
- **Microservices architecture** with proper service boundaries
<!--#if (orleans)-->
- **Orleans-based stateful processing** for complex business workflows
<!--#endif-->
- **Comprehensive testing strategy** with unit and integration tests

<!--#if ((!no-sample))-->
### Key Business Domains

- **Cashiers**: Management of cashier entities and their payment capabilities
- **Invoices**: Invoice lifecycle management with state transitions and payment processing
<!--#endif-->

## Architecture

```mermaid
graph TB
    Client[Client Applications]
    <!--#if (api)-->
    subgraph "Front Office (Synchronous)"
        API[AppDomain.Api<br/>REST & gRPC]
    end
    <!--#endif-->
    
    <!--#if (back-office || orleans)-->
    subgraph "Back Office (Asynchronous)"
        <!--#if (back-office)-->
        BackOffice[AppDomain.BackOffice<br/>Event Processing]
        <!--#endif-->
        <!--#if (orleans)-->
        Orleans[AppDomain.BackOffice.Orleans<br/>Stateful Processing]
        <!--#endif-->
    end
    <!--#endif-->
    
    subgraph "Core Domain"
        Domain[AppDomain<br/>Commands, Queries, Events]
        Contracts[AppDomain.Contracts<br/>Integration Events]
    end
    
    subgraph "Infrastructure"
        <!--#if ((db == "npgsql") || (db == "liquibase"))-->
        DB[(PostgreSQL<br/>Primary Database)]
        <!--#endif-->
        <!--#if (kafka)-->
        Kafka[Apache Kafka<br/>Event Streaming]
        <!--#endif-->
        <!--#if (aspire)-->
        Aspire[.NET Aspire<br/>Orchestration]
        <!--#endif-->
    end
    
    <!--#if (api)-->
    Client --> API
    API --> Domain
    <!--#endif-->
    <!--#if (back-office)-->
    BackOffice --> Domain
    <!--#endif-->
    <!--#if (orleans)-->
    Orleans --> Domain
    <!--#endif-->
    Domain --> Contracts
    <!--#if (api && ((db == "npgsql") || (db == "liquibase")))-->
    API --> DB
    <!--#endif-->
    <!--#if (back-office && ((db == "npgsql") || (db == "liquibase")))-->
    BackOffice --> DB
    <!--#endif-->
    <!--#if (orleans && ((db == "npgsql") || (db == "liquibase")))-->
    Orleans --> DB
    <!--#endif-->
    <!--#if (back-office && kafka)-->
    BackOffice --> Kafka
    <!--#endif-->
    <!--#if (orleans && kafka)-->
    Orleans --> Kafka
    <!--#endif-->
    <!--#if (api && kafka)-->
    API --> Kafka
    <!--#endif-->
    <!--#if (aspire)-->
    <!--#if (api)-->
    Aspire --> API
    <!--#endif-->
    <!--#if (back-office)-->
    Aspire --> BackOffice
    <!--#endif-->
    <!--#if (orleans)-->
    Aspire --> Orleans
    <!--#endif-->
    <!--#if ((db == "npgsql") || (db == "liquibase"))-->
    Aspire --> DB
    <!--#endif-->
    <!--#if (kafka)-->
    Aspire --> Kafka
    <!--#endif-->
    <!--#endif-->
```

## Project Structure

<!--#if (api)-->
- **AppDomain.Api**: REST and gRPC endpoints for synchronous operations
<!--#endif-->
<!--#if (back-office)-->
- **AppDomain.BackOffice**: Background event processing and integration workflows
<!--#endif-->
<!--#if (orleans)-->
- **AppDomain.BackOffice.Orleans**: Stateful processing using Microsoft Orleans
<!--#endif-->
<!--#if (aspire)-->
- **AppDomain.AppHost**: .NET Aspire orchestration for local development
<!--#endif-->
- **AppDomain**: Core domain logic with commands, queries, and events
- **AppDomain.Contracts**: Shared contracts and integration events
<!--#if ((db == "liquibase"))-->
- **infra/AppDomain.Database**: Liquibase database migrations
<!--#endif-->
<!--#if (docs)-->
- **docs**: VitePress documentation site with auto-generated event docs
<!--#endif-->

## Port Allocations

The solution uses the following port allocations (default base port: SERVICE_BASE_PORT):

| Service | HTTP Port | HTTPS Port | Description |
|---------|-----------|------------|-------------|
<!--#if (api)-->
| API (HTTP) | $MainApiHttpPort | $MainApiHttpsPort | REST API endpoints |
| API (gRPC) | $MainApiGrpcPort | - | gRPC service endpoints |
<!--#endif-->
<!--#if (back-office)-->
| BackOffice | $BackOfficeHttpPort | $BackOfficeHttpsPort | Background processing service |
<!--#endif-->
<!--#if (orleans)-->
| Orleans | $OrleansHttpPort | $OrleansHttpsPort | Orleans silo endpoints |
<!--#endif-->
<!--#if (aspire)-->
| Aspire Dashboard | $AspireHttpPort | $AspireHttpsPort | .NET Aspire dashboard |
<!--#endif-->
<!--#if (docs)-->
| Documentation | $DocumentationHttpPort | - | VitePress documentation site |
<!--#endif-->
<!--#if ((db == "npgsql") || (db == "liquibase"))-->
| PostgreSQL | 54320 | - | Database server |
| pgAdmin | 54321 | - | Database management UI |
<!--#endif-->
<!--#if (kafka)-->
| Kafka | 59092 | - | Kafka broker |
<!--#endif-->

## Prerequisites

- **.NET 9 SDK** or later
- **Docker Desktop** with Docker Compose
<!--#if ((db == "npgsql") || (db == "liquibase"))-->
- **PostgreSQL** (handled by Docker Compose)
<!--#endif-->
<!--#if (kafka)-->
- **Apache Kafka** (handled by Docker Compose)
<!--#endif-->
- **Git** for version control

### Optional Tools

<!--#if ((db == "npgsql") || (db == "liquibase"))-->
- **pgAdmin** (included in Docker setup)
<!--#endif-->
- **Visual Studio 2022** or **JetBrains Rider**
- **Postman** or similar for API testing

## Getting Started

<!--#if (aspire)-->
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
- Start all services with proper orchestration
- Launch the Aspire dashboard at https://localhost:$AspireHttpsPort
- Set up service discovery and health monitoring
- Configure distributed tracing with OpenTelemetry

<!--#endif-->

### Option 2: Using Docker Compose

Run specific service profiles:

```bash
<!--#if (api)-->
# Run API services
docker compose --profile api up

<!--#endif-->
<!--#if (back-office)-->
# Run BackOffice services
docker compose --profile backoffice up

<!--#endif-->
<!--#if (orleans)-->
# Run Orleans services
docker compose --profile orleans up

<!--#endif-->
# Run all services
docker compose up
```

<!--#if ((db == "liquibase"))-->
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

- **Connection String**: `Host=localhost;Port=54320;Database=app_domain;Username=postgres;Password=password@`
- **pgAdmin**: http://localhost:54321 (admin@example.com / admin)

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

<!--#if ((!no-sample))-->
### Adding New Features

1. **Define the Command/Query** in `src/AppDomain/[Domain]/Commands/` or `Queries/`
2. **Implement the Handler** using Wolverine's handler pattern
3. **Create Integration Events** in `src/AppDomain.Contracts/IntegrationEvents/`
<!--#if (api)-->
4. **Add API Endpoints** in `src/AppDomain.Api/[Domain]/Controller.cs`
<!--#endif-->
<!--#if ((db == "liquibase"))-->
5. **Create Database Migrations** in `infra/AppDomain.Database/Liquibase/`
<!--#endif-->
6. **Write Tests** in `tests/AppDomain.Tests/`

<!--#endif-->

<!--#if (api)-->
## API Documentation

### Accessing API Documentation

- **Scalar UI**: http://localhost:$MainApiHttpPort/scalar
- **OpenAPI Spec**: http://localhost:$MainApiHttpPort/openapi/v1.json
- **gRPC Reflection**: Enabled on port $MainApiGrpcPort

### API Endpoints

<!--#if ((!no-sample))-->
#### Cashiers API

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/cashiers` | List all cashiers |
| GET | `/api/cashiers/{id}` | Get cashier by ID |
| POST | `/api/cashiers` | Create new cashier |
| PUT | `/api/cashiers/{id}` | Update cashier |
| DELETE | `/api/cashiers/{id}` | Delete cashier |

#### Invoices API

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/invoices` | List all invoices |
| GET | `/api/invoices/{id}` | Get invoice by ID |
| POST | `/api/invoices` | Create new invoice |
| POST | `/api/invoices/{id}/pay` | Mark invoice as paid |
<!--#endif-->

### Authentication

The API supports multiple authentication schemes:
- **API Key**: Pass via `X-API-Key` header
- **JWT Bearer**: Standard OAuth 2.0 bearer tokens
- **Basic Auth**: For development/testing only

<!--#endif-->

<!--#if (kafka)-->
## Event-Driven Architecture

### Integration Events

The solution uses Apache Kafka for event streaming:

<!--#if ((!no-sample))-->
| Event | Producer | Consumers | Description |
|-------|----------|-----------|-------------|
| CashierCreated | API | BackOffice | New cashier registered |
| CashierUpdated | API | BackOffice | Cashier details modified |
| InvoiceCreated | API | BackOffice, Orleans | New invoice generated |
| InvoicePaid | API | BackOffice, Orleans | Payment received |
| PaymentReceived | BackOffice | Orleans | Payment processing complete |
<!--#endif-->

### Kafka Topics

Topics follow the naming convention: `AppDomain.[domain].[event-type]`

Example: `AppDomain.cashiers.created`, `AppDomain.invoices.paid`

<!--#endif-->

## Configuration

### Environment Variables

| Variable | Default | Description |
|----------|---------|-------------|
| ASPNETCORE_ENVIRONMENT | Development | Runtime environment |
<!--#if ((db == "npgsql") || (db == "liquibase"))-->
| ConnectionStrings__AppDomainDb | - | PostgreSQL connection string |
| ConnectionStrings__ServiceBus | - | Service bus database connection |
<!--#endif-->
<!--#if (kafka)-->
| Kafka__BootstrapServers | localhost:59092 | Kafka broker addresses |
<!--#endif-->
<!--#if (orleans)-->
| Orleans__ClusterId | dev | Orleans cluster identifier |
| Orleans__ServiceId | AppDomain | Orleans service identifier |
<!--#endif-->

### Application Settings

Configuration files follow the standard .NET pattern:
- `appsettings.json` - Base configuration
- `appsettings.Development.json` - Development overrides
- `appsettings.Production.json` - Production settings

## Deployment

### Docker Build

```bash
<!--#if (api)-->
# Build API image
docker build -f src/AppDomain.Api/Dockerfile -t appdomain-api .

<!--#endif-->
<!--#if (back-office)-->
# Build BackOffice image
docker build -f src/AppDomain.BackOffice/Dockerfile -t appdomain-backoffice .

<!--#endif-->
<!--#if (orleans)-->
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
- `/health/live` - Liveness probe
- `/health/ready` - Readiness probe
- `/health/internal` - Detailed health status

### Metrics

OpenTelemetry metrics are exposed at `/metrics` (Prometheus format)

### Distributed Tracing

<!--#if (aspire)-->
Traces are collected by the Aspire dashboard and can be exported to:
<!--#else-->
Traces can be exported to:
<!--#endif-->
- Jaeger
- Zipkin
- Azure Application Insights
- AWS X-Ray

## Troubleshooting

### Common Issues

#### Database Connection Failed
```bash
# Check PostgreSQL is running
docker ps | grep postgres

# Verify connection string
psql -h localhost -p 54320 -U postgres -d app_domain
```

<!--#if (kafka)-->
#### Kafka Connection Issues
```bash
# Check Kafka is running
docker ps | grep kafka

# Test Kafka connectivity
docker exec -it <kafka-container> kafka-topics.sh --list --bootstrap-server localhost:9092
```
<!--#endif-->

<!--#if (api)-->
#### API Not Responding
```bash
# Check service logs
docker logs appdomain-api

# Verify port binding
netstat -an | grep $MainApiHttpPort
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

## Contributing

Please read [CONTRIBUTING.md](CONTRIBUTING.md) for details on our code of conduct and the process for submitting pull requests.

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Acknowledgments

Built with:
- [.NET 9](https://dot.net)
<!--#if (aspire)-->
- [.NET Aspire](https://learn.microsoft.com/dotnet/aspire)
<!--#endif-->
<!--#if (orleans)-->
- [Microsoft Orleans](https://orleans.io)
<!--#endif-->
- [Wolverine](https://wolverine.netlify.app)
<!--#if (kafka)-->
- [Apache Kafka](https://kafka.apache.org)
<!--#endif-->
<!--#if ((db == "npgsql") || (db == "liquibase"))-->
- [PostgreSQL](https://postgresql.org)
<!--#endif-->
<!--#if ((db == "liquibase"))-->
- [Liquibase](https://liquibase.org)
<!--#endif-->

---

Generated with OrgName