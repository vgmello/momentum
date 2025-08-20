# syntax=docker/dockerfile:1-labs
# Base
FROM mcr.microsoft.com/dotnet/aspnet:9.0-alpine AS base
EXPOSE 8080
EXPOSE 8443

RUN apk add --no-cache \
    ca-certificates librdkafka-dev tzdata \
    && rm -rf /var/cache/apk/* \
    && adduser -D -u 1001 appuser

USER appuser
WORKDIR /app

## Patch Kafka Native Lib
RUN mkdir -p runtimes/linux-arm64/native \
    && ln -s /usr/lib/librdkafka.so.1 runtimes/linux-arm64/native/alpine-librdkafka.so

# ----

# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0-alpine AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

## Copy all project files to restore dependencies
COPY --parents **/**/**/*.csproj .
COPY Directory.Build.props Directory.Packages.props ./

## Restore packages - this layer is cached unless dependencies change
RUN dotnet restore "src/AppDomain.BackOffice.Orleans/AppDomain.BackOffice.Orleans.csproj"

## Copy source code (this layer invalidates on any code change)
COPY . .
WORKDIR /src/src/AppDomain.BackOffice.Orleans/

## Create Wolverine generated source files
RUN dotnet run --no-restore -- codegen write && dotnet run --no-restore -- codegen test

## Build & Publish
RUN dotnet publish \
    -c "$BUILD_CONFIGURATION" \
    -o /app/publish \
    --no-restore \
    /p:UseAppHost=false

# ----

# Final stage
FROM base AS final
COPY --from=build --chown=appuser:appuser /app/publish .

ENTRYPOINT ["dotnet", "AppDomain.BackOffice.Orleans.dll"]
