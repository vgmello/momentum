---
title: Development Environment Setup
description: Complete guide to setting up your development environment for AppDomain Solution
date: 2025-01-07
---

# Development Environment Setup

This guide walks you through setting up a complete development environment for the AppDomain Solution, including all required tools, dependencies, and configuration.

## Prerequisites

Before starting, ensure you have administrative privileges on your development machine and a stable internet connection for downloading dependencies.

## Required Software

### .NET Development

**.NET 10 SDK**:

```bash
# Windows (using winget)
winget install Microsoft.DotNet.SDK.9

# macOS (using Homebrew)
brew install --cask dotnet

# Linux (Ubuntu/Debian)
wget https://packages.microsoft.com/config/ubuntu/20.04/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
sudo dpkg -i packages-microsoft-prod.deb
sudo apt-get update
sudo apt-get install -y dotnet-sdk-10.0
```

**Verify Installation**:

```bash
dotnet --version
# Expected output: 10.0.x
```

### Container Platform

**Docker Desktop**:

- **Windows**: Download from [Docker Desktop for Windows](https://desktop.docker.com/win/main/amd64/Docker%20Desktop%20Installer.exe)
- **macOS**: Download from [Docker Desktop for Mac](https://desktop.docker.com/mac/main/amd64/Docker.dmg)
- **Linux**: Follow [Docker Engine installation guide](https://docs.docker.com/engine/install/)

**Verify Installation**:

```bash
docker --version
docker-compose --version

# Test Docker functionality
docker run hello-world
```

### Bun Runtime

**Bun (for documentation)**:

```bash
# macOS/Linux (recommended)
curl -fsSL https://bun.sh/install | bash

# Windows (using PowerShell)
powershell -c "irm bun.sh/install.ps1 | iex"

# Verify installation
bun --version
```

## Development Tools

### Code Editor

**Visual Studio Code** (Recommended):

```bash
# Windows
winget install Microsoft.VisualStudioCode

# macOS
brew install --cask visual-studio-code

# Linux
sudo snap install code --classic
```

**Required VS Code Extensions**:

```json
{
  "recommendations": [
    "ms-dotnettools.csdevkit",
    "ms-dotnettools.csharp",
    "ms-vscode.vscode-json",
    "bradlc.vscode-tailwindcss",
    "ms-vscode.rest-client",
    "ms-azuretools.vscode-docker",
    "ckolkman.vscode-postgres",
    "ms-dotnettools.aspire"
  ]
}
```

**Alternative: Visual Studio**:

- **Visual Studio 2022 Community/Professional** with .NET Aspire workload
- **JetBrains Rider** with .NET plugin

### Database Tools

**pgAdmin** (Optional but recommended):

```bash
# Windows
winget install PostgreSQL.pgAdmin

# macOS
brew install --cask pgadmin4

# Linux
sudo apt-get install pgadmin4
```

**Azure Data Studio** (Alternative):

```bash
# Cross-platform database tool
# Download from https://docs.microsoft.com/en-us/sql/azure-data-studio/download
```

## Project Setup

### Clone Repository

```bash
# Clone the AppDomain Solution repository
git clone https://github.com/your-org/momentum.git
cd momentum

# Verify project structure
ls -la
# Should show: src/, tests/, docs/, infra/, libs/
```

### Install Dependencies

**.NET Dependencies**:

```bash
# Restore all NuGet packages
dotnet restore

# Build the entire solution
dotnet build

# Verify no build errors
echo $? # Should output 0 on success
```

**Documentation Dependencies**:

```bash
cd docs
bun install

# Verify documentation build
bun run docs:build
```

### Environment Configuration

**Local Settings**:

`appsettings.Local.json` is committed to source control and excluded from Docker images via `.dockerignore`. It is loaded after `appsettings.json` only when `ASPNETCORE_ENVIRONMENT=Development`. Connection strings in it intentionally omit credentials — add those via user secrets (see below).

**Database credentials (user secrets)**:

The local Docker postgres password is `password@` (set by `Parameters:DbPassword` in `AppHost/appsettings.json`). Store credentials using `dotnet user-secrets` so they never end up in any file:

```bash
# Run once per service — sets credentials for the local docker-compose postgres
cd src/AppDomain.Api
dotnet user-secrets set "ConnectionStrings:AppDomainDb" "Host=localhost;Port=54320;Database=app_domain;Username=postgres;Password=password@;Maximum Pool Size=100;Minimum Pool Size=5;Connection Idle Lifetime=60;"
dotnet user-secrets set "ConnectionStrings:ServiceBus"  "Host=localhost;Port=54320;Database=service_bus;Username=postgres;Password=password@;Maximum Pool Size=100;Minimum Pool Size=5;Connection Idle Lifetime=60;"

cd ../AppDomain.BackOffice
dotnet user-secrets set "ConnectionStrings:AppDomainDb" "Host=localhost;Port=54320;Database=app_domain;Username=postgres;Password=password@;"
dotnet user-secrets set "ConnectionStrings:ServiceBus"  "Host=localhost;Port=54320;Database=service_bus;Username=postgres;Password=password@;"

cd ../AppDomain.BackOffice.Orleans
dotnet user-secrets set "ConnectionStrings:AppDomainDb" "Host=localhost;Port=54320;Database=app_domain;Username=postgres;Password=password@;"
dotnet user-secrets set "ConnectionStrings:ServiceBus"  "Host=localhost;Port=54320;Database=service_bus;Username=postgres;Password=password@;"
```

When running via Aspire (`dotnet run --project src/AppDomain.AppHost`), credentials are injected automatically — user secrets are only needed when running services directly.

## Infrastructure Setup

### Database Setup

**Start PostgreSQL with Docker Compose**:

```bash
# From project root
docker compose up AppDomain-db -d

# Verify database is running
docker compose ps
# Should show AppDomain-db as running

# Apply database migrations
docker compose up AppDomain-db-migrations

# Verify connection
docker compose exec AppDomain-db psql -U postgres -d AppDomain -c "SELECT version();"
```

**Database Connection Details**:

- **Host**: `localhost`
- **Port**: `54320`
- **Database**: `AppDomain`
- **Username**: `postgres`
- **Password**: `password`

### Message Bus Setup

**Start Kafka Services**:

```bash
# Start Kafka cluster
docker compose --profile kafka up -d

# Verify Kafka is running
docker compose ps kafka
```

**Kafka Configuration**:

- **Bootstrap Servers**: `localhost:9092`
- **Schema Registry**: `http://localhost:8081`
- **Kafka UI**: `http://localhost:8080`

## Running the Application

### Complete Application Stack

**Using .NET Aspire AppHost** (Recommended):

```bash
# Start all services with orchestration
dotnet run --project src/AppDomain.AppHost

# Access Aspire Dashboard
open http://localhost:15888
```

**Services Started**:

- API Service (`http://localhost:8101`)
- BackOffice Service
- Orleans Silo
- PostgreSQL Database
- Kafka Message Bus

### Individual Services

**API Service Only**:

```bash
dotnet run --project src/AppDomain.Api --launch-profile https
# Access at: https://localhost:7201
```

**BackOffice Service**:

```bash
dotnet run --project src/AppDomain.BackOffice
```

**Orleans Silo**:

```bash
dotnet run --project src/AppDomain.BackOffice.Orleans
```

### Documentation Server

```bash
cd docs
bun run dev

# Access documentation at: http://localhost:5173
```

## Development Workflow

### Code Organization

**Recommended Folder Structure**:

```
workspace/
├── momentum/                 # Main project
├── tools/                   # Development tools
└── temp/                    # Temporary files
```

### Git Configuration

**Configure Git for the project**:

```bash
# Set up Git user (if not already configured)
git config --global user.name "Your Name"
git config --global user.email "your.email@example.com"

# Configure line endings (Windows)
git config --global core.autocrlf true

# Configure line endings (macOS/Linux)
git config --global core.autocrlf input
```

**Git Hooks Setup**:

```bash
# Install pre-commit hooks (if available)
# This runs code formatting and linting before commits
```

### Code Style and Formatting

**EditorConfig** (automatically applied):

The project includes `.editorconfig` for consistent code formatting across editors.

**Format Code**:

```bash
# Format all C# files
dotnet format

# Format specific project
dotnet format src/AppDomain.Api/
```

## Testing Setup

### Unit Tests

**Run All Tests**:

```bash
dotnet test

# Run with coverage
dotnet test --collect:"XPlat Code Coverage"

# Run specific test project
dotnet test tests/AppDomain.Tests/
```

### Integration Tests

**TestContainers Setup**:

Integration tests use TestContainers to spin up real infrastructure:

```bash
# Ensure Docker is running before integration tests
docker --version

# Run integration tests
dotnet test tests/AppDomain.Tests/ --filter "Category=Integration"
```

**Test Database Cleanup**:

```bash
# Clean up test containers (if needed)
docker container prune -f
docker volume prune -f
```

## Troubleshooting

### Common Issues

**Port Conflicts**:

```bash
# Check what's using port 5432 (PostgreSQL)
lsof -i :5432  # macOS/Linux
netstat -ano | findstr :5432  # Windows

# Kill process using port
kill -9 <PID>  # macOS/Linux
taskkill /PID <PID> /F  # Windows
```

**Docker Issues**:

```bash
# Reset Docker Desktop (Windows/macOS)
# Docker Desktop -> Troubleshoot -> Reset to factory defaults

# Linux: Restart Docker service
sudo systemctl restart docker

# Clear Docker cache
docker system prune -a
```

**Database Connection Issues**:

```bash
# Check container logs
docker compose logs AppDomain-db

# Reset database completely
docker compose down -v
docker compose up AppDomain-db AppDomain-db-migrations
```

**NuGet Package Issues**:

```bash
# Clear NuGet cache
dotnet nuget locals all --clear

# Restore packages
dotnet restore --force
```

### Performance Optimization

**Development Performance**:

```bash
# Increase Docker memory allocation (4GB recommended)
# Docker Desktop -> Settings -> Resources -> Memory

# Use faster file system for Docker volumes
# Use named volumes instead of bind mounts for large datasets
```

**Build Performance**:

```json
// Add to global.json for faster builds
{
  "msbuild-sdks": {
    "Microsoft.Build.NoTargets": "3.7.0"
  },
  "sdk": {
    "version": "10.0.0",
    "rollForward": "latestMinor"
  }
}
```

## IDE Configuration

### Visual Studio Code

**Settings Configuration** (`.vscode/settings.json`):

```json
{
  "dotnet.defaultSolution": "AppDomain.sln",
  "files.exclude": {
    "**/bin": true,
    "**/obj": true,
    "**/.vs": true
  },
  "csharp.semanticHighlighting.enabled": true,
  "editor.formatOnSave": true,
  "editor.codeActionsOnSave": {
    "source.organizeImports": "explicit"
  }
}
```

**Launch Configuration** (`.vscode/launch.json`):

```json
{
  "version": "0.2.0",
  "configurations": [
    {
      "name": "Launch AppHost",
      "type": "coreclr",
      "request": "launch",
      "program": "${workspaceFolder}/src/AppDomain.AppHost/bin/Debug/net10.0/AppDomain.AppHost.dll",
      "args": [],
      "cwd": "${workspaceFolder}/src/AppDomain.AppHost",
      "preLaunchTask": "build"
    }
  ]
}
```

### Visual Studio

**Startup Project Configuration**:

1. Right-click solution in Solution Explorer
2. Select **Set Startup Projects**
3. Choose **Multiple startup projects**
4. Set **AppDomain.AppHost** to **Start**

## Security Considerations

### Development Secrets

1. **Never commit secrets** to version control
2. **Use User Secrets** for development credentials
3. **Use Environment Variables** for CI/CD
4. **Rotate secrets regularly** in production

### HTTPS Development Certificates

```bash
# Trust development certificates
dotnet dev-certs https --trust

# Clean and regenerate if issues occur
dotnet dev-certs https --clean
dotnet dev-certs https --trust
```

## Next Steps

Once your development environment is set up:

1. **Explore the codebase**: Start with [Getting Started](/guide/getting-started)
2. **Make your first contribution**: Follow the [First Contribution Guide](/guide/first-contribution)
3. **Learn debugging techniques**: Review [Debugging Tips](/guide/debugging)
4. **Understand the architecture**: Read [Architecture Overview](/arch/)

## Support and Resources

### Documentation

- [Getting Started Guide](/guide/getting-started)
- [Architecture Documentation](/arch/)
- [API Reference](/reference/AppDomain)

### External Resources

- [.NET 10 Documentation](https://docs.microsoft.com/en-us/dotnet/)
- [.NET Aspire Documentation](https://learn.microsoft.com/en-us/dotnet/aspire/)
- [Docker Documentation](https://docs.docker.com/)
- [PostgreSQL Documentation](https://www.postgresql.org/docs/)

### Community

- GitHub Discussions for questions and feedback
- Team chat channels for immediate support
- Code review process for learning and collaboration

> [!TIP]
> Keep your development environment updated regularly by running `git pull`, `dotnet restore`, and `bun install` to stay current with the latest changes.
