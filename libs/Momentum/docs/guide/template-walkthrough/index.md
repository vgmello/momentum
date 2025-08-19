---
title: Template Walkthrough
description: Step-by-step guide through creating and exploring a Momentum application, from template generation to deployment.
date: 2024-01-15
---

# Template Walkthrough

Step-by-step guide through creating and exploring a Momentum application, from template generation to deployment.

## Overview

This walkthrough demonstrates how to create a complete Momentum application, exploring the generated structure, understanding the patterns, and extending the application with new features.

## Prerequisites

- .NET 9.0 SDK
- Docker and Docker Compose
- Node.js and pnpm (for documentation)
- Visual Studio 2022 or VS Code

## Step 1: Template Installation

```bash
# Install the Momentum template
dotnet new install Momentum.Template

# Verify installation
dotnet new list | grep -i momentum
```

## Step 2: Generate Your First Application

```bash
# Create a new application
dotnet new mmt -n "BookStore" --org "Acme Corp" --port 8200

# Navigate to the generated solution
cd BookStore

# Explore the structure
ls -la
```

## Step 3: Explore the Generated Structure

### Solution Overview
```
BookStore/
├── src/
│   ├── BookStore.Api/              # REST/gRPC API endpoints
│   ├── BookStore.BackOffice/       # Background processing
│   ├── BookStore.AppHost/          # .NET Aspire orchestration
│   ├── BookStore/                  # Core domain logic
│   └── BookStore.Contracts/        # Integration events
├── tests/
│   └── BookStore.Tests/            # Comprehensive testing
├── infra/
│   └── BookStore.Database/         # Liquibase migrations
├── docs/                           # VitePress documentation
└── BookStore.sln                  # Solution file
```

### Key Files to Examine
- `src/BookStore/Cashiers/` - Sample domain implementation
- `src/BookStore.Api/Cashiers/` - API endpoints
- `src/BookStore.Contracts/` - Integration events
- `infra/BookStore.Database/` - Database schema

## Step 4: Build and Run

```bash
# Build the entire solution
dotnet build

# Run with .NET Aspire (recommended)
dotnet run --project src/BookStore.AppHost

# Or run individual services
dotnet run --project src/BookStore.Api
```

### Access Points
- **API**: https://localhost:8211 (or your configured port)
- **Aspire Dashboard**: https://localhost:18210
- **Documentation**: https://localhost:8219

## Step 5: Explore the Sample Domain

### Cashier Domain
The template includes a sample "Cashiers" domain demonstrating:

#### Commands
```csharp
[DbCommand(sp: "create_cashier")]
public record CreateCashier(string Name, string Email) : ICommand<Guid>;
```

#### Queries
```csharp
[DbCommand(sp: "get_cashier_by_id")]
public record GetCashierById(Guid CashierId) : IQuery<Cashier?>;
```

#### Events
```csharp
[EventTopic("book_store.cashiers.cashier-created")]
public record CashierCreated(Guid CashierId, string Name, string Email);
```

### API Endpoints
```csharp
app.MapPost("/cashiers", async (CreateCashier command, IMediator mediator) =>
{
    var cashierId = await mediator.Send(command);
    return Results.Created($"/cashiers/{cashierId}", new { CashierId = cashierId });
});
```

## Step 6: Add Your Own Domain

### Create a Book Domain

1. **Add Domain Folder**
```bash
mkdir -p src/BookStore/Books/{Commands,Queries,Data}
```

2. **Define Book Entity**
```csharp
// src/BookStore/Books/Book.cs
public record Book(
    Guid BookId,
    string Title,
    string Author,
    string ISBN,
    decimal Price,
    int StockQuantity
);
```

3. **Add Commands**
```csharp
// src/BookStore/Books/Commands/CreateBook.cs
[DbCommand(sp: "create_book")]
public record CreateBook(
    string Title,
    string Author,
    string ISBN,
    decimal Price,
    int StockQuantity
) : ICommand<Guid>;
```

4. **Add Queries**
```csharp
// src/BookStore/Books/Queries/GetBookById.cs
[DbCommand(sp: "get_book_by_id")]
public record GetBookById(Guid BookId) : IQuery<Book?>;
```

5. **Add Database Migration**
```sql
-- infra/BookStore.Database/changsets/003-books.sql
CREATE TABLE books (
    book_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    title VARCHAR(255) NOT NULL,
    author VARCHAR(255) NOT NULL,
    isbn VARCHAR(13) UNIQUE NOT NULL,
    price DECIMAL(10,2) NOT NULL,
    stock_quantity INTEGER NOT NULL DEFAULT 0,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);
```

6. **Add API Endpoints**
```csharp
// src/BookStore.Api/Books/BooksEndpoints.cs
public static class BooksEndpoints
{
    public static void MapBooksEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/books").WithTags("Books");

        group.MapPost("/", CreateBook);
        group.MapGet("/{id:guid}", GetBook);
    }

    private static async Task<IResult> CreateBook(
        CreateBook command,
        IMediator mediator)
    {
        var bookId = await mediator.Send(command);
        return Results.Created($"/books/{bookId}", new { BookId = bookId });
    }

    private static async Task<IResult> GetBook(
        Guid id,
        IMediator mediator)
    {
        var book = await mediator.Send(new GetBookById(id));
        return book is not null ? Results.Ok(book) : Results.NotFound();
    }
}
```

## Step 7: Test Your Changes

### Run Tests
```bash
# Run all tests
dotnet test

# Run specific test category
dotnet test --filter "Category=Unit"
dotnet test --filter "Category=Integration"
```

### Manual Testing
1. Start the application
2. Navigate to the Swagger UI
3. Test your new Book endpoints
4. Verify database changes

## Step 8: Explore Advanced Features

### Add Integration Events
```csharp
[EventTopic("book_store.books.book-created")]
public record BookCreated(Guid BookId, string Title, string Author);
```

### Add Background Processing
```csharp
public class BookCreatedHandler : IEventHandler<BookCreated>
{
    public Task Handle(BookCreated @event, CancellationToken cancellationToken)
    {
        // Update search index, send notifications, etc.
        return Task.CompletedTask;
    }
}
```

### Add Validation
```csharp
public class CreateBookValidator : AbstractValidator<CreateBook>
{
    public CreateBookValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(255);
        RuleFor(x => x.ISBN).NotEmpty().Length(13);
        RuleFor(x => x.Price).GreaterThan(0);
    }
}
```

## Step 9: Documentation

### Generate API Documentation
The OpenAPI documentation is automatically generated and available at:
- Swagger UI: `/swagger`
- OpenAPI Spec: `/swagger/v1/swagger.json`

### Update Project Documentation
```bash
# Navigate to docs directory
cd docs

# Install dependencies
pnpm install

# Start development server
pnpm dev
```

## Step 10: Deployment Preparation

### Docker Support
```bash
# Build container images
docker compose build

# Run the full stack
docker compose up
```

### Production Configuration
1. Update connection strings for production databases
2. Configure external Kafka brokers
3. Set up authentication and authorization
4. Configure logging and monitoring
5. Set up health checks

## Next Steps

### Extend the Application
- Add more domains (Authors, Orders, Inventory)
- Implement complex business workflows
- Add authentication and authorization
- Integrate with external services

### Explore Advanced Patterns
- Event sourcing with Orleans
- Saga patterns for distributed transactions
- Advanced query patterns with CQRS
- Performance optimization techniques

### Production Readiness
- Set up CI/CD pipelines
- Implement monitoring and alerting
- Add security hardening
- Plan for scalability and high availability

## Common Patterns Demonstrated

- **Domain-Driven Design**: Clear domain boundaries and ubiquitous language
- **CQRS**: Separation of read and write operations
- **Event-Driven Architecture**: Loose coupling through events
- **Source Generation**: Compile-time code generation for performance
- **Clean Architecture**: Dependency inversion and testability

## Troubleshooting

### Common Issues
- **Port conflicts**: Ensure configured ports are available
- **Database connection**: Verify PostgreSQL is running
- **Missing dependencies**: Run `dotnet restore`
- **Build errors**: Check .NET 9.0 SDK installation

### Getting Help
- Check the documentation
- Review sample implementations
- Examine generated code
- Use debugging tools and logging

## Related Topics

- [Template Options](../template-options/index.md)
- [Adding Domains](../adding-domains/index.md)
- [CQRS](../cqrs/index.md)
- [Testing](../testing/index.md)
- [Deployment](../deployment/index.md)