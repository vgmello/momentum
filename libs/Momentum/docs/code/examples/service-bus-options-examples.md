## Basic Configuration in appsettings.json

```json
{
    "ServiceBus": {
        "Domain": "ECommerce",
        "PublicServiceName": "order-service",
        "CloudEvents": {
            "Source": "https://api.mystore.com/orders",
            "DefaultType": "com.mystore.orders"
        }
    },
    "ConnectionStrings": {
        // Standalone fallback only: used when no application NpgsqlDataSource is
        // registered in DI. A generated service reuses its application data source
        // (e.g. AppDomainDb) automatically and omits this entry.
        "ServiceBus": "Host=postgres;Database=order_messaging;Username=app;Password=secret"
    }
}
```

## Multi-Environment Configuration

```json
// appsettings.Local.json (excluded from Docker — local overrides)
{
  "ServiceBus": {
    "Domain": "ECommerce",
    "PublicServiceName": "order-service-dev"
  }
}

// appsettings.Production.json
{
  "ServiceBus": {
    "Domain": "ECommerce",
    "PublicServiceName": "order-service",
    "CloudEvents": {
      "Source": "https://api.production.mystore.com/orders",
      "Subject": "orders"
    }
  }
}
```

## Service URN Generation Examples

```csharp
// Domain: "ECommerce", PublicServiceName: "order-service"
// Generated URN: "/e_commerce/order-service"

// Domain: "CustomerManagement", PublicServiceName: "customer-api"
// Generated URN: "/customer_management/customer-api"
```

## CloudEvents Configuration

```json
{
    "ServiceBus": {
        "CloudEvents": {
            "Source": "https://api.mystore.com/orders",
            "DefaultType": "com.mystore.orders",
            "Subject": "orders",
            "DataContentType": "application/json"
        }
    }
}
```

## Generated CloudEvent Example

```json
{
    "specversion": "1.0",
    "type": "com.mystore.orders.order-created",
    "source": "https://api.mystore.com/orders",
    "subject": "orders/12345",
    "id": "550e8400-e29b-41d4-a716-446655440000",
    "time": "2024-01-15T10:30:00Z",
    "datacontenttype": "application/json",
    "data": {
        "orderId": "12345",
        "customerId": "67890",
        "totalAmount": 99.99
    }
}
```

## Service Name Conversion Examples

```csharp
GetServiceName("ECommerce.OrderService") // Returns: "ecommerce-orderservice"
GetServiceName("Customer.API") // Returns: "customer-api"
GetServiceName("payment-processor") // Returns: "payment-processor"
```

## Automatic Configuration Example

```csharp
// Given application name: "ECommerce.OrderService"
// And configuration:
{
  "ServiceBus": {
    "Domain": "ECommerce"
    // PublicServiceName not specified
  }
}

// After post-configuration:
// PublicServiceName = "ecommerce-orderservice" (derived from app name)
// ServiceUrn = "/e_commerce/ecommerce-orderservice" (generated URN)
```

## Missing Data Source Scenario

```csharp
// Standalone use with neither an application NpgsqlDataSource registered in DI
// nor a ServiceBus connection string configured throws at startup:
// "No NpgsqlDataSource is registered and the DB string 'ServiceBus' is not set."

// A generated service always registers an application NpgsqlDataSource, so message
// persistence reuses it automatically and this path is never hit. For standalone
// library use, provide ConnectionStrings:ServiceBus as the fallback.
```
