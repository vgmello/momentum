# Claude Code Configuration

This directory contains Claude Code configuration for the Momentum .NET project.

## Contents

### Commands (`./commands/`)

Custom slash commands for Claude Code:

#### Requirements Management Commands

Based on [Claude Code Requirements Builder](https://github.com/rizethereum/claude-code-requirements-builder):

- `/requirements-start [description]` - Begin gathering requirements for a new feature
- `/requirements-status` - Check progress on current requirement
- `/requirements-current` - View current requirement details
- `/requirements-list` - List all requirements
- `/requirements-end` - Finalize current requirement
- `/requirements-implement` - Synthesize requirements into implementation plan
- `/requirements-remind` (alias: `/remind`) - Remind Claude to follow requirements gathering rules

**How it works:**
1. **Phase 1**: Initial codebase analysis
2. **Phase 2**: 5 high-level yes/no discovery questions
3. **Phase 3**: Autonomous code research based on answers
4. **Phase 4**: 5 expert yes/no detail questions
5. **Phase 5**: Generate comprehensive requirements spec

All questions support "idk" to use smart defaults based on best practices and codebase patterns.

**Requirements Storage:**
- Location: `/workspace/requirements/` (gitignored)
- Format: `YYYY-MM-DD-HHMM-feature-name/`
- Files: metadata, questions, answers, findings, spec
- Index: `requirements/index.md`

### Agents (`./agents/`)

Specialized AI agents for specific tasks:

- **code-reviewer** - Expert code review, security, and best practices
- **dotnet-dev** - .NET 9 specialist with Momentum architecture expertise
- **dotnet-docs-writer** - Technical documentation for .NET projects

## Setup

The requirements system is already configured. The `requirements/` directory will be created automatically on first use and is excluded from git.

To verify setup:
```bash
# Check commands are available
ls .claude/commands/

# Check requirements directory (will be created on first use)
ls requirements/
```

## Usage Example

```bash
# Start gathering requirements
/requirements-start add customer authentication system

# Claude will:
# 1. Analyze the codebase
# 2. Ask 5 discovery questions (yes/no with defaults)
# 3. Research relevant code autonomously
# 4. Ask 5 expert questions (yes/no with defaults)
# 5. Generate comprehensive requirements spec

# Check status anytime
/requirements-status

# View completed requirements
/requirements-list

# Implement completed requirement
/requirements-implement
```

## Integration with Momentum

This requirements system integrates with:
- **CLAUDE.md** - Project-specific guidance and patterns
- **Architecture Tests** - CQRS and domain isolation rules
- **Template System** - Understanding of mmt template structure
- **Aspire Configuration** - Service orchestration patterns
- **Database Migrations** - Liquibase changelog conventions

## Notes

- Requirements are local and not committed to the repository
- Use `/remind` if Claude strays from requirements gathering protocol
- Smart defaults are based on Momentum best practices and architecture
- All file paths in specs reference actual codebase locations
- Implementation plans map to testable acceptance criteria
