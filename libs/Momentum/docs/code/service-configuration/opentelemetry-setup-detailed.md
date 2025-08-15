# OpenTelemetry Setup Detailed Configuration Guide

## Overview

This method configures a complete observability stack suitable for cloud-native microservices with comprehensive instrumentation for logging, metrics, and distributed tracing.

## Distributed Tracing

- W3C Trace Context and Baggage propagation for cross-service correlation
- ASP.NET Core request tracing with health check filtering
- HTTP client instrumentation with sensitive path exclusion
- gRPC client tracing with URI enrichment
- Environment-aware sampling (100% dev, 10% production)

## Metrics Collection

- ASP.NET Core metrics (request duration, response status)
- HTTP client metrics (request duration, retry counts)
- .NET runtime metrics (GC, memory, thread pool)
- Wolverine messaging metrics (message processing, errors)
- Custom application metrics via configurable meters

## OTLP Export

- OTLP exporter for compatibility with Jaeger, Zipkin, and cloud providers
- Automatic resource attribution with environment labeling
- Efficient batching and compression for production workloads

## Configuration Options

| Setting | Description |
|---------|-------------|
| OpenTelemetry:ActivitySourceName | Custom activity source name (defaults to application name) |
| OpenTelemetry:MessagingMeterName | Custom meter name for messaging metrics (defaults to {AppName}.Messaging) |
| OTEL_EXPORTER_OTLP_ENDPOINT | OTLP collector endpoint (e.g., http://jaeger:4317) |
| OTEL_SERVICE_NAME | Service name for telemetry attribution |

## Features

### Environment-Aware Configuration

The setup automatically adjusts sampling rates and export configurations based on the hosting environment:

- **Development**: 100% sampling for complete visibility during development
- **Production**: Optimized sampling rates to balance performance and observability

### Instrumentation Coverage

Comprehensive instrumentation across the entire application stack:

- **HTTP Traffic**: Complete request/response tracking with timing and status codes
- **Database Operations**: Query timing and performance monitoring
- **Message Processing**: End-to-end message flow tracking
- **Custom Business Logic**: Support for custom activity sources and meters

### Performance Optimization

Production-ready configuration with:

- Efficient batching to minimize network overhead
- Compression for reduced bandwidth usage
- Smart filtering to exclude noisy endpoints like health checks
- Resource attribution for proper service identification