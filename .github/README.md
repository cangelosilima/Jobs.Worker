# GitHub Workflows & CI/CD

This directory contains GitHub Actions workflows and repository configuration for the Jobs.Worker project.

## Workflows

### 1. Push Validation - Bots & AI Only (`push-validation.yml`) 🤖

**Triggered on:** Every push to any branch (excluding documentation) **by bots or AI**

**Purpose:** Ensures automated code (from bots/AI) passes quality checks before being accepted, while allowing human developers to iterate freely.

**Bot Detection (Based on Authentication):**
Uses GitHub's native authentication metadata to identify bots/AI:

1. **`github.event.pusher.type == "Bot"`** ← Most reliable
   - GitHub Apps and automated tools use Bot type
   - Native GitHub authentication check

2. **GitHub Bot Naming Convention**
   - Username ends with `[bot]` (e.g., `dependabot[bot]`)

3. **Commit Author Email Pattern**
   - Bot-specific email patterns in commit metadata

4. **GitHub Actions Token**
   - Identifies `github-actions` automation

**Human Detection:**
- OAuth authentication (web login)
- Personal Access Token (PAT)
- SSH keys
- Any non-bot GitHub authentication

**Jobs:**

1. **Check if pusher is Bot/AI**
   - Analyzes `github.event.pusher.type` (primary)
   - Checks bot naming conventions (secondary)
   - Verifies commit author patterns (fallback)
   - Determines if pusher is human or automated
   - Skips validation for human developers

2. **Quick Build & Test Validation** (only if bot/AI)
   - Fast validation of both backend and frontend
   - Builds .NET solution
   - Runs all backend tests with minimal verbosity
   - Builds React application
   - Runs all frontend tests
   - Provides clear success/failure summary

3. **Block If Validation Fails** (only if bot/AI)
   - Explicitly fails the workflow if validation doesn't pass
   - Provides helpful error message
   - Prevents broken automated code from being pushed

**Why this matters:**
- 🤖 Bot/AI code is validated immediately (catch errors fast)
- 👨‍💻 Human developers can iterate freely (no slowdown)
- ✅ Automated pushes meet quality standards
- 🚀 Best of both worlds: safety + velocity

### 2. Pull Request Validation (`pr-validation.yml`)

**Triggered on:** Pull requests to `main` or `develop` branches

**Jobs:**
- **Backend Build & Test**
  - Builds .NET solution
  - Runs all test suites (Domain, Application, API, Architecture)
  - Publishes test results
  - Uploads test artifacts

- **Frontend Build & Test**
  - Builds React application
  - Runs linter
  - Runs unit and integration tests
  - Generates coverage reports
  - Uploads build artifacts

- **Code Quality**
  - Checks code formatting
  - Runs security scans
  - Validates code standards

- **PR Validation Summary**
  - Aggregates all check results
  - Comments on PR with status table
  - Fails PR if any check fails

### 3. Main Branch CI (`ci-main.yml`)

**Triggered on:** Pushes to `main` branch or manual dispatch

**Jobs:**
- Builds and tests both backend and frontend
- Generates and uploads code coverage reports
- Creates release tags automatically
- Publishes GitHub releases with changelogs

### 4. Auto Issue Management (`auto-issue-management.yml`) 🎯

**Triggered on:**
- Workflow completion (any CI workflow)
- Pull request closure
- Branch deletion

**Purpose:** Automatically creates and manages GitHub issues for CI failures, keeping your issue tracker clean and up-to-date.

**Features:**

**📝 Auto-Create Issues on CI Failure:**
- Creates a new issue when any CI workflow fails
- Labels: `ci-failure`, `branch:branch-name`, `bug`, `automated`
- Includes detailed failure information:
  - Workflow name and run number
  - Branch and commit SHA
  - Direct link to failed workflow run
  - Action items for fixing the issue
- Updates existing issue if multiple failures occur on the same branch

**✅ Auto-Close Issues on Success:**
- Automatically closes the issue when CI succeeds again
- Adds a detailed success comment with:
  - Link to successful workflow run
  - Commit that fixed the issue
  - Issue reference (e.g., "Closes #123")
  - Timestamp of resolution
- Sets issue state to "completed"

**🗑️ Auto-Close on PR/Branch Deletion:**
- Closes related issues when PR is merged or closed
- Closes related issues when branch is deleted
- Sets issue state to "not_planned"
- Adds explanation comment

**Jobs:**

1. **Handle Workflow Completion**
   - Monitors all CI workflows (Push Validation, PR Validation, Main CI)
   - Creates issues for failures
   - Closes issues for successes
   - Prevents duplicate issues per branch/workflow

2. **Handle PR Closure**
   - Detects when PRs are closed or merged
   - Closes all related CI failure issues
   - Adds context about PR state (merged vs closed)

3. **Handle Branch Deletion**
   - Detects branch deletions
   - Closes all CI failure issues for that branch
   - Cleans up stale issues

**Issue Lifecycle Example:**

```
CI Failure on feature/new-feature
    ↓
🔴 Issue Created: "CI Failure: Pull Request Validation on feature/new-feature"
    ↓
Developer fixes the issue and pushes
    ↓
CI Success
    ↓
✅ Issue Auto-Closed with comment: "Fixed! See successful run #123"
    ↓
Issue closed with reference to the fix
```

**Benefits:**
- 🎯 Never miss a CI failure - automatic issue tracking
- 🧹 No manual issue management - fully automated lifecycle
- 📊 Clear audit trail of CI failures and resolutions
- 🔗 Direct links between issues and workflow runs
- 🚀 Keeps issue tracker clean and current

## Workflow Hierarchy

```
Push to any branch
    ↓
[Push Validation] ← Runs immediately on push
    ↓                     ↓ (if failure)
Create Pull Request      [Auto Issue] ← Creates issue
    ↓                     ↓ (on success)
[PR Validation] ← Comprehensive checks
    ↓                     ↓ (if failure)
Merge to main            [Auto Issue] ← Closes issue on fix
    ↓
[CI Main] ← Release automation
    ↓
[Auto Issue] ← Tracks failures/successes

Branch/PR Deletion
    ↓
[Auto Issue] ← Closes related issues
```

## Repository Configuration

### Pull Request Template (`pull_request_template.md`)
Standardized template for all PRs including:
- Description and type of change
- Related issues
- Testing checklist
- Code review checklist

### Code Owners (`CODEOWNERS`)
Defines code ownership for automatic review requests:
- Backend: `@cangelosilima`
- Frontend: `@cangelosilima`
- CI/CD: `@cangelosilima`

### Dependabot (`dependabot.yml`)
Automated dependency updates:
- .NET NuGet packages (weekly)
- Frontend npm packages (weekly)
- GitHub Actions (weekly)

## Status Badges

Add these to your README.md:

```markdown
![PR Validation](https://github.com/cangelosilima/Jobs.Worker/workflows/Pull%20Request%20Validation/badge.svg)
![CI Main](https://github.com/cangelosilima/Jobs.Worker/workflows/CI%20-%20Main%20Branch/badge.svg)
[![codecov](https://codecov.io/gh/cangelosilima/Jobs.Worker/branch/main/graph/badge.svg)](https://codecov.io/gh/cangelosilima/Jobs.Worker)
```

## Testing Requirements

### Backend
All tests must pass:
- ✅ Domain unit tests
- ✅ Application unit tests
- ✅ API integration tests
- ✅ Architecture tests

### Frontend
All tests must pass:
- ✅ Unit tests (stores, components)
- ✅ Integration tests (API)
- ✅ Architecture tests
- ✅ Build must succeed

## Code Coverage

Coverage reports are automatically uploaded to Codecov:
- Backend coverage via XPlat Code Coverage
- Frontend coverage via Vitest
- Minimum thresholds (configurable):
  - Domain: 80%
  - Application: 75%
  - Frontend: 75%

## Local Testing

Before pushing, run tests locally:

### Backend
```bash
# Run all tests
dotnet test

# Run specific test project
dotnet test tests/Jobs.Worker.Domain.Tests/

# With coverage
dotnet test --collect:"XPlat Code Coverage"
```

### Frontend
```bash
cd frontend

# Run tests
npm test

# Run with coverage
npm run test:coverage

# Run in watch mode
npm test -- --watch
```

## Troubleshooting

### Tests Failing in CI but Pass Locally

1. Check if dependencies are properly restored
2. Verify environment variables are set correctly
3. Check for timezone-dependent tests
4. Ensure database/external dependencies are mocked

### Build Failures

1. Check .NET SDK version matches (8.0.x)
2. Verify Node.js version matches (20.x)
3. Clear caches and rebuild:
   ```bash
   dotnet clean
   dotnet restore
   npm ci --cache .npm
   ```

### Security Scan Failures

1. Review security scan output
2. Update vulnerable packages
3. If false positive, add to ignore list

### Auto-Generated CI Failure Issues

The `auto-issue-management.yml` workflow automatically creates issues when CI fails:

**What to do when you receive a CI failure issue:**

1. 📋 **Check the issue** - It contains a direct link to the failed workflow run
2. 🔍 **Review the logs** - Click the workflow run link and examine the failure
3. 🛠️ **Fix the issue** - Make the necessary code changes
4. ✅ **Push your fix** - The CI will run again
5. 🎉 **Automatic closure** - If CI passes, the issue closes automatically

**Issue Labels:**
- `ci-failure` - Identifies auto-generated CI failure issues
- `branch:branch-name` - Shows which branch failed
- `bug` - Categorizes as a bug
- `automated` - Indicates automated creation

**Manual Closure:**
If you want to close an issue without fixing (e.g., abandoned branch):
- Just delete the branch or close the PR - the issue closes automatically
- Or manually close the issue with a comment explaining why

**Viewing All CI Failures:**
Filter issues by label: `label:ci-failure` to see all current CI failures

## Workflow Secrets

Required secrets (configure in Settings → Secrets):
- `GITHUB_TOKEN` - Automatically provided by GitHub
- `CODECOV_TOKEN` - For code coverage uploads (optional)

## Manual Workflow Triggers

Some workflows support manual triggering:

```bash
# Trigger CI workflow manually
gh workflow run ci-main.yml
```

## Questions?

- GitHub Actions: https://docs.github.com/en/actions
- Dependabot: https://docs.github.com/en/code-security/dependabot
- Code Owners: https://docs.github.com/en/repositories/managing-your-repositorys-settings-and-features/customizing-your-repository/about-code-owners
