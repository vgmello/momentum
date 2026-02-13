---
title: Testing Strategies
description: Comprehensive testing approaches and patterns for the AppDomain Solution
date: 2025-01-07
---

# Testing Strategies

The AppDomain Solution implements a comprehensive testing strategy that ensures reliability, maintainability, and confidence in code changes. This guide covers unit testing, integration testing, architecture testing, and performance testing approaches used throughout the system.

## Testing Pyramid Overview

```mermaid
graph TB
    subgraph "Testing Pyramid"
        E2E[End-to-End Tests<br/>Few, High-Value Scenarios]
        Integration[Integration Tests<br/>Service Boundaries & Infrastructure]
        Unit[Unit Tests<br/>Business Logic & Domain Models]
    end

    subgraph "Additional Testing"
        Arch[Architecture Tests<br/>Design Constraints]
        Perf[Performance Tests<br/>Load & Stress Testing]
        Contract[Contract Tests<br/>API Compatibility]
    end

    Unit -/-> Integration
    Integration -/-> E2E
    Unit -/-> Arch
    Integration -/-> Perf
    Integration -/-> Contract
```

## Unit Testing

### Domain Logic Testing

**Testing Value Objects**:

```csharp
[TestFixture]
public class MoneyTests
{
    [Test]
    public void Should_Create_Valid_Money_With_Positive_Amount()
    {
        // Arrange & Act
        var money = new Money(100.50m, Currency.USD);

        // Assert
        money.Amount.ShouldBe(100.50m);
        money.Currency.ShouldBe(Currency.USD);
    }

    [Test]
    public void Should_Add_Money_With_Same_Currency()
    {
        // Arrange
        var money1 = new Money(50.25m, Currency.USD);
        var money2 = new Money(25.75m, Currency.USD);

        // Act
        var result = money1 + money2;

        // Assert
        result.Amount.ShouldBe(76.00m);
        result.Currency.ShouldBe(Currency.USD);
    }

    [Test]
    public void Should_Throw_When_Adding_Different_Currencies()
    {
        // Arrange
        var usdMoney = new Money(50.25m, Currency.USD);
        var eurMoney = new Money(25.75m, Currency.EUR);

        // Act & Assert
        var action = () => usdMoney + eurMoney;
        action.ShouldThrow<InvalidOperationException>()
              .Message.ShouldContain("Cannot add money with different currencies");
    }
}
```

**Testing Entities**:

```csharp
[TestFixture]
public class CashierTests
{
    [Test]
    public void Should_Create_Cashier_With_Valid_Properties()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var cashierId = Ulid.NewUlid();
        var name = "John Doe";
        var email = "john.doe@example.com";

        // Act
        var cashier = new Cashier
        {
            TenantId = tenantId,
            CashierId = cashierId,
            Name = name,
            Email = email
        };

        // Assert
        cashier.TenantId.ShouldBe(tenantId);
        cashier.CashierId.ShouldBe(cashierId);
        cashier.Name.ShouldBe(name);
        cashier.Email.ShouldBe(email);
        cashier.CashierPayments.ShouldNotBeNull();
        cashier.CashierPayments.ShouldBeEmpty();
    }

    [Test]
    public void Should_Add_Payment_To_Cashier()
    {
        // Arrange
        var cashier = CreateTestCashier();
        var payment = new CashierPayment
        {
            Amount = 150.00m,
            Currency = "USD",
            ProcessedAt = DateTimeOffset.UtcNow
        };

        // Act
        cashier.CashierPayments.Add(payment);

        // Assert
        cashier.CashierPayments.ShouldHaveCount(1);
        cashier.CashierPayments.First().Amount.ShouldBe(150.00m);
    }

    private static Cashier CreateTestCashier() =>
        new()
        {
            TenantId = Guid.NewGuid(),
            CashierId = Ulid.NewUlid(),
            Name = "Test Cashier",
            Email = "test@example.com"
        };
}
```

### Command Handler Testing

**Testing with Mocked Dependencies**:

```csharp
[TestFixture]
public class CreateCashierCommandHandlerTests
{
    private Mock<IDbConnection> _connectionMock;
    private Mock<IMessagePublisher> _publisherMock;
    private Mock<ILogger<CreateCashierCommandHandler>> _loggerMock;
    private CreateCashierCommandHandler _handler;

    [SetUp]
    public void SetUp()
    {
        _connectionMock = new Mock<IDbConnection>();
        _publisherMock = new Mock<IMessagePublisher>();
        _loggerMock = new Mock<ILogger<CreateCashierCommandHandler>>();

        _handler = new CreateCashierCommandHandler(
            _connectionMock.Object,
            _publisherMock.Object,
            _loggerMock.Object);
    }

    [Test]
    public async Task Should_Create_Cashier_Successfully()
    {
        // Arrange
        var command = new CreateCashierCommand
        {
            Name = "Jane Smith",
            Email = "jane.smith@example.com"
        };

        var expectedCashier = new Cashier
        {
            TenantId = Guid.NewGuid(),
            CashierId = Ulid.NewUlid(),
            Name = command.Name,
            Email = command.Email
        };

        _connectionMock
            .Setup(x => x.QuerySingleAsync<Cashier>(
                It.IsAny<string>(),
                It.IsAny<object>(),
                It.IsAny<IDbTransaction>(),
                It.IsAny<int?>(),
                It.IsAny<CommandType?>()))
            .ReturnsAsync(expectedCashier);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.Name.ShouldBe(command.Name);
        result.Value.Email.ShouldBe(command.Email);

        // Verify integration event was published
        _publisherMock.Verify(x => x.PublishAsync(
            It.Is<CashierCreated>(e =>
                e.CashierId == expectedCashier.CashierId &&
                e.Name == command.Name &&
                e.Email == command.Email),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task Should_Return_Failure_When_Database_Error_Occurs()
    {
        // Arrange
        var command = new CreateCashierCommand
        {
            Name = "Jane Smith",
            Email = "jane.smith@example.com"
        };

        _connectionMock
            .Setup(x => x.QuerySingleAsync<Cashier>(
                It.IsAny<string>(),
                It.IsAny<object>(),
                It.IsAny<IDbTransaction>(),
                It.IsAny<int?>(),
                It.IsAny<CommandType?>()))
            .ThrowsAsync(new SqlException("Connection timeout"));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Error.ShouldContain("database error");

        // Verify no integration event was published
        _publisherMock.Verify(x => x.PublishAsync(
            It.IsAny<CashierCreated>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }
}
```

### Validation Testing

**FluentValidation Tests**:

```csharp
[TestFixture]
public class CreateCashierValidatorTests
{
    private CreateCashierValidator _validator;

    [SetUp]
    public void SetUp()
    {
        _validator = new CreateCashierValidator();
    }

    [Test]
    public void Should_Pass_Validation_With_Valid_Command()
    {
        // Arrange
        var command = new CreateCashierCommand
        {
            Name = "John Doe",
            Email = "john.doe@example.com"
        };

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.ShouldBeTrue();
        result.Errors.ShouldBeEmpty();
    }

    [TestCase("")]
    [TestCase("   ")]
    [TestCase(null)]
    public void Should_Fail_When_Name_Is_Empty_Or_Null(string name)
    {
        // Arrange
        var command = new CreateCashierCommand
        {
            Name = name,
            Email = "john.doe@example.com"
        };

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e =>
            e.PropertyName == nameof(CreateCashierCommand.Name) &&
            e.ErrorMessage.Contains("required"));
    }

    [TestCase("not-an-email")]
    [TestCase("user@")]
    [TestCase("@domain.com")]
    [TestCase("")]
    public void Should_Fail_When_Email_Is_Invalid(string email)
    {
        // Arrange
        var command = new CreateCashierCommand
        {
            Name = "John Doe",
            Email = email
        };

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e =>
            e.PropertyName == nameof(CreateCashierCommand.Email));
    }

    [Test]
    public void Should_Fail_When_Name_Exceeds_Maximum_Length()
    {
        // Arrange
        var command = new CreateCashierCommand
        {
            Name = new string('A', 101), // Exceeds 100 character limit
            Email = "john.doe@example.com"
        };

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e =>
            e.PropertyName == nameof(CreateCashierCommand.Name) &&
            e.ErrorMessage.Contains("100 characters"));
    }
}
```

## Integration Testing

### TestContainers Setup

**Database Integration Tests**:

```csharp
[TestFixture]
public class CashierDatabaseIntegrationTests
{
    private PostgreSqlContainer _container;
    private IDbConnection _connection;
    private string _connectionString;

    [OneTimeSetUp]
    public async Task OneTimeSetUp()
    {
        _container = new PostgreSqlBuilder()
            .WithImage("postgres:15")
            .WithDatabase("AppDomain")
            .WithUsername("postgres")
            .WithPassword("password")
            .WithPortBinding(0, 5432) // Use random port
            .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(5432))
            .WithCleanUp(true)
            .Build();

        await _container.StartAsync();
        _connectionString = _container.GetConnectionString();

        // Run database migrations
        await ApplyDatabaseMigrations();

        _connection = new NpgsqlConnection(_connectionString);
        await _connection.OpenAsync();
    }

    [SetUp]
    public async Task SetUp()
    {
        // Clean up data before each test
        await CleanupTestData();
    }

    [Test]
    public async Task Should_Create_And_Retrieve_Cashier_From_Database()
    {
        // Arrange
        var createCommand = new CreateCashierCommand
        {
            Name = "Integration Test Cashier",
            Email = "integration@test.com"
        };

        var handler = new CreateCashierCommandHandler(_connection, Mock.Of<IMessagePublisher>(), Mock.Of<ILogger<CreateCashierCommandHandler>>());

        // Act - Create cashier
        var createResult = await handler.Handle(createCommand, CancellationToken.None);

        // Assert - Verify creation
        createResult.IsSuccess.ShouldBeTrue();
        var createdCashier = createResult.Value;

        // Act - Retrieve cashier
        var getQuery = new GetCashierQuery { CashierId = createdCashier.CashierId };
        var queryHandler = new GetCashierQueryHandler(_connection, Mock.Of<ILogger<GetCashierQueryHandler>>());
        var getResult = await queryHandler.Handle(getQuery, CancellationToken.None);

        // Assert - Verify retrieval
        getResult.IsSuccess.ShouldBeTrue();
        var retrievedCashier = getResult.Value;

        retrievedCashier.CashierId.ShouldBe(createdCashier.CashierId);
        retrievedCashier.Name.ShouldBe(createCommand.Name);
        retrievedCashier.Email.ShouldBe(createCommand.Email);
    }

    [Test]
    public async Task Should_Handle_Duplicate_Email_Error()
    {
        // Arrange
        var firstCommand = new CreateCashierCommand
        {
            Name = "First Cashier",
            Email = "duplicate@test.com"
        };

        var secondCommand = new CreateCashierCommand
        {
            Name = "Second Cashier",
            Email = "duplicate@test.com" // Same email
        };

        var handler = new CreateCashierCommandHandler(_connection, Mock.Of<IMessagePublisher>(), Mock.Of<ILogger<CreateCashierCommandHandler>>());

        // Act - Create first cashier
        var firstResult = await handler.Handle(firstCommand, CancellationToken.None);

        // Act - Try to create second cashier with same email
        var secondResult = await handler.Handle(secondCommand, CancellationToken.None);

        // Assert
        firstResult.IsSuccess.ShouldBeTrue();
        secondResult.IsSuccess.ShouldBeFalse();
        secondResult.Error.ShouldContain("email already exists");
    }

    [OneTimeTearDown]
    public async Task OneTimeTearDown()
    {
        if (_connection != null)
        {
            await _connection.CloseAsync();
            await _connection.DisposeAsync();
        }

        if (_container != null)
        {
            await _container.DisposeAsync();
        }
    }

    private async Task ApplyDatabaseMigrations()
    {
        // Apply Liquibase migrations or execute schema scripts
        var migrationContainer = new GenericContainerBuilder()
            .WithImage("liquibase/liquibase:4.25")
            .WithCommand("--changeLogFile=changelog.sql", "--url=" + _connectionString, "update")
            .WithBindMount("./migrations", "/liquibase/changelog")
            .WithWaitStrategy(Wait.ForUnixContainer().UntilCommandIsCompleted())
            .Build();

        await migrationContainer.StartAsync();
    }

    private async Task CleanupTestData()
    {
        await _connection.ExecuteAsync("DELETE FROM cashiers WHERE email LIKE '%test.com%'");
    }
}
```

### API Integration Tests

**WebApplicationFactory Testing**:

```csharp
public class AppDomainApiFactory : WebApplicationFactory<Program>
{
    private readonly PostgreSqlContainer _dbContainer;
    private readonly KafkaContainer _kafkaContainer;

    public AppDomainApiFactory()
    {
        _dbContainer = new PostgreSqlBuilder()
            .WithImage("postgres:15")
            .WithDatabase("AppDomain")
            .WithUsername("postgres")
            .WithPassword("password")
            .Build();

        _kafkaContainer = new KafkaBuilder()
            .WithImage("confluentinc/cp-kafka:7.4.0")
            .Build();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureTestServices(services =>
        {
            // Replace database connection
            services.RemoveAll<IDbConnection>();
            services.AddScoped<IDbConnection>(_ =>
                new NpgsqlConnection(_dbContainer.GetConnectionString()));

            // Replace Kafka configuration
            services.Configure<KafkaOptions>(options =>
            {
                options.BootstrapServers = _kafkaContainer.GetBootstrapAddress();
            });

            // Add test-specific services
            services.AddScoped<TestDataSeeder>();
        });

        builder.UseEnvironment("Testing");
    }

    public async Task InitializeAsync()
    {
        await Task.WhenAll(
            _dbContainer.StartAsync(),
            _kafkaContainer.StartAsync()
        );

        await ApplyMigrationsAsync();
    }

    public new async Task DisposeAsync()
    {
        await Task.WhenAll(
            _dbContainer.DisposeAsync().AsTask(),
            _kafkaContainer.DisposeAsync().AsTask()
        );

        await base.DisposeAsync();
    }

    private async Task ApplyMigrationsAsync()
    {
        using var scope = Services.CreateScope();
        var seeder = scope.ServiceProvider.GetRequiredService<TestDataSeeder>();
        await seeder.SeedAsync();
    }
}

[TestFixture]
public class CashiersControllerIntegrationTests : IClassFixture<AppDomainApiFactory>
{
    private readonly AppDomainApiFactory _factory;
    private readonly HttpClient _client;

    public CashiersControllerIntegrationTests()
    {
        _factory = new AppDomainApiFactory();
        _client = _factory.CreateClient();
    }

    [OneTimeSetUp]
    public async Task OneTimeSetUp()
    {
        await _factory.InitializeAsync();
    }

    [Test]
    public async Task Should_Create_Cashier_Via_API()
    {
        // Arrange
        var request = new CreateCashierRequest("API Test Cashier", "apitest@example.com");

        // Act
        var response = await _client.PostAsJsonAsync("/api/cashiers", request);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Created);

        var createdCashier = await response.Content.ReadFromJsonAsync<CashierResponse>();
        createdCashier.ShouldNotBeNull();
        createdCashier.Name.ShouldBe(request.Name);
        createdCashier.Email.ShouldBe(request.Email);
    }

    [Test]
    public async Task Should_Return_ValidationError_For_Invalid_Email()
    {
        // Arrange
        var request = new CreateCashierRequest("Test Cashier", "invalid-email");

        // Act
        var response = await _client.PostAsJsonAsync("/api/cashiers", request);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);

        var problemDetails = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        problemDetails.ShouldNotBeNull();
        problemDetails.Errors.ShouldContainKey("Email");
    }

    [Test]
    public async Task Should_Get_Cashier_By_Id()
    {
        // Arrange - Create a cashier first
        var createRequest = new CreateCashierRequest("Get Test Cashier", "gettest@example.com");
        var createResponse = await _client.PostAsJsonAsync("/api/cashiers", createRequest);
        var createdCashier = await createResponse.Content.ReadFromJsonAsync<CashierResponse>();

        // Act
        var getResponse = await _client.GetAsync($"/api/cashiers/{createdCashier.CashierId}");

        // Assert
        getResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

        var retrievedCashier = await getResponse.Content.ReadFromJsonAsync<CashierResponse>();
        retrievedCashier.ShouldNotBeNull();
        retrievedCashier.CashierId.ShouldBe(createdCashier.CashierId);
        retrievedCashier.Name.ShouldBe(createRequest.Name);
    }

    [OneTimeTearDown]
    public async Task OneTimeTearDown()
    {
        _client?.Dispose();
        await _factory?.DisposeAsync();
    }
}
```

### Event Integration Tests

**Testing Event Publishing and Handling**:

```csharp
[TestFixture]
public class EventIntegrationTests
{
    private IServiceProvider _serviceProvider;
    private PostgreSqlContainer _dbContainer;
    private KafkaContainer _kafkaContainer;

    [OneTimeSetUp]
    public async Task OneTimeSetUp()
    {
        _dbContainer = new PostgreSqlBuilder()
            .WithImage("postgres:15")
            .WithDatabase("AppDomain")
            .WithUsername("postgres")
            .WithPassword("password")
            .Build();

        _kafkaContainer = new KafkaBuilder()
            .WithImage("confluentinc/cp-kafka:7.4.0")
            .Build();

        await Task.WhenAll(
            _dbContainer.StartAsync(),
            _kafkaContainer.StartAsync()
        );

        // Configure services for integration testing
        var services = new ServiceCollection();
        ConfigureServices(services);
        _serviceProvider = services.BuildServiceProvider();
    }

    [Test]
    public async Task Should_Publish_And_Handle_CashierCreated_Event()
    {
        // Arrange
        var publisher = _serviceProvider.GetRequiredService<IMessagePublisher>();
        var eventStore = _serviceProvider.GetRequiredService<IEventStore>();

        var @event = new CashierCreated(
            CashierId: Ulid.NewUlid(),
            Name: "Event Test Cashier",
            Email: "eventtest@example.com",
            CreatedAt: DateTimeOffset.UtcNow
        );

        // Act
        await publisher.PublishAsync(@event);

        // Wait for event processing
        await Task.Delay(TimeSpan.FromSeconds(2));

        // Assert
        var processedEvents = await eventStore.GetEventsAsync($"cashier-{@event.CashierId}");
        var storedEvent = processedEvents.FirstOrDefault(e => e.EventType == nameof(CashierCreated));

        storedEvent.ShouldNotBeNull();
        storedEvent.EventType.ShouldBe(nameof(CashierCreated));
    }

    [Test]
    public async Task Should_Handle_Event_Processing_Failure_And_Retry()
    {
        // Arrange
        var publisher = _serviceProvider.GetRequiredService<IMessagePublisher>();

        // Create an event that will initially fail processing
        var @event = new InvoiceCreated(
            InvoiceId: Ulid.NewUlid(),
            InvoiceNumber: "FAIL-001", // Special invoice number that triggers failure
            Amount: 1000.00m,
            Currency: "USD",
            CashierId: Ulid.NewUlid(),
            CreatedAt: DateTimeOffset.UtcNow,
            DueDate: DateOnly.FromDateTime(DateTime.Today.AddDays(30))
        );

        // Act
        await publisher.PublishAsync(@event);

        // Wait for retry attempts
        await Task.Delay(TimeSpan.FromSeconds(5));

        // Assert
        // Verify the event was retried and eventually moved to dead letter queue
        var deadLetterEvents = await GetDeadLetterEvents();
        deadLetterEvents.ShouldContain(e => e.EventType == nameof(InvoiceCreated));
    }

    private void ConfigureServices(IServiceCollection services)
    {
        services.AddWolverine(opts =>
        {
            opts.UseKafka(_kafkaContainer.GetBootstrapAddress());
            opts.LocalQueue("test-queue").Sequential();

            // Configure retry policies for testing
            opts.OnException<InvalidOperationException>()
                .RetryTimes(2)
                .Then
                .MoveToErrorQueue();
        });

        services.AddScoped<IDbConnection>(_ =>
            new NpgsqlConnection(_dbContainer.GetConnectionString()));

        services.AddScoped<IEventStore, EventStore>();
        services.AddLogging();
    }

    private async Task<IEnumerable<EventEnvelope>> GetDeadLetterEvents()
    {
        // Implementation to retrieve dead letter queue events
        // This would depend on your specific error queue implementation
        return Array.Empty<EventEnvelope>();
    }

    [OneTimeTearDown]
    public async Task OneTimeTearDown()
    {
        _serviceProvider?.Dispose();

        if (_dbContainer != null)
            await _dbContainer.DisposeAsync();

        if (_kafkaContainer != null)
            await _kafkaContainer.DisposeAsync();
    }
}
```

## Architecture Testing

### NetArchTest Implementation

**Architecture Constraints**:

```csharp
[TestFixture]
public class ArchitectureTests
{
    private static readonly Assembly DomainAssembly = typeof(IAppDomainAssembly).Assembly;
    private static readonly Assembly ApiAssembly = typeof(CashiersController).Assembly;
    private static readonly Assembly BackOfficeAssembly = typeof(AppDomainInboxHandler).Assembly;

    [Test]
    public void Domain_Should_Not_Have_Dependencies_On_Infrastructure()
    {
        // Arrange & Act
        var result = Types.InAssembly(DomainAssembly)
            .Should()
            .NotHaveDependencyOn("Microsoft.EntityFrameworkCore")
            .And()
            .NotHaveDependencyOn("Dapper")
            .And()
            .NotHaveDependencyOn("Npgsql")
            .And()
            .NotHaveDependencyOn("System.Data.SqlClient")
            .GetResult();

        // Assert
        result.IsSuccessful.ShouldBeTrue();
    }

    [Test]
    public void Commands_Should_Be_Immutable()
    {
        // Arrange & Act
        var result = Types.InAssembly(DomainAssembly)
            .That()
            .ImplementInterface(typeof(IRequest<>))
            .Should()
            .BeSealed()
            .Or()
            .HaveNameEndingWith("Command")
            .GetResult();

        // Assert
        result.IsSuccessful.ShouldBeTrue();
    }

    [Test]
    public void Command_Handlers_Should_Follow_Naming_Convention()
    {
        // Arrange & Act
        var result = Types.InAssembly(DomainAssembly)
            .That()
            .ImplementInterface(typeof(IRequestHandler<,>))
            .Should()
            .HaveNameEndingWith("Handler")
            .And()
            .BePublic()
            .GetResult();

        // Assert
        result.IsSuccessful.ShouldBeTrue();
    }

    [Test]
    public void Controllers_Should_Only_Exist_In_Api_Assembly()
    {
        // Arrange & Act
        var result = Types.InAssemblies(DomainAssembly, BackOfficeAssembly)
            .Should()
            .NotInheritClass(typeof(ControllerBase))
            .GetResult();

        // Assert
        result.IsSuccessful.ShouldBeTrue();
    }

    [Test]
    public void Integration_Events_Should_Be_Immutable_Records()
    {
        // Arrange & Act
        var result = Types.InAssembly(DomainAssembly)
            .That()
            .ImplementInterface(typeof(IIntegrationEvent))
            .Should()
            .BeSealed()
            .And()
            .BeRecord()
            .GetResult();

        // Assert
        result.IsSuccessful.ShouldBeTrue();
    }

    [Test]
    public void Event_Handlers_Should_Be_In_BackOffice_Assembly()
    {
        // Arrange & Act
        var eventHandlerTypes = Types.InAssembly(BackOfficeAssembly)
            .That()
            .HaveMethodWithName("Handle")
            .And()
            .HaveNameEndingWith("Handler")
            .GetTypes();

        // Assert
        eventHandlerTypes.ShouldNotBeEmpty();

        foreach (var handlerType in eventHandlerTypes)
        {
            var handleMethods = handlerType.GetMethods()
                .Where(m => m.Name == "Handle" && m.GetParameters().Length == 1)
                .ToList();

            handleMethods.ShouldNotBeEmpty($"{handlerType.Name} should have at least one Handle method");
        }
    }

    [Test]
    public void Validators_Should_Inherit_From_AbstractValidator()
    {
        // Arrange & Act
        var result = Types.InAssembly(DomainAssembly)
            .That()
            .HaveNameEndingWith("Validator")
            .Should()
            .Inherit(typeof(AbstractValidator<>))
            .GetResult();

        // Assert
        result.IsSuccessful.ShouldBeTrue();
    }

    [Test]
    public void Database_Entities_Should_Not_Be_Public_Outside_Data_Namespace()
    {
        // Arrange & Act
        var result = Types.InAssembly(DomainAssembly)
            .That()
            .ResideInNamespaceEndingWith(".Data.Entities")
            .Should()
            .NotBePublic()
            .Or()
            .BeUsedOnlyBy(Types.InAssembly(DomainAssembly).That().ResideInNamespaceEndingWith(".Data"))
            .GetResult();

        // Assert
        result.IsSuccessful.ShouldBeTrue();
    }
}
```

### Custom Architecture Rules

**Domain-Specific Rules**:

```csharp
[TestFixture]
public class DomainArchitectureTests
{
    [Test]
    public void DbCommand_Attributes_Should_Only_Be_On_Commands()
    {
        // Arrange & Act
        var typesWithDbCommand = Types.InAssembly(typeof(IAppDomainAssembly).Assembly)
            .That()
            .HaveCustomAttribute(typeof(DbCommandAttribute))
            .GetTypes();

        // Assert
        typesWithDbCommand.ShouldNotBeEmpty();

        foreach (var type in typesWithDbCommand)
        {
            type.Name.ShouldEndWith("Command", $"{type.Name} should be a command");
            type.ShouldImplement(typeof(IRequest<>), $"{type.Name} should implement IRequest");
        }
    }

    [Test]
    public void Integration_Events_Should_Have_Xml_Documentation()
    {
        // Arrange
        var integrationEventTypes = Types.InAssembly(typeof(IAppDomainAssembly).Assembly)
            .That()
            .ImplementInterface(typeof(IIntegrationEvent))
            .GetTypes();

        // Act & Assert
        foreach (var eventType in integrationEventTypes)
        {
            var xmlDocFile = Path.ChangeExtension(eventType.Assembly.Location, ".xml");

            if (File.Exists(xmlDocFile))
            {
                var xmlDoc = XDocument.Load(xmlDocFile);
                var typeDoc = xmlDoc.Descendants("member")
                    .FirstOrDefault(m => m.Attribute("name")?.Value == $"T:{eventType.FullName}");

                typeDoc.ShouldNotBeNull($"{eventType.Name} should have XML documentation for event schema generation");
            }
        }
    }

    [Test]
    public void Contracts_Should_Only_Contain_Data_Transfer_Objects()
    {
        // Arrange & Act
        var contractTypes = Types.InAssembly(typeof(IAppDomainAssembly).Assembly)
            .That()
            .ResideInNamespaceEndingWith(".Contracts")
            .GetTypes();

        // Assert
        foreach (var type in contractTypes)
        {
            // Contracts should be data-only (records, interfaces, or simple classes)
            var isValidContractType = type.IsInterface
                                    || type.IsRecord()
                                    || (type.IsClass && HasOnlyDataProperties(type));

            isValidContractType.ShouldBeTrue($"{type.Name} in Contracts namespace should only contain data");
        }
    }

    private static bool HasOnlyDataProperties(Type type)
    {
        var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);

        // Only allow property getters/setters and constructors
        var behaviorMethods = methods.Where(m =>
            !m.IsSpecialName && // Exclude property accessors
            !m.IsConstructor &&
            m.Name != "ToString" &&
            m.Name != "GetHashCode" &&
            m.Name != "Equals" &&
            m.Name != "Deconstruct"); // Allow record deconstruct

        return !behaviorMethods.Any();
    }
}

public static class TypeExtensions
{
    public static bool IsRecord(this Type type)
    {
        return type.GetMethod("<Clone>$") != null;
    }
}
```

## Performance Testing

### Load Testing with NBomber

**API Endpoint Load Testing**:

```csharp
[TestFixture]
[Category("Performance")]
public class CashierApiPerformanceTests
{
    private AppDomainApiFactory _factory;
    private HttpClient _httpClient;

    [OneTimeSetUp]
    public async Task OneTimeSetUp()
    {
        _factory = new AppDomainApiFactory();
        await _factory.InitializeAsync();
        _httpClient = _factory.CreateClient();
    }

    [Test]
    [Category("LoadTest")]
    public async Task Should_Handle_Concurrent_Cashier_Creation_Requests()
    {
        // Arrange
        var scenario = Scenario.Create("create_cashier", async context =>
        {
            var request = new CreateCashierRequest(
                $"Load Test Cashier {context.ScenarioInfo.InstanceId}",
                $"loadtest{context.ScenarioInfo.InstanceId}@example.com"
            );

            var response = await _httpClient.PostAsJsonAsync("/api/cashiers", request);

            return response.IsSuccessStatusCode ? Response.Ok() : Response.Fail();
        })
        .WithLoadSimulations(
            Simulation.InjectPerSec(rate: 50, during: TimeSpan.FromMinutes(1))
        );

        // Act
        var stats = NBomberRunner
            .RegisterScenarios(scenario)
            .WithReportFolder("performance-reports")
            .WithReportFormats(ReportFormat.Html, ReportFormat.Csv)
            .Run();

        // Assert
        stats.AllOkCount.ShouldBeGreaterThan(0);
        stats.AllFailCount.ShouldBe(0);
        stats.ScenarioStats[0].Ok.Response.Mean.ShouldBeLessThan(500); // Response time < 500ms
    }

    [Test]
    [Category("StressTest")]
    public async Task Should_Maintain_Performance_Under_High_Load()
    {
        // Arrange
        var scenario = Scenario.Create("high_load_test", async context =>
        {
            // Mix of operations: 70% reads, 30% writes
            if (context.ScenarioInfo.InstanceId % 10 < 7)
            {
                // Read operation
                var cashiers = await _httpClient.GetAsync("/api/cashiers?pageSize=10");
                return cashiers.IsSuccessStatusCode ? Response.Ok() : Response.Fail();
            }
            else
            {
                // Write operation
                var request = new CreateCashierRequest(
                    $"Stress Test {context.ScenarioInfo.InstanceId}",
                    $"stress{context.ScenarioInfo.InstanceId}@example.com"
                );

                var response = await _httpClient.PostAsJsonAsync("/api/cashiers", request);
                return response.IsSuccessStatusCode ? Response.Ok() : Response.Fail();
            }
        })
        .WithLoadSimulations(
            Simulation.InjectPerSec(rate: 100, during: TimeSpan.FromMinutes(2)),
            Simulation.KeepConstant(copies: 50, during: TimeSpan.FromMinutes(3))
        );

        // Act
        var stats = NBomberRunner
            .RegisterScenarios(scenario)
            .WithReportFolder("stress-test-reports")
            .Run();

        // Assert
        stats.AllOkCount.ShouldBeGreaterThan(0);
        stats.AllFailCount.ShouldBeLessThan(stats.AllOkCount * 0.01m); // Less than 1% failure rate
        stats.ScenarioStats[0].Ok.Response.Mean.ShouldBeLessThan(1000); // Response time < 1s under stress
    }

    [OneTimeTearDown]
    public async Task OneTimeTearDown()
    {
        _httpClient?.Dispose();
        await _factory?.DisposeAsync();
    }
}
```

### Database Performance Testing

**Query Performance Tests**:

```csharp
[TestFixture]
[Category("Performance")]
public class DatabasePerformanceTests
{
    private PostgreSqlContainer _container;
    private IDbConnection _connection;

    [OneTimeSetUp]
    public async Task OneTimeSetUp()
    {
        _container = new PostgreSqlBuilder()
            .WithImage("postgres:15")
            .WithDatabase("AppDomain")
            .WithUsername("postgres")
            .WithPassword("password")
            .Build();

        await _container.StartAsync();
        _connection = new NpgsqlConnection(_container.GetConnectionString());
        await _connection.OpenAsync();

        await SeedPerformanceTestData();
    }

    [Test]
    public async Task Should_Execute_Cashier_Queries_Within_Performance_Limits()
    {
        // Arrange
        const int iterations = 1000;
        var stopwatch = Stopwatch.StartNew();

        // Act
        for (int i = 0; i < iterations; i++)
        {
            await _connection.QueryAsync<Cashier>(
                "SELECT * FROM cashiers WHERE tenant_id = @TenantId LIMIT 50",
                new { TenantId = Guid.NewGuid() }
            );
        }

        stopwatch.Stop();

        // Assert
        var averageExecutionTime = stopwatch.ElapsedMilliseconds / (double)iterations;
        averageExecutionTime.ShouldBeLessThan(10, "Average query execution should be under 10ms");
    }

    [Test]
    public async Task Should_Handle_Large_Result_Sets_Efficiently()
    {
        // Arrange
        const int expectedRecords = 10000;
        var stopwatch = Stopwatch.StartNew();

        // Act
        var cashiers = await _connection.QueryAsync<Cashier>(
            "SELECT * FROM cashiers LIMIT @Limit",
            new { Limit = expectedRecords }
        );

        stopwatch.Stop();

        // Assert
        var results = cashiers.ToList();
        results.ShouldHaveCount(expectedRecords);
        stopwatch.ElapsedMilliseconds.ShouldBeLessThan(1000, "Large result set query should complete within 1 second");
    }

    private async Task SeedPerformanceTestData()
    {
        // Seed large dataset for performance testing
        const int recordCount = 50000;
        var batchSize = 1000;

        for (int batch = 0; batch < recordCount / batchSize; batch++)
        {
            var cashiers = Enumerable.Range(batch * batchSize, batchSize)
                .Select(i => new
                {
                    tenant_id = Guid.NewGuid(),
                    cashier_id = Ulid.NewUlid().ToString(),
                    name = $"Performance Test Cashier {i}",
                    email = $"perf{i}@test.com",
                    created_at = DateTimeOffset.UtcNow
                });

            await _connection.ExecuteAsync(
                @"INSERT INTO cashiers (tenant_id, cashier_id, name, email, created_at)
                  VALUES (@tenant_id, @cashier_id, @name, @email, @created_at)",
                cashiers
            );
        }
    }

    [OneTimeTearDown]
    public async Task OneTimeTearDown()
    {
        if (_connection != null)
        {
            await _connection.CloseAsync();
            await _connection.DisposeAsync();
        }

        if (_container != null)
        {
            await _container.DisposeAsync();
        }
    }
}
```

## Test Configuration and CI/CD

### Test Categories and Filtering

**Test Execution Strategy**:

```bash
# Run all tests (default)
dotnet test

# Run tests by trait/category
dotnet test --filter "Category=Unit"
dotnet test --filter "Category=Integration"
dotnet test --filter "Category=Architecture"
dotnet test --filter "Category=Performance"

# Run tests for specific domain
dotnet test --filter "FullyQualifiedName~Cashiers"
dotnet test --filter "FullyQualifiedName~Invoices"

# Exclude slow-running tests
dotnet test --filter "Category!=Performance"

# Run with coverage collection
dotnet test --collect:"XPlat Code Coverage"
```

### GitHub Actions CI Pipeline

```yaml
name: Test Pipeline

on:
  push:
    branches: [main, develop]
  pull_request:
    branches: [main]

jobs:
  unit-tests:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4

      - name: Setup .NET
        uses: actions/setup-dotnet@v5
        with:
          dotnet-version: '10.0.x'

      - name: Restore dependencies
        run: dotnet restore

      - name: Run unit tests
        run: dotnet test --filter "Category=Unit" --logger trx --collect:"XPlat Code Coverage"

      - name: Upload coverage reports
        uses: codecov/codecov-action@v3
        with:
          file: coverage.cobertura.xml

  integration-tests:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4

      - name: Setup .NET
        uses: actions/setup-dotnet@v5
        with:
          dotnet-version: '10.0.x'

      - name: Start containers
        run: docker compose up -d AppDomain-db kafka

      - name: Wait for services
        run: |
          timeout 60 bash -c 'until docker compose ps | grep -q healthy; do sleep 2; done'

      - name: Run integration tests
        run: dotnet test --filter "Category=Integration" --logger trx

      - name: Stop containers
        run: docker compose down

  architecture-tests:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4

      - name: Setup .NET
        uses: actions/setup-dotnet@v5
        with:
          dotnet-version: '10.0.x'

      - name: Run architecture tests
        run: dotnet test --filter "Category=Architecture" --logger trx
```

## Best Practices

### Test Organization

1. **Follow AAA Pattern**: Arrange, Act, Assert
2. **Descriptive Test Names**: Use "Should_ExpectedBehavior_When_StateUnderTest" format
3. **Single Responsibility**: One test, one assertion
4. **Independent Tests**: No dependencies between tests
5. **Fast Unit Tests**: Mock external dependencies

### Test Data Management

```csharp
public static class TestDataBuilders
{
    public static CreateCashierCommand.Builder CreateCashierCommand()
        => new CreateCashierCommand.Builder();

    public static Cashier.Builder Cashier()
        => new Cashier.Builder();
}

public static class CreateCashierCommandExtensions
{
    public class Builder
    {
        private string _name = "Test Cashier";
        private string _email = "test@example.com";

        public Builder WithName(string name)
        {
            _name = name;
            return this;
        }

        public Builder WithEmail(string email)
        {
            _email = email;
            return this;
        }

        public CreateCashierCommand Build()
            => new CreateCashierCommand { Name = _name, Email = _email };

        public static implicit operator CreateCashierCommand(Builder builder)
            => builder.Build();
    }
}
```

### Assertion Patterns

```csharp
// Use Shouldly for better readability
result.ShouldNotBeNull();
result.IsSuccess.ShouldBeTrue();
result.Value.Name.ShouldBe("Expected Name");

// Group related assertions
result.ShouldBeEquivalentTo(expected);

// Custom assertions for domain concepts
result.IsSuccess.ShouldBeTrue();
result.Errors.ShouldContain("Expected error message");
```

## Related Resources

- [Architecture Overview](/arch/) - Understanding the system design
- [Event-Driven Architecture](/arch/events) - Testing event-driven systems
- [Background Processing](/arch/background-processing) - Testing Orleans and async processing
- [Debugging Tips](/guide/debugging) - Debugging failing tests
- [TestContainers Documentation](https://testcontainers.com/) - Container-based testing
- [Shouldly Documentation](https://docs.shouldly.org/) - Better test assertions
- [xUnit Documentation](https://xunit.net/) - Testing framework documentation
