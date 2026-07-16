---
editLink: false
---

<!-- // @formatter:off -->
<!-- prettier-ignore -->

# Dictionary`2

## Description

Represents a dictionary`2 entity.

## Schema

<!-- #region schema -->

| Property | Type | Required | Description |
| -------- | ---- | -------- | ----------- |
| [Comparer](/events/schemas/System.Collections.Generic.IEqualityComparer`1[[System.String, System.Private.CoreLib, Version=10.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]].md)| `IEqualityComparer<string>` | | Gets or sets the comparer. |
| Count| `int` | | Gets or sets the count. |
| Capacity| `int` | | Gets or sets the capacity. |
| [Keys](/events/schemas/System.Collections.Generic.Dictionary`2+KeyCollection[[System.String, System.Private.CoreLib, Version=10.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e],[System.String, System.Private.CoreLib, Version=10.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]].md)| `KeyCollection<string, string>` | | Gets or sets the keys. |
| [Values](/events/schemas/System.Collections.Generic.Dictionary`2+ValueCollection[[System.String, System.Private.CoreLib, Version=10.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e],[System.String, System.Private.CoreLib, Version=10.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]].md)| `ValueCollection<string, string>` | | Gets or sets the values. |
| Item| `string` | | Gets or sets the item. |


<!-- #endregion schema -->

### Reference Schemas

#### IEqualityComparer<string>
<!--@include: @/events/schemas/System.Collections.Generic.IEqualityComparer`1[[System.String, System.Private.CoreLib, Version=10.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]].md#schema-->

#### KeyCollection<string, string>
<!--@include: @/events/schemas/System.Collections.Generic.Dictionary`2+KeyCollection[[System.String, System.Private.CoreLib, Version=10.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e],[System.String, System.Private.CoreLib, Version=10.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]].md#schema-->

#### ValueCollection<string, string>
<!--@include: @/events/schemas/System.Collections.Generic.Dictionary`2+ValueCollection[[System.String, System.Private.CoreLib, Version=10.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e],[System.String, System.Private.CoreLib, Version=10.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]].md#schema-->

