# syntax=docker/dockerfile:1-labs
# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS build
ARG BUILD_CONFIGURATION=Release

## Install gRPC tools
RUN apk add --no-cache grpc-plugins
ENV PROTOBUF_PROTOC=/usr/bin/protoc
ENV GRPC_PROTOC_PLUGIN=/usr/bin/grpc_csharp_plugin

WORKDIR /src

## Copy all project files to restore dependencies
COPY --link --parents **/**/**/*.csproj .
COPY --link Directory.Build.props Directory.Packages.props ./

## Restore packages - this layer is cached unless dependencies change
RUN dotnet restore "src/AppDomain.Api/AppDomain.Api.csproj"

## Copy source code (this layer invalidates on any code change)
COPY --link . .
WORKDIR /src/src/AppDomain.Api/

## Create Wolverine generated source files
RUN dotnet run --no-restore -- codegen write && dotnet run --no-restore -- codegen test

## Build / Publish
RUN dotnet publish \
    -c "$BUILD_CONFIGURATION" \
    -o /app/publish \
    --no-restore \
    /p:UseAppHost=false

# ----

# Final stage — alpine-extra already includes ca-certificates, tzdata, ICU libs,
# and creates a non-root 'app' user (UID 1654) so we reuse it directly.
FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine-extra AS final
EXPOSE 8080
EXPOSE 8443

RUN apk add --no-cache librdkafka-dev

WORKDIR /app

## Patch Kafka Native Lib — on ARM64 Alpine the NuGet-bundled glibc librdkafka crashes,
## so we symlink the Alpine system lib. Confluent.Kafka checks for 'alpine-librdkafka.so' first.
RUN TARGETARCH=$(uname -m) && \
    if [ "$TARGETARCH" = "aarch64" ]; then \
    mkdir -p runtimes/linux-arm64/native && \
    ln -sf /usr/lib/librdkafka.so.1 runtimes/linux-arm64/native/alpine-librdkafka.so; \
    fi

USER app
COPY --link --from=build --chown=$APP_UID:$APP_UID /app/publish .

ENTRYPOINT ["dotnet", "AppDomain.Api.dll"]
