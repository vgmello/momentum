# Unit Testing in Momentum

Unit tests in Momentum applications focus on testing individual components in isolation, particularly command handlers, query handlers, validators, and domain logic. This guide covers patterns and best practices for effective unit testing.

## Overview

Momentum's architecture makes unit testing straightforward by separating concerns:

- **Command/Query Handlers**: Business logic with clear inputs and outputs
- **Validators**: Input validation rules
- **Domain Models**: Business entities and rules
- **Mapping Extensions**: Data transformation logic

## Testing Command Handlers

### Basic Command Handler Testing

```csharp
[TestFixture]
public class CreateCashierCommandHandlerTests
{
    [Test]
    public async Task Handle_ValidCommand_ReturnsSuccessResult()
    {
        // Arrange
        var command = new CreateCashierCommand(
            Guid.NewGuid(), 
            "John Doe", 
            "john@example.com");

        var mockMessaging = new Mock<IMessageBus>();
        var expectedEntity = new Data.Entities.Cashier
        {
            CashierId = Guid.NewGuid(),
            TenantId = command.TenantId,
            Name = command.Name,
            Email = command.Email,
            CreatedDateUtc = DateTime.UtcNow,
            UpdatedDateUtc = DateTime.UtcNow
        };

        mockMessaging.Setup(m => m.InvokeCommandAsync(
                It.IsAny<CreateCashierCommandHandler.DbCommand>(), 
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedEntity);

        // Act
        var (result, integrationEvent) = await CreateCashierCommandHandler.Handle(
            command, 
            mockMessaging.Object, 
            CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.TenantId.Should().Be(command.TenantId);
        result.Value.Name.Should().Be(command.Name);
        result.Value.Email.Should().Be(command.Email);
        
        integrationEvent.Should().NotBeNull();
        integrationEvent!.TenantId.Should().Be(command.TenantId);
        integrationEvent.Cashier.Name.Should().Be(command.Name);
    }

    [Test]
    public async Task Handle_DatabaseError_ReturnsFailureResult()
    {
        // Arrange
        var command = new CreateCashierCommand(
            Guid.NewGuid(), 
            "John Doe", 
            "john@example.com");

        var mockMessaging = new Mock<IMessageBus>();
        mockMessaging.Setup(m => m.InvokeCommandAsync(
                It.IsAny<CreateCashierCommandHandler.DbCommand>(), 
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Database connection failed"));

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            CreateCashierCommandHandler.Handle(
                command, 
                mockMessaging.Object, 
                CancellationToken.None));

        exception.Message.Should().Be("Database connection failed");
    }

    [Test]
    public void CreateInsertCommand_ValidInput_CreatesCorrectDbCommand()
    {
        // Arrange
        var command = new CreateCashierCommand(
            Guid.NewGuid(), 
            "John Doe", 
            "john@example.com");

        // Act
        var dbCommand = CreateCashierCommandHandler.CreateInsertCommand(command);

        // Assert
        dbCommand.Should().NotBeNull();
        dbCommand.Cashier.TenantId.Should().Be(command.TenantId);
        dbCommand.Cashier.Name.Should().Be(command.Name);
        dbCommand.Cashier.Email.Should().Be(command.Email);
        dbCommand.Cashier.CashierId.Should().NotBeEmpty();
        dbCommand.Cashier.CreatedDateUtc.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        dbCommand.Cashier.UpdatedDateUtc.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }
}
```

### Testing Complex Command Handlers

For command handlers with multiple operations:

```csharp
[TestFixture]
public class UpdateCashierCommandHandlerTests
{
    private Mock<IMessageBus> _mockMessaging;

    [SetUp]
    public void Setup()
    {
        _mockMessaging = new Mock<IMessageBus>();
    }

    [Test]
    public async Task Handle_ValidCommand_UpdatesAndReturnsResult()
    {
        // Arrange
        var existingCashier = new Cashier
        {
            Id = Guid.NewGuid(),
            TenantId = Guid.NewGuid(),
            Name = "Original Name",
            Email = "original@example.com",
            CreatedDate = DateTime.UtcNow.AddDays(-1),
            UpdatedDate = DateTime.UtcNow.AddHours(-1)
        };

        var command = new UpdateCashierCommand(
            existingCashier.TenantId,
            existingCashier.Id,
            "Updated Name",
            "updated@example.com");

        // Setup get query to return existing cashier
        _mockMessaging.Setup(m => m.InvokeAsync(
                It.Is<GetCashierQuery>(q => 
                    q.TenantId == command.TenantId && 
                    q.Id == command.Id),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<Cashier>.Success(existingCashier));

        // Setup update command to return updated entity
        var updatedEntity = new Data.Entities.Cashier
        {
            CashierId = existingCashier.Id,
            TenantId = existingCashier.TenantId,
            Name = command.Name,
            Email = command.Email,
            CreatedDateUtc = existingCashier.CreatedDate,
            UpdatedDateUtc = DateTime.UtcNow
        };

        _mockMessaging.Setup(m => m.InvokeCommandAsync(
                It.IsAny<UpdateCashierCommandHandler.DbCommand>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(updatedEntity);

        // Act
        var (result, integrationEvent) = await UpdateCashierCommandHandler.Handle(
            command, 
            _mockMessaging.Object, 
            CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be(command.Name);
        result.Value.Email.Should().Be(command.Email);
        result.Value.UpdatedDate.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));

        integrationEvent.Should().NotBeNull();
        integrationEvent!.Cashier.Name.Should().Be(command.Name);
        
        // Verify interactions
        _mockMessaging.Verify(m => m.InvokeAsync(
            It.Is<GetCashierQuery>(q => q.Id == command.Id),
            It.IsAny<CancellationToken>()), Times.Once);
            
        _mockMessaging.Verify(m => m.InvokeCommandAsync(
            It.Is<UpdateCashierCommandHandler.DbCommand>(cmd => 
                cmd.Cashier.Name == command.Name),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task Handle_CashierNotFound_ReturnsFailureResult()
    {
        // Arrange
        var command = new UpdateCashierCommand(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Updated Name",
            "updated@example.com");

        _mockMessaging.Setup(m => m.InvokeAsync(
                It.IsAny<GetCashierQuery>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<Cashier>.Failure("Cashier not found"));

        // Act
        var (result, integrationEvent) = await UpdateCashierCommandHandler.Handle(
            command, 
            _mockMessaging.Object, 
            CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain("Cashier not found");
        integrationEvent.Should().BeNull();

        // Verify that update was not called
        _mockMessaging.Verify(m => m.InvokeCommandAsync(
            It.IsAny<UpdateCashierCommandHandler.DbCommand>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }
}
```

## Testing Query Handlers

### Simple Query Handler Testing

```csharp
[TestFixture]
public class GetCashierQueryHandlerTests
{
    private Mock<AppDomainDb> _mockDb;

    [SetUp]
    public void Setup()
    {
        _mockDb = new Mock<AppDomainDb>();
    }

    [Test]
    public async Task Handle_ExistingCashier_ReturnsSuccessResult()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var cashierId = Guid.NewGuid();
        var query = new GetCashierQuery(tenantId, cashierId);

        var cashierEntity = new Data.Entities.Cashier
        {
            CashierId = cashierId,
            TenantId = tenantId,
            Name = "John Doe",
            Email = "john@example.com",
            CreatedDateUtc = DateTime.UtcNow.AddDays(-1),
            UpdatedDateUtc = DateTime.UtcNow
        };

        var mockDbSet = new Mock<DbSet<Data.Entities.Cashier>>();
        mockDbSet.Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<Expression<Func<Data.Entities.Cashier, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(cashierEntity);

        _mockDb.Setup(db => db.Cashiers).Returns(mockDbSet.Object);

        // Act
        var result = await GetCashierQueryHandler.Handle(
            query, 
            _mockDb.Object, 
            CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.Id.Should().Be(cashierId);
        result.Value.TenantId.Should().Be(tenantId);
        result.Value.Name.Should().Be("John Doe");
        result.Value.Email.Should().Be("john@example.com");
    }

    [Test]
    public async Task Handle_NonExistingCashier_ReturnsFailureResult()
    {
        // Arrange
        var query = new GetCashierQuery(Guid.NewGuid(), Guid.NewGuid());

        var mockDbSet = new Mock<DbSet<Data.Entities.Cashier>>();
        mockDbSet.Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<Expression<Func<Data.Entities.Cashier, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Data.Entities.Cashier?)null);

        _mockDb.Setup(db => db.Cashiers).Returns(mockDbSet.Object);

        // Act
        var result = await GetCashierQueryHandler.Handle(
            query, 
            _mockDb.Object, 
            CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain("Cashier not found");
    }
}
```

### Testing Complex Queries with Pagination

```csharp
[TestFixture]
public class GetCashiersQueryHandlerTests
{
    [Test]
    public async Task Handle_ValidQuery_ReturnsPagedResults()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var query = new GetCashiersQuery(tenantId, page: 1, pageSize: 2);

        var cashiers = new List<Data.Entities.Cashier>
        {
            new() { CashierId = Guid.NewGuid(), TenantId = tenantId, Name = "Alice", Email = "alice@example.com" },
            new() { CashierId = Guid.NewGuid(), TenantId = tenantId, Name = "Bob", Email = "bob@example.com" },
            new() { CashierId = Guid.NewGuid(), TenantId = tenantId, Name = "Charlie", Email = "charlie@example.com" }
        }.AsQueryable();

        var mockDb = new Mock<AppDomainDb>();
        var mockDbSet = MockDbSet(cashiers);
        mockDb.Setup(db => db.Cashiers).Returns(mockDbSet.Object);

        // Act
        var result = await GetCashiersQueryHandler.Handle(
            query, 
            mockDb.Object, 
            CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.TotalCount.Should().Be(3);
        result.Value.Page.Should().Be(1);
        result.Value.PageSize.Should().Be(2);
        result.Value.Items.Should().HaveCount(2);
        result.Value.Items.First().Name.Should().Be("Alice"); // Ordered by name
    }

    private static Mock<DbSet<T>> MockDbSet<T>(IQueryable<T> data) where T : class
    {
        var mockSet = new Mock<DbSet<T>>();
        mockSet.As<IQueryable<T>>().Setup(m => m.Provider).Returns(data.Provider);
        mockSet.As<IQueryable<T>>().Setup(m => m.Expression).Returns(data.Expression);
        mockSet.As<IQueryable<T>>().Setup(m => m.ElementType).Returns(data.ElementType);
        mockSet.As<IQueryable<T>>().Setup(m => m.GetEnumerator()).Returns(data.GetEnumerator());
        return mockSet;
    }
}
```

## Testing Validators

### Basic Validator Testing

```csharp
[TestFixture]
public class CreateCashierValidatorTests
{
    private CreateCashierValidator _validator;

    [SetUp]
    public void Setup()
    {
        _validator = new CreateCashierValidator();
    }

    [Test]
    public void Validate_ValidCommand_ReturnsValid()
    {
        // Arrange
        var command = new CreateCashierCommand(
            Guid.NewGuid(),
            "John Doe",
            "john@example.com");

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Test]
    public void Validate_EmptyTenantId_ReturnsValidationError()
    {
        // Arrange
        var command = new CreateCashierCommand(
            Guid.Empty,
            "John Doe",
            "john@example.com");

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(command.TenantId));
    }

    [Test]
    public void Validate_EmptyName_ReturnsValidationError()
    {
        // Arrange
        var command = new CreateCashierCommand(
            Guid.NewGuid(),
            "",
            "john@example.com");

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(command.Name));
        result.Errors.First(e => e.PropertyName == nameof(command.Name))
            .ErrorMessage.Should().Be("'Name' must not be empty.");
    }

    [TestCase("a")]
    [TestCase("")]
    [TestCase(" ")]
    public void Validate_NameTooShort_ReturnsValidationError(string name)
    {
        // Arrange
        var command = new CreateCashierCommand(
            Guid.NewGuid(),
            name,
            "john@example.com");

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => 
            e.PropertyName == nameof(command.Name) &&
            e.ErrorMessage.Contains("minimum length"));
    }

    [Test]
    public void Validate_NameTooLong_ReturnsValidationError()
    {
        // Arrange
        var command = new CreateCashierCommand(
            Guid.NewGuid(),
            new string('x', 101), // 101 characters
            "john@example.com");

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => 
            e.PropertyName == nameof(command.Name) &&
            e.ErrorMessage.Contains("maximum length"));
    }

    [TestCase("invalid-email")]
    [TestCase("@example.com")]
    [TestCase("user@")]
    [TestCase("user@@example.com")]
    [TestCase("")]
    public void Validate_InvalidEmail_ReturnsValidationError(string email)
    {
        // Arrange
        var command = new CreateCashierCommand(
            Guid.NewGuid(),
            "John Doe",
            email);

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(command.Email));
    }

    [TestCase("user@example.com")]
    [TestCase("user.name@example.com")]
    [TestCase("user+tag@example.com")]
    [TestCase("user@sub.example.com")]
    public void Validate_ValidEmail_ReturnsValid(string email)
    {
        // Arrange
        var command = new CreateCashierCommand(
            Guid.NewGuid(),
            "John Doe",
            email);

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }
}
```

### Testing Complex Validation Rules

```csharp
[TestFixture]
public class UpdateCashierValidatorTests
{
    private UpdateCashierValidator _validator;

    [SetUp]
    public void Setup()
    {
        _validator = new UpdateCashierValidator();
    }

    [Test]
    public void Validate_AllFieldsValid_ReturnsValid()
    {
        // Arrange
        var command = new UpdateCashierCommand(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "John Doe",
            "john@example.com");

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Test]
    public void Validate_MultipleErrors_ReturnsAllErrors()
    {
        // Arrange
        var command = new UpdateCashierCommand(
            Guid.Empty,       // Invalid tenant ID
            Guid.Empty,       // Invalid cashier ID
            "",              // Invalid name
            "invalid-email"); // Invalid email

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().HaveCount(4);
        result.Errors.Should().Contain(e => e.PropertyName == nameof(command.TenantId));
        result.Errors.Should().Contain(e => e.PropertyName == nameof(command.Id));
        result.Errors.Should().Contain(e => e.PropertyName == nameof(command.Name));
        result.Errors.Should().Contain(e => e.PropertyName == nameof(command.Email));
    }
}
```

## Testing Domain Models and Extensions

### Testing Model Mapping Extensions

```csharp
[TestFixture]
public class CashierMappingTests
{
    [Test]
    public void ToModel_ValidEntity_ReturnsMappedModel()
    {
        // Arrange
        var entity = new Data.Entities.Cashier
        {
            CashierId = Guid.NewGuid(),
            TenantId = Guid.NewGuid(),
            Name = "John Doe",
            Email = "john@example.com",
            CreatedDateUtc = DateTime.UtcNow.AddDays(-1),
            UpdatedDateUtc = DateTime.UtcNow
        };

        // Act
        var model = entity.ToModel();

        // Assert
        model.Should().NotBeNull();
        model.Id.Should().Be(entity.CashierId);
        model.TenantId.Should().Be(entity.TenantId);
        model.Name.Should().Be(entity.Name);
        model.Email.Should().Be(entity.Email);
        model.CreatedDate.Should().Be(entity.CreatedDateUtc);
        model.UpdatedDate.Should().Be(entity.UpdatedDateUtc);
    }

    [Test]
    public void ToEntity_ValidModel_ReturnsMappedEntity()
    {
        // Arrange
        var model = new Cashier
        {
            Id = Guid.NewGuid(),
            TenantId = Guid.NewGuid(),
            Name = "John Doe",
            Email = "john@example.com",
            CreatedDate = DateTime.UtcNow.AddDays(-1),
            UpdatedDate = DateTime.UtcNow
        };

        // Act
        var entity = model.ToEntity();

        // Assert
        entity.Should().NotBeNull();
        entity.CashierId.Should().Be(model.Id);
        entity.TenantId.Should().Be(model.TenantId);
        entity.Name.Should().Be(model.Name);
        entity.Email.Should().Be(model.Email);
        entity.CreatedDateUtc.Should().Be(model.CreatedDate);
        entity.UpdatedDateUtc.Should().Be(model.UpdatedDate);
    }

    [Test]
    public void ToModel_NullEntity_ThrowsArgumentNullException()
    {
        // Arrange
        Data.Entities.Cashier? entity = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => entity!.ToModel());
    }
}
```

### Testing Business Logic

```csharp
[TestFixture]
public class CashierBusinessLogicTests
{
    [Test]
    public void CalculateTotalSales_ValidInvoices_ReturnsCorrectTotal()
    {
        // Arrange
        var cashier = new Cashier
        {
            Id = Guid.NewGuid(),
            TenantId = Guid.NewGuid(),
            Name = "John Doe",
            Email = "john@example.com"
        };

        var invoices = new List<Invoice>
        {
            new() { Amount = 100.50m, Status = InvoiceStatus.Paid },
            new() { Amount = 200.75m, Status = InvoiceStatus.Paid },
            new() { Amount = 50.00m, Status = InvoiceStatus.Pending } // Should be ignored
        };

        // Act
        var total = cashier.CalculateTotalSales(invoices);

        // Assert
        total.Should().Be(301.25m);
    }

    [Test]
    public void CalculateTotalSales_EmptyInvoices_ReturnsZero()
    {
        // Arrange
        var cashier = new Cashier { Id = Guid.NewGuid() };
        var invoices = new List<Invoice>();

        // Act
        var total = cashier.CalculateTotalSales(invoices);

        // Assert
        total.Should().Be(0);
    }

    [Test]
    public void CalculateTotalSales_NullInvoices_ThrowsArgumentNullException()
    {
        // Arrange
        var cashier = new Cashier { Id = Guid.NewGuid() };

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            cashier.CalculateTotalSales(null!));
    }
}
```

## Testing Utilities and Helpers

### Test Data Builders

Create builders for test data to make tests more readable:

```csharp
public class CashierTestDataBuilder
{
    private Guid _tenantId = Guid.NewGuid();
    private Guid _cashierId = Guid.NewGuid();
    private string _name = "Default Name";
    private string _email = "default@example.com";
    private DateTime _createdDate = DateTime.UtcNow.AddDays(-1);
    private DateTime _updatedDate = DateTime.UtcNow;

    public CashierTestDataBuilder WithTenantId(Guid tenantId)
    {
        _tenantId = tenantId;
        return this;
    }

    public CashierTestDataBuilder WithId(Guid cashierId)
    {
        _cashierId = cashierId;
        return this;
    }

    public CashierTestDataBuilder WithName(string name)
    {
        _name = name;
        return this;
    }

    public CashierTestDataBuilder WithEmail(string email)
    {
        _email = email;
        return this;
    }

    public CashierTestDataBuilder CreatedOn(DateTime createdDate)
    {
        _createdDate = createdDate;
        return this;
    }

    public CashierTestDataBuilder UpdatedOn(DateTime updatedDate)
    {
        _updatedDate = updatedDate;
        return this;
    }

    public Cashier BuildModel()
    {
        return new Cashier
        {
            Id = _cashierId,
            TenantId = _tenantId,
            Name = _name,
            Email = _email,
            CreatedDate = _createdDate,
            UpdatedDate = _updatedDate
        };
    }

    public Data.Entities.Cashier BuildEntity()
    {
        return new Data.Entities.Cashier
        {
            CashierId = _cashierId,
            TenantId = _tenantId,
            Name = _name,
            Email = _email,
            CreatedDateUtc = _createdDate,
            UpdatedDateUtc = _updatedDate
        };
    }

    public CreateCashierCommand BuildCreateCommand()
    {
        return new CreateCashierCommand(_tenantId, _name, _email);
    }
}

// Usage in tests
[Test]
public async Task Handle_ValidCommand_ReturnsSuccessResult()
{
    // Arrange
    var cashier = new CashierTestDataBuilder()
        .WithName("John Doe")
        .WithEmail("john@example.com")
        .BuildEntity();

    var command = new CashierTestDataBuilder()
        .WithTenantId(cashier.TenantId)
        .WithName("John Doe")
        .WithEmail("john@example.com")
        .BuildCreateCommand();

    // ... rest of test
}
```

### Custom Test Attributes

Create custom attributes for common test scenarios:

```csharp
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public class AutoMockDataAttribute : Attribute, ITestDataSource
{
    public IEnumerable<object[]> GetData(MethodInfo methodInfo)
    {
        var fixture = new Fixture();
        fixture.Customize(new AutoMoqCustomization());
        
        var parameters = methodInfo.GetParameters();
        var args = new object[parameters.Length];
        
        for (int i = 0; i < parameters.Length; i++)
        {
            args[i] = fixture.Create(parameters[i].ParameterType);
        }
        
        yield return args;
    }

    public string GetDisplayName(MethodInfo methodInfo, object[] data)
    {
        return $"{methodInfo.Name}({string.Join(", ", data)})";
    }
}

// Usage
[Test, AutoMockData]
public async Task Handle_ShouldProcessCorrectly(
    CreateCashierCommand command,
    Mock<IMessageBus> mockMessaging)
{
    // Test implementation with auto-generated data
}
```

## Test Organization and Best Practices

### Test Project Structure

```
tests/
├── AppDomain.Tests/
│   ├── Unit/
│   │   ├── Cashiers/
│   │   │   ├── Commands/
│   │   │   │   ├── CreateCashierCommandHandlerTests.cs
│   │   │   │   └── UpdateCashierCommandHandlerTests.cs
│   │   │   ├── Queries/
│   │   │   │   └── GetCashierQueryHandlerTests.cs
│   │   │   └── Validators/
│   │   │       └── CreateCashierValidatorTests.cs
│   │   └── Common/
│   │       ├── MappingTests.cs
│   │       └── TestDataBuilders.cs
│   ├── Integration/
│   │   └── (Integration test files)
│   └── TestUtilities/
│       ├── MockExtensions.cs
│       └── TestFixtures.cs
```

### Test Naming Conventions

Follow consistent naming patterns:

```csharp
// Pattern: MethodName_StateUnderTest_ExpectedBehavior
[Test]
public void Handle_ValidCommand_ReturnsSuccessResult() { }

[Test]
public void Handle_InvalidEmail_ReturnsValidationError() { }

[Test]
public void Validate_EmptyTenantId_ReturnsValidationFailure() { }

[Test]
public void ToModel_ValidEntity_ReturnsMappedModel() { }
```

### Arrange-Act-Assert Pattern

Structure all tests consistently:

```csharp
[Test]
public async Task Handle_ValidCommand_ReturnsSuccessResult()
{
    // Arrange - Set up test data and mocks
    var command = new CreateCashierCommand(/* parameters */);
    var mockMessaging = new Mock<IMessageBus>();
    // ... setup mocks and expectations

    // Act - Execute the system under test
    var result = await CommandHandler.Handle(command, mockMessaging.Object, CancellationToken.None);

    // Assert - Verify the results
    result.IsSuccess.Should().BeTrue();
    result.Value.Should().NotBeNull();
    // ... verify all expectations
}
```

### Testing Guidelines

1. **Test one thing at a time**: Each test should verify a single behavior
2. **Use descriptive test names**: Test names should explain what is being tested
3. **Keep tests simple**: Avoid complex logic in tests
4. **Use builders for test data**: Make test data creation reusable and readable
5. **Mock external dependencies**: Unit tests should not depend on external systems
6. **Test edge cases**: Include tests for boundary conditions and error scenarios
7. **Verify interactions**: Use mocks to verify that dependencies are called correctly

## Running Unit Tests

### Command Line

```bash
# Run all unit tests
dotnet test tests/AppDomain.Tests/

# Run tests with coverage
dotnet test tests/AppDomain.Tests/ --collect:"XPlat Code Coverage"

# Run specific test class
dotnet test tests/AppDomain.Tests/ --filter "FullyQualifiedName~CreateCashierCommandHandlerTests"

# Run tests by category
dotnet test tests/AppDomain.Tests/ --filter "Category=Unit"
```

### IDE Integration

Most IDEs provide integrated test runners:

- **Visual Studio**: Test Explorer window
- **VS Code**: .NET Test Explorer extension
- **JetBrains Rider**: Unit Test Explorer

### Continuous Integration

Configure CI pipelines to run tests automatically:

```yaml
# GitHub Actions example
- name: Run Unit Tests
  run: dotnet test tests/AppDomain.Tests/ --no-restore --verbosity normal --collect:"XPlat Code Coverage"

- name: Upload Coverage
  uses: codecov/codecov-action@v1
  with:
    file: coverage.xml
```

## Next Steps

- Learn about [Integration Testing](./integration-tests) for end-to-end testing
- Explore [CQRS Testing](../cqrs/) patterns for commands and queries
- See [Best Practices](../best-practices) for overall testing strategy
- Review [Troubleshooting](../troubleshooting) for common testing issues