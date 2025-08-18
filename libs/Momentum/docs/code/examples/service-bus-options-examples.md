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
    "ServiceBus": "Host=postgres;Database=order_messaging;Username=app;Password=secret"
  }
}
```

## Multi-Environment Configuration

```json
// appsettings.Development.json
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

## Warning Scenarios

```csharp
// Missing connection string warning:
// "ConnectionStrings:ServiceBus is not set. Transactional Inbox/Outbox 
//  and Message Persistence features disabled"

// This allows the application to start without messaging persistence
// but logs a clear warning about reduced functionality
```