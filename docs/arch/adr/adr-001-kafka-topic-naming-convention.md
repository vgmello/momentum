---
number: ADR-001
title: Kafka Topic Naming Convention
status: Proposed
date: 2025-07-22
---

# Kafka Topic Naming Convention

## Context

As our platform's reliance on Apache Kafka has grown, we have encountered several operational and developmental challenges due to the absence of a standardized topic naming convention. Key problems include:

-   **Ambiguity:** Topic names are often inconsistent and do not clearly communicate their purpose, data owner, or intended audience.

-   **Security Complexity:** The lack of a predictable structure makes it difficult to apply consistent, prefix-based Access Control Lists (ACLs), often forcing the use of complex `DENY` rules.

-   **Ordering Uncertainty:** Developers cannot easily determine if a topic guarantees the chronological order of events for a specific entity, leading to a risk of data consistency bugs in consumer applications.

-   **Schema Management Overhead:** Ad-hoc names complicate schema organization and governance within our Schema Registry.

A clear, enforceable standard is required to address these issues and provide a scalable foundation for our event-driven architecture.

## Decision

We will adopt a mandatory, five-part, dot-separated naming convention for all Kafka topics. This structure is designed to be self-documenting, machine-parsable, and aligned with security best practices.

**The chosen structure is:** `<env>.<domain>.<scope>.<object|event_description>.<version>`

### Component Definitions:

1. **`<env>`:** The deployment environment (e.g., `prod`, `staging`, `dev`).

2. **`<domain>`:** The business domain or bounded context owning the data (e.g., `AppDomain`, `identity`).

3. **`<scope>`:** The visibility and ACL boundary.

    - `public`: For data products intended for cross-domain consumption.

    - `internal`: For data used exclusively within the domain's boundary.

4. **`<object|event_description>`:** The core content identifier.

    - **Entity Topic (Guaranteed Order):** A **plural noun** (e.g., `invoices`, `users`) for topics containing multiple event types for a single entity, partitioned by the entity ID.

    - **Independent Event Topic (No Guaranteed Order):** A **past-tense verb phrase** (e.g., `login_failures`) for topics containing a single, discrete event type.

5. **`<version>`:** The major contract version (e.g., `v1`, `v2`) to manage breaking changes.

## Consequences

### Positive Consequences

-   **Improved Clarity:** Topic names will immediately convey their context, owner, scope, and purpose.

-   **Simplified Security:** The structure enables a simple, `DENY`-free security model using prefix-based ACLs on the `<scope>` component.

-   **Explicit Ordering Guarantees:** The distinction between entity topics (e.g., `users`) and event topics (e.g., `login_failures`) makes ordering behavior explicit.

-   **Safe Migrations:** The `<version>` component provides a clear strategy for introducing breaking changes without impacting existing consumers.

-   **Organized Schema Management:** When used with the `TopicRecordNameStrategy`, this convention allows for clean management of multiple event schemas within a single topic.

### Negative Consequences

-   **Increased Verbosity:** Topic names will be longer than our current ad-hoc names. This is a deliberate trade-off for clarity.

-   **Governance Overhead:** The convention requires enforcement through processes like code reviews, CI/CD checks, or automated tooling to be effective.

-   **Configuration Requirement:** Producers for consolidated entity topics must be correctly configured to use the `TopicRecordNameStrategy` in the Schema Registry.

## Alternatives Considered

### 1. Asymmetric Naming (Implied Public Scope)

-   **Description:** An alternative where `public` topics have no scope segment (e.g., `prod.app_domain.invoices.v1`) and only internal topics have a scope (e.g., `prod.AppDomain.internal.reconciliation.v1`).

-   **Reason for Rejection:** While slightly less verbose for public topics, this model requires the use of `DENY` ACLs to properly secure internal topics, which we aim to avoid. A symmetric structure is safer and easier to manage.

### 2. Single Topic per Event Type (Always)

-   **Description:** A simpler model where every unique event type gets its own topic (e.g., `invoice_created`, `invoice_paid`).

-   **Reason for Rejection:** This model fails to provide ordering guarantees for related events. It is impossible to ensure that an `invoice_paid` event is processed after the corresponding `invoice_created` event, leading to critical data consistency problems.

### 3. Convention without a `<version>` segment

-   **Description:** A four-part structure that omits the contract version.

-   **Reason for Rejection:** This provides no clean mechanism for managing breaking changes. Migrating consumers becomes a high-risk "big bang" deployment rather than a safe, gradual process.
