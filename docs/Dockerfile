FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine

ENV BUN_INSTALL="/root/.bun"
ENV PATH="${BUN_INSTALL}/bin:${PATH}:/root/.dotnet/tools"

# Install git, unzip (required for bun), bun, and docfx
RUN apk add --no-cache git unzip curl bash && \
    curl -fsSL https://bun.sh/install | bash && \
    dotnet tool install -g docfx  && \
    dotnet tool install -g Momentum.Extensions.EventMarkdownGenerator

WORKDIR /app/docs
COPY --link docs/package.json docs/bun.lock ./
RUN bun install --frozen-lockfile

WORKDIR /app/docs
CMD ["bun", "run", "dev", "--", "--host"]
