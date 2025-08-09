---
title: Port Allocation Pattern
description: Standardized port allocation strategy for Momentum microservices
date: 2024-01-15
---

# Port Allocation Pattern

Momentum uses a systematic port allocation strategy to ensure consistent and predictable service endpoints across development, testing, and production environments.

## Port Assignment Pattern

The system follows a structured approach to port allocation that makes it easy to remember and configure service endpoints.

### Aspire Dashboard Ports

The Aspire Dashboard uses a special port range offset by 10,000 from the base service port:

- **HTTP**: Service base + 10,000 (e.g., 8100 → 18100)
- **HTTPS**: Service base + 10,010 (e.g., 8100 → 18110)

### Service Port Pattern

Each service block uses a range of 20 ports (XX00-XX19), with specific assignments:

```
Aspire Resource Service: XX00 (HTTP) / XX10 (HTTPS)
Main API:               XX01 (HTTP) / XX11 (HTTPS) / XX02 (gRPC-HTTP)
BackOffice:             XX03 (HTTP) / XX13 (HTTPS)
Orleans:                XX04 (HTTP) / XX14 (HTTPS)
UI/Frontend:            XX05 (HTTP) / XX15 (HTTPS)
Documentation:          XX19 (reserved for last port of range)
```

### Default Port Assignments (8100-8119)

#### Aspire Services
- **18100**: Aspire Dashboard (HTTP)
- **18110**: Aspire Dashboard (HTTPS)
- **8100**: Aspire Resource Service (HTTP)
- **8110**: Aspire Resource Service (HTTPS)

#### Application Services
- **8101**: API Service (HTTP)
- **8111**: API Service (HTTPS)
- **8102**: API Service (gRPC insecure)
- **8103**: BackOffice Service (HTTP)
- **8113**: BackOffice Service (HTTPS)
- **8104**: Orleans Service (HTTP)
- **8114**: Orleans Service (HTTPS)
- **8105**: UI/Frontend Service (HTTP)
- **8115**: UI/Frontend Service (HTTPS)
- **8119**: Documentation Service

#### Infrastructure Services
- **54320**: PostgreSQL Database
- **9092**: Apache Kafka (standard port)
- **4317**: OpenTelemetry OTLP (gRPC)
- **4318**: OpenTelemetry OTLP (HTTP)

## Multi-Domain Configuration

When running multiple domains or services, allocate port blocks in increments of 100:

### Domain 1 (8100-8119)
```
API:        8101 (HTTP) / 8111 (HTTPS) / 8102 (gRPC)
BackOffice: 8103 (HTTP) / 8113 (HTTPS)
Orleans:    8104 (HTTP) / 8114 (HTTPS)
UI:         8105 (HTTP) / 8115 (HTTPS)
```

### Domain 2 (8200-8219)
```
API:        8201 (HTTP) / 8211 (HTTPS) / 8202 (gRPC)
BackOffice: 8203 (HTTP) / 8213 (HTTPS)
Orleans:    8204 (HTTP) / 8214 (HTTPS)
UI:         8205 (HTTP) / 8215 (HTTPS)
```

### Domain 3 (8300-8319)
```
API:        8301 (HTTP) / 8311 (HTTPS) / 8302 (gRPC)
BackOffice: 8303 (HTTP) / 8313 (HTTPS)
Orleans:    8304 (HTTP) / 8314 (HTTPS)
UI:         8305 (HTTP) / 8315 (HTTPS)
```

## Configuration in Code

### Aspire AppHost Configuration

```csharp
// In your AppHost Program.cs
var builder = DistributedApplication.CreateBuilder(args);

// API Service
var api = builder.AddProject<Projects.YourDomain_Api>("api")
    .WithHttpEndpoint(port: 8101, name: "http")
    .WithHttpsEndpoint(port: 8111, name: "https")
    .WithEndpoint("grpc", scheme: "http", port: 8102);

// BackOffice Service
var backOffice = builder.AddProject<Projects.YourDomain_BackOffice>("backoffice")
    .WithHttpEndpoint(port: 8103, name: "http")
    .WithHttpsEndpoint(port: 8113, name: "https");

// Orleans Service
var orleans = builder.AddProject<Projects.YourDomain_Orleans>("orleans")
    .WithHttpEndpoint(port: 8104, name: "http")
    .WithHttpsEndpoint(port: 8114, name: "https");

// UI Service
var ui = builder.AddProject<Projects.YourDomain_UI>("ui")
    .WithHttpEndpoint(port: 8105, name: "http")
    .WithHttpsEndpoint(port: 8115, name: "https");

// Documentation
var docs = builder.AddProject<Projects.YourDomain_Docs>("docs")
    .WithHttpEndpoint(port: 8119, name: "http");
```

### Environment Variables

Set environment-specific ports using environment variables:

```bash
# .env file or environment configuration
ASPNETCORE_URLS=http://+:8101;https://+:8111
GRPC_PORT=8102
ASPIRE_DASHBOARD_URL=https://localhost:18110
```

### Docker Compose Configuration

```yaml
services:
  api:
    ports:
      - "8101:8101"  # HTTP
      - "8111:8111"  # HTTPS
      - "8102:8102"  # gRPC
  
  backoffice:
    ports:
      - "8103:8103"  # HTTP
      - "8113:8113"  # HTTPS
  
  orleans:
    ports:
      - "8104:8104"  # HTTP
      - "8114:8114"  # HTTPS
  
  ui:
    ports:
      - "8105:8105"  # HTTP
      - "8115:8115"  # HTTPS
  
  docs:
    ports:
      - "8119:8119"  # Documentation
```

## Production Considerations

### Load Balancer Configuration

In production, services typically run behind a load balancer on standard ports:

```nginx
# Example NGINX configuration
upstream api_backend {
    server api-1:8101;
    server api-2:8101;
    server api-3:8101;
}

server {
    listen 443 ssl http2;
    server_name api.yourdomain.com;
    
    location / {
        proxy_pass http://api_backend;
    }
    
    location /grpc {
        grpc_pass grpc://api-1:8102;
    }
}
```

### Kubernetes Service Configuration

```yaml
apiVersion: v1
kind: Service
metadata:
  name: api-service
spec:
  ports:
    - name: http
      port: 80
      targetPort: 8101
    - name: https
      port: 443
      targetPort: 8111
    - name: grpc
      port: 8102
      targetPort: 8102
  selector:
    app: api
```

## Port Conflict Resolution

If you encounter port conflicts:

1. **Check for running services**:
   ```bash
   # Linux/Mac
   lsof -i :8101
   
   # Windows
   netstat -ano | findstr :8101
   ```

2. **Override ports via environment variables**:
   ```bash
   export ASPNETCORE_URLS="http://+:9101;https://+:9111"
   dotnet run --project src/YourDomain.Api
   ```

3. **Use dynamic port allocation** for testing:
   ```csharp
   // In test configuration
   builder.WebHost.UseUrls("http://localhost:0");
   ```

## Best Practices

1. **Document your port allocations** in your project README
2. **Use environment variables** for configuration flexibility
3. **Reserve port ranges** for different environments (dev, staging, prod)
4. **Implement health checks** on all service ports
5. **Use service discovery** in production instead of hardcoded ports
6. **Monitor port usage** to prevent conflicts

## Port Allocation Table Template

Use this template to document your service port allocations:

| Service | HTTP | HTTPS | gRPC | Notes |
|---------|------|-------|------|-------|
| Aspire Dashboard | 18100 | 18110 | - | Development only |
| API Gateway | 8101 | 8111 | 8102 | Public facing |
| BackOffice | 8103 | 8113 | - | Internal only |
| Orleans | 8104 | 8114 | - | Stateful processing |
| UI/Frontend | 8105 | 8115 | - | Web application |
| Documentation | 8119 | - | - | Development only |
| PostgreSQL | 54320 | - | - | Database |
| Kafka | 9092 | - | - | Message broker |
| OpenTelemetry | - | - | 4317 | Observability |

## Troubleshooting

### Common Issues

**Port already in use**:
- Check for zombie processes
- Verify Docker containers are properly stopped
- Use `docker compose down` to clean up

**Cannot connect to service**:
- Verify firewall rules
- Check if service is binding to correct interface (0.0.0.0 vs localhost)
- Ensure HTTPS certificates are properly configured

**Service discovery not working**:
- Verify Aspire configuration
- Check environment variable propagation
- Review service registration logs

For more details on service configuration, see the [Service Configuration Guide](./service-defaults).