---
title: First Contribution Guide
description: Step-by-step guide for making your first contribution to the AppDomain Solution
date: 2025-01-07
---

# First Contribution Guide

Welcome to the AppDomain Solution! This guide will walk you through making your first contribution, from setting up your development environment to submitting your first pull request.

## Prerequisites

- Complete [Development Environment Setup](/guide/dev-setup)
- Basic understanding of Git and GitHub workflows
- Familiarity with C# and .NET development
- Understanding of the [AppDomain Solution architecture](/arch/)

## Before You Start

### Understanding the Project

The AppDomain Solution is a microservices-based system that follows these key principles:

- **Real-world mirroring**: Code structure reflects actual business operations
- **Event-driven architecture**: Services communicate through integration events
- **CQRS patterns**: Separate commands (writes) from queries (reads)
- **Domain-driven design**: Business logic organized around domain concepts

### Code of Conduct

Please review our code of conduct and contribution guidelines before making your first contribution. We value:

- **Respectful communication** in all interactions
- **Constructive feedback** during code reviews
- **Collaborative problem-solving** when facing challenges
- **Knowledge sharing** to help others learn and grow

## Finding Your First Issue

### Good First Issues

Look for issues labeled with:

- `good-first-issue`: Perfect for newcomers
- `documentation`: Help improve our docs
- `bug`: Fix small, well-defined problems
- `enhancement`: Add minor features or improvements

### Issue Categories

**Documentation Improvements**:
- Fix typos or grammar
- Add missing code examples
- Improve existing explanations
- Create new how-to guides

**Code Improvements**:
- Add unit tests for existing functionality
- Improve error messages
- Add validation to existing endpoints
- Refactor small code sections

**Bug Fixes**:
- Fix failing tests
- Resolve configuration issues
- Correct API response formats
- Address logging problems

## Setting Up Your Contribution

### Fork and Clone

```bash
# Fork the repository on GitHub (click Fork button)

# Clone your fork locally
git clone https://github.com/your-username/momentum.git
cd momentum

# Add upstream remote
git remote add upstream https://github.com/original-owner/momentum.git

# Verify remotes
git remote -v
```

### Create a Feature Branch

```bash
# Sync with latest changes
git checkout main
git pull upstream main

# Create your feature branch
git checkout -b feature/your-feature-name

# Example branch names:
# - fix/cashier-validation-error
# - docs/improve-debugging-guide
# - feature/add-invoice-filtering
```

### Verify Your Setup

```bash
# Ensure everything builds
dotnet build

# Run tests to verify nothing is broken
dotnet test

# Start the application
dotnet run --project src/AppDomain.AppHost
```

## Making Your Changes

### Example: Adding a New Validation Rule

Let's walk through adding a validation rule to the `CreateCashierCommand`:

**1. Identify the Component**:

```csharp
// Location: src/AppDomain/Cashiers/Commands/CreateCashierValidator.cs
public class CreateCashierValidator : AbstractValidator<CreateCashierCommand>
{
    public CreateCashierValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Cashier name is required");

        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress()
            .WithMessage("Valid email address is required");
    }
}
```

**2. Add Your Validation**:

```csharp
public class CreateCashierValidator : AbstractValidator<CreateCashierCommand>
{
    public CreateCashierValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Cashier name is required")
            .MaximumLength(100) // [!code ++]
            .WithMessage("Cashier name cannot exceed 100 characters"); // [!code ++]

        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress()
            .WithMessage("Valid email address is required");
    }
}
```

**3. Add a Unit Test**:

```csharp
// Location: tests/AppDomain.Tests/Unit/Cashiers/Commands/CreateCashierValidatorTests.cs
[Test]
public void Should_Fail_When_Name_Exceeds_Maximum_Length()
{
    // Arrange
    var validator = new CreateCashierValidator();
    var command = new CreateCashierCommand
    {
        Name = new string('A', 101), // Exceeds 100 character limit
        Email = "valid@email.com"
    };

    // Act
    var result = validator.Validate(command);

    // Assert
    result.IsValid.Should().BeFalse();
    result.Errors.Should().Contain(e => 
        e.PropertyName == nameof(CreateCashierCommand.Name) &&
        e.ErrorMessage.Contains("cannot exceed 100 characters"));
}
```

**4. Run Tests to Verify**:

```bash
# Run the specific test
dotnet test --filter "CreateCashierValidatorTests"

# Run all cashier tests
dotnet test --filter "Cashiers"
```

### Example: Adding Documentation

**1. Improve an Existing Guide**:

```markdown
<!-- Before -->
## Database Setup

Start the database with Docker:

```bash
docker compose up AppDomain-db -d
```

<!-- After -->
## Database Setup

Start the database with Docker Compose:

```bash
# Start PostgreSQL database
docker compose up AppDomain-db -d

# Verify database is running
docker compose ps AppDomain-db

# Apply database migrations
docker compose up AppDomain-db-migrations
```

**Connection Details**:
- **Host**: `localhost`
- **Port**: `54320` 
- **Database**: `AppDomain`
- **Username**: `postgres`
- **Password**: `password`
```

**2. Test Documentation Changes**:

```bash
cd docs
pnpm dev

# Verify your changes at http://localhost:5173
# Check for broken links or formatting issues
```

## Testing Your Changes

### Running Tests

**Unit Tests**:

```bash
# Run all unit tests
dotnet test --filter "Category=Unit"

# Run tests for specific domain
dotnet test --filter "Cashiers&Category=Unit"

# Run with coverage
dotnet test --collect:"XPlat Code Coverage"
```

**Integration Tests**:

```bash
# Ensure Docker is running first
docker --version

# Run integration tests
dotnet test --filter "Category=Integration"

# Run specific integration test
dotnet test --filter "CashierApiTests"
```

### Testing Your Feature

**Manual Testing**:

```bash
# Start the application
dotnet run --project src/AppDomain.AppHost

# Test your changes using:
# 1. Swagger UI at http://localhost:8101/scalar
# 2. REST client (VS Code extension)
# 3. Postman or similar tool
```

**Example REST Client Test**:

```http
### Create Cashier with Long Name (should fail)
POST http://localhost:8101/api/cashiers
Content-Type: application/json

{
  "name": "This is a very long cashier name that exceeds the maximum allowed length of 100 characters and should fail validation",
  "email": "test@example.com"
}
```

### Code Quality Checks

```bash
# Format code
dotnet format

# Analyze code
dotnet build --verbosity normal

# Check for security vulnerabilities
dotnet list package --vulnerable
```

## Committing Your Changes

### Commit Message Guidelines

Follow conventional commit format:

```bash
# Feature commits
git commit -m "feat(cashiers): add name length validation to CreateCashierCommand"

# Bug fixes
git commit -m "fix(invoices): resolve null reference in payment processing"

# Documentation
git commit -m "docs(guide): improve database setup instructions"

# Tests
git commit -m "test(cashiers): add validation tests for CreateCashierCommand"
```

**Commit Message Structure**:

```
<type>(<scope>): <description>

[optional body]

[optional footer(s)]
```

**Types**: `feat`, `fix`, `docs`, `style`, `refactor`, `test`, `chore`
**Scopes**: `cashiers`, `invoices`, `bills`, `api`, `docs`, `tests`

### Making Your Commits

```bash
# Stage your changes
git add .

# Commit with descriptive message
git commit -m "feat(cashiers): add name length validation

- Add MaximumLength(100) validation rule
- Include appropriate error message
- Add unit test for validation behavior
- Update documentation with validation details"

# Push to your fork
git push origin feature/your-feature-name
```

## Submitting Your Pull Request

### Creating the Pull Request

1. **Push Your Branch**:

```bash
git push origin feature/your-feature-name
```

2. **Open Pull Request on GitHub**:
   - Navigate to your fork on GitHub
   - Click **Compare & pull request**
   - Fill out the PR template

### Pull Request Template

```markdown
## Description

Brief description of your changes and why they're needed.

## Type of Change

- [ ] Bug fix (non-breaking change that fixes an issue)
- [ ] New feature (non-breaking change that adds functionality)
- [ ] Breaking change (fix or feature that would cause existing functionality to change)
- [ ] Documentation update

## Testing

- [ ] Unit tests added/updated
- [ ] Integration tests added/updated
- [ ] Manual testing completed
- [ ] All existing tests pass

## Checklist

- [ ] Code follows project style guidelines
- [ ] Self-review completed
- [ ] Code is properly documented
- [ ] Changes generate no new warnings
- [ ] Tests prove the fix/feature works
```

### Example Pull Request

**Title**: `feat(cashiers): add name length validation to CreateCashierCommand`

**Description**:
```markdown
## Description

Adds maximum length validation (100 characters) to the cashier name field in `CreateCashierCommand`. This prevents database errors and provides clear feedback to users when names are too long.

## Changes Made

- Added `MaximumLength(100)` validation rule to `CreateCashierValidator`
- Added appropriate error message for length validation
- Created unit test `Should_Fail_When_Name_Exceeds_Maximum_Length`
- Updated API documentation with validation details

## Testing

- [x] Unit tests added and passing
- [x] Manual testing with Swagger UI
- [x] All existing tests still pass
- [x] Validation error message displays correctly

## Related Issues

Closes #123 - Add validation for cashier name length
```

## Code Review Process

### What to Expect

1. **Automated Checks**: CI/CD pipeline runs tests and code analysis
2. **Peer Review**: Team members review your code and provide feedback
3. **Iterations**: You may need to make changes based on feedback
4. **Approval**: Once approved, a maintainer will merge your PR

### Responding to Feedback

**Making Changes**:

```bash
# Make requested changes
# Commit the changes
git add .
git commit -m "fix: address code review feedback"

# Push to update the PR
git push origin feature/your-feature-name
```

**Common Review Comments**:

- **Code Style**: Follow existing patterns and conventions
- **Test Coverage**: Add tests for new functionality
- **Documentation**: Update docs for public APIs
- **Performance**: Consider impact of changes
- **Security**: Validate inputs and handle errors properly

## After Your PR is Merged

### Cleanup

```bash
# Switch back to main
git checkout main

# Pull latest changes
git pull upstream main

# Delete your feature branch
git branch -d feature/your-feature-name

# Delete remote branch
git push origin --delete feature/your-feature-name
```

### Next Steps

**Continue Contributing**:

- Look for more `good-first-issue` labels
- Help review other contributors' PRs
- Suggest improvements or new features
- Share your experience with other newcomers

**Growing Your Expertise**:

- Learn more about the architecture patterns used
- Contribute to more complex features
- Help improve testing strategies
- Contribute to documentation and guides

## Common Pitfalls and Solutions

### Build Failures

**NuGet Package Issues**:

```bash
dotnet clean
dotnet restore --force
dotnet build
```

**Test Failures**:

```bash
# Run specific failing test with verbose output
dotnet test --filter "TestName" --logger "console;verbosity=detailed"
```

### Git Issues

**Merge Conflicts**:

```bash
# Sync with latest main
git checkout main
git pull upstream main

# Rebase your feature branch
git checkout feature/your-feature-name
git rebase main

# Resolve conflicts in your editor
# Then continue rebase
git rebase --continue
```

**Wrong Branch**:

```bash
# Move commits to correct branch
git stash
git checkout correct-branch-name
git stash pop
```

## Getting Help

### Resources

- **Project Documentation**: Start with [Architecture Overview](/arch/)
- **Development Setup**: Review [Dev Setup Guide](/guide/dev-setup)
- **Debugging Help**: Check [Debugging Tips](/guide/debugging)

### Community Support

- **GitHub Discussions**: Ask questions and share ideas
- **Code Reviews**: Learn from feedback on your PRs
- **Team Chat**: Get real-time help from maintainers
- **Pair Programming**: Work with experienced contributors

### Best Practices for Getting Help

1. **Search First**: Check existing issues and documentation
2. **Be Specific**: Provide error messages, code snippets, and steps to reproduce
3. **Show Your Work**: Describe what you've already tried
4. **Be Patient**: Maintainers are volunteers with other responsibilities

## Conclusion

Making your first contribution is an important step in becoming part of the AppDomain Solution community. Remember:

- **Start Small**: Simple contributions are valuable and help you learn
- **Ask Questions**: The community is here to help you succeed
- **Be Patient**: Learning takes time, and everyone started as a beginner
- **Have Fun**: Contributing to open source should be enjoyable and rewarding

Welcome to the team! We're excited to see what you'll contribute to the AppDomain Solution.

## Related Resources

- [Development Environment Setup](/guide/dev-setup)
- [Debugging Tips](/guide/debugging)
- [Architecture Overview](/arch/)
- [Testing Strategies](/arch/testing)
- [GitHub Flow Guide](https://docs.github.com/en/get-started/quickstart/github-flow)