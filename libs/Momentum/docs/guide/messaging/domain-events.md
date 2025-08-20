# Domain Events in Momentum

Domain events represent significant business events that occur within your service boundaries. Unlike integration events, domain events are processed internally and help decouple business logic within the same service.

## What are Domain Events?

Domain events are:
- **Internal to the service**: Processed within the same service boundary
- **Business significant**: Represent important business state changes
- **Synchronous or asynchronous**: Can be processed immediately or queued
- **Decoupling mechanism**: Separate core business logic from side effects

## Domain Event vs Integration Event

| Aspect | Domain Events | Integration Events |
|--------|---------------|-------------------|
| **Scope** | Within service boundary | Cross-service communication |
| **Processing** | Internal handlers | External service handlers |
| **Coupling** | Loose coupling within service | Service decoupling |
| **Transport** | In-memory or local queues | Message brokers (Kafka) |
| **Namespace** | `*.DomainEvents` | `*.IntegrationEvents` |

## Event Definition

Domain events follow a similar pattern to integration events but are kept internal:

```csharp
[EventTopic<InvoiceGenerated>]
public record InvoiceGenerated(
    [PartitionKey] Guid TenantId,
    Guid InvoiceId,
    decimal InvoiceAmount
);
```

### Key Characteristics

1. **Namespace Convention**: Place in `*.DomainEvents` namespace
2. **EventTopic Attribute**: Configure with `Internal = true`
3. **Partition Keys**: Use for ordering when needed
4. **Documentation**: Describe the business event and its triggers

## Real-World Example

From the AppDomain reference implementation:

```csharp
// In AppDomain.Invoices.Contracts.DomainEvents namespace
namespace AppDomain.Invoices.Contracts.DomainEvents;

[EventTopic<InvoiceGenerated>(Internal = true)]
public record InvoiceGenerated(
    [PartitionKey] Guid TenantId,
    Guid InvoiceId,
    decimal InvoiceAmount
);
```

## Publishing Domain Events

### From Command Handlers

Domain events can be published alongside integration events:

```csharp
public static class CreateInvoiceCommandHandler
{
    public record DbCommand(Data.Entities.Invoice Invoice) : ICommand<Data.Entities.Invoice>;

    public static async Task<(Result<Invoice>, (InvoiceCreated?, InvoiceGenerated?))> Handle(
        CreateInvoiceCommand command, 
        IMessageBus messaging,
        CancellationToken cancellationToken)
    {
        // Execute business logic
        var dbCommand = CreateInsertCommand(command);
        var insertedInvoice = await messaging.InvokeCommandAsync(dbCommand, cancellationToken);

        var result = insertedInvoice.ToModel();
        
        // Create integration event (for external services)
        var integrationEvent = new InvoiceCreated(result.TenantId, result);
        
        // Create domain event (for internal processing)
        var domainEvent = new InvoiceGenerated(
            result.TenantId, 
            result.Id, 
            result.Amount
        );

        return (result, (integrationEvent, domainEvent));
    }

    public static async Task<Data.Entities.Invoice> Handle(
        DbCommand command, 
        AppDomainDb db, 
        CancellationToken cancellationToken)
    {
        return await db.Invoices.InsertWithOutputAsync(command.Invoice, token: cancellationToken);
    }

    private static DbCommand CreateInsertCommand(CreateInvoiceCommand command) =>
        new(new Data.Entities.Invoice
        {
            TenantId = command.TenantId,
            InvoiceId = Guid.CreateVersion7(),
            Amount = command.Amount,
            Description = command.Description,
            CashierId = command.CashierId,
            Status = InvoiceStatus.Draft,
            CreatedDateUtc = DateTime.UtcNow,
            UpdatedDateUtc = DateTime.UtcNow
        });
}
```

### Manual Publishing

You can also publish domain events manually:

```csharp
public static class InvoiceService
{
    public static async Task ProcessInvoiceAsync(
        Guid invoiceId,
        IMessageBus messageBus,
        CancellationToken cancellationToken)
    {
        // Business logic here...
        
        // Publish domain event
        var domainEvent = new InvoiceGenerated(tenantId, invoiceId, amount);
        await messageBus.PublishAsync(domainEvent, cancellationToken);
    }
}
```

## Domain Event Handlers

Domain event handlers process events within the same service:

```csharp
// Internal audit handler
public static class InvoiceGeneratedAuditHandler
{
    public static async Task Handle(
        InvoiceGenerated invoiceGenerated,
        IAuditService auditService,
        ILogger<InvoiceGeneratedAuditHandler> logger,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Recording audit entry for invoice {InvoiceId}", 
            invoiceGenerated.InvoiceId);

        await auditService.RecordEventAsync(
            "InvoiceGenerated",
            invoiceGenerated.TenantId,
            invoiceGenerated.InvoiceId,
            new { invoiceGenerated.InvoiceAmount },
            cancellationToken);
    }
}

// Internal notification handler
public static class InvoiceGeneratedNotificationHandler
{
    public static async Task Handle(
        InvoiceGenerated invoiceGenerated,
        INotificationService notificationService,
        CancellationToken cancellationToken)
    {
        // Send internal notifications
        await notificationService.NotifyInvoiceTeamAsync(
            invoiceGenerated.TenantId,
            invoiceGenerated.InvoiceId,
            invoiceGenerated.InvoiceAmount,
            cancellationToken);
    }
}
```

## Common Domain Event Patterns

### State Change Events

Track important state transitions:

```csharp
[EventTopic<InvoiceStatus>(Internal = true)]
public record InvoiceStatusChanged(
    [PartitionKey] Guid TenantId,
    Guid InvoiceId,
    InvoiceStatus FromStatus,
    InvoiceStatus ToStatus,
    DateTime ChangedAt,
    string? Reason = null
);

// Handler for state change
public static class InvoiceStatusChangedHandler
{
    public static async Task Handle(
        InvoiceStatusChanged statusChanged,
        IInvoiceHistoryService historyService,
        CancellationToken cancellationToken)
    {
        await historyService.RecordStatusChangeAsync(
            statusChanged.InvoiceId,
            statusChanged.FromStatus,
            statusChanged.ToStatus,
            statusChanged.ChangedAt,
            statusChanged.Reason,
            cancellationToken);
    }
}
```

### Validation Events

Handle business rule validations:

```csharp
[EventTopic<ValidationResult>(Internal = true)]
public record InvoiceValidationCompleted(
    [PartitionKey] Guid TenantId,
    Guid InvoiceId,
    bool IsValid,
    List<string> ValidationErrors,
    DateTime ValidatedAt
);

public static class InvoiceValidationCompletedHandler
{
    public static async Task Handle(
        InvoiceValidationCompleted validationCompleted,
        IInvoiceService invoiceService,
        CancellationToken cancellationToken)
    {
        if (!validationCompleted.IsValid)
        {
            await invoiceService.MarkAsInvalidAsync(
                validationCompleted.InvoiceId,
                validationCompleted.ValidationErrors,
                cancellationToken);
        }
        else
        {
            await invoiceService.MarkAsValidAsync(
                validationCompleted.InvoiceId,
                cancellationToken);
        }
    }
}
```

### Calculation Events

Trigger calculations and updates:

```csharp
[EventTopic<decimal>(Internal = true)]
public record InvoiceTotalRecalculated(
    [PartitionKey] Guid TenantId,
    Guid InvoiceId,
    decimal OldTotal,
    decimal NewTotal,
    string RecalculationReason
);

public static class InvoiceTotalRecalculatedHandler
{
    public static async Task Handle(
        InvoiceTotalRecalculated recalculated,
        IReportingService reportingService,
        CancellationToken cancellationToken)
    {
        await reportingService.UpdateInvoiceTotalsAsync(
            recalculated.InvoiceId,
            recalculated.NewTotal,
            cancellationToken);
    }
}
```

## Event Ordering and Processing

### Sequential Processing

Use partition keys to ensure events are processed in order:

```csharp
[EventTopic<InvoiceId>(Internal = true)]
public record InvoiceProcessingStep(
    [PartitionKey] Guid InvoiceId,  // Ensures sequential processing per invoice
    string StepName,
    InvoiceProcessingStatus Status,
    DateTime ProcessedAt
);
```

### Parallel Processing

For events that can be processed independently, avoid partition keys or use different keys:

```csharp
[EventTopic<Guid>(Internal = true)]
public record InvoiceMetricsUpdated(
    Guid TenantId,  // No PartitionKey - can be processed in parallel
    Guid InvoiceId,
    Dictionary<string, object> Metrics
);
```

## Error Handling

Domain events support the same error handling patterns as integration events:

```csharp
public static class InvoiceGeneratedHandler
{
    public static async Task Handle(
        InvoiceGenerated invoiceGenerated,
        IBusinessRuleService businessRuleService,
        ILogger<InvoiceGeneratedHandler> logger,
        CancellationToken cancellationToken)
    {
        try
        {
            await businessRuleService.ApplyBusinessRulesAsync(
                invoiceGenerated.InvoiceId,
                cancellationToken);
        }
        catch (BusinessRuleException ex)
        {
            logger.LogWarning("Business rule failed for invoice {InvoiceId}: {Message}",
                invoiceGenerated.InvoiceId, ex.Message);
            
            // Don't retry business rule failures
            return;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to process invoice generated event for {InvoiceId}",
                invoiceGenerated.InvoiceId);
            
            // Re-throw to trigger retry
            throw;
        }
    }
}
```

## Testing Domain Events

### Testing Event Publishing

```csharp
[Test]
public async Task Handle_CreateInvoice_PublishesDomainEvent()
{
    // Arrange
    var command = new CreateInvoiceCommand(
        Guid.NewGuid(),
        Guid.NewGuid(),
        100.00m,
        "Test Invoice"
    );

    var mockMessaging = new Mock<IMessageBus>();
    // ... setup mocks

    // Act
    var (result, (integrationEvent, domainEvent)) = await CreateInvoiceCommandHandler.Handle(
        command, mockMessaging.Object, CancellationToken.None);

    // Assert
    result.IsSuccess.Should().BeTrue();
    domainEvent.Should().NotBeNull();
    domainEvent!.InvoiceId.Should().Be(result.Value.Id);
    domainEvent.InvoiceAmount.Should().Be(command.Amount);
}
```

### Testing Event Handlers

```csharp
[Test]
public async Task Handle_InvoiceGenerated_RecordsAuditEntry()
{
    // Arrange
    var domainEvent = new InvoiceGenerated(
        Guid.NewGuid(),
        Guid.NewGuid(),
        150.00m
    );

    var mockAuditService = new Mock<IAuditService>();
    var logger = new Mock<ILogger<InvoiceGeneratedAuditHandler>>();

    // Act
    await InvoiceGeneratedAuditHandler.Handle(
        domainEvent,
        mockAuditService.Object,
        logger.Object,
        CancellationToken.None);

    // Assert
    mockAuditService.Verify(
        x => x.RecordEventAsync(
            "InvoiceGenerated",
            domainEvent.TenantId,
            domainEvent.InvoiceId,
            It.IsAny<object>(),
            It.IsAny<CancellationToken>()),
        Times.Once);
}
```

## Advanced Patterns

### Event Sourcing

Use domain events to build event-sourced aggregates:

```csharp
public class InvoiceAggregate
{
    private readonly List<InvoiceDomainEvent> _uncommittedEvents = new();
    
    public void Apply(InvoiceGenerated @event)
    {
        // Apply event to aggregate state
        _uncommittedEvents.Add(@event);
    }
    
    public void Apply(InvoiceStatusChanged @event)
    {
        // Apply event to aggregate state
        _uncommittedEvents.Add(@event);
    }
    
    public IReadOnlyList<InvoiceDomainEvent> GetUncommittedEvents()
    {
        return _uncommittedEvents.AsReadOnly();
    }
    
    public void MarkEventsAsCommitted()
    {
        _uncommittedEvents.Clear();
    }
}
```

### Saga Coordination

Use domain events to coordinate complex business processes:

```csharp
[EventTopic<InvoiceId>(Internal = true)]
public record InvoiceProcessingStarted(
    [PartitionKey] Guid InvoiceId,
    Guid TenantId,
    List<string> RequiredSteps
);

public static class InvoiceProcessingSagaHandler
{
    public static async Task Handle(
        InvoiceProcessingStarted processingStarted,
        ISagaOrchestrator orchestrator,
        CancellationToken cancellationToken)
    {
        await orchestrator.StartProcessingAsync(
            processingStarted.InvoiceId,
            processingStarted.RequiredSteps,
            cancellationToken);
    }
}
```

## Best Practices

### Event Design

1. **Keep events focused**: One event should represent one business concept
2. **Include context**: Events should contain enough information for handlers
3. **Use meaningful names**: Event names should describe what happened
4. **Consider timing**: Think about when events should be published

### Handler Design

1. **Single responsibility**: Each handler should have one job
2. **Idempotent operations**: Handlers should be safe to run multiple times
3. **Quick processing**: Keep handlers fast, queue heavy work if needed
4. **Error handling**: Handle both business and technical errors appropriately

### Performance

1. **Async processing**: Use async handlers for non-critical side effects
2. **Batch operations**: Group related operations when possible
3. **Monitor throughput**: Watch for processing bottlenecks
4. **Resource management**: Be mindful of database connections and memory usage

### Testing

1. **Test event publishing**: Verify events are created correctly
2. **Test handlers independently**: Unit test each handler in isolation
3. **Integration testing**: Test the full event flow end-to-end
4. **Mock external dependencies**: Use mocks for external services

## Configuration

Domain events are processed using the same infrastructure as integration events but remain within the service boundary. They can be:

- Processed in-memory for immediate side effects
- Queued in local PostgreSQL queues for reliable processing
- Handled synchronously or asynchronously based on requirements

## Next Steps

- Learn about [Integration Events](./integration-events) for cross-service communication
- Understand [Kafka Configuration](./kafka) for message broker setup
- Explore [Wolverine](./wolverine) messaging framework details
- Event Sourcing patterns (coming soon)