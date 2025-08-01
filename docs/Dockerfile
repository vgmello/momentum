FROM mcr.microsoft.com/dotnet/sdk:9.0-alpine

ENV PNPM_HOME="/pnpm"
ENV PATH="${PNPM_HOME}:${PATH}:/root/.dotnet/tools"

# Install node, pnpm, and docfx
RUN apk add --no-cache git nodejs && \
    wget -qO- https://get.pnpm.io/install.sh | ENV="$HOME/.shrc" SHELL="$(which sh)" sh - && \
    dotnet tool install -g docfx

# Install dependencies into the isolated /build directory.
# This directory will NOT be affected by the volume mount.
WORKDIR /app/docs
COPY docs/package.json docs/pnpm-lock.yaml ./
RUN --mount=type=cache,id=pnpm,target=/pnpm/store pnpm install --frozen-lockfile

# Build the markdown generator tool in the container
# TODO: Remove this once the tool is shipped as a standalone tool
WORKDIR /generator
COPY .editorconfig ./
COPY Directory.Build.props ./
COPY Directory.Packages.props ./
COPY libs/Operations/src/Directory.Build.props ./libs/Operations/src/
COPY libs/Operations/src/Operations.Extensions.EventMarkdownGenerator ./libs/Operations/src/Operations.Extensions.EventMarkdownGenerator
COPY libs/Operations/src/Operations.Extensions ./libs/Operations/src/Operations.Extensions
COPY libs/Operations/src/Operations.Extensions.Abstractions ./libs/Operations/src/Operations.Extensions.Abstractions
COPY libs/Operations/src/Operations.Extensions.XmlDocs ./libs/Operations/src/Operations.Extensions.XmlDocs

RUN dotnet build libs/Operations/src/Operations.Extensions.EventMarkdownGenerator \
    -c Debug -p:IsPackable=false -p:TreatWarningsAsErrors=false -p:RunCodeAnalysis=false

WORKDIR /app/docs
CMD ["pnpm", "dev", "--host"]
