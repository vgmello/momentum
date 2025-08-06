---
editLink: false
---

# MissingParamDocsEvent

- **Status:** Active
- **Version:** v1
- **Entity:** `missing`
- **Type:** Integration Event
- **Topic:** `{env}.missing.public.docs.v1`

## Description

Event with missing parameter documentation

## Event Payload

| Property | Type | Required | Description |
| ----------------------------------------------------------------- | --------- | -------- | --------------------------------------------------------------------- |
| TenantId | `Guid` | ✓ | No description available |
| EventId | `string` | ✓ | No description available |

## Technical Details

- **Full Type:** [`Missing.Docs.MissingParamDocsEvent`](https://[github.url.from.config.com]/Missing/Docs/MissingParamDocsEvent.cs)
- **Namespace:** `Missing.Docs`
- **Topic Attribute:** `[EventTopic<Missing>]`