# AppDomain Microservices Solution

A comprehensive .NET 9 microservices solution implementing Domain-Driven Design principles with event-driven architecture. The solution mirrors real-world business operations with separate front office (synchronous APIs) and back office (asynchronous event processing) components.

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

<!--#if (!INCLUDE_SAMPLE)-->

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
```
