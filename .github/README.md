# GitHub Workflows & CI/CD

This directory contains GitHub Actions workflows and repository configuration for the Jobs.Worker project.

## Workflows

### 1. Push Validation (`push-validation.yml`) 🆕

**Triggered on:** Every push to any branch (excluding documentation)

**Purpose:** Ensures all code pushed to the repository passes basic quality checks, even on feature branches.

**Jobs:**
- **Quick Build & Test Validation**
  - Fast validation of both backend and frontend
  - Builds .NET solution
  - Runs all backend tests with minimal verbosity
  - Builds React application
  - Runs all frontend tests
  - Provides clear success/failure summary

- **Block If Validation Fails**
  - Explicitly fails the workflow if validation doesn't pass
  - Provides helpful error message with local testing commands
  - Prevents broken code from being pushed

**Why this matters:**
- Catches errors immediately after push (not just on PR)
- Validates feature branches continuously
- Ensures Claude's pushes (and everyone's) meet quality standards
- Fast feedback loop for developers

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

## Workflow Hierarchy

```
Push to any branch
    ↓
[Push Validation] ← Runs immediately on push
    ↓
Create Pull Request
    ↓
[PR Validation] ← Comprehensive checks
    ↓
Merge to main
    ↓
[CI Main] ← Release automation
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
