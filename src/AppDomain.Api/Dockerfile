# syntax=docker/dockerfile:1-labs
FROM mcr.microsoft.com/dotnet/aspnet:9.0-alpine AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8443

# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0-alpine AS build
ARG BUILD_CONFIGURATION=Release

## Install gRPC tools
RUN apk add --no-cache grpc-plugins
ENV PROTOBUF_PROTOC=/usr/bin/protoc
ENV GRPC_PROTOC_PLUGIN=/usr/bin/grpc_csharp_plugin

WORKDIR /src

## Copy all project files to restore dependencies
COPY --parents **/**/**/*.csproj .
COPY Directory.Build.props Directory.Packages.props ./

## Restore packages - this layer is cached unless dependencies change
RUN dotnet restore "src/AppDomain.Api/AppDomain.Api.csproj"

## Copy source code (this layer invalidates on any code change)
COPY . .

# Create Wolverine generated source files
WORKDIR /src/src/AppDomain.Api/
# RUN dotnet run -- codegen write & dotnet run -- codegen test

# # Build & Publish
# RUN dotnet publish "src/AppDomain.Api/AppDomain.Api.csproj" \
#     -c "$BUILD_CONFIGURATION" \
#     -o /app/publish \
#     --no-restore \
#     /p:UseAppHost=false

# FROM base AS final
# COPY --from=build /app/publish .

# ENTRYPOINT ["dotnet", "AppDomain.Api.dll"]
