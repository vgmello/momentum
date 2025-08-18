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
COPY libs/Momentum/src/Directory.Build.props ./libs/Momentum/src/
COPY libs/Momentum/src/Momentum.Extensions.EventMarkdownGenerator ./libs/Momentum/src/Momentum.Extensions.EventMarkdownGenerator
COPY libs/Momentum/src/Momentum.Extensions ./libs/Momentum/src/Momentum.Extensions
COPY libs/Momentum/src/Momentum.Extensions.Abstractions ./libs/Momentum/src/Momentum.Extensions.Abstractions
COPY libs/Momentum/src/Momentum.Extensions.XmlDocs ./libs/Momentum/src/Momentum.Extensions.XmlDocs

RUN dotnet build libs/Momentum/src/Momentum.Extensions.EventMarkdownGenerator \
    -p:IsPackable=false -p:TreatWarningsAsErrors=false -p:RunCodeAnalysis=false

WORKDIR /app/docs
CMD ["pnpm", "dev", "--host"]
