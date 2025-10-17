FROM mcr.microsoft.com/dotnet/sdk:9.0-noble

ENV DEBIAN_FRONTEND=noninteractive

# Install system dependencies as root
RUN apt-get update && \
    apt-get install -y \
    ca-certificates \
    curl \
    git \
    jq \
    python3 \
    python3-pip \
    python3-venv \
    sudo \
    wget && \
    #
    # Install GitHub CLI
    mkdir -p -m 755 /etc/apt/keyrings && \
    wget -nv -O /tmp/githubcli-archive-keyring.gpg https://cli.github.com/packages/githubcli-archive-keyring.gpg && \
    cat /tmp/githubcli-archive-keyring.gpg | tee /etc/apt/keyrings/githubcli-archive-keyring.gpg > /dev/null && \
    chmod go+r /etc/apt/keyrings/githubcli-archive-keyring.gpg && \
    echo "deb [arch=$(dpkg --print-architecture) signed-by=/etc/apt/keyrings/githubcli-archive-keyring.gpg] https://cli.github.com/packages stable main" | \
    tee /etc/apt/sources.list.d/github-cli.list > /dev/null && \
    apt-get update && \
    apt-get install -y gh && \
    #
    # Cleanup
    rm -rf /var/lib/apt/lists/* /tmp/githubcli-archive-keyring.gpg

# Create claude user with sudo access and workspace directory
RUN useradd -m -s /bin/bash claude && \
    mkdir -p /workspace && \
    chown -R claude:claude /workspace && \
    usermod -aG sudo claude && \
    echo "claude ALL=(ALL) NOPASSWD:ALL" > /etc/sudoers.d/claude && \
    chmod 0440 /etc/sudoers.d/claude

# Switch to claude user (non-root installs)
USER claude

# Set up environment for claude user
ENV NVM_DIR="/home/claude/.nvm" \
    PATH="/home/claude/.nvm/versions/node/default/bin:/home/claude/.local/bin:$PATH"

# Install nvm and Node.js
RUN curl -o- https://raw.githubusercontent.com/nvm-sh/nvm/v0.40.3/install.sh | bash && \
    . "$NVM_DIR/nvm.sh" && \
    nvm install 22 && \
    nvm alias default 22 && \
    nvm use default && \
    # Add node to PATH
    echo 'export NVM_DIR="$HOME/.nvm"' >> /home/claude/.bashrc && \
    echo '[ -s "$NVM_DIR/nvm.sh" ] && \. "$NVM_DIR/nvm.sh"' >> /home/claude/.bashrc && \
    echo '[ -s "$NVM_DIR/bash_completion" ] && \. "$NVM_DIR/bash_completion"' >> /home/claude/.bashrc

# Install Claude Code CLI
RUN curl -fsSL https://claude.ai/install.sh | bash

COPY --chmod=755 entrypoint.sh /usr/local/bin/entrypoint.sh

WORKDIR /workspace
ENTRYPOINT ["/usr/local/bin/entrypoint.sh"]
