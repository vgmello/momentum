# Copyright (c) ORG_NAME. All rights reserved.

FROM mcr.microsoft.com/dotnet/runtime:9.0 AS base
USER $APP_UID
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copy project files and restore dependencies
COPY ["src/AppDomain.BackOffice/AppDomain.BackOffice.csproj", "src/AppDomain.BackOffice/"]
COPY ["src/AppDomain/AppDomain.csproj", "src/AppDomain/"]
COPY ["src/AppDomain.Contracts/AppDomain.Contracts.csproj", "src/AppDomain.Contracts/"]
//#if (libs == defaults)
COPY ["libs/Momentum/src/Momentum.ServiceDefaults/Momentum.ServiceDefaults.csproj", "libs/Momentum/src/Momentum.ServiceDefaults/"]
//#endif
//#if (libs == ext)
COPY ["libs/Momentum/src/Momentum.Extensions/Momentum.Extensions.csproj", "libs/Momentum/src/Momentum.Extensions/"]
//#endif
//#if (libs == generators)
COPY ["libs/Momentum/src/Momentum.Extensions.SourceGenerators/Momentum.Extensions.SourceGenerators.csproj", "libs/Momentum/src/Momentum.Extensions.SourceGenerators/"]
//#endif
//#if (libs == kafka)
COPY ["libs/Momentum/src/Momentum.Extensions.Messaging.Kafka/Momentum.Extensions.Messaging.Kafka.csproj", "libs/Momentum/src/Momentum.Extensions.Messaging.Kafka/"]
//#endif

# Copy package files
COPY ["Directory.Build.props", "Directory.Build.props"]
COPY ["Directory.Packages.props", "Directory.Packages.props"]

RUN dotnet restore "src/AppDomain.BackOffice/AppDomain.BackOffice.csproj"

# Copy source code
COPY . .

WORKDIR "/src/src/AppDomain.BackOffice"
# Build the application
RUN dotnet build "AppDomain.BackOffice.csproj" -c $BUILD_CONFIGURATION -o /app/build

# Generate Wolverine code files
RUN dotnet run --project "AppDomain.BackOffice.csproj" -c $BUILD_CONFIGURATION -- codegen write

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "AppDomain.BackOffice.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Set environment variables for containerized applications
ENV DOTNET_RUNNING_IN_CONTAINER=1
ENV DOTNET_NOLOGO=1
ENV DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1

ENTRYPOINT ["dotnet", "AppDomain.BackOffice.dll"]
