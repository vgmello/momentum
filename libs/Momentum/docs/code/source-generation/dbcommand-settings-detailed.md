# DbCommand Source Generator Settings Detailed Guide

## MSBuild Integration

These settings are configured through MSBuild properties in the project file:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <DbCommandDefaultParamCase>SnakeCase</DbCommandDefaultParamCase>
    <DbCommandParamPrefix>p_</DbCommandParamPrefix>
  </PropertyGroup>
</Project>
```

## Parameter Name Generation Order

1. **Base Name:** Start with C# property name (e.g., "FirstName")
2. **Column Override:** Apply [Column("custom")] if present → "custom"
3. **Case Conversion:** Apply DbCommandDefaultParamCase if no Column attribute → "first_name"
4. **Global Prefix:** Apply DbCommandParamPrefix → "p_first_name"

## Configuration Precedence

- **Highest:** [Column("name")] attribute on individual properties
- **Medium:** DbCommandAttribute.ParamsCase on individual commands
- **Lowest:** Global MSBuild settings (DbCommandDefaultParamCase, DbCommandParamPrefix)

## Parameter Case Options

- **None:** Use property names as-is (e.g., "FirstName" → "@FirstName")
- **SnakeCase:** Convert to snake_case (e.g., "FirstName" → "@first_name")

## Global Prefix Usage

The DbCommandParamPrefix is useful for:

- **Stored Procedure Conventions:** Some teams prefix all SP parameters (e.g., "p_user_id")
- **Avoiding Keywords:** Prefix to avoid SQL reserved words (e.g., "p_order" instead of "order")
- **Multi-Tenant Systems:** Different prefixes for different tenant databases
- **Legacy System Integration:** Match existing database parameter naming conventions

## Build-Time Resolution

These settings are resolved during compilation from the MSBuild context, allowing different configurations for different build environments (Development, Staging, Production).