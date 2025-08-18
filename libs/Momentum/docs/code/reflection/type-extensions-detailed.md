# TypeExtensions Detailed Documentation

This file contains detailed documentation for the TypeExtensions class and its methods.

## Class Overview {#class-overview}

This class contains utilities specifically designed to work with modern C# language features
including records, init-only properties, and primary constructors. These extensions are particularly
useful for framework code that needs to discover attributes and metadata from both traditional
properties and record parameters.

Key scenarios supported:
- Attribute discovery on record parameters and their corresponding properties
- Primary constructor detection for records and classes
- Init-only property identification for immutable data patterns
- Reflection-based metadata extraction for validation and serialization

## GetPropertiesWithAttribute Method {#get-properties-with-attribute}

This method is essential for working with C# records where attributes can be applied
to constructor parameters and are not automatically inherited by the generated properties.
It searches both the property itself and the matching constructor parameter.

The method handles the following scenarios:
- Traditional properties with attributes directly applied
- Record properties where attributes are applied to constructor parameters
- Mixed scenarios where some properties have direct attributes and others have parameter attributes
- Classes with primary constructors (C# 12+ feature)

This functionality is commonly used by validation frameworks, serializers,
and other reflection-based libraries that need to discover metadata consistently
across different property declaration styles.

## GetCustomAttribute Method {#get-custom-attribute}

This method implements a two-stage attribute lookup strategy:
1. First, check if the attribute is directly applied to the property
2. If not found and a primary constructor is provided, check the corresponding constructor parameter

This approach is necessary because in C# records, attributes applied to constructor
parameters are not automatically inherited by the generated properties. This method provides
a unified way to retrieve attributes regardless of where they were originally declared.

The method matches constructor parameters to properties by name and type, ensuring
that the correct parameter attribute is associated with the corresponding property.

## GetPrimaryConstructor Method {#get-primary-constructor}

This method implements a heuristic approach to identify primary constructors by:
1. Finding all init-only properties on the type
2. Locating non-compiler-generated constructors
3. Matching constructor parameters to init-only properties by name and type
4. Returning the constructor where all parameters have corresponding init-only properties

This approach is particularly effective for:
- C# records (which always have primary constructors)
- Classes with primary constructors (C# 12+ feature)
- Immutable classes following init-only property patterns
- DTOs and value objects designed for immutability

**Important:** This method relies on naming conventions and property patterns.
It may not work correctly for types with multiple constructors that could match the heuristics,
or for types that don't follow standard immutable object patterns.

## IsInitOnly Method {#is-init-only}

This method detects init-only properties by examining the property's setter method
for the presence of the `IsExternalInit` modifier. Init-only properties
can only be set during object initialization (in constructors, initializers, or with statements).

Init-only properties are commonly used in:
- Immutable data transfer objects (DTOs)
- Value objects and entity models
- Configuration objects
- Record types (which use init-only properties by default)
- API request/response models

This information is useful for framework code that needs to distinguish between
mutable and immutable properties, such as serializers, validators, and ORM mapping.