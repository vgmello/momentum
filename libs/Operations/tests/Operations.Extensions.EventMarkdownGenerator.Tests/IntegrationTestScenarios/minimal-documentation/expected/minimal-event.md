---
editLink: false
---

# MinimalEvent

- **Status:** Active
- **Version:** v1
- **Entity:** `minimal`
- **Type:** Integration Event
- **Topic:** `{env}.events.public.core.v1`

## Description

Simple event with minimal documentation

## Event Payload

| Property | Type | Required | Description |
| ----------------------------------------------------------------- | --------- | -------- | --------------------------------------------------------------------- |
| Id | `Guid` | ✓ | Event identifier |

## Technical Details

- **Full Type:** [`Events.Core.MinimalEvent`](https://[github.url.from.config.com]/Events/Core/MinimalEvent.cs)
- **Namespace:** `Events.Core`
- **Topic Attribute:** `[EventTopic<Minimal>]`