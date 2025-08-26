# Integration Testing in Momentum

Integration tests verify that different components of your Momentum application work correctly together. They test the complete workflow from HTTP requests to database operations and event publishing.

## Overview

Integration tests in Momentum applications typically cover:

- **End-to-end API workflows**: From HTTP request to response
- **Database operations**: Real database interactions with transactions
- **Event publishing**: Integration events being published to Kafka
- **Service interactions**: Communication between different services
- **Authentication and authorization**: Security workflows

## Test Infrastructure Setup

### Using Testcontainers

Momentum applications use Testcontainers to provide real infrastructure for testing:

```csharp
public class IntegrationTestFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _dbContainer;
    private readonly KafkaContainer _kafkaContainer;
    private WebApplicationFactory<Program> _factory = default!;

    public IntegrationTestFixture()
    {
        _dbContainer = new PostgreSqlBuilder()
            .WithDatabase("test_db")
            .WithUsername("test_user")
            .WithPassword("test_password")
            .WithCleanUp(true)
            .Build();

        _kafkaContainer = new KafkaBuilder()
            .WithCleanUp(true)
            .Build();
    }

    public HttpClient HttpClient { get; private set; } = default!;
    public IServiceProvider Services { get; private set; } = default!;
    public string ConnectionString => _dbContainer.GetConnectionString();
    public string KafkaBootstrapServers => _kafkaContainer.GetBootstrapAddress();

    public async Task InitializeAsync()
    {
        // Start containers
        await _dbContainer.StartAsync();
        await _kafkaContainer.StartAsync();

        // Create test application
        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureAppConfiguration((context, config) =>
                {
                    config.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        ["ConnectionStrings:DefaultConnection"] = ConnectionString,
                        ["Kafka:BootstrapServers"] = KafkaBootstrapServers,
                        ["Kafka:GroupId"] = "test-consumer-group"
                    });
                });

                builder.ConfigureServices(services =>
                {
                    // Override production services for testing
                    services.RemoveAll<IEmailService>();
                    services.AddScoped<IEmailService, TestEmailService>();
                });
            });

        HttpClient = _factory.CreateClient();
        Services = _factory.Services;

        // Run database migrations
        await RunMigrationsAsync();
        
        // Wait for Kafka to be ready
        await WaitForKafkaAsync();
    }

    public async Task DisposeAsync()
    {
        HttpClient?.Dispose();
        await _factory?.DisposeAsync()!;
        await _dbContainer.StopAsync();
        await _kafkaContainer.StopAsync();
    }

    private async Task RunMigrationsAsync()
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDomainDb>();
        await db.Database.MigrateAsync();
    }

    private async Task WaitForKafkaAsync()
    {
        using var scope = Services.CreateScope();
        var producer = scope.ServiceProvider.GetRequiredService<IProducer<Null, string>>();
        
        // Test Kafka connectivity
        var retries = 10;
        while (retries > 0)
        {
            try
            {
                var metadata = producer.GetMetadata(TimeSpan.FromSeconds(5));
                if (metadata.Brokers.Any())
                    break;
            }
            catch
            {
                // Kafka not ready yet
            }
            
            retries--;
            await Task.Delay(1000);
        }
    }
}
```

### Test Email Service

Create mock services for testing:

```csharp
public class TestEmailService : IEmailService
{
    private readonly List<EmailMessage> _sentEmails = new();
    
    public IReadOnlyList<EmailMessage> SentEmails => _sentEmails.AsReadOnly();

    public Task SendEmailAsync(string to, string subject, string body, CancellationToken cancellationToken = default)
    {
        _sentEmails.Add(new EmailMessage(to, subject, body, DateTime.UtcNow));
        return Task.CompletedTask;
    }

    public Task SendWelcomeEmailAsync(string email, string name, CancellationToken cancellationToken = default)
    {
        return SendEmailAsync(email, "Welcome!", $"Welcome {name}!", cancellationToken);
    }

    public void ClearSentEmails() => _sentEmails.Clear();
}

public record EmailMessage(string To, string Subject, string Body, DateTime SentAt);
```

## API Integration Tests

### Basic CRUD Operations

```csharp
public class CashierApiTests : IClassFixture<IntegrationTestFixture>
{
    private readonly IntegrationTestFixture _fixture;
    private readonly HttpClient _client;

    public CashierApiTests(IntegrationTestFixture fixture)
    {
        _fixture = fixture;
        _client = fixture.HttpClient;
    }

    [Fact]
    public async Task CreateCashier_ValidData_ReturnsCreatedResponse()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var createRequest = new CreateCashierRequest(
            tenantId,
            "John Doe",
            "john@example.com");

        // Act
        var response = await _client.PostAsJsonAsync("/api/cashiers", createRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        
        var createdCashier = await response.Content.ReadFromJsonAsync<Cashier>();
        createdCashier.Should().NotBeNull();
        createdCashier!.TenantId.Should().Be(tenantId);
        createdCashier.Name.Should().Be("John Doe");
        createdCashier.Email.Should().Be("john@example.com");
        createdCashier.Id.Should().NotBeEmpty();

        // Verify Location header
        response.Headers.Location.Should().NotBeNull();
        response.Headers.Location!.ToString().Should()
            .Contain($"/api/cashiers/{createdCashier.Id}");
    }

    [Fact]
    public async Task GetCashier_ExistingCashier_ReturnsOkResponse()
    {
        // Arrange - Create a cashier first
        var tenantId = Guid.NewGuid();
        var createdCashier = await CreateTestCashierAsync(tenantId, "Jane Doe", "jane@example.com");

        // Act
        var response = await _client.GetAsync(
            $"/api/cashiers/{createdCashier.Id}?tenantId={tenantId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var cashier = await response.Content.ReadFromJsonAsync<Cashier>();
        cashier.Should().NotBeNull();
        cashier!.Id.Should().Be(createdCashier.Id);
        cashier.TenantId.Should().Be(tenantId);
        cashier.Name.Should().Be("Jane Doe");
        cashier.Email.Should().Be("jane@example.com");
    }

    [Fact]
    public async Task GetCashier_NonExistentCashier_ReturnsNotFound()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var cashierId = Guid.NewGuid();

        // Act
        var response = await _client.GetAsync(
            $"/api/cashiers/{cashierId}?tenantId={tenantId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        
        var errorResponse = await response.Content.ReadAsStringAsync();
        errorResponse.Should().Contain("Cashier not found");
    }

    [Fact]
    public async Task UpdateCashier_ValidData_ReturnsOkResponse()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var createdCashier = await CreateTestCashierAsync(tenantId, "Original Name", "original@example.com");
        
        var updateRequest = new UpdateCashierRequest("Updated Name", "updated@example.com");

        // Act
        var response = await _client.PutAsJsonAsync(
            $"/api/cashiers/{createdCashier.Id}?tenantId={tenantId}", 
            updateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var updatedCashier = await response.Content.ReadFromJsonAsync<Cashier>();
        updatedCashier.Should().NotBeNull();
        updatedCashier!.Id.Should().Be(createdCashier.Id);
        updatedCashier.Name.Should().Be("Updated Name");
        updatedCashier.Email.Should().Be("updated@example.com");
        updatedCashier.UpdatedDate.Should().BeAfter(createdCashier.UpdatedDate);
    }

    [Fact]
    public async Task DeleteCashier_ExistingCashier_ReturnsNoContent()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var createdCashier = await CreateTestCashierAsync(tenantId, "To Delete", "delete@example.com");

        // Act
        var response = await _client.DeleteAsync(
            $"/api/cashiers/{createdCashier.Id}?tenantId={tenantId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify it's actually deleted
        var getResponse = await _client.GetAsync(
            $"/api/cashiers/{createdCashier.Id}?tenantId={tenantId}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateCashier_InvalidData_ReturnsBadRequest()
    {
        // Arrange
        var createRequest = new CreateCashierRequest(
            Guid.Empty,        // Invalid tenant ID
            "",               // Invalid name
            "invalid-email"); // Invalid email

        // Act
        var response = await _client.PostAsJsonAsync("/api/cashiers", createRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        
        var errorContent = await response.Content.ReadAsStringAsync();
        errorContent.Should().Contain("TenantId");
        errorContent.Should().Contain("Name");
        errorContent.Should().Contain("Email");
    }

    private async Task<Cashier> CreateTestCashierAsync(Guid tenantId, string name, string email)
    {
        var createRequest = new CreateCashierRequest(tenantId, name, email);
        var response = await _client.PostAsJsonAsync("/api/cashiers", createRequest);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<Cashier>() 
               ?? throw new InvalidOperationException("Failed to create test cashier");
    }
}
```

### Pagination and Search Tests

```csharp
public class CashierListApiTests : IClassFixture<IntegrationTestFixture>
{
    private readonly IntegrationTestFixture _fixture;
    private readonly HttpClient _client;

    public CashierListApiTests(IntegrationTestFixture fixture)
    {
        _fixture = fixture;
        _client = fixture.HttpClient;
    }

    [Fact]
    public async Task GetCashiers_WithPagination_ReturnsPagedResults()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        
        // Create test data
        var cashiers = new[]
        {
            await CreateTestCashierAsync(tenantId, "Alice Johnson", "alice@example.com"),
            await CreateTestCashierAsync(tenantId, "Bob Smith", "bob@example.com"),
            await CreateTestCashierAsync(tenantId, "Charlie Brown", "charlie@example.com")
        };

        // Act
        var response = await _client.GetAsync(
            $"/api/cashiers?tenantId={tenantId}&page=1&pageSize=2");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var pagedResult = await response.Content.ReadFromJsonAsync<PagedResult<Cashier>>();
        pagedResult.Should().NotBeNull();
        pagedResult!.Items.Should().HaveCount(2);
        pagedResult.TotalCount.Should().Be(3);
        pagedResult.Page.Should().Be(1);
        pagedResult.PageSize.Should().Be(2);
        pagedResult.TotalPages.Should().Be(2);
        
        // Should be ordered by name
        pagedResult.Items[0].Name.Should().Be("Alice Johnson");
        pagedResult.Items[1].Name.Should().Be("Bob Smith");
    }

    [Fact]
    public async Task SearchCashiers_WithSearchTerm_ReturnsMatchingResults()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        
        await CreateTestCashierAsync(tenantId, "John Doe", "john@example.com");
        await CreateTestCashierAsync(tenantId, "Jane Smith", "jane@example.com");
        await CreateTestCashierAsync(tenantId, "Johnny Walker", "johnny@example.com");

        // Act - Search by name
        var response = await _client.GetAsync(
            $"/api/cashiers/search?tenantId={tenantId}&searchTerm=john");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var searchResults = await response.Content.ReadFromJsonAsync<List<Cashier>>();
        searchResults.Should().NotBeNull();
        searchResults.Should().HaveCount(2);
        searchResults.Should().Contain(c => c.Name == "John Doe");
        searchResults.Should().Contain(c => c.Name == "Johnny Walker");
        searchResults.Should().NotContain(c => c.Name == "Jane Smith");
    }

    private async Task<Cashier> CreateTestCashierAsync(Guid tenantId, string name, string email)
    {
        var createRequest = new CreateCashierRequest(tenantId, name, email);
        var response = await _client.PostAsJsonAsync("/api/cashiers", createRequest);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<Cashier>() 
               ?? throw new InvalidOperationException("Failed to create test cashier");
    }
}
```

## Database Integration Tests

### Direct Database Testing

```csharp
public class CashierDatabaseTests : IClassFixture<IntegrationTestFixture>
{
    private readonly IntegrationTestFixture _fixture;

    public CashierDatabaseTests(IntegrationTestFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task CreateCashier_ValidData_PersistsToDatabase()
    {
        // Arrange
        using var scope = _fixture.Services.CreateScope();
        var messageBus = scope.ServiceProvider.GetRequiredService<IMessageBus>();
        var db = scope.ServiceProvider.GetRequiredService<AppDomainDb>();

        var tenantId = Guid.NewGuid();
        var command = new CreateCashierCommand(tenantId, "Database Test", "db@example.com");

        // Act
        var (result, integrationEvent) = await messageBus.InvokeAsync(command);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();

        // Verify in database
        var cashier = await db.Cashiers
            .FirstOrDefaultAsync(c => c.CashierId == result.Value.Id);
        
        cashier.Should().NotBeNull();
        cashier!.TenantId.Should().Be(tenantId);
        cashier.Name.Should().Be("Database Test");
        cashier.Email.Should().Be("db@example.com");
        cashier.CreatedDateUtc.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
        cashier.UpdatedDateUtc.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));

        // Verify integration event
        integrationEvent.Should().NotBeNull();
        integrationEvent!.Cashier.Id.Should().Be(result.Value.Id);
    }

    [Fact]
    public async Task UpdateCashier_ExistingCashier_UpdatesDatabase()
    {
        // Arrange
        using var scope = _fixture.Services.CreateScope();
        var messageBus = scope.ServiceProvider.GetRequiredService<IMessageBus>();
        var db = scope.ServiceProvider.GetRequiredService<AppDomainDb>();

        var tenantId = Guid.NewGuid();
        
        // Create initial cashier
        var createCommand = new CreateCashierCommand(tenantId, "Original Name", "original@example.com");
        var (createResult, _) = await messageBus.InvokeAsync(createCommand);
        createResult.IsSuccess.Should().BeTrue();

        var originalUpdateDate = createResult.Value.UpdatedDate;
        
        // Wait to ensure update timestamp is different
        await Task.Delay(100);

        // Act - Update cashier
        var updateCommand = new UpdateCashierCommand(
            tenantId, 
            createResult.Value.Id, 
            "Updated Name", 
            "updated@example.com");
            
        var (updateResult, updateEvent) = await messageBus.InvokeAsync(updateCommand);

        // Assert
        updateResult.IsSuccess.Should().BeTrue();

        // Verify in database
        var updatedCashier = await db.Cashiers
            .FirstOrDefaultAsync(c => c.CashierId == createResult.Value.Id);
        
        updatedCashier.Should().NotBeNull();
        updatedCashier!.Name.Should().Be("Updated Name");
        updatedCashier.Email.Should().Be("updated@example.com");
        updatedCashier.UpdatedDateUtc.Should().BeAfter(originalUpdateDate);

        // Verify event
        updateEvent.Should().NotBeNull();
        updateEvent!.Cashier.Name.Should().Be("Updated Name");
    }

    [Fact]
    public async Task DeleteCashier_ExistingCashier_RemovesFromDatabase()
    {
        // Arrange
        using var scope = _fixture.Services.CreateScope();
        var messageBus = scope.ServiceProvider.GetRequiredService<IMessageBus>();
        var db = scope.ServiceProvider.GetRequiredService<AppDomainDb>();

        var tenantId = Guid.NewGuid();
        
        // Create cashier to delete
        var createCommand = new CreateCashierCommand(tenantId, "To Delete", "delete@example.com");
        var (createResult, _) = await messageBus.InvokeAsync(createCommand);
        createResult.IsSuccess.Should().BeTrue();

        // Act
        var deleteCommand = new DeleteCashierCommand(tenantId, createResult.Value.Id);
        var (deleteResult, deleteEvent) = await messageBus.InvokeAsync(deleteCommand);

        // Assert
        deleteResult.IsSuccess.Should().BeTrue();
        deleteResult.Value.Should().BeTrue();

        // Verify deletion in database
        var deletedCashier = await db.Cashiers
            .FirstOrDefaultAsync(c => c.CashierId == createResult.Value.Id);
        
        deletedCashier.Should().BeNull();

        // Verify event
        deleteEvent.Should().NotBeNull();
        deleteEvent!.CashierId.Should().Be(createResult.Value.Id);
    }

    [Fact]
    public async Task CreateCashier_DuplicateEmail_ThrowsException()
    {
        // Arrange
        using var scope = _fixture.Services.CreateScope();
        var messageBus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        var tenantId = Guid.NewGuid();
        var email = "duplicate@example.com";

        // Create first cashier
        var firstCommand = new CreateCashierCommand(tenantId, "First User", email);
        var (firstResult, _) = await messageBus.InvokeAsync(firstCommand);
        firstResult.IsSuccess.Should().BeTrue();

        // Act & Assert
        var secondCommand = new CreateCashierCommand(tenantId, "Second User", email);
        
        // Should throw due to unique constraint on email
        var exception = await Assert.ThrowsAsync<PostgresException>(() => 
            messageBus.InvokeAsync(secondCommand));
            
        exception.SqlState.Should().Be("23505"); // Unique violation
    }
}
```

### Transaction Testing

```csharp
public class TransactionIntegrationTests : IClassFixture<IntegrationTestFixture>
{
    private readonly IntegrationTestFixture _fixture;

    public TransactionIntegrationTests(IntegrationTestFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task ComplexOperation_PartialFailure_RollsBackTransaction()
    {
        // Arrange
        using var scope = _fixture.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDomainDb>();

        var tenantId = Guid.NewGuid();

        // Act & Assert
        await using var transaction = await db.BeginTransactionAsync();
        
        try
        {
            // Step 1: Create cashier (should succeed)
            var cashier = new Data.Entities.Cashier
            {
                TenantId = tenantId,
                CashierId = Guid.CreateVersion7(),
                Name = "Transaction Test",
                Email = "transaction@example.com",
                CreatedDateUtc = DateTime.UtcNow,
                UpdatedDateUtc = DateTime.UtcNow
            };

            var insertedCashier = await db.Cashiers.InsertWithOutputAsync(cashier);
            insertedCashier.Should().NotBeNull();

            // Verify cashier was inserted (within transaction)
            var foundCashier = await db.Cashiers
                .FirstOrDefaultAsync(c => c.CashierId == insertedCashier.CashierId);
            foundCashier.Should().NotBeNull();

            // Step 2: Force an error (simulating business rule failure)
            throw new InvalidOperationException("Simulated business rule failure");
        }
        catch (InvalidOperationException)
        {
            await transaction.RollbackAsync();
        }

        // Verify rollback - cashier should not exist
        var rolledBackCashier = await db.Cashiers
            .FirstOrDefaultAsync(c => c.TenantId == tenantId);
        rolledBackCashier.Should().BeNull();
    }

    [Fact]
    public async Task ComplexOperation_Success_CommitsTransaction()
    {
        // Arrange
        using var scope = _fixture.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDomainDb>();

        var tenantId = Guid.NewGuid();

        // Act
        await using var transaction = await db.BeginTransactionAsync();
        
        // Step 1: Create cashier
        var cashier = new Data.Entities.Cashier
        {
            TenantId = tenantId,
            CashierId = Guid.CreateVersion7(),
            Name = "Transaction Success",
            Email = "success@example.com",
            CreatedDateUtc = DateTime.UtcNow,
            UpdatedDateUtc = DateTime.UtcNow
        };

        var insertedCashier = await db.Cashiers.InsertWithOutputAsync(cashier);

        // Step 2: Create related data (simulate complex operation)
        var metadata = new Data.Entities.CashierMetadata
        {
            CashierId = insertedCashier.CashierId,
            Department = "Sales",
            HireDate = DateTime.UtcNow,
            IsActive = true
        };

        await db.CashierMetadata.InsertAsync(metadata);

        await transaction.CommitAsync();

        // Assert - Verify both entities were committed
        var persistedCashier = await db.Cashiers
            .FirstOrDefaultAsync(c => c.CashierId == insertedCashier.CashierId);
        persistedCashier.Should().NotBeNull();

        var persistedMetadata = await db.CashierMetadata
            .FirstOrDefaultAsync(m => m.CashierId == insertedCashier.CashierId);
        persistedMetadata.Should().NotBeNull();
    }
}
```

## Event Integration Tests

### Kafka Event Publishing Tests

```csharp
public class EventPublishingTests : IClassFixture<IntegrationTestFixture>
{
    private readonly IntegrationTestFixture _fixture;

    public EventPublishingTests(IntegrationTestFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task CreateCashier_Success_PublishesIntegrationEvent()
    {
        // Arrange
        using var scope = _fixture.Services.CreateScope();
        var messageBus = scope.ServiceProvider.GetRequiredService<IMessageBus>();
        var eventCapture = scope.ServiceProvider.GetRequiredService<TestEventCapture>();

        var tenantId = Guid.NewGuid();
        var command = new CreateCashierCommand(tenantId, "Event Test", "event@example.com");

        // Act
        var (result, integrationEvent) = await messageBus.InvokeAsync(command);

        // Assert
        result.IsSuccess.Should().BeTrue();
        integrationEvent.Should().NotBeNull();

        // Wait for event to be published
        await Task.Delay(1000);

        // Verify event was captured
        var capturedEvents = eventCapture.GetEvents<CashierCreated>();
        capturedEvents.Should().HaveCount(1);
        
        var capturedEvent = capturedEvents.First();
        capturedEvent.TenantId.Should().Be(tenantId);
        capturedEvent.Cashier.Name.Should().Be("Event Test");
        capturedEvent.Cashier.Email.Should().Be("event@example.com");
    }

    [Fact]
    public async Task UpdateCashier_Success_PublishesUpdateEvent()
    {
        // Arrange
        using var scope = _fixture.Services.CreateScope();
        var messageBus = scope.ServiceProvider.GetRequiredService<IMessageBus>();
        var eventCapture = scope.ServiceProvider.GetRequiredService<TestEventCapture>();

        var tenantId = Guid.NewGuid();
        
        // Create cashier first
        var createCommand = new CreateCashierCommand(tenantId, "Original", "original@example.com");
        var (createResult, _) = await messageBus.InvokeAsync(createCommand);
        createResult.IsSuccess.Should().BeTrue();

        eventCapture.Clear(); // Clear create events

        // Act - Update cashier
        var updateCommand = new UpdateCashierCommand(
            tenantId, 
            createResult.Value.Id, 
            "Updated", 
            "updated@example.com");
            
        var (updateResult, updateEvent) = await messageBus.InvokeAsync(updateCommand);

        // Assert
        updateResult.IsSuccess.Should().BeTrue();
        updateEvent.Should().NotBeNull();

        // Wait for event to be published
        await Task.Delay(1000);

        // Verify update event was captured
        var capturedEvents = eventCapture.GetEvents<CashierUpdated>();
        capturedEvents.Should().HaveCount(1);
        
        var capturedEvent = capturedEvents.First();
        capturedEvent.TenantId.Should().Be(tenantId);
        capturedEvent.Cashier.Name.Should().Be("Updated");
        capturedEvent.Cashier.Email.Should().Be("updated@example.com");
    }
}

// Test event capture service
public class TestEventCapture
{
    private readonly List<object> _capturedEvents = new();

    public void CaptureEvent(object evt)
    {
        _capturedEvents.Add(evt);
    }

    public List<T> GetEvents<T>() where T : class
    {
        return _capturedEvents.OfType<T>().ToList();
    }

    public void Clear()
    {
        _capturedEvents.Clear();
    }
}
```

### Event Handler Tests

```csharp
public class EventHandlerIntegrationTests : IClassFixture<IntegrationTestFixture>
{
    private readonly IntegrationTestFixture _fixture;

    public EventHandlerIntegrationTests(IntegrationTestFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task CashierCreated_Handler_SendsWelcomeEmail()
    {
        // Arrange
        using var scope = _fixture.Services.CreateScope();
        var messageBus = scope.ServiceProvider.GetRequiredService<IMessageBus>();
        var testEmailService = scope.ServiceProvider.GetRequiredService<IEmailService>() as TestEmailService;
        testEmailService.Should().NotBeNull();

        var tenantId = Guid.NewGuid();
        var command = new CreateCashierCommand(tenantId, "Email Test", "email@example.com");

        // Act
        var (result, integrationEvent) = await messageBus.InvokeAsync(command);

        // Wait for event handlers to process
        await Task.Delay(2000);

        // Assert
        result.IsSuccess.Should().BeTrue();

        // Verify email was sent
        testEmailService!.SentEmails.Should().HaveCount(1);
        var sentEmail = testEmailService.SentEmails.First();
        sentEmail.To.Should().Be("email@example.com");
        sentEmail.Subject.Should().Be("Welcome!");
        sentEmail.Body.Should().Contain("Email Test");
    }

    [Fact]
    public async Task MultipleEvents_HandlersProcessCorrectly()
    {
        // Arrange
        using var scope = _fixture.Services.CreateScope();
        var messageBus = scope.ServiceProvider.GetRequiredService<IMessageBus>();
        var testEmailService = scope.ServiceProvider.GetRequiredService<IEmailService>() as TestEmailService;
        
        var tenantId = Guid.NewGuid();

        // Act - Create multiple cashiers
        var commands = new[]
        {
            new CreateCashierCommand(tenantId, "User 1", "user1@example.com"),
            new CreateCashierCommand(tenantId, "User 2", "user2@example.com"),
            new CreateCashierCommand(tenantId, "User 3", "user3@example.com")
        };

        var results = new List<(Result<Cashier> result, CashierCreated? evt)>();
        foreach (var command in commands)
        {
            var result = await messageBus.InvokeAsync(command);
            results.Add(result);
        }

        // Wait for all event handlers to process
        await Task.Delay(3000);

        // Assert
        results.Should().AllSatisfy(r => r.result.IsSuccess.Should().BeTrue());

        // Verify all emails were sent
        testEmailService!.SentEmails.Should().HaveCount(3);
        testEmailService.SentEmails.Should().Contain(e => e.To == "user1@example.com");
        testEmailService.SentEmails.Should().Contain(e => e.To == "user2@example.com");
        testEmailService.SentEmails.Should().Contain(e => e.To == "user3@example.com");
    }
}
```

## Authentication and Authorization Tests

### JWT Token Testing

```csharp
public class AuthenticationIntegrationTests : IClassFixture<IntegrationTestFixture>
{
    private readonly IntegrationTestFixture _fixture;
    private readonly HttpClient _client;

    public AuthenticationIntegrationTests(IntegrationTestFixture fixture)
    {
        _fixture = fixture;
        _client = fixture.HttpClient;
    }

    [Fact]
    public async Task ProtectedEndpoint_WithoutToken_ReturnsUnauthorized()
    {
        // Arrange
        var tenantId = Guid.NewGuid();

        // Act
        var response = await _client.GetAsync($"/api/cashiers/{Guid.NewGuid()}?tenantId={tenantId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ProtectedEndpoint_WithValidToken_ReturnsOk()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var cashier = await CreateTestCashierWithAuthAsync(tenantId, "Auth Test", "auth@example.com");

        var token = GenerateTestJwtToken(tenantId, "test-user");
        _client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.GetAsync($"/api/cashiers/{cashier.Id}?tenantId={tenantId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ProtectedEndpoint_WithWrongTenant_ReturnsForbidden()
    {
        // Arrange
        var tenantId1 = Guid.NewGuid();
        var tenantId2 = Guid.NewGuid();
        
        var cashier = await CreateTestCashierWithAuthAsync(tenantId1, "Tenant Test", "tenant@example.com");

        // Create token for different tenant
        var token = GenerateTestJwtToken(tenantId2, "test-user");
        _client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.GetAsync($"/api/cashiers/{cashier.Id}?tenantId={tenantId1}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    private string GenerateTestJwtToken(Guid tenantId, string userId)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.ASCII.GetBytes("test-secret-key-that-is-long-enough-for-hmac");
        
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim("sub", userId),
                new Claim("tenant_id", tenantId.ToString()),
                new Claim("role", "user")
            }),
            Expires = DateTime.UtcNow.AddHours(1),
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(key), 
                SecurityAlgorithms.HmacSha256Signature)
        };
        
        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }

    private async Task<Cashier> CreateTestCashierWithAuthAsync(Guid tenantId, string name, string email)
    {
        // Temporarily use admin token for creation
        var adminToken = GenerateTestJwtToken(tenantId, "admin");
        var originalAuth = _client.DefaultRequestHeaders.Authorization;
        
        try
        {
            _client.DefaultRequestHeaders.Authorization = 
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", adminToken);
                
            var createRequest = new CreateCashierRequest(tenantId, name, email);
            var response = await _client.PostAsJsonAsync("/api/cashiers", createRequest);
            response.EnsureSuccessStatusCode();
            
            return await response.Content.ReadFromJsonAsync<Cashier>() 
                   ?? throw new InvalidOperationException("Failed to create test cashier");
        }
        finally
        {
            _client.DefaultRequestHeaders.Authorization = originalAuth;
        }
    }
}
```

## Performance and Load Testing

### Response Time Testing

```csharp
public class PerformanceIntegrationTests : IClassFixture<IntegrationTestFixture>
{
    private readonly IntegrationTestFixture _fixture;
    private readonly HttpClient _client;

    public PerformanceIntegrationTests(IntegrationTestFixture fixture)
    {
        _fixture = fixture;
        _client = fixture.HttpClient;
    }

    [Fact]
    public async Task CreateCashier_ResponseTime_WithinAcceptableLimit()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var createRequest = new CreateCashierRequest(tenantId, "Performance Test", "perf@example.com");

        // Act
        var stopwatch = Stopwatch.StartNew();
        var response = await _client.PostAsJsonAsync("/api/cashiers", createRequest);
        stopwatch.Stop();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(1000); // Less than 1 second
    }

    [Fact]
    public async Task GetCashiers_LargeDataSet_PerformsWell()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        
        // Create large dataset
        var tasks = Enumerable.Range(1, 100)
            .Select(i => CreateTestCashierAsync(tenantId, $"User {i}", $"user{i}@example.com"));
        await Task.WhenAll(tasks);

        // Act
        var stopwatch = Stopwatch.StartNew();
        var response = await _client.GetAsync($"/api/cashiers?tenantId={tenantId}&pageSize=20");
        stopwatch.Stop();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(500); // Less than 500ms
        
        var pagedResult = await response.Content.ReadFromJsonAsync<PagedResult<Cashier>>();
        pagedResult!.Items.Should().HaveCount(20);
        pagedResult.TotalCount.Should().Be(100);
    }

    [Fact]
    public async Task ConcurrentRequests_HandledCorrectly()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var concurrentRequests = 10;

        var tasks = Enumerable.Range(1, concurrentRequests)
            .Select(i => CreateTestCashierAsync(tenantId, $"Concurrent {i}", $"concurrent{i}@example.com"))
            .ToList();

        // Act
        var stopwatch = Stopwatch.StartNew();
        var results = await Task.WhenAll(tasks);
        stopwatch.Stop();

        // Assert
        results.Should().HaveCount(concurrentRequests);
        results.Should().OnlyContain(c => c != null);
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(5000); // All requests complete in 5 seconds
    }

    private async Task<Cashier> CreateTestCashierAsync(Guid tenantId, string name, string email)
    {
        var createRequest = new CreateCashierRequest(tenantId, name, email);
        var response = await _client.PostAsJsonAsync("/api/cashiers", createRequest);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<Cashier>() 
               ?? throw new InvalidOperationException("Failed to create test cashier");
    }
}
```

## Test Data Management

### Test Data Cleanup

```csharp
public class TestDataManager
{
    private readonly AppDomainDb _db;
    private readonly List<Guid> _createdTenants = new();
    private readonly List<Guid> _createdCashiers = new();

    public TestDataManager(AppDomainDb db)
    {
        _db = db;
    }

    public async Task<Cashier> CreateTestCashierAsync(string name, string email, Guid? tenantId = null)
    {
        tenantId ??= Guid.NewGuid();
        _createdTenants.Add(tenantId.Value);

        var entity = new Data.Entities.Cashier
        {
            CashierId = Guid.CreateVersion7(),
            TenantId = tenantId.Value,
            Name = name,
            Email = email,
            CreatedDateUtc = DateTime.UtcNow,
            UpdatedDateUtc = DateTime.UtcNow
        };

        var created = await _db.Cashiers.InsertWithOutputAsync(entity);
        _createdCashiers.Add(created.CashierId);

        return created.ToModel();
    }

    public async Task CleanupAsync()
    {
        // Clean up in reverse order of dependencies
        
        if (_createdCashiers.Any())
        {
            await _db.Cashiers
                .Where(c => _createdCashiers.Contains(c.CashierId))
                .DeleteAsync();
        }

        // Could also clean up tenants if they were created
        // But typically tenants are shared across tests

        _createdCashiers.Clear();
        _createdTenants.Clear();
    }
}

// Usage in tests
public class IntegrationTestWithCleanup : IClassFixture<IntegrationTestFixture>, IAsyncLifetime
{
    private readonly IntegrationTestFixture _fixture;
    private TestDataManager _testDataManager = default!;

    public IntegrationTestWithCleanup(IntegrationTestFixture fixture)
    {
        _fixture = fixture;
    }

    public async Task InitializeAsync()
    {
        using var scope = _fixture.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDomainDb>();
        _testDataManager = new TestDataManager(db);
    }

    public async Task DisposeAsync()
    {
        await _testDataManager.CleanupAsync();
    }

    [Fact]
    public async Task TestWithAutoCleanup()
    {
        // Test creates data using _testDataManager
        // Data is automatically cleaned up after test
        var cashier = await _testDataManager.CreateTestCashierAsync("Test", "test@example.com");
        
        // Test logic here
        cashier.Should().NotBeNull();
        
        // No manual cleanup needed
    }
}
```

## Running Integration Tests

### Command Line

```bash
# Run all integration tests
dotnet test tests/AppDomain.Tests/ --filter Category=Integration

# Run with specific verbosity
dotnet test tests/AppDomain.Tests/ --filter Category=Integration --verbosity detailed

# Run specific test class
dotnet test tests/AppDomain.Tests/ --filter "FullyQualifiedName~CashierApiTests"

# Run tests with coverage
dotnet test tests/AppDomain.Tests/ --filter Category=Integration --collect:"XPlat Code Coverage"
```

### Environment Variables

Set environment variables for test configuration:

```bash
# Set test environment
export ASPNETCORE_ENVIRONMENT=Testing

# Override connection strings if needed
export ConnectionStrings__DefaultConnection="Host=localhost;Database=test_db;..."

# Set log levels for debugging
export Logging__LogLevel__Default=Debug
```

### CI/CD Pipeline Configuration

```yaml
# GitHub Actions example
name: Integration Tests

on:
  push:
    branches: [ main, develop ]
  pull_request:
    branches: [ main ]

jobs:
  integration-tests:
    runs-on: ubuntu-latest
    
    steps:
    - uses: actions/checkout@v3
    
    - name: Setup .NET
      uses: actions/setup-dotnet@v3
      with:
        dotnet-version: '9.0.x'
    
    - name: Restore dependencies
      run: dotnet restore
    
    - name: Build
      run: dotnet build --no-restore
    
    - name: Run Integration Tests
      run: |
        dotnet test tests/AppDomain.Tests/ \
          --filter Category=Integration \
          --no-build \
          --verbosity normal \
          --collect:"XPlat Code Coverage" \
          --logger trx \
          --results-directory TestResults/
    
    - name: Upload Test Results
      uses: actions/upload-artifact@v3
      if: always()
      with:
        name: test-results
        path: TestResults/
    
    - name: Upload Coverage Reports
      uses: codecov/codecov-action@v3
      with:
        files: TestResults/*/coverage.cobertura.xml
```

## Best Practices

### Test Organization
1. **Group related tests**: Use test classes to group related functionality
2. **Use descriptive names**: Test names should explain the scenario
3. **Follow AAA pattern**: Arrange, Act, Assert
4. **Clean up test data**: Ensure tests don't interfere with each other

### Performance
1. **Use test containers**: Provide isolated infrastructure
2. **Parallel execution**: Configure tests to run in parallel where possible
3. **Optimize test data**: Create minimal data needed for each test
4. **Cache expensive setup**: Share expensive setup across tests

### Reliability
1. **Handle timing issues**: Use proper waits for async operations
2. **Isolate tests**: Each test should be independent
3. **Use unique identifiers**: Avoid conflicts with GUIDs/unique names
4. **Handle failures gracefully**: Include proper error handling

### Maintainability
1. **Use test utilities**: Create reusable test helpers
2. **Keep tests simple**: Complex test logic is hard to maintain
3. **Document complex scenarios**: Add comments for non-obvious test logic
4. **Regular cleanup**: Remove obsolete tests

## Next Steps

- Review [Unit Testing](./unit-tests) for component-level testing
- Explore [Best Practices](../best-practices) for overall testing strategy
- See [Troubleshooting](../troubleshooting) for common testing issues
- Learn about [CQRS Testing](../cqrs/) patterns for commands and queries