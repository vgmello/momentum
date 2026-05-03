FROM oven/bun:alpine AS bun

FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine

ENV PATH="${PATH}:/root/.dotnet/tools"

# Copy bun binary from the official bun image (avoids curl/unzip/bash)
COPY --from=bun /usr/local/bin/bun /usr/local/bin/bun

# Install git (required by vitepress lastUpdated and docfx), then install dotnet tools
RUN apk add --no-cache git && \
    dotnet tool install -g docfx && \
    dotnet tool install -g Momentum.Extensions.EventMarkdownGenerator

WORKDIR /app/docs

CMD ["sh", "-c", "bun install --frozen-lockfile && bun run docs:serve"]
