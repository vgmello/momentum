---
title: Testing
description: Comprehensive testing strategies and tools for Momentum applications, covering unit tests, integration tests, architecture tests, and specialized testing scenarios with practical implementation examples.
date: 2025-01-15
---

# Testing

Comprehensive testing strategies and tools for Momentum applications, covering unit tests, integration tests, architecture tests, and specialized testing scenarios with practical implementation examples.

## Overview

Momentum provides a sophisticated testing framework supporting multiple testing strategies with real infrastructure provisioning through Testcontainers.

### Core Testing Principles

- **Domain-Centric Testing**: Tests align with business domains and use cases
- **Real Infrastructure**: Testcontainers provide PostgreSQL, Kafka, and Liquibase migrations
- **Result Pattern Testing**: Comprehensive validation of `Result<T>` success and failure scenarios
- **Multi-Tenant Validation**: Tests verify tenant isolation and data segregation
- **Event-Driven Verification**: Integration event publishing and consumption validation

## Testing Architecture

### Test Project Structure

```
tests/
├── AppDomain.Tests/
│   ├── Unit/                    # Fast, isolated unit tests
│   │   ├── [Domain]/
│   │   │   ├── Commands/        # Command handler tests
│   │   │   ├── Queries/         # Query handler tests
│   │   │   └── Validation/      # FluentValidation tests
│   ├── Integration/             # End-to-end integration tests
│   │   ├── [Domain]/            # Domain-specific scenarios
│   │   ├── _Internal/           # Test infrastructure
│   │   └── IntegrationTestFixture.cs
│   └── Architecture/            # Architecture constraint tests
│       ├── CqrsPatternRulesTests.cs
│       ├── DomainIsolationRulesTests.cs
│       └── MultiTenancyRulesTests.cs
```

## Unit Testing

### CQRS Handler Testing

Test command and query handlers in isolation using mocked dependencies.

#### Command Handler Example

```csharp
public class CreateCashierCommandHandlerTests
{
    [Fact]
    public async Task Handle_WithValidCommand_ShouldCreateCashierAndReturnResult()
    {
        // Arrange
        var messagingMock = Substitute.For<IMessageBus>();
        messagingMock.InvokeCommandAsync(Arg.Any<CreateCashierCommandHandler.DbCommand>(), Arg.Any<CancellationToken>())
            .Returns(new Cashier
            {
                TenantId = Guid.Empty,
                CashierId = Guid.NewGuid(),
                Name = "John Doe",
                Email = "john.doe@example.com",
                CreatedDateUtc = DateTime.UtcNow,
                UpdatedDateUtc = DateTime.UtcNow,
                Version = 12345
            });

        var command = new CreateCashierCommand(Guid.Empty, "John Doe", "john.doe@example.com");

        // Act
        var (result, integrationEvent) = await CreateCashierCommandHandler.Handle(command, messagingMock, CancellationToken.None);

        // Assert - Verify Result<T> success
        var cashier = result.Match(success => success, _ => null!);
        cashier.ShouldNotBeNull();
        cashier.Name.ShouldBe("John Doe");
        cashier.Email.ShouldBe("john.doe@example.com");
        cashier.CashierId.ShouldNotBe(Guid.Empty);

        // Verify integration event publishing
        integrationEvent.ShouldNotBeNull();
        integrationEvent.ShouldBeOfType<CashierCreated>();
        integrationEvent.Cashier.CashierId.ShouldBe(cashier.CashierId);
        integrationEvent.Cashier.Name.ShouldBe(cashier.Name);
        integrationEvent.Cashier.Email.ShouldBe(cashier.Email);

        // Verify messaging was called correctly
        await messagingMock.Received(1).InvokeCommandAsync(
            Arg.Is<CreateCashierCommandHandler.DbCommand>(cmd =>
                cmd.Cashier.TenantId == Guid.Empty &&
                cmd.Cashier.Name == "John Doe" &&
                cmd.Cashier.Email == "john.doe@example.com"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithDatabaseFailure_ShouldThrowException()
    {
        // Arrange
        var messagingMock = Substitute.For<IMessageBus>();
        messagingMock.InvokeCommandAsync(Arg.Any<CreateCashierCommandHandler.DbCommand>(), Arg.Any<CancellationToken>())
            .Throws(new InvalidOperationException("Database connection failed"));

        var command = new CreateCashierCommand(Guid.Empty, "John Doe", "john.doe@example.com");

        // Act & Assert
        await Should.ThrowAsync<InvalidOperationException>(async () =>
            await CreateCashierCommandHandler.Handle(command, messagingMock, CancellationToken.None));
    }
}
```

### Result Pattern Testing

Test both success and failure paths of the `Result<T>` pattern:

```csharp
[Fact]
public void ResultPattern_WithValidationFailures_ShouldReturnFailures()
{
    // Arrange
    var failures = new List<ValidationFailure>
    {
        new("Email", "Email is required"),
        new("Name", "Name must be at least 3 characters")
    };

    // Act
    Result<Cashier> result = failures;

    // Assert
    result.IsT1.ShouldBeTrue(); // Is failures
    var validationFailures = result.AsT1;
    validationFailures.Count.ShouldBe(2);
    validationFailures.ShouldContain(f => f.PropertyName == "Email");
    validationFailures.ShouldContain(f => f.PropertyName == "Name");
}
```

### Multi-Tenant Testing

Verify tenant isolation in business logic:

```csharp
[Fact]
public async Task Handle_WithDifferentTenants_ShouldIsolateTenantData()
{
    // Arrange
    var tenant1Id = Guid.NewGuid();
    var tenant2Id = Guid.NewGuid();

    var command1 = new GetCashiersQuery(tenant1Id);
    var command2 = new GetCashiersQuery(tenant2Id);

    // Act & Assert
    // Verify each tenant only sees their own data
}
```

## Integration Testing

### Test Fixture Setup

The `IntegrationTestFixture` provides a complete testing environment with real infrastructure:

```csharp
public class IntegrationTestFixture : WebApplicationFactory<AppDomain.Api.Program>, IAsyncLifetime
{
    private readonly INetwork _containerNetwork = new NetworkBuilder().Build();
    private readonly PostgreSqlContainer _postgres;
    private readonly KafkaContainer _kafka;

    public GrpcChannel GrpcChannel { get; private set; } = null!;
    public ITestOutputHelper? TestOutput { get; set; }

    public IntegrationTestFixture()
    {
        _postgres = new PostgreSqlBuilder()
            .WithImage("postgres:17-alpine")
            .WithUsername("postgres")
            .WithPassword("postgres")
            .WithNetwork(_containerNetwork)
            .Build();

        _kafka = new KafkaBuilder()
            .WithImage("confluentinc/cp-kafka:7.6.0")
            .WithNetwork(_containerNetwork)
            .Build();
    }

    public async ValueTask InitializeAsync()
    {
        await _containerNetwork.CreateAsync();
        await _postgres.StartAsync();
        await _kafka.StartAsync();

        // Run Liquibase migrations
        await using var liquibaseMigrationContainer = new LiquibaseMigrationContainer(
            _postgres.Name, _containerNetwork);
        await liquibaseMigrationContainer.StartAsync();

        GrpcChannel = GrpcChannel.ForAddress(Server.BaseAddress, new GrpcChannelOptions
        {
            HttpHandler = Server.CreateHandler()
        });
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("ConnectionStrings:AppDomainDb",
            _postgres.GetDbConnectionString("app_domain"));
        builder.UseSetting("ConnectionStrings:ServiceBus",
            _postgres.GetDbConnectionString("service_bus"));
        builder.UseSetting("ConnectionStrings:Messaging",
            _kafka.GetBootstrapAddress());
        builder.UseSetting("Orleans:UseLocalhostClustering", "true");

        builder.ConfigureServices((ctx, services) =>
        {
            services.RemoveServices<IHostedService>();
            services.RemoveServices<ILoggerFactory>();
            services.AddLogging(logging => logging
                .ClearProviders()
                .AddSerilog(CreateTestLogger(nameof(AppDomain))));

            services.AddWolverineWithDefaults(ctx.HostingEnvironment, ctx.Configuration,
                opt => opt.ApplicationAssembly = typeof(AppDomain.Api.Program).Assembly);
        });

        builder.Configure(app =>
        {
            app.UseRouting();
            app.UseEndpoints(endpoints => endpoints.MapGrpcServices(typeof(AppDomain.Api.Program).Assembly));
        });
    }
}
```

### End-to-End Integration Tests

Test complete user scenarios through the API:

```csharp
public class CreateCashierIntegrationTests(IntegrationTestFixture fixture) : IntegrationTest(fixture)
{
    private readonly CashiersService.CashiersServiceClient _client = new(fixture.GrpcChannel);

    [Fact]
    public async Task CreateCashier_ShouldCreateCashierInDatabase()
    {
        // Arrange
        var request = new CreateCashierRequest
        {
            Name = "Integration Test Cashier",
            Email = "integration@test.com"
        };

        // Act
        var response = await _client.CreateCashierAsync(request,
            cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        response.Name.ShouldBe(request.Name);
        response.Email.ShouldBe(request.Email);
        Guid.Parse(response.CashierId).ShouldNotBe(Guid.Empty);

        // Verify in database
        var getResponse = await _client.GetCashierAsync(new GetCashierRequest
        {
            CashierId = response.CashierId
        });
        getResponse.ShouldNotBeNull();
    }

    [Fact]
    public async Task CreateCashier_WithConcurrentRequests_ShouldHandleAllRequests()
    {
        // Arrange
        var tasks = Enumerable.Range(1, 10)
            .Select(i => _client.CreateCashierAsync(new CreateCashierRequest
            {
                Name = $"Cashier {i}",
                Email = $"cashier{i}@test.com"
            }))
            .ToArray();

        // Act
        var responses = await Task.WhenAll(tasks);

        // Assert
        responses.Length.ShouldBe(10);
        responses.Select(r => r.CashierId).Distinct().Count().ShouldBe(10);
    }
}
```

### Event Testing

Test integration event publishing and consumption:

```csharp
public class EventIntegrationTests(IntegrationTestFixture fixture) : IntegrationTest(fixture)
{
    [Fact]
    public async Task CreateCashier_ShouldPublishCashierCreatedEvent()
    {
        // Arrange
        var eventCollector = new IntegrationEventCollector<CashierCreated>();

        // Subscribe to Kafka topic
        await eventCollector.StartListening("main.cashiers.created");

        var request = new CreateCashierRequest
        {
            Name = "Event Test Cashier",
            Email = "event@test.com"
        };

        // Act
        var response = await _client.CreateCashierAsync(request);

        // Assert
        var publishedEvent = await eventCollector.WaitForEvent(TimeSpan.FromSeconds(5));
        publishedEvent.ShouldNotBeNull();
        publishedEvent.Cashier.Name.ShouldBe(request.Name);
        publishedEvent.Cashier.Email.ShouldBe(request.Email);
    }
}
```

## Architecture Testing

### Enforcing Design Constraints

Use NetArchTest.Rules to enforce architectural boundaries:

```csharp
public class CqrsPatternRulesTests : ArchitectureTestBase
{
    [Fact]
    public void Commands_ShouldBeInCommandsNamespace()
    {
        var result = GetAppDomainTypes()
            .That()
            .HaveName(x => x.EndsWith("Command"))
            .Should()
            .ResideInNamespace("Commands")
            .GetResult();

        result.IsSuccessful.ShouldBeTrue();
    }

    [Fact]
    public void CommandHandlers_ShouldNotDependOnOtherDomains()
    {
        var result = GetAppDomainTypes()
            .That()
            .HaveName(x => x.EndsWith("CommandHandler"))
            .Should()
            .NotHaveDependencyOn("AppDomain.*.Queries")
            .GetResult();

        result.IsSuccessful.ShouldBeTrue();
    }
}

public class MultiTenancyRulesTests : ArchitectureTestBase
{
    [Fact]
    public void Entities_ShouldHaveTenantIdProperty()
    {
        var entityTypes = GetAppDomainTypes()
            .That()
            .ResideInNamespace("Data.Entities")
            .GetTypes();

        foreach (var entityType in entityTypes)
        {
            entityType.GetProperty("TenantId").ShouldNotBeNull();
        }
    }
}
```

## Validation Testing

### FluentValidation Testing

Test validation rules using FluentValidation.TestHelper:

```csharp
public class CashierValidationTests
{
    [Fact]
    public void CreateCashierValidator_WithValidData_ShouldNotHaveValidationErrors()
    {
        // Arrange
        var validator = new CreateCashierValidator();
        var command = new CreateCashierCommand(Guid.NewGuid(), "John Doe", "john.doe@example.com");

        // Act
        var result = validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void CreateCashierValidator_WithEmptyName_ShouldHaveValidationError(string name)
    {
        // Arrange
        var validator = new CreateCashierValidator();
        var command = new CreateCashierCommand(Guid.NewGuid(), name, "john.doe@example.com");

        // Act
        var result = validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Name)
            .WithErrorMessage("'Name' must not be empty.");
    }

    [Fact]
    public void CreateCashierValidator_WithNameTooShort_ShouldHaveValidationError()
    {
        // Arrange
        var validator = new CreateCashierValidator();
        var command = new CreateCashierCommand(Guid.NewGuid(), "A", "john.doe@example.com");

        // Act
        var result = validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Name)
            .WithErrorMessage("The length of 'Name' must be at least 2 characters. You entered 1 characters.");
    }

    [Fact]
    public void CreateCashierValidator_WithValidNameBoundaries_ShouldNotHaveValidationErrors()
    {
        // Arrange
        var validator = new CreateCashierValidator();
        var minLengthCommand = new CreateCashierCommand(Guid.NewGuid(), "Jo", "test@example.com"); // 2 characters
        var maxLengthCommand = new CreateCashierCommand(Guid.NewGuid(), new string('A', 100), "test@example.com"); // 100 characters

        // Act
        var minResult = validator.TestValidate(minLengthCommand);
        var maxResult = validator.TestValidate(maxLengthCommand);

        // Assert
        minResult.ShouldNotHaveAnyValidationErrors();
        maxResult.ShouldNotHaveAnyValidationErrors();
    }
}
```

## Performance Testing

### Load Testing with Custom Performance Tools

```csharp
public class CashierPerformanceTests(IntegrationTestFixture fixture) : IntegrationTest(fixture)
{
    [Fact]
    public async Task CreateCashier_ShouldMaintainPerformanceUnderLoad()
    {
        // Arrange
        const int numberOfRequests = 100;
        const int concurrentUsers = 10;
        var stopwatch = Stopwatch.StartNew();

        // Act
        var semaphore = new SemaphoreSlim(concurrentUsers);
        var tasks = Enumerable.Range(1, numberOfRequests)
            .Select(async i =>
            {
                await semaphore.WaitAsync();
                try
                {
                    return await _client.CreateCashierAsync(new CreateCashierRequest
                    {
                        Name = $"Load Test Cashier {i}",
                        Email = $"loadtest{i}@test.com"
                    });
                }
                finally
                {
                    semaphore.Release();
                }
            });

        var responses = await Task.WhenAll(tasks);
        stopwatch.Stop();

        // Assert
        responses.Length.ShouldBe(numberOfRequests);
        var averageResponseTime = stopwatch.ElapsedMilliseconds / (double)numberOfRequests;
        averageResponseTime.ShouldBeLessThan(100); // Less than 100ms average

        // Verify all requests succeeded
        responses.ShouldAllBe(r => !string.IsNullOrEmpty(r.CashierId));
    }

    [Fact]
    public async Task GetCashiers_ShouldHandleHighVolumeReads()
    {
        // Test read performance with large datasets
    }
}
```

### Memory and Resource Testing

```csharp
[Fact]
public async Task BulkOperations_ShouldNotCauseMemoryLeaks()
{
    // Arrange
    var initialMemory = GC.GetTotalMemory(forceFullCollection: true);

    // Act - Perform bulk operations
    for (int i = 0; i < 1000; i++)
    {
        await _client.CreateCashierAsync(new CreateCashierRequest
        {
            Name = $"Bulk Cashier {i}",
            Email = $"bulk{i}@test.com"
        });
    }

    // Force garbage collection
    GC.Collect();
    GC.WaitForPendingFinalizers();
    GC.Collect();

    var finalMemory = GC.GetTotalMemory(forceFullCollection: true);

    // Assert
    var memoryIncrease = finalMemory - initialMemory;
    memoryIncrease.ShouldBeLessThan(50 * 1024 * 1024); // Less than 50MB increase
}
```

## Testing Tools and Utilities

### Core Testing Stack

- **xUnit v3**: Primary testing framework with collection fixtures
- **Shouldly**: Expressive assertions for readable test failures
- **NSubstitute**: Mocking framework for dependencies
- **Testcontainers**: Real infrastructure provisioning (PostgreSQL, Kafka)
- **NetArchTest.Rules**: Architecture constraint validation
- **FluentValidation.TestHelper**: Validation testing utilities

### Custom Test Utilities

```csharp
public class IntegrationEventCollector<T> where T : class
{
    private readonly TaskCompletionSource<T> _eventReceived = new();
    private IConsumer<string, string> _consumer;

    public async Task StartListening(string topicName)
    {
        var config = new ConsumerConfig
        {
            BootstrapServers = "localhost:9092",
            GroupId = $"test-{Guid.NewGuid()}",
            AutoOffsetReset = AutoOffsetReset.Latest
        };

        _consumer = new ConsumerBuilder<string, string>(config).Build();
        _consumer.Subscribe(topicName);

        _ = Task.Run(async () =>
        {
            while (!_eventReceived.Task.IsCompleted)
            {
                var result = _consumer.Consume(TimeSpan.FromMilliseconds(100));
                if (result?.Message?.Value != null)
                {
                    var eventData = JsonSerializer.Deserialize<T>(result.Message.Value);
                    _eventReceived.SetResult(eventData);
                    break;
                }
            }
        });
    }

    public async Task<T> WaitForEvent(TimeSpan timeout)
    {
        using var cts = new CancellationTokenSource(timeout);
        return await _eventReceived.Task.WaitAsync(cts.Token);
    }
}
```

## Best Practices

### Test Organization

1. **Follow the AAA Pattern**: Arrange, Act, Assert in every test
2. **One Assertion Per Test**: Focus on single behavior verification
3. **Descriptive Test Names**: Use `Should_Expected_When_Condition` format
4. **Test Categories**: Use `[Trait]` attributes for test categorization

### Data Management

1. **Isolated Test Data**: Each test creates its own data
2. **Cleanup Strategies**: Use transaction rollback or container recreation
3. **Realistic Test Data**: Use domain-appropriate values, not generic strings

### Performance Considerations

1. **Fast Unit Tests**: Keep unit tests under 1ms execution time
2. **Reasonable Integration Tests**: Aim for under 5 seconds per integration test
3. **Parallel Execution**: Design tests for parallel execution safety
4. **Resource Cleanup**: Properly dispose of containers and connections

### Maintenance

1. **Shared Test Infrastructure**: Reuse test fixtures and utilities
2. **Clear Test Documentation**: Document complex test scenarios
3. **Regular Test Review**: Remove obsolete tests and update assertions
4. **Continuous Monitoring**: Track test execution times and failure rates

## Getting Started

1. **Set up Test Project**: Create test project with Momentum testing packages
2. **Configure Integration Fixtures**: Set up Testcontainers with PostgreSQL and Kafka
3. **Write Unit Tests**: Start with command/query handler tests
4. **Add Integration Tests**: Test complete API scenarios
5. **Implement Architecture Tests**: Enforce design constraints
6. **Performance Testing**: Add load tests for critical paths

## Related Topics

- [CQRS](../cqrs/index.md) - Command and Query patterns
- [Messaging](../messaging/index.md) - Event-driven communication
- [Database](../database/index.md) - Data access patterns
- [Service Configuration](../service-configuration/index.md) - Application setup
