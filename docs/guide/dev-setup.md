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

**.NET 9 SDK**:

```bash
# Windows (using winget)
winget install Microsoft.DotNet.SDK.9

# macOS (using Homebrew)
brew install --cask dotnet

# Linux (Ubuntu/Debian)
wget https://packages.microsoft.com/config/ubuntu/20.04/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
sudo dpkg -i packages-microsoft-prod.deb
sudo apt-get update
sudo apt-get install -y dotnet-sdk-9.0
```

**Verify Installation**:

```bash
dotnet --version
# Expected output: 9.0.x
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

### Node.js and Package Management

**Node.js (for documentation)**:

```bash
# Using Node Version Manager (recommended)
curl -o- https://raw.githubusercontent.com/nvm-sh/nvm/v0.39.0/install.sh | bash
nvm install 18
nvm use 18

# Or direct installation
# Windows: Download from https://nodejs.org/
# macOS: brew install node@18
# Linux: Use your distribution's package manager
```

**pnpm (for documentation build)**:

```bash
npm install -g pnpm

# Verify installation
pnpm --version
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
pnpm install

# Verify documentation build
pnpm docs:build
```

### Environment Configuration

**Development Settings**:

Create `src/AppDomain.Api/appsettings.Development.json`:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "AppDomain": "Debug"
    }
  },
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=54320;Database=AppDomain;Username=postgres;Password=password;"
  },
  "Aspire": {
    "Dashboard": {
      "Url": "http://localhost:15888"
    }
  }
}
```

**User Secrets** (for sensitive data):

```bash
# Initialize user secrets for API project
cd src/AppDomain.Api
dotnet user-secrets init

# Add development secrets (example)
dotnet user-secrets set "ExternalApi:ApiKey" "development-api-key"
dotnet user-secrets set "JWT:SecretKey" "super-secret-development-key-that-is-very-long"
```

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
pnpm dev

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
    "version": "9.0.0",
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
      "program": "${workspaceFolder}/src/AppDomain.AppHost/bin/Debug/net9.0/AppDomain.AppHost.dll",
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

- [.NET 9 Documentation](https://docs.microsoft.com/en-us/dotnet/)
- [.NET Aspire Documentation](https://learn.microsoft.com/en-us/dotnet/aspire/)
- [Docker Documentation](https://docs.docker.com/)
- [PostgreSQL Documentation](https://www.postgresql.org/docs/)

### Community

- GitHub Discussions for questions and feedback
- Team chat channels for immediate support
- Code review process for learning and collaboration

> [!TIP]
> Keep your development environment updated regularly by running `git pull`, `dotnet restore`, and `pnpm install` to stay current with the latest changes.