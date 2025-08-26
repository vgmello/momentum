---
title: Validation
description: Input validation using FluentValidation, providing a consistent and powerful way to validate commands and queries before they reach handlers.
date: 2024-01-15
---

# Validation in Momentum

Momentum uses FluentValidation for input validation, providing a consistent and powerful way to validate commands and queries before they reach your handlers.

## Validation Setup

Validation is automatically configured when you use `AddServiceDefaults()`:

```csharp
var builder = WebApplication.CreateBuilder(args);

// This automatically registers all validators from domain assemblies
builder.AddServiceDefaults();

var app = builder.Build();
```

Validators are discovered from assemblies marked with the `DomainAssembly` attribute:

```csharp
[assembly: DomainAssembly(typeof(IAppDomainAssembly))]
```

## Basic Validation

### Command Validation

Create validators for your commands by inheriting from `AbstractValidator<T>`:

```csharp
public record CreateCashierCommand(Guid TenantId, string Name, string Email) : ICommand<Result<Cashier>>;

public class CreateCashierValidator : AbstractValidator<CreateCashierCommand>
{
    public CreateCashierValidator()
    {
        RuleFor(c => c.TenantId)
            .NotEmpty()
            .WithMessage("Tenant ID is required");

        RuleFor(c => c.Name)
            .NotEmpty()
            .WithMessage("Name is required")
            .MinimumLength(2)
            .WithMessage("Name must be at least 2 characters")
            .MaximumLength(100)
            .WithMessage("Name cannot exceed 100 characters");

        RuleFor(c => c.Email)
            .NotEmpty()
            .WithMessage("Email is required")
            .EmailAddress()
            .WithMessage("Please provide a valid email address");
    }
}
```

### Query Validation

Queries can also have validators, though they're typically simpler:

```csharp
public record GetCashierQuery(Guid TenantId, Guid Id) : IQuery<Result<Cashier>>;

public class GetCashierValidator : AbstractValidator<GetCashierQuery>
{
    public GetCashierValidator()
    {
        RuleFor(q => q.TenantId)
            .NotEmpty()
            .WithMessage("Tenant ID is required");

        RuleFor(q => q.Id)
            .NotEmpty()
            .WithMessage("Cashier ID is required");
    }
}
```

## Advanced Validation Rules

### Complex Validation Logic

```csharp
public class UpdateInvoiceValidator : AbstractValidator<UpdateInvoiceCommand>
{
    public UpdateInvoiceValidator()
    {
        RuleFor(c => c.Id)
            .NotEmpty();

        RuleFor(c => c.TenantId)
            .NotEmpty();

        RuleFor(c => c.Amount)
            .GreaterThan(0)
            .WithMessage("Amount must be greater than zero")
            .LessThanOrEqualTo(1000000)
            .WithMessage("Amount cannot exceed 1,000,000");

        RuleFor(c => c.Description)
            .NotEmpty()
            .WithMessage("Description is required")
            .MaximumLength(500)
            .WithMessage("Description cannot exceed 500 characters");

        RuleFor(c => c.DueDate)
            .GreaterThanOrEqualTo(DateTime.Today)
            .WithMessage("Due date cannot be in the past")
            .When(c => c.DueDate.HasValue);

        // Conditional validation
        When(c => c.CashierId.HasValue, () =>
        {
            RuleFor(c => c.CashierId!.Value)
                .NotEmpty()
                .WithMessage("Cashier ID must be valid when specified");
        });
    }
}
```

### Custom Validation Rules

```csharp
public class CreateUserValidator : AbstractValidator<CreateUserCommand>
{
    public CreateUserValidator()
    {
        RuleFor(c => c.Email)
            .NotEmpty()
            .EmailAddress()
            .Must(BeUniqueEmail)
            .WithMessage("Email address is already in use");

        RuleFor(c => c.Password)
            .NotEmpty()
            .MinimumLength(8)
            .Must(HaveStrongPassword)
            .WithMessage("Password must contain at least one uppercase letter, one lowercase letter, and one number");

        RuleFor(c => c.Age)
            .InclusiveBetween(18, 120)
            .When(c => c.Age.HasValue)
            .WithMessage("Age must be between 18 and 120");
    }

    private bool BeUniqueEmail(string email)
    {
        // Note: This is synchronous validation
        // For async validation, use MustAsync
        return !_existingEmails.Contains(email);
    }

    private bool HaveStrongPassword(string password)
    {
        return password.Any(char.IsUpper) &&
               password.Any(char.IsLower) &&
               password.Any(char.IsDigit);
    }
}
```

### Async Validation

For validation that requires database calls or external services:

```csharp
public class CreateCashierValidator : AbstractValidator<CreateCashierCommand>
{
    private readonly IUserService _userService;

    public CreateCashierValidator(IUserService userService)
    {
        _userService = userService;

        RuleFor(c => c.Email)
            .NotEmpty()
            .EmailAddress()
            .MustAsync(BeUniqueEmailAsync)
            .WithMessage("Email address is already in use");
    }

    private async Task<bool> BeUniqueEmailAsync(string email, CancellationToken cancellationToken)
    {
        return await _userService.IsEmailUniqueAsync(email, cancellationToken);
    }
}
```

## Cross-Field Validation

Validate relationships between multiple fields:

```csharp
public class CreateInvoiceValidator : AbstractValidator<CreateInvoiceCommand>
{
    public CreateInvoiceValidator()
    {
        RuleFor(c => c.Amount)
            .GreaterThan(0);

        RuleFor(c => c.DueDate)
            .GreaterThanOrEqualTo(c => c.IssueDate)
            .WithMessage("Due date must be on or after the issue date");

        RuleFor(c => c.DiscountAmount)
            .LessThanOrEqualTo(c => c.Amount)
            .WithMessage("Discount cannot exceed the invoice amount")
            .When(c => c.DiscountAmount.HasValue);

        // Complex cross-field validation
        RuleFor(c => c)
            .Must(HaveValidPaymentTerms)
            .WithMessage("Payment terms are invalid for the specified due date")
            .When(c => c.PaymentTerms != PaymentTerms.Custom);
    }

    private bool HaveValidPaymentTerms(CreateInvoiceCommand command)
    {
        return command.PaymentTerms switch
        {
            PaymentTerms.Net30 => command.DueDate <= command.IssueDate.AddDays(30),
            PaymentTerms.Net60 => command.DueDate <= command.IssueDate.AddDays(60),
            PaymentTerms.Immediate => command.DueDate <= command.IssueDate.AddDays(1),
            _ => true
        };
    }
}
```

## Conditional Validation

Use conditional validation for different scenarios:

```csharp
public class UpdateUserValidator : AbstractValidator<UpdateUserCommand>
{
    public UpdateUserValidator()
    {
        RuleFor(c => c.Id).NotEmpty();
        RuleFor(c => c.TenantId).NotEmpty();

        // Only validate email if it's being changed
        When(c => !string.IsNullOrEmpty(c.Email), () =>
        {
            RuleFor(c => c.Email)
                .EmailAddress()
                .MaximumLength(255);
        });

        // Validate password only for active users
        When(c => c.IsActive, () =>
        {
            RuleFor(c => c.Password)
                .NotEmpty()
                .MinimumLength(8)
                .When(c => !string.IsNullOrEmpty(c.Password));
        });

        // Admin-specific validation
        When(c => c.Role == UserRole.Admin, () =>
        {
            RuleFor(c => c.Permissions)
                .NotEmpty()
                .WithMessage("Admin users must have at least one permission");
        });
    }
}
```

## Validation Rule Sets

Use rule sets for different validation scenarios:

```csharp
public class UserValidator : AbstractValidator<User>
{
    public UserValidator()
    {
        // Default rules (always applied)
        RuleFor(u => u.Id).NotEmpty();

        // Rules for creation
        RuleSet("Create", () =>
        {
            RuleFor(u => u.Email)
                .NotEmpty()
                .EmailAddress();
                
            RuleFor(u => u.Password)
                .NotEmpty()
                .MinimumLength(8);
        });

        // Rules for updates
        RuleSet("Update", () =>
        {
            RuleFor(u => u.Email)
                .EmailAddress()
                .When(u => !string.IsNullOrEmpty(u.Email));
                
            RuleFor(u => u.Password)
                .MinimumLength(8)
                .When(u => !string.IsNullOrEmpty(u.Password));
        });

        // Rules for admin operations
        RuleSet("Admin", () =>
        {
            RuleFor(u => u.Role)
                .NotNull()
                .Must(r => r != UserRole.SuperAdmin)
                .WithMessage("Cannot create or modify super admin users");
        });
    }
}

// Usage in handler
public static async Task<(Result<User>, UserCreated?)> Handle(
    CreateUserCommand command, 
    IValidator<CreateUserCommand> validator,
    IMessageBus messaging,
    CancellationToken cancellationToken)
{
    // Validate using specific rule set
    var validationResult = await validator.ValidateAsync(command, options =>
    {
        options.IncludeRuleSets("Create");
    }, cancellationToken);

    if (!validationResult.IsValid)
    {
        return (validationResult.Errors.ToResult<User>(), null);
    }

    // Continue with handler logic...
}
```

## Child Object Validation

Validate nested objects and collections:

```csharp
public record CreateOrderCommand(
    Guid TenantId,
    Guid CustomerId,
    List<OrderItem> Items,
    Address ShippingAddress
) : ICommand<Result<Order>>;

public class CreateOrderValidator : AbstractValidator<CreateOrderCommand>
{
    public CreateOrderValidator()
    {
        RuleFor(c => c.TenantId).NotEmpty();
        RuleFor(c => c.CustomerId).NotEmpty();

        RuleFor(c => c.Items)
            .NotEmpty()
            .WithMessage("Order must contain at least one item");

        RuleForEach(c => c.Items)
            .SetValidator(new OrderItemValidator());

        RuleFor(c => c.ShippingAddress)
            .NotNull()
            .SetValidator(new AddressValidator());
    }
}

public class OrderItemValidator : AbstractValidator<OrderItem>
{
    public OrderItemValidator()
    {
        RuleFor(i => i.ProductId).NotEmpty();
        RuleFor(i => i.Quantity).GreaterThan(0);
        RuleFor(i => i.Price).GreaterThan(0);
    }
}

public class AddressValidator : AbstractValidator<Address>
{
    public AddressValidator()
    {
        RuleFor(a => a.Street).NotEmpty().MaximumLength(200);
        RuleFor(a => a.City).NotEmpty().MaximumLength(100);
        RuleFor(a => a.PostalCode).NotEmpty().MaximumLength(20);
        RuleFor(a => a.Country).NotEmpty().MaximumLength(50);
    }
}
```

## Error Messages and Localization

### Custom Error Messages

```csharp
public class CreateCashierValidator : AbstractValidator<CreateCashierCommand>
{
    public CreateCashierValidator()
    {
        RuleFor(c => c.Name)
            .NotEmpty()
            .WithMessage("Please enter the cashier's name")
            .MinimumLength(2)
            .WithMessage("The cashier's name must be at least 2 characters long")
            .MaximumLength(100)
            .WithMessage("The cashier's name cannot be longer than 100 characters");

        RuleFor(c => c.Email)
            .NotEmpty()
            .WithMessage("Please enter an email address")
            .EmailAddress()
            .WithMessage("Please enter a valid email address");
    }
}
```

### Message Placeholders

```csharp
RuleFor(c => c.Name)
    .Length(2, 100)
    .WithMessage("Name must be between {MinLength} and {MaxLength} characters. You entered {TotalLength} characters.");

RuleFor(c => c.Age)
    .InclusiveBetween(18, 65)
    .WithMessage("{PropertyName} must be between {From} and {To}. You entered {PropertyValue}.");
```

### Localized Messages

```csharp
public class CreateCashierValidator : AbstractValidator<CreateCashierCommand>
{
    public CreateCashierValidator(IStringLocalizer<CreateCashierValidator> localizer)
    {
        RuleFor(c => c.Name)
            .NotEmpty()
            .WithMessage(localizer["NameRequired"])
            .MinimumLength(2)
            .WithMessage(localizer["NameTooShort"]);
    }
}
```

## Testing Validators

### Unit Testing Validators

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
    public void Should_Have_Error_When_Name_Is_Empty()
    {
        // Arrange
        var command = new CreateCashierCommand(Guid.NewGuid(), "", "test@example.com");

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(c => c.Name)
            .WithErrorMessage("Name is required");
    }

    [Test]
    public void Should_Have_Error_When_Email_Is_Invalid()
    {
        // Arrange
        var command = new CreateCashierCommand(Guid.NewGuid(), "John Doe", "invalid-email");

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(c => c.Email);
    }

    [Test]
    public void Should_Not_Have_Error_When_Command_Is_Valid()
    {
        // Arrange
        var command = new CreateCashierCommand(Guid.NewGuid(), "John Doe", "john@example.com");

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }
}
```

### Testing Async Validators

```csharp
[Test]
public async Task Should_Have_Error_When_Email_Already_Exists()
{
    // Arrange
    var mockUserService = new Mock<IUserService>();
    mockUserService
        .Setup(s => s.IsEmailUniqueAsync("existing@example.com", It.IsAny<CancellationToken>()))
        .ReturnsAsync(false);

    var validator = new CreateCashierValidator(mockUserService.Object);
    var command = new CreateCashierCommand(Guid.NewGuid(), "John Doe", "existing@example.com");

    // Act
    var result = await validator.TestValidateAsync(command);

    // Assert
    result.ShouldHaveValidationErrorFor(c => c.Email)
        .WithErrorMessage("Email address is already in use");
}
```

## Integration with Handlers

Validation is automatically executed before handlers run. If validation fails, the handler is not executed:

```csharp
// This validation happens automatically
public static async Task<(Result<Cashier>, CashierCreated?)> Handle(
    CreateCashierCommand command, 
    IMessageBus messaging,
    CancellationToken cancellationToken)
{
    // Handler only executes if validation passes
    // If validation fails, this code never runs
    
    var dbCommand = CreateInsertCommand(command);
    var insertedCashier = await messaging.InvokeCommandAsync(dbCommand, cancellationToken);
    
    var result = insertedCashier.ToModel();
    var createdEvent = new CashierCreated(result.TenantId, PartitionKeyTest: 0, result);
    
    return (result, createdEvent);
}
```

## Best Practices

### Validator Design

1. **Keep It Simple**: Validators should focus on input validation, not business logic
2. **Fast Validation**: Avoid expensive operations in validators
3. **Clear Messages**: Provide helpful error messages for users
4. **Use Async Sparingly**: Only use async validation when absolutely necessary

### Performance

1. **Avoid Database Calls**: Minimize database access in validators
2. **Cache Lookups**: Cache frequently accessed validation data
3. **Early Exit**: Order rules so expensive checks come last
4. **Use When Clauses**: Skip unnecessary validation with conditional rules

### Error Handling

1. **User-Friendly Messages**: Write error messages for end users, not developers
2. **Localization**: Support multiple languages if needed
3. **Consistent Formatting**: Use consistent error message formats
4. **Property Names**: Use clear property names in error messages

### Testing

1. **Test All Rules**: Ensure every validation rule is tested
2. **Test Edge Cases**: Include boundary conditions and special cases
3. **Test Error Messages**: Verify error messages are correct
4. **Mock Dependencies**: Use mocks for external dependencies in async validators

## Next Steps

- Learn about [Commands](./commands) and their handlers
- Understand [Queries](./queries) patterns
- Explore [Handlers](./handlers) architecture
- See [Error Handling](../error-handling) patterns