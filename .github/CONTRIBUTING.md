# Contributing to Momentum .NET

Thank you for your interest in contributing! Momentum .NET is a dual-purpose project — a **dotnet template** (`mmt`) that generates production-ready microservices solutions, and a set of **Momentum NuGet libraries** used by those solutions.

## Table of Contents

- [Project Structure](#project-structure)
- [Prerequisites](#prerequisites)
- [Development Setup](#development-setup)
- [Types of Contributions](#types-of-contributions)
- [Coding Conventions](#coding-conventions)
- [Testing Requirements](#testing-requirements)
- [Commit Messages](#commit-messages)
- [Submitting a Pull Request](#submitting-a-pull-request)
- [Getting Help](#getting-help)

---

## Project Structure

```
momentum/
├── src/                         # Sample AppDomain application (template source)
├── libs/Momentum/               # Standalone Momentum NuGet libraries
│   ├── src/
│   │   ├── Momentum.Extensions
│   │   ├── Momentum.ServiceDefaults
│   │   ├── Momentum.ServiceDefaults.Api
│   │   ├── Momentum.Extensions.SourceGenerators
│   │   └── Momentum.Extensions.Messaging.Kafka
│   └── tests/
├── tests/                       # AppDomain integration, E2E, and architecture tests
├── infra/                       # Liquibase database migrations
├── docs/                        # VitePress documentation site
└── .template.config/            # dotnet new template configuration
```

Changes to **`libs/Momentum/`** affect the libraries published to NuGet.  
Changes to everything else (especially `.template.config/` and `src/`) affect the `mmt` template.

---

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/) — required for integration tests
- [Bun](https://bun.sh/) — required for the documentation site
- Git

---

## Development Setup

### Working on the Template

```bash
# Install the template locally
dotnet new install ./ --force

# Generate a test solution using the installed template
dotnet new mmt -n TestService --allow-scripts yes

# Generate with locally built Momentum packages
dotnet new mmt -n TestService --allow-scripts yes --local

# Run the full template test suite
./scripts/Run-TemplateTests.ps1

# Run a specific test category
./scripts/Run-TemplateTests.ps1 -Category component-isolation
```

Available test categories: `component-isolation`, `database-config`, `port-config`, `org-names`, `library-config`, `real-world-patterns`, `orleans-combinations`, `edge-cases`

### Working on the Libraries

```bash
# Build all Momentum libraries
cd libs/Momentum
dotnet build Momentum.slnx

# Run library tests
dotnet test Momentum.slnx

# Pack libraries to the local NuGet feed (enables --local template testing)
dotnet pack src/<LibraryName> --configuration Release
```

### Running the Sample Application

```bash
# Start infrastructure (PostgreSQL + Kafka)
docker-compose --profile db --profile messaging up -d

# Start the Aspire-orchestrated application
dotnet run --project src/AppDomain.AppHost
# API:            https://localhost:8101 (REST), :8102 (gRPC)
# Aspire Dashboard: https://localhost:18110

# Start the documentation site
cd docs && bun install && bun run dev
# Documentation: http://localhost:8119
```

---

## Types of Contributions

### Bug Fixes

- Open an issue using the appropriate bug report template before submitting a fix for a non-trivial bug.
- Include a failing test that reproduces the issue when possible.

### New Features

- Open a feature request issue to discuss the change before implementing it.
- Keep changes focused — one feature per PR.

### Documentation

- VitePress docs live in `docs/`. Run `bun run dev` inside `docs/` to preview locally.
- Correct typos, improve clarity, and add missing examples.

### Library Contributions

- New library functionality belongs in `libs/Momentum/`.
- Public APIs must include XML doc comments.
- Use source generators where appropriate (see `Momentum.Extensions.SourceGenerators`).

---

## Coding Conventions

- **Language**: C# 13 / .NET 10 with nullable reference types enabled.
- **Style**: Enforced by `.editorconfig` and `AppDomain.ruleset`. Run `dotnet format` before committing.
- **Architecture**: Follow Domain-Oriented Vertical Slice (CQRS + Event-Driven). Keep commands, queries, and events in their respective domain folders.
- **Database migrations**: Each table in its own file under `tables/`. Constraints, indexes, and triggers stay in the same file as the table — never in separate constraint files.
- **Events**: Use CloudEvents format with the `[EventTopic(...)]` attribute.

---

## Testing Requirements

All PRs must maintain or improve test coverage. Run the full suite before opening a PR:

```bash
# Unit and architecture tests
dotnet test

# Integration tests (requires Docker)
dotnet test tests/AppDomain.Tests/Integration/

# End-to-end tests
dotnet test tests/AppDomain.Tests.E2E/

# Template validation (runs all template combinations)
./scripts/Run-TemplateTests.ps1
```

For library changes, also run:

```bash
dotnet test libs/Momentum/
```

---

## Commit Messages

Follow the [Conventional Commits](https://www.conventionalcommits.org/) format:

```
<type>(<scope>): <short description>

[optional body]

[optional footer(s)]
```

**Types**: `feat`, `fix`, `docs`, `refactor`, `test`, `chore`, `perf`

**Scopes**: match the changed area — e.g., `template`, `extensions`, `service-defaults`, `kafka`, `generators`, `docs`, `ci`

**Examples**:

```bash
feat(template): add --bff flag for BFF component generation
fix(extensions): resolve null reference in Result<T>.Map when value is empty
docs(guide): add troubleshooting section for Kafka connection issues
test(generators): add snapshot tests for DbCommand source generator
chore(ci): upgrade SonarQube action to v4
```

---

## Submitting a Pull Request

1. Fork the repository and create a branch from `main`:
   ```bash
   git checkout -b fix/your-fix-name
   ```

2. Make your changes and ensure all tests pass.

3. Run `dotnet format` to enforce code style.

4. Push your branch and open a PR against `main`. Fill out the PR template.

5. The CI pipeline will run:
   - Build and test (`dotnet build`, `dotnet test`)
   - SonarCloud quality analysis
   - Template validation (on template-related changes)

6. Address any review feedback. Push additional commits to the same branch — do not force-push after a review has started.

7. Once approved, a maintainer will squash-merge your PR.

---

## Getting Help

- **Docs**: [Architecture overview](../docs/arch/), [Development setup](../docs/guide/dev-setup.md), [First contribution guide](../docs/guide/first-contribution.md)
- **Issues**: Search existing issues before opening a new one.
- **Security vulnerabilities**: See [SECURITY.md](SECURITY.md) — do not open a public issue.
- **Code of Conduct**: This project follows the [Contributor Covenant](CODE_OF_CONDUCT.md).
