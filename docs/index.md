---
layout: home

hero:
    name: "AppDomain Solution"
    text: "Enterprise AppDomain Management"
    tagline: A comprehensive system for managing cashiers, invoices, and AppDomain operations with event-driven architecture
    actions:
        - theme: brand
          text: API Definition
          link: "http://localhost:8101/scalar"
        - theme: alt
          text: AppDomain Events
          link: /events/
        - theme: alt
          text: New Dev
          link: /guide/getting-started

<!-- prettier-ignore-start -->

features:
<!--#if (INCLUDE_SAMPLE)-->
    - title: 💰 Billing & Invoicing
      details: Complete app_domain system with invoices, payments, and cashier management. Multi-tenant support with comprehensive audit trails and event-driven architecture.
      link: /guide/bills/
    - title: 👥 Cashier Management
      details: Full cashier lifecycle management with role-based access control, activity tracking, and integration with invoice processing workflows.
      link: /guide/cashiers/
    - title: 📄 Invoice Processing
      details: End-to-end invoice workflow from creation to payment. Includes validation, state management, and automated event publishing for downstream systems.
      link: /guide/invoices/
<!--#else-->
    - title: 🎯 Domain-Driven Design
      details: Build your business domains with clean separation of concerns. CQRS patterns, event sourcing ready, and vertical slice architecture.
      link: /guide/adding-domains/
    - title: 🔄 Event-Driven Architecture
      details: Asynchronous messaging with Kafka, integration events, and Orleans stateful processing for complex workflows.
      link: /guide/messaging/
    - title: 🛡️ Production-Ready Infrastructure
      details: Built-in observability with OpenTelemetry, health checks, distributed tracing, and comprehensive testing with Testcontainers.
      link: /guide/service-configuration/
<!--#endif-->
---

<!-- prettier-ignore-end -->
