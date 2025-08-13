# Copyright (c) OrgName. All rights reserved.

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE mainApiHttp
EXPOSE mainApiHttps

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copy project files and restore dependencies
COPY ["src/AppDomain.Api/AppDomain.Api.csproj", "src/AppDomain.Api/"]
COPY ["src/AppDomain/AppDomain.csproj", "src/AppDomain/"]
COPY ["src/AppDomain.Contracts/AppDomain.Contracts.csproj", "src/AppDomain.Contracts/"]
COPY ["libs/Momentum/src/Momentum.ServiceDefaults.Api/Momentum.ServiceDefaults.Api.csproj", "libs/Momentum/src/Momentum.ServiceDefaults.Api/"]
COPY ["libs/Momentum/src/Momentum.ServiceDefaults/Momentum.ServiceDefaults.csproj", "libs/Momentum/src/Momentum.ServiceDefaults/"]
COPY ["libs/Momentum/src/Momentum.Extensions/Momentum.Extensions.csproj", "libs/Momentum/src/Momentum.Extensions/"]
COPY ["libs/Momentum/src/Momentum.Extensions.SourceGenerators/Momentum.Extensions.SourceGenerators.csproj", "libs/Momentum/src/Momentum.Extensions.SourceGenerators/"]
COPY ["libs/Momentum/src/Momentum.Extensions.Abstractions/Momentum.Extensions.Abstractions.csproj", "libs/Momentum/src/Momentum.Extensions.Abstractions/"]

# Copy package files
COPY ["Directory.Build.props", "Directory.Build.props"]
COPY ["Directory.Packages.props", "Directory.Packages.props"]

RUN dotnet restore "src/AppDomain.Api/AppDomain.Api.csproj"

# Copy source code
COPY . .

WORKDIR "/src/src/AppDomain.Api"
RUN dotnet build "AppDomain.Api.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "AppDomain.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "AppDomain.Api.dll"]
