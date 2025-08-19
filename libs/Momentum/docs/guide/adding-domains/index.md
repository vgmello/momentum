---
title: Adding Domains
description: Guide for adding new business domains to your Momentum application, following Domain-Driven Design principles and CQRS patterns.
date: 2024-01-15
---

# Adding Domains

Guide for adding new business domains to your Momentum application, following Domain-Driven Design principles and CQRS patterns.

## Overview

Adding domains in Momentum follows a structured approach that maintains consistency, testability, and performance. Each domain encapsulates related business logic and follows established patterns for commands, queries, and data access.

**Key Architectural Principles:**
- **Multi-Tenant by Design**: All entities use composite primary keys with `TenantId`
- **Source Generation**: Database access leverages compile-time code generation
- **Wolverine Message Bus**: Uses Wolverine instead of MediatR for message handling
- **Result<T> Pattern**: Commands and queries return strongly-typed results with error handling
- **Function-Based Database Access**: PostgreSQL functions with `$` prefix for source generation

## Domain Structure

### Standard Domain Layout
```
src/AppDomain/Orders/
├── Commands/
│   ├── CreateOrderCommand.cs
│   ├── UpdateOrderStatusCommand.cs
│   └── CancelOrderCommand.cs
├── Queries/
│   ├── GetOrderByIdQuery.cs
│   ├── GetOrdersByCustomerQuery.cs
│   └── GetOrderHistoryQuery.cs
├── Data/
│   ├── Entities/
│   │   ├── Order.cs
│   │   └── OrderItem.cs
│   └── DbMapper.cs
└── Contracts/
    ├── DomainEvents/
    │   ├── OrderCreated.cs
    │   └── OrderStatusChanged.cs
    └── IntegrationEvents/
        ├── OrderCreated.cs
        └── OrderCompleted.cs
```

### Multi-Tenant Database Functions
```
infra/AppDomain.Database/Liquibase/app_domain/orders/functions/
├── orders_create.sql
├── orders_get_by_id.sql
├── orders_get_by_customer.sql
└── orders_update_status.sql
```

## Step-by-Step Domain Creation

### 1. Define Domain Entities

```csharp
// src/AppDomain/Orders/Data/Entities/Order.cs
using LinqToDB.Mapping;
using Momentum.Extensions.Common.Data;

/// <summary>
///     Represents an order entity in the database with tenant-scoped identification.
/// </summary>
[Table(Schema = "app_domain", Name = "orders")]
public record Order : DbEntity
{
    /// <summary>
    ///     Gets or sets the tenant identifier that owns this order.
    /// </summary>
    [PrimaryKey(Order = 0)]
    [Column("tenant_id")]
    public Guid TenantId { get; set; }

    /// <summary>
    ///     Gets or sets the unique identifier for this order within the tenant.
    /// </summary>
    [PrimaryKey(Order = 1)]
    [Column("order_id")]
    public Guid OrderId { get; set; }

    /// <summary>
    ///     Gets or sets the customer identifier for this order.
    /// </summary>
    [Column("customer_id")]
    public Guid CustomerId { get; set; }

    /// <summary>
    ///     Gets or sets the order date.
    /// </summary>
    [Column("order_date")]
    public DateTime OrderDate { get; set; }

    /// <summary>
    ///     Gets or sets the current status of the order.
    /// </summary>
    [Column("status")]
    public OrderStatus Status { get; set; }

    /// <summary>
    ///     Gets or sets the total amount for the order.
    /// </summary>
    [Column("total_amount")]
    public decimal TotalAmount { get; set; }
}

// src/AppDomain/Orders/Data/Entities/OrderItem.cs
[Table(Schema = "app_domain", Name = "order_items")]
public record OrderItem : DbEntity
{
    [PrimaryKey(Order = 0)]
    [Column("tenant_id")]
    public Guid TenantId { get; set; }

    [PrimaryKey(Order = 1)]
    [Column("order_item_id")]
    public Guid OrderItemId { get; set; }

    [Column("order_id")]
    public Guid OrderId { get; set; }

    [Column("product_id")]
    public Guid ProductId { get; set; }

    [Column("product_name")]
    public string ProductName { get; set; } = string.Empty;

    [Column("quantity")]
    public int Quantity { get; set; }

    [Column("unit_price")]
    public decimal UnitPrice { get; set; }
}

public enum OrderStatus
{
    Pending,
    Confirmed,
    Shipped,
    Delivered,
    Cancelled
}
```

### 2. Create Commands

```csharp
// src/AppDomain/Orders/Commands/CreateOrderCommand.cs
using Momentum.Extensions.Common.Messaging;
using Momentum.Extensions.Common.Results;
using Momentum.Extensions.Database;
using Wolverine;

/// <summary>
///     Command to create a new order in the system.
/// </summary>
/// <param name="TenantId">Unique identifier for the tenant</param>
/// <param name="CustomerId">Unique identifier for the customer</param>
/// <param name="Items">List of items to include in the order</param>
public record CreateOrderCommand(
    Guid TenantId,
    Guid CustomerId,
    IReadOnlyList<CreateOrderItem> Items
) : ICommand<Result<Order>>;

public record CreateOrderItem(
    Guid ProductId,
    string ProductName,
    int Quantity,
    decimal UnitPrice
);

/// <summary>
///     Handler for the CreateOrderCommand using two-tier pattern:
///     1. Business logic validation and processing
///     2. Database command execution via source generation
/// </summary>
public static partial class CreateOrderCommandHandler
{
    /// <summary>
    ///     Database command using function with source generation.
    ///     The '$' prefix generates: SELECT * FROM app_domain.orders_create(...)
    /// </summary>
    [DbCommand(fn: "$app_domain.orders_create")]
    public partial record DbCommand(
        Guid TenantId, 
        Guid CustomerId, 
        string ItemsJson
    ) : ICommand<Result<Guid>>;

    public static async Task<Result<Order>> Handle(
        CreateOrderCommand command, 
        IMessageBus messaging,
        CancellationToken cancellationToken)
    {
        // Business logic validation
        if (!command.Items.Any())
            return Result.Failure<Order>("Order must contain at least one item");

        if (command.Items.Any(item => item.Quantity <= 0))
            return Result.Failure<Order>("All items must have positive quantity");

        // Serialize items to JSON for database function
        var itemsJson = JsonSerializer.Serialize(command.Items);
        
        // Execute database command
        var dbCommand = new DbCommand(command.TenantId, command.CustomerId, itemsJson);
        var result = await messaging.InvokeAsync(dbCommand, cancellationToken);
        
        if (result.IsFailure)
            return Result.Failure<Order>(result.Error);

        // Fetch created order
        var getOrderQuery = new GetOrderByIdQuery.DbQuery(command.TenantId, result.Value);
        var order = await messaging.InvokeAsync(getOrderQuery, cancellationToken);
        
        return order is not null 
            ? Result.Success(order)
            : Result.Failure<Order>("Failed to retrieve created order");
    }
}
```

### 3. Create Queries

```csharp
// src/AppDomain/Orders/Queries/GetOrderByIdQuery.cs
using Momentum.Extensions.Common.Messaging;
using Momentum.Extensions.Database;
using Wolverine;

/// <summary>
///     Query to retrieve a specific order by its identifier.
/// </summary>
/// <param name="TenantId">Unique identifier for the tenant</param>
/// <param name="OrderId">Unique identifier for the order</param>
public record GetOrderByIdQuery(Guid TenantId, Guid OrderId) : IQuery<Order?>;

/// <summary>
///     Handler for the GetOrderByIdQuery using source-generated database access.
/// </summary>
public static partial class GetOrderByIdQueryHandler
{
    /// <summary>
    ///     Database query using function with source generation.
    ///     The '$' prefix generates: SELECT * FROM app_domain.orders_get_by_id(...)
    /// </summary>
    [DbCommand(fn: "$app_domain.orders_get_by_id")]
    public partial record DbQuery(Guid TenantId, Guid OrderId) : IQuery<Order?>;

    public static async Task<Order?> Handle(
        GetOrderByIdQuery query, 
        IMessageBus messaging,
        CancellationToken cancellationToken)
    {
        var dbQuery = new DbQuery(query.TenantId, query.OrderId);
        return await messaging.InvokeAsync(dbQuery, cancellationToken);
    }
}

// src/AppDomain/Orders/Queries/GetOrdersByCustomerQuery.cs
/// <summary>
///     Query to retrieve orders for a specific customer with pagination.
/// </summary>
/// <param name="TenantId">Unique identifier for the tenant</param>
/// <param name="CustomerId">Unique identifier for the customer</param>
/// <param name="Limit">Maximum number of records to return</param>
/// <param name="Offset">Number of records to skip for pagination</param>
public record GetOrdersByCustomerQuery(Guid TenantId, Guid CustomerId, int Limit = 10, int Offset = 0) 
    : IQuery<IEnumerable<GetOrdersByCustomerQuery.Result>>
{
    public record Result(Guid OrderId, DateTime OrderDate, decimal TotalAmount, OrderStatus Status);
}

/// <summary>
///     Handler for the GetOrdersByCustomerQuery using source-generated database access.
/// </summary>
public static partial class GetOrdersByCustomerQueryHandler
{
    /// <summary>
    ///     Database query using function with source generation.
    ///     The '$' prefix generates: SELECT * FROM app_domain.orders_get_by_customer(...)
    /// </summary>
    [DbCommand(fn: "$app_domain.orders_get_by_customer")]
    public partial record DbQuery(Guid TenantId, Guid CustomerId, int Limit, int Offset) 
        : IQuery<IEnumerable<Data.Entities.Order>>;

    public static async Task<IEnumerable<GetOrdersByCustomerQuery.Result>> Handle(
        GetOrdersByCustomerQuery query, 
        IMessageBus messaging,
        CancellationToken cancellationToken)
    {
        var dbQuery = new DbQuery(query.TenantId, query.CustomerId, query.Limit, query.Offset);
        var orders = await messaging.InvokeAsync(dbQuery, cancellationToken);

        return orders.Select(o => new GetOrdersByCustomerQuery.Result(
            o.OrderId, o.OrderDate, o.TotalAmount, o.Status));
    }
}
```

### 4. Define Events

```csharp
// src/AppDomain.Contracts/IntegrationEvents/OrderCreated.cs
using Momentum.Extensions.Kafka.Events;

/// <summary>
///     Integration event published when an order is successfully created.
///     This event is consumed by other bounded contexts and external systems.
/// </summary>
[EventTopic("app_domain.orders.order-created")]
public record OrderCreated(
    Guid TenantId,
    Guid OrderId,
    Guid CustomerId,
    decimal TotalAmount,
    DateTime OrderDate,
    string Status
) : IIntegrationEvent;

// src/AppDomain/Orders/Contracts/DomainEvents/OrderStatusChanged.cs
using Momentum.Extensions.Common.Messaging;

/// <summary>
///     Domain event published when an order's status changes.
///     This event is handled within the same bounded context.
/// </summary>
public record OrderStatusChanged(
    Guid TenantId,
    Guid OrderId,
    OrderStatus OldStatus,
    OrderStatus NewStatus,
    DateTime ChangedAt
) : IDomainEvent;

// src/AppDomain/Orders/Contracts/IntegrationEvents/OrderCompleted.cs
/// <summary>
///     Integration event published when an order is completed.
/// </summary>
[EventTopic("app_domain.orders.order-completed")]
public record OrderCompleted(
    Guid TenantId,
    Guid OrderId,
    Guid CustomerId,
    decimal TotalAmount,
    DateTime CompletedDate
) : IIntegrationEvent;
```

### 5. Database Schema

```sql
-- infra/AppDomain.Database/Liquibase/app_domain/orders/changesets/001-orders-tables.sql
-- Multi-tenant table structure with composite primary keys
CREATE TABLE app_domain.orders (
    tenant_id UUID NOT NULL,
    order_id UUID NOT NULL,
    customer_id UUID NOT NULL,
    order_date TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT timezone('utc', now()),
    status VARCHAR(20) NOT NULL DEFAULT 'Pending',
    total_amount DECIMAL(12,2) NOT NULL,
    created_date_utc TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT timezone('utc', now()),
    updated_date_utc TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT timezone('utc', now()),
    
    -- Composite primary key ensures tenant isolation
    PRIMARY KEY (tenant_id, order_id),
    
    -- Check constraints for business rules
    CONSTRAINT chk_orders_total_amount_positive CHECK (total_amount >= 0),
    CONSTRAINT chk_orders_status_valid CHECK (status IN ('Pending', 'Confirmed', 'Shipped', 'Delivered', 'Cancelled'))
);

CREATE TABLE app_domain.order_items (
    tenant_id UUID NOT NULL,
    order_item_id UUID NOT NULL,
    order_id UUID NOT NULL,
    product_id UUID NOT NULL,
    product_name VARCHAR(255) NOT NULL,
    quantity INTEGER NOT NULL,
    unit_price DECIMAL(10,2) NOT NULL,
    created_date_utc TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT timezone('utc', now()),
    updated_date_utc TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT timezone('utc', now()),
    
    -- Composite primary key for tenant isolation
    PRIMARY KEY (tenant_id, order_item_id),
    
    -- Foreign key to orders table with tenant awareness
    FOREIGN KEY (tenant_id, order_id) REFERENCES app_domain.orders(tenant_id, order_id) ON DELETE CASCADE,
    
    -- Business rule constraints
    CONSTRAINT chk_order_items_quantity_positive CHECK (quantity > 0),
    CONSTRAINT chk_order_items_unit_price_positive CHECK (unit_price >= 0)
);

-- Indexes for efficient queries within tenant boundaries
CREATE INDEX idx_orders_customer_id ON app_domain.orders(tenant_id, customer_id);
CREATE INDEX idx_orders_status ON app_domain.orders(tenant_id, status);
CREATE INDEX idx_orders_order_date ON app_domain.orders(tenant_id, order_date DESC);
CREATE INDEX idx_order_items_order_id ON app_domain.order_items(tenant_id, order_id);
CREATE INDEX idx_order_items_product_id ON app_domain.order_items(tenant_id, product_id);

-- Add table comments for documentation
COMMENT ON TABLE app_domain.orders IS 'Orders table with multi-tenant architecture using composite primary keys';
COMMENT ON TABLE app_domain.order_items IS 'Order items table linked to orders with tenant-aware foreign keys';
```

### 6. Database Functions

```sql
-- infra/AppDomain.Database/Liquibase/app_domain/orders/functions/orders_create.sql
/**
 * Creates a new order with items in a multi-tenant architecture.
 * Returns the newly created order ID or raises an exception on failure.
 */
CREATE OR REPLACE FUNCTION app_domain.orders_create(
    p_tenant_id UUID,
    p_customer_id UUID,
    p_items_json TEXT
) RETURNS UUID AS $$
DECLARE
    v_order_id UUID;
    v_total_amount DECIMAL(12,2) := 0;
    v_items JSONB;
    v_item JSONB;
BEGIN
    -- Parse items JSON
    v_items := p_items_json::JSONB;
    
    -- Validate inputs
    IF p_tenant_id IS NULL THEN
        RAISE EXCEPTION 'Tenant ID cannot be null';
    END IF;
    
    IF p_customer_id IS NULL THEN
        RAISE EXCEPTION 'Customer ID cannot be null';
    END IF;
    
    IF jsonb_array_length(v_items) = 0 THEN
        RAISE EXCEPTION 'Order must contain at least one item';
    END IF;
    
    -- Generate new order ID
    v_order_id := gen_random_uuid();
    
    -- Calculate total amount and validate items
    FOR v_item IN SELECT * FROM jsonb_array_elements(v_items)
    LOOP
        -- Validate item data
        IF (v_item->>'quantity')::INTEGER <= 0 THEN
            RAISE EXCEPTION 'Item quantity must be positive';
        END IF;
        
        IF (v_item->>'unitPrice')::DECIMAL < 0 THEN
            RAISE EXCEPTION 'Item unit price cannot be negative';
        END IF;
        
        v_total_amount := v_total_amount + 
            ((v_item->>'quantity')::INTEGER * (v_item->>'unitPrice')::DECIMAL);
    END LOOP;
    
    -- Create order with tenant isolation
    INSERT INTO app_domain.orders (
        tenant_id, order_id, customer_id, total_amount, status
    ) VALUES (
        p_tenant_id, v_order_id, p_customer_id, v_total_amount, 'Pending'
    );
    
    -- Create order items with tenant isolation
    FOR v_item IN SELECT * FROM jsonb_array_elements(v_items)
    LOOP
        INSERT INTO app_domain.order_items (
            tenant_id, order_item_id, order_id, product_id, 
            product_name, quantity, unit_price
        ) VALUES (
            p_tenant_id,
            gen_random_uuid(),
            v_order_id,
            (v_item->>'productId')::UUID,
            v_item->>'productName',
            (v_item->>'quantity')::INTEGER,
            (v_item->>'unitPrice')::DECIMAL
        );
    END LOOP;
    
    RETURN v_order_id;
EXCEPTION
    WHEN OTHERS THEN
        -- Re-raise with context
        RAISE EXCEPTION 'Failed to create order: %', SQLERRM;
END;
$$ LANGUAGE plpgsql;

-- infra/AppDomain.Database/Liquibase/app_domain/orders/functions/orders_get_by_id.sql
/**
 * Retrieves an order by ID within tenant boundaries.
 */
CREATE OR REPLACE FUNCTION app_domain.orders_get_by_id(
    p_tenant_id UUID,
    p_order_id UUID
) RETURNS TABLE(
    tenant_id UUID,
    order_id UUID,
    customer_id UUID,
    order_date TIMESTAMP WITH TIME ZONE,
    status VARCHAR(20),
    total_amount DECIMAL(12,2),
    created_date_utc TIMESTAMP WITH TIME ZONE,
    updated_date_utc TIMESTAMP WITH TIME ZONE
) AS $$
BEGIN
    RETURN QUERY
    SELECT o.tenant_id, o.order_id, o.customer_id, o.order_date, 
           o.status, o.total_amount, o.created_date_utc, o.updated_date_utc
    FROM app_domain.orders o
    WHERE o.tenant_id = p_tenant_id 
      AND o.order_id = p_order_id;
END;
$$ LANGUAGE plpgsql;

-- infra/AppDomain.Database/Liquibase/app_domain/orders/functions/orders_get_by_customer.sql
/**
 * Retrieves orders for a specific customer with pagination and tenant isolation.
 */
CREATE OR REPLACE FUNCTION app_domain.orders_get_by_customer(
    p_tenant_id UUID,
    p_customer_id UUID,
    p_limit INTEGER DEFAULT 10,
    p_offset INTEGER DEFAULT 0
) RETURNS TABLE(
    tenant_id UUID,
    order_id UUID,
    customer_id UUID,
    order_date TIMESTAMP WITH TIME ZONE,
    status VARCHAR(20),
    total_amount DECIMAL(12,2),
    created_date_utc TIMESTAMP WITH TIME ZONE,
    updated_date_utc TIMESTAMP WITH TIME ZONE
) AS $$
BEGIN
    RETURN QUERY
    SELECT o.tenant_id, o.order_id, o.customer_id, o.order_date, 
           o.status, o.total_amount, o.created_date_utc, o.updated_date_utc
    FROM app_domain.orders o
    WHERE o.tenant_id = p_tenant_id 
      AND o.customer_id = p_customer_id
    ORDER BY o.order_date DESC
    LIMIT p_limit
    OFFSET p_offset;
END;
$$ LANGUAGE plpgsql;
```

### 7. API Endpoints

```csharp
// src/AppDomain.Api/Orders/OrdersEndpoints.cs
using Microsoft.AspNetCore.Mvc;
using Wolverine;
using Momentum.Extensions.Api.Results;
using AppDomain.Orders.Commands;
using AppDomain.Orders.Queries;

/// <summary>
///     REST API endpoints for order management with multi-tenant support.
/// </summary>
public static class OrdersEndpoints
{
    public static void MapOrdersEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/orders")
            .WithTags("Orders")
            .WithOpenApi()
            .RequireAuthorization(); // Ensure authentication for tenant context

        group.MapPost("/", CreateOrder)
            .WithSummary("Create a new order")
            .WithDescription("Creates a new order with the specified items for the authenticated tenant");

        group.MapGet("/{id:guid}", GetOrder)
            .WithSummary("Get order by ID")
            .WithDescription("Retrieves a specific order by ID within the tenant context");

        group.MapGet("/customer/{customerId:guid}", GetOrdersByCustomer)
            .WithSummary("Get orders by customer")
            .WithDescription("Retrieves all orders for a specific customer with pagination");

        group.MapPut("/{id:guid}/status", UpdateOrderStatus)
            .WithSummary("Update order status")
            .WithDescription("Updates the status of an existing order");
    }

    /// <summary>
    ///     Creates a new order using Wolverine message bus.
    /// </summary>
    private static async Task<IResult> CreateOrder(
        [FromBody] CreateOrderRequest request,
        IMessageBus messageBus,
        HttpContext context)
    {
        // Extract tenant ID from authenticated user context
        var tenantId = context.GetTenantId(); // Extension method for tenant extraction
        
        var command = new CreateOrderCommand(
            tenantId,
            request.CustomerId,
            request.Items.Select(i => new CreateOrderItem(
                i.ProductId, i.ProductName, i.Quantity, i.UnitPrice
            )).ToList()
        );
        
        var result = await messageBus.InvokeAsync(command);
        
        return result.Match(
            onSuccess: order => Results.Created($"/orders/{order.OrderId}", order),
            onFailure: error => Results.BadRequest(new { Error = error })
        );
    }

    /// <summary>
    ///     Retrieves an order by ID within tenant context.
    /// </summary>
    private static async Task<IResult> GetOrder(
        Guid id,
        IMessageBus messageBus,
        HttpContext context)
    {
        var tenantId = context.GetTenantId();
        var query = new GetOrderByIdQuery(tenantId, id);
        
        var order = await messageBus.InvokeAsync(query);
        return order is not null ? Results.Ok(order) : Results.NotFound();
    }

    /// <summary>
    ///     Retrieves orders for a customer with pagination.
    /// </summary>
    private static async Task<IResult> GetOrdersByCustomer(
        Guid customerId,
        [FromQuery] int limit = 10,
        [FromQuery] int offset = 0,
        IMessageBus messageBus,
        HttpContext context)
    {
        var tenantId = context.GetTenantId();
        var query = new GetOrdersByCustomerQuery(tenantId, customerId, limit, offset);
        
        var orders = await messageBus.InvokeAsync(query);
        return Results.Ok(new { Orders = orders, Limit = limit, Offset = offset });
    }

    /// <summary>
    ///     Updates order status.
    /// </summary>
    private static async Task<IResult> UpdateOrderStatus(
        Guid id,
        [FromBody] UpdateOrderStatusRequest request,
        IMessageBus messageBus,
        HttpContext context)
    {
        var tenantId = context.GetTenantId();
        var command = new UpdateOrderStatusCommand(tenantId, id, request.Status);
        
        var result = await messageBus.InvokeAsync(command);
        
        return result.Match(
            onSuccess: _ => Results.NoContent(),
            onFailure: error => Results.BadRequest(new { Error = error })
        );
    }
}

/// <summary>
///     Request model for creating a new order.
/// </summary>
public record CreateOrderRequest(
    Guid CustomerId,
    IReadOnlyList<CreateOrderItemRequest> Items
);

public record CreateOrderItemRequest(
    Guid ProductId,
    string ProductName,
    int Quantity,
    decimal UnitPrice
);

/// <summary>
///     Request model for updating order status.
/// </summary>
public record UpdateOrderStatusRequest(OrderStatus Status);
```

### 8. Register Endpoints

```csharp
// src/AppDomain.Api/Program.cs
using AppDomain.Api.Orders;

// Register domain endpoints
app.MapOrdersEndpoints();

// Extension method for tenant context extraction
public static class HttpContextExtensions
{
    /// <summary>
    ///     Extracts the tenant ID from the authenticated user's claims.
    /// </summary>
    public static Guid GetTenantId(this HttpContext context)
    {
        var tenantClaim = context.User.FindFirst("tenant_id")?.Value;
        
        if (string.IsNullOrEmpty(tenantClaim) || !Guid.TryParse(tenantClaim, out var tenantId))
        {
            throw new UnauthorizedAccessException("Invalid or missing tenant context");
        }
        
        return tenantId;
    }
}
```

## Domain Patterns

### Aggregate Roots
Define clear aggregate boundaries:

```csharp
public record Order
{
    public void AddItem(OrderItem item)
    {
        // Business logic for adding items
        // Validate business rules
        // Raise domain events
    }

    public void ChangeStatus(OrderStatus newStatus)
    {
        // Validate status transitions
        // Apply business rules
        // Raise domain events
    }
}
```

### Value Objects
Encapsulate related values:

```csharp
public record Money(decimal Amount, string Currency)
{
    public static Money Zero(string currency) => new(0, currency);
    
    public Money Add(Money other)
    {
        if (Currency != other.Currency)
            throw new InvalidOperationException("Cannot add different currencies");
        
        return new Money(Amount + other.Amount, Currency);
    }
}
```

### Domain Services
Complex business logic that doesn't belong to entities:

```csharp
public class OrderPricingService
{
    public decimal CalculateTotal(IEnumerable<OrderItem> items, Customer customer)
    {
        var subtotal = items.Sum(item => item.Quantity * item.UnitPrice);
        var discount = CalculateDiscount(subtotal, customer);
        var tax = CalculateTax(subtotal - discount);
        
        return subtotal - discount + tax;
    }
}
```

## Testing Domains

### Unit Tests
```csharp
public class CreateOrderHandlerTests
{
    [Fact]
    public async Task CreateOrder_WithValidData_ReturnsOrderId()
    {
        // Arrange
        var command = new CreateOrder(
            CustomerId: Guid.NewGuid(),
            Items: [new CreateOrderItem(Guid.NewGuid(), 2, 19.99m)]
        );

        // Act
        var orderId = await _mediator.Send(command);

        // Assert
        orderId.Should().NotBeEmpty();
    }
}
```

### Integration Tests
```csharp
public class OrdersIntegrationTests : IntegrationTestFixture
{
    [Fact]
    public async Task CreateOrder_EndToEnd_Success()
    {
        // Arrange
        var createCommand = new CreateOrder(
            CustomerId: Guid.NewGuid(),
            Items: [new CreateOrderItem(Guid.NewGuid(), 1, 29.99m)]
        );

        // Act
        var orderId = await SendAsync(createCommand);
        var order = await SendAsync(new GetOrderById(orderId));

        // Assert
        order.Should().NotBeNull();
        order!.TotalAmount.Should().Be(29.99m);
    }
}
```

## Source Generation and Partial Classes

### Understanding Source Generation

Momentum uses source generators to eliminate boilerplate code and ensure type safety. The `[DbCommand]` attribute triggers compile-time code generation:

```csharp
// Your partial class definition
[DbCommand(fn: "$app_domain.orders_create")]
public partial record DbCommand(Guid TenantId, Guid CustomerId, string ItemsJson) 
    : ICommand<Result<Guid>>;

// Generated code (simplified view)
public partial record DbCommand
{
    public static async Task<Result<Guid>> Handle(
        DbCommand command,
        IDbConnection connection,
        CancellationToken cancellationToken)
    {
        // Generated SQL execution code
        var result = await connection.QuerySingleAsync<Guid>(
            "SELECT * FROM app_domain.orders_create(@TenantId, @CustomerId, @ItemsJson)",
            command,
            cancellationToken);
        return Result.Success(result);
    }
}
```

### Function Prefix Convention

The `$` prefix in `fn: "$app_domain.orders_create"` tells the generator to:
1. **Auto-generate SQL**: `SELECT * FROM app_domain.orders_create(...)`
2. **Map parameters**: Automatically map record properties to function parameters
3. **Handle results**: Convert database results to strongly-typed objects
4. **Error handling**: Wrap exceptions in Result<T> pattern

### Partial Class Requirements

```csharp
// ✅ Correct: Partial class with static handler
public static partial class CreateOrderCommandHandler
{
    [DbCommand(fn: "$app_domain.orders_create")]
    public partial record DbCommand(...) : ICommand<Result<Guid>>;
    
    // Your business logic handler
    public static async Task<Result<Order>> Handle(
        CreateOrderCommand command,
        IMessageBus messaging,
        CancellationToken cancellationToken)
    {
        // Business validation
        // Database call via DbCommand
        // Result transformation
    }
}

// ❌ Incorrect: Missing partial keyword
public class CreateOrderCommandHandler { ... }

// ❌ Incorrect: Non-static class
public partial class CreateOrderCommandHandler { ... }
```

## Result<T> Pattern and Error Handling

### Understanding Result<T>

Momentum uses the Result<T> pattern for explicit error handling without exceptions:

```csharp
// Result<T> represents either success with a value or failure with an error
public record Result<T>
{
    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public T Value { get; } // Only available when IsSuccess = true
    public string Error { get; } // Only available when IsFailure = true
}

// Creating results
var success = Result.Success(order); // Result<Order>
var failure = Result.Failure<Order>("Order not found");
```

### Command Result Patterns

```csharp
public static async Task<Result<Order>> Handle(
    CreateOrderCommand command,
    IMessageBus messaging,
    CancellationToken cancellationToken)
{
    // Validation with early returns
    if (!command.Items.Any())
        return Result.Failure<Order>("Order must contain at least one item");
    
    if (command.Items.Any(item => item.Quantity <= 0))
        return Result.Failure<Order>("All items must have positive quantity");
    
    // Database operation
    var dbResult = await messaging.InvokeAsync(new DbCommand(...));
    if (dbResult.IsFailure)
        return Result.Failure<Order>(dbResult.Error);
    
    // Success path
    var order = await GetCreatedOrder(dbResult.Value);
    return order is not null 
        ? Result.Success(order)
        : Result.Failure<Order>("Failed to retrieve created order");
}
```

### API Result Handling

```csharp
// Extension method for Result<T> in APIs
public static IResult ToApiResult<T>(this Result<T> result)
{
    return result.Match(
        onSuccess: value => Results.Ok(value),
        onFailure: error => Results.BadRequest(new { Error = error })
    );
}

// Usage in endpoints
private static async Task<IResult> CreateOrder(
    CreateOrderRequest request,
    IMessageBus messageBus,
    HttpContext context)
{
    var command = new CreateOrderCommand(...);
    var result = await messageBus.InvokeAsync(command);
    
    return result.Match(
        onSuccess: order => Results.Created($"/orders/{order.OrderId}", order),
        onFailure: error => Results.BadRequest(new { Error = error })
    );
}
```

### Query Result Patterns

```csharp
// Queries typically return nullable results instead of Result<T>
public static async Task<Order?> Handle(
    GetOrderByIdQuery query,
    IMessageBus messaging,
    CancellationToken cancellationToken)
{
    var dbQuery = new DbQuery(query.TenantId, query.OrderId);
    return await messaging.InvokeAsync(dbQuery, cancellationToken);
}

// API handling for nullable queries
private static async Task<IResult> GetOrder(
    Guid id,
    IMessageBus messageBus,
    HttpContext context)
{
    var order = await messageBus.InvokeAsync(new GetOrderByIdQuery(...));
    return order is not null ? Results.Ok(order) : Results.NotFound();
}
```

## Best Practices

### Naming Conventions
- **Commands**: Use action verbs with "Command" suffix (CreateOrderCommand, UpdateOrderStatusCommand)
- **Queries**: Use "Get" prefix with "Query" suffix (GetOrderByIdQuery, GetOrdersByCustomerQuery)
- **Events**: Use past tense (OrderCreated, OrderStatusChanged)
- **Entities**: Use singular nouns (Order, OrderItem)
- **Database Functions**: Use underscore notation (orders_create, orders_get_by_id)

### Multi-Tenant Architecture
- **Always include TenantId**: Every entity, command, and query must include tenant context
- **Composite Primary Keys**: Use (tenant_id, entity_id) pattern for all tables
- **Tenant Isolation**: Ensure all queries filter by tenant_id
- **Index Strategy**: Create indexes with tenant_id as the first column

### Performance Considerations
- **Pagination**: Use limit/offset pattern for queries returning multiple records
- **Tenant-Aware Indexing**: All indexes should start with tenant_id
- **Function-Based Access**: Use PostgreSQL functions instead of raw SQL for better performance
- **Source Generation**: Leverage compile-time generation to eliminate runtime reflection

### Error Handling Strategy
- **Commands**: Return Result<T> for operations that can fail
- **Queries**: Return nullable types for single entities, collections for lists
- **Database Errors**: Let functions handle validation and constraints
- **API Layer**: Convert Result<T> to appropriate HTTP responses

### Testing Strategy
- **Unit Tests**: Test business logic in command/query handlers
- **Integration Tests**: Test database functions and complete workflows
- **Architecture Tests**: Enforce domain boundaries and tenant isolation
- **API Tests**: Test complete request/response cycles with tenant context

## Common Patterns

### Validation with FluentValidation
```csharp
// FluentValidation with Wolverine integration
public class CreateOrderCommandValidator : AbstractValidator<CreateOrderCommand>
{
    public CreateOrderCommandValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty().WithMessage("Tenant ID is required");
        RuleFor(x => x.CustomerId).NotEmpty().WithMessage("Customer ID is required");
        RuleFor(x => x.Items).NotEmpty().WithMessage("Order must contain at least one item");
        
        RuleForEach(x => x.Items).ChildRules(item => {
            item.RuleFor(x => x.ProductId).NotEmpty();
            item.RuleFor(x => x.ProductName).NotEmpty().MaximumLength(255);
            item.RuleFor(x => x.Quantity).GreaterThan(0).WithMessage("Quantity must be positive");
            item.RuleFor(x => x.UnitPrice).GreaterThanOrEqualTo(0).WithMessage("Unit price cannot be negative");
        });
    }
}

// Validation is automatically executed by Wolverine before handler execution
```

### Event Publishing Patterns
```csharp
// In command handler - two-tier event publishing
public static async Task<Result<Order>> Handle(
    CreateOrderCommand command,
    IMessageBus messaging,
    CancellationToken cancellationToken)
{
    // Execute database operation
    var result = await ExecuteCreateOrder(command, messaging);
    if (result.IsFailure) return result;
    
    var order = result.Value;
    
    // Publish domain event (internal to bounded context)
    var domainEvent = new OrderStatusChanged(
        order.TenantId, order.OrderId, OrderStatus.Pending, 
        OrderStatus.Pending, DateTime.UtcNow);
    await messaging.PublishAsync(domainEvent, cancellationToken);
    
    // Publish integration event (cross-bounded context)
    var integrationEvent = new OrderCreated(
        order.TenantId, order.OrderId, order.CustomerId,
        order.TotalAmount, order.OrderDate, order.Status.ToString());
    await messaging.PublishAsync(integrationEvent, cancellationToken);
    
    return Result.Success(order);
}
```

### Error Handling Strategies
```csharp
// Domain-specific exceptions for Result<T> pattern
public static class OrderErrors
{
    public static string OrderNotFound(Guid orderId) => $"Order {orderId} not found";
    public static string InvalidOrderStatus(OrderStatus current, OrderStatus requested) => 
        $"Cannot change order status from {current} to {requested}";
    public static string InsufficientInventory(Guid productId, int requested, int available) => 
        $"Insufficient inventory for product {productId}: requested {requested}, available {available}";
}

// Usage in handlers
if (order is null)
    return Result.Failure<Order>(OrderErrors.OrderNotFound(command.OrderId));

if (!IsValidStatusTransition(order.Status, command.NewStatus))
    return Result.Failure<Order>(OrderErrors.InvalidOrderStatus(order.Status, command.NewStatus));

// Global error handling middleware for unhandled exceptions
app.UseExceptionHandler(exceptionHandlerApp =>
{
    exceptionHandlerApp.Run(async context =>
    {
        var exception = context.Features.Get<IExceptionHandlerPathFeature>()?.Error;
        
        var response = exception switch
        {
            UnauthorizedAccessException => (StatusCodes.Status401Unauthorized, "Unauthorized access"),
            ArgumentException argEx => (StatusCodes.Status400BadRequest, argEx.Message),
            _ => (StatusCodes.Status500InternalServerError, "An error occurred")
        };
        
        context.Response.StatusCode = response.Item1;
        await context.Response.WriteAsJsonAsync(new { Error = response.Item2 });
    });
});
```

## Related Topics

- [CQRS](../cqrs/index.md)
- [Database](../database/index.md)
- [Messaging](../messaging/index.md)
- [Testing](../testing/index.md)
- [Template Walkthrough](../template-walkthrough/index.md)