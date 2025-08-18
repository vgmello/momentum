# Kafka Events Configuration Detailed Guide

## Overview

Sets up distributed event routing for Kafka based on event attributes with comprehensive support for event discovery, topic routing, and partition management.

## Features

- Discovers all distributed event types from domain assemblies
- Configures Kafka topic routing based on EventTopicAttribute
- Sets up partition key routing using PartitionKeyAttribute or IDistributedEvent.GetPartitionKey()
- Generates environment-specific topic names (dev, test, prod)
- Automatically configures subscriptions based on integration event handlers

## Event Discovery

### Integration Events

Integration events are identified by:

- Having their namespace end with "IntegrationEvents"
- Being decorated with EventTopicAttribute

### Domain Events

Domain events are identified by:

- Having their namespace end with "DomainEvents"
- Being decorated with EventTopicAttribute

## Configuration Features

### Topic Routing

Automatic topic routing configuration based on:

- EventTopicAttribute settings for topic names
- Domain configuration for namespace organization
- Environment-specific prefixing for isolation

### Partition Management

Intelligent partition key assignment using:

- PartitionKeyAttribute for explicit partition key specification
- IDistributedEvent.GetPartitionKey() method for dynamic keys
- Default routing strategies for optimal message distribution

### Subscription Management

Automatic subscription setup for:

- Integration event handlers in the current service
- Cross-service event consumption patterns
- Dead letter queue handling for failed messages

## Environment Isolation

Support for environment-specific topic naming to ensure proper isolation between development, testing, and production environments.