# Copyright (c) ORG_NAME. All rights reserved.

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE SERVICE_BASE_PORT1
EXPOSE SERVICE_BASE_PORT2

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copy project files and restore dependencies
COPY ["src/AppDomain.Api/AppDomain.Api.csproj", "src/AppDomain.Api/"]
COPY ["src/AppDomain/AppDomain.csproj", "src/AppDomain/"]
COPY ["src/AppDomain.Contracts/AppDomain.Contracts.csproj", "src/AppDomain.Contracts/"]
//#if (libs == api || libs == defaults)
COPY ["libs/Momentum/src/Momentum.ServiceDefaults.Api/Momentum.ServiceDefaults.Api.csproj", "libs/Momentum/src/Momentum.ServiceDefaults.Api/"]
//#endif
//#if (libs == defaults)
COPY ["libs/Momentum/src/Momentum.ServiceDefaults/Momentum.ServiceDefaults.csproj", "libs/Momentum/src/Momentum.ServiceDefaults/"]
//#endif
//#if (libs == ext)
COPY ["libs/Momentum/src/Momentum.Extensions/Momentum.Extensions.csproj", "libs/Momentum/src/Momentum.Extensions/"]
//#endif
//#if (libs == generators)
COPY ["libs/Momentum/src/Momentum.Extensions.SourceGenerators/Momentum.Extensions.SourceGenerators.csproj", "libs/Momentum/src/Momentum.Extensions.SourceGenerators/"]
//#endif

# Copy package files
COPY ["Directory.Build.props", "Directory.Build.props"]
COPY ["Directory.Packages.props", "Directory.Packages.props"]

RUN dotnet restore "src/AppDomain.Api/AppDomain.Api.csproj"

# Copy source code
COPY . .

WORKDIR "/src/src/AppDomain.Api"
# Build the application
RUN dotnet build "AppDomain.Api.csproj" -c $BUILD_CONFIGURATION -o /app/build

# Generate Wolverine code files
RUN dotnet run --project "AppDomain.Api.csproj" -c $BUILD_CONFIGURATION -- codegen write

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "AppDomain.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

ENTRYPOINT ["dotnet", "AppDomain.Api.dll"]
