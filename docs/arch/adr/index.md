---
title: Architectural Decision Records
---

# Architectural Decision Records (ADRs)

This page serves as a catalog for all Architectural Decision Records (ADRs) made for this project.

> An ADR is a short document that captures an important architectural decision, including its context and consequences.

## Log

The following table lists all ADRs in chronological order. Each title links to the full ADR document.

<!--@include: ./table.md-->

### Status Meanings

::: tip

-   **Proposed**: A decision has been proposed and is under review.
-   **Accepted**: The decision has been approved and should be followed.
-   **Deprecated**: The decision was accepted but is no longer recommended. It should be avoided in new code.
-   **Superseded**: The decision has been replaced by a new ADR.

:::

## How to Propose a New ADR

1.  **Copy the template:** Create a new file in this directory (`/adrs/`) by copying the [`TEMPLATE.md`](./TEMPLATE.md) file.
2.  **Name the file:** Use the format `adr-NNN-short-title.md`, where `NNN` is the next sequential number (e.g., `adr-002-database-migration-strategy.md`).
3.  **Fill out the template:** Detail the context, decision, and consequences following the template structure.
4.  **Submit a Pull Request:** Open a PR to have the ADR reviewed and discussed by the team.
5.  **Update this log:** Once the ADR's status is finalized, add or update its entry in the [`table.md`](./table.md) file.

## Template Structure

The [`TEMPLATE.md`](./TEMPLATE.md) file provides a standardized structure for all ADRs, including:

-   **Frontmatter:** Metadata with number, title, status, and date
-   **Context:** The situation and forces driving the decision
-   **Decision:** The specific choice made and how it addresses the context
-   **Consequences:** Positive, negative, and neutral impacts of the decision
-   **Alternatives Considered:** Other options that were evaluated
-   **Implementation Notes:** Practical guidance for applying the decision

## Why this page is important

Keeping a log of these decisions helps us:

-   Understand the "why" behind our architecture.
-   Onboard new team members more effectively.
-   Avoid repeating past mistakes or discussions.
-   Maintain consistency across architectural decisions.
