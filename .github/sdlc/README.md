# SDLC - Self-hosted Claude Code for GitHub Actions

Automate your development workflow with Claude Code AI running on self-hosted GitHub Actions runners. Simply mention `@claude` in any issue or pull request, and Claude will autonomously work on your request.

## 🚀 Quick Start

### Prerequisites

- Docker and docker-compose installed
- A GitHub repository or organization where you want to use Claude
- Claude Code CLI installed locally

### Step 1: Generate Claude OAuth Token

Run the following command to generate your Claude authentication token:

```bash
claude setup-token
```

Copy the token value that is displayed.

### Step 2: Configure GitHub Secret

1. Go to your GitHub repository
2. Navigate to: **Settings** → **Secrets and variables** → **Actions**
3. Click **New repository secret**
4. Name: `CLAUDE_CODE_OAUTH_TOKEN`
5. Value: Paste the token from Step 1
6. Click **Add secret**

### Step 3: Generate GitHub Personal Access Token

1. Go to: https://github.com/settings/personal-access-tokens
2. Click **Generate new token**
3. Give it a descriptive name (e.g., "SDLC Self-hosted Runner")
4. Select Permissions based on your setup:
   - **For Repository-level runners**: ✅ **Administration** (Read-write)
   - **For Organization-level runners**: ✅ **Administration** (Read-write) for organization access
5. Click **Generate token**
6. **Copy the token** (you won't be able to see it again)

### Step 4: Run Setup

Run the setup script and provide the information when prompted:

```bash
./sdlc.sh --setup
```

The script will:

- Build the Claude Code Docker container
- Prompt you for:
  - **GitHub Token**: Paste the personal access token from Step 3
  - **Repository or Organization**: Enter either:
    - Repository format: `owner/repo-name` (for repository-level runners)
    - Organization format: `org-name` (for organization-level runners available to all repos)
  - **Runner Prefix**: (Optional) A prefix for your runner names
- Create the runner configuration automatically

### Step 5: Start the Runners

```bash
./sdlc.sh
```

This will start 5 self-hosted GitHub Actions runners.

### Step 6: Verify Setup

**For Repository-level runners:**

1. Go to your repository: **Settings** → **Actions** → **Runners**
2. You should see 5 runners online (e.g., `gh-runner-1`, `gh-runner-2`, etc.)

**For Organization-level runners:**

1. Go to your organization: **Settings** → **Actions** → **Runners**
2. You should see 5 runners online and available to all repositories in the organization

## 💬 Usage

Simply mention `@claude` in:

- Issue descriptions or comments
- Pull request descriptions or comments
- Pull request reviews

Claude will:

- Analyze your request
- Create a task breakdown
- Implement the changes
- Create a pull request when done
- Keep you updated with progress comments

**Example:**

```
@claude please add unit tests for the authentication module
```
