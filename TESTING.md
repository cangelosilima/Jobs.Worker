# Testing Guide

This document provides comprehensive information about testing the Job Scheduler Worker system, including unit tests, integration tests, and architecture tests for both backend and frontend.

## Table of Contents

- [Backend Tests (.NET)](#backend-tests-net)
  - [Unit Tests](#backend-unit-tests)
  - [Integration Tests](#backend-integration-tests)
  - [Architecture Tests](#backend-architecture-tests)
- [Frontend Tests (React/TypeScript)](#frontend-tests-reacttypescript)
  - [Unit Tests](#frontend-unit-tests)
  - [Integration Tests](#frontend-integration-tests)
  - [Architecture Tests](#frontend-architecture-tests)
- [Running Tests](#running-tests)
- [Test Coverage](#test-coverage)
- [CI/CD Integration](#cicd-integration)

---

## Backend Tests (.NET)

The backend uses **xUnit**, **FluentAssertions**, **Moq**, and **NetArchTest** for testing.

### Project Structure

```
tests/
├── Jobs.Worker.Domain.Tests/
│   ├── Entities/
│   │   └── JobDefinitionTests.cs
│   └── ValueObjects/
│       └── RetryPolicyTests.cs
├── Jobs.Worker.Application.Tests/
│   └── Handlers/
│       └── GetDashboardStatsQueryHandlerTests.cs
├── Jobs.Worker.Api.Tests/
│   └── Integration/
│       └── JobsApiTests.cs
└── Jobs.Worker.ArchitectureTests/
    └── ArchitectureTests.cs
```

### Backend Unit Tests

#### Domain Layer Tests

Tests for entities, value objects, and business logic:

**JobDefinitionTests.cs**
- ✅ Initial state verification
- ✅ State transitions (Activate, Disable, Archive)
- ✅ Retry policy configuration
- ✅ Concurrency settings
- ✅ Version incrementation

**RetryPolicyTests.cs**
- ✅ Linear retry strategy
- ✅ Exponential backoff calculation
- ✅ Exponential with jitter
- ✅ Input validation
- ✅ Retry limit enforcement

#### Application Layer Tests

Tests for handlers using Moq for mocking dependencies:

**GetDashboardStatsQueryHandlerTests.cs**
- ✅ Stats calculation
- ✅ Success rate computation
- ✅ Repository interactions
- ✅ Edge cases (no data, single job)

**Run Domain Tests:**
```bash
cd tests/Jobs.Worker.Domain.Tests
dotnet test

# With coverage
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=opencover
```

**Run Application Tests:**
```bash
cd tests/Jobs.Worker.Application.Tests
dotnet test
```

### Backend Integration Tests

Integration tests using **WebApplicationFactory** and **In-Memory Database**:

**JobsApiTests.cs**
- ✅ GET /api/jobs endpoint
- ✅ POST /api/jobs (create job)
- ✅ GET /api/jobs/{id} (not found scenario)
- ✅ Health check endpoints

**Run Integration Tests:**
```bash
cd tests/Jobs.Worker.Api.Tests
dotnet test
```

### Backend Architecture Tests

Tests using **NetArchTest.Rules** to enforce clean architecture:

**ArchitectureTests.cs**
- ✅ Domain doesn't depend on Application
- ✅ Domain doesn't depend on Infrastructure
- ✅ Application doesn't depend on Infrastructure
- ✅ Handlers are in Application layer
- ✅ Repositories are in Infrastructure layer
- ✅ Value objects are immutable
- ✅ Interfaces start with 'I'

**Run Architecture Tests:**
```bash
cd tests/Jobs.Worker.ArchitectureTests
dotnet test
```

### Run All Backend Tests

```bash
# From project root
dotnet test

# With detailed output
dotnet test --logger "console;verbosity=detailed"

# Generate coverage report
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=html
```

---

## Frontend Tests (React/TypeScript)

The frontend uses **Vitest**, **React Testing Library**, **MSW**, and custom architecture tests.

### Project Structure

```
frontend/src/
├── state/__tests__/
│   ├── auth.store.test.ts
│   └── notification.store.test.ts
├── modules/dashboard/components/__tests__/
│   └── StatsCard.test.tsx
├── api/__tests__/
│   └── jobs.api.test.ts
└── test/
    ├── setup.ts
    └── architecture.test.ts
```

### Frontend Unit Tests

#### Store Tests

**auth.store.test.ts**
- ✅ Default user initialization
- ✅ Role checks (hasRole, hasAnyRole)
- ✅ Permission checks (canEdit, canDelete, canTrigger)
- ✅ Login/logout functionality

**notification.store.test.ts**
- ✅ Adding notifications
- ✅ Marking as read
- ✅ Removing notifications
- ✅ Clear all functionality
- ✅ Notification limit (100 max)

#### Component Tests

**StatsCard.test.tsx**
- ✅ Rendering title and value
- ✅ Number formatting
- ✅ Icon rendering
- ✅ Color application

**Run Unit Tests:**
```bash
cd frontend

# Run all tests
npm test

# Run tests in watch mode
npm test -- --watch

# Run specific test file
npm test auth.store.test.ts
```

### Frontend Integration Tests

API integration tests using **MSW (Mock Service Worker)**:

**jobs.api.test.ts**
- ✅ Dashboard stats fetching
- ✅ Jobs API calls
- ✅ Executions API calls
- ✅ Error handling (404, 500)

**Run Integration Tests:**
```bash
cd frontend
npm test -- api
```

### Frontend Architecture Tests

Custom architecture tests checking import rules and module structure:

**architecture.test.ts**
- ✅ No relative imports using ../..
- ✅ Using @ alias for absolute imports
- ✅ Test files in proper locations
- ✅ Modules don't import from other modules
- ✅ API layer doesn't import UI components
- ✅ Services don't import UI components
- ✅ State stores don't import UI components

**Run Architecture Tests:**
```bash
cd frontend
npm test -- architecture
```

### Frontend Test Scripts

```bash
# Run all tests
npm test

# Run tests with UI
npm run test:ui

# Run tests with coverage
npm run test:coverage

# Watch mode
npm test -- --watch

# Run specific test pattern
npm test -- dashboard
```

---

## Running Tests

### Backend Tests (All)

```bash
# From project root
dotnet test

# Specific project
dotnet test tests/Jobs.Worker.Domain.Tests/

# With coverage
dotnet test /p:CollectCoverage=true

# Generate HTML coverage report
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=html

# Filter by test name
dotnet test --filter "FullyQualifiedName~JobDefinition"
```

### Frontend Tests (All)

```bash
cd frontend

# Run all tests once
npm test

# Watch mode (re-run on file changes)
npm test -- --watch

# With coverage
npm run test:coverage

# UI mode (interactive browser UI)
npm run test:ui

# Specific file
npm test auth.store.test.ts

# Pattern matching
npm test -- dashboard
```

---

## Test Coverage

### Backend Coverage

```bash
# Generate coverage for all projects
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=lcov

# View coverage report (requires reportgenerator)
dotnet tool install -g dotnet-reportgenerator-globaltool
reportgenerator -reports:./tests/**/coverage.info -targetdir:./coverage-report
```

**Coverage Targets:**
- Domain Layer: > 80%
- Application Layer: > 75%
- Infrastructure Layer: > 60%
- API Layer: > 70%

### Frontend Coverage

```bash
cd frontend
npm run test:coverage
```

Coverage report will be generated in `frontend/coverage/` directory.

**Coverage Targets:**
- Stores: > 90%
- Components: > 75%
- Services: > 80%
- API Client: > 85%

**View Coverage Report:**
```bash
# Open HTML report
open frontend/coverage/index.html  # macOS
xdg-open frontend/coverage/index.html  # Linux
start frontend/coverage/index.html  # Windows
```

---

## CI/CD Integration

### GitHub Actions Example

```yaml
name: Tests

on: [push, pull_request]

jobs:
  backend-tests:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3
      - name: Setup .NET
        uses: actions/setup-dotnet@v3
        with:
          dotnet-version: '8.0.x'
      - name: Run tests
        run: dotnet test
      - name: Generate coverage
        run: dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=lcov
      - name: Upload coverage
        uses: coverallsapp/github-action@v2
        with:
          github-token: ${{ secrets.GITHUB_TOKEN }}
          file: ./tests/**/coverage.info

  frontend-tests:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3
      - name: Setup Node
        uses: actions/setup-node@v3
        with:
          node-version: '18'
      - name: Install dependencies
        run: cd frontend && npm install
      - name: Run tests
        run: cd frontend && npm run test:coverage
      - name: Upload coverage
        uses: codecov/codecov-action@v3
        with:
          directory: ./frontend/coverage
```

---

## Best Practices

### Backend

1. **Arrange-Act-Assert (AAA)** pattern
2. **One assertion per test** (when possible)
3. **Descriptive test names** (use underscores)
4. **Mock external dependencies**
5. **Test edge cases and error scenarios**
6. **Avoid test interdependencies**

### Frontend

1. **Test user behavior**, not implementation
2. **Query by accessibility** (role, label, text)
3. **Mock external APIs** with MSW
4. **Test component integration**, not just units
5. **Avoid testing implementation details**
6. **Use data-testid sparingly**

---

## Troubleshooting

### Backend

**Issue:** Tests fail with database connection errors
```bash
# Ensure using InMemory database for tests
services.AddDbContext<JobSchedulerDbContext>(options =>
    options.UseInMemoryDatabase("TestDatabase"));
```

**Issue:** Flaky tests due to DateTime
```csharp
// Use deterministic dates in tests
var fixedDate = new DateTime(2024, 1, 1, 12, 0, 0, DateTimeKind.Utc);
```

### Frontend

**Issue:** `window.matchMedia is not a function`
```ts
// Already handled in src/test/setup.ts
// Ensure setup file is loaded in vitest.config.ts
```

**Issue:** Material UI tests failing
```bash
# Install @mui/material as devDependency if needed
npm install --save-dev @mui/material
```

**Issue:** MSW not intercepting requests
```ts
// Ensure server is started before tests
beforeAll(() => server.listen())
afterEach(() => server.resetHandlers())
afterAll(() => server.close())
```

---

## Contributing

When adding new features:

1. ✅ Write tests **before** implementation (TDD)
2. ✅ Ensure **all tests pass** before committing
3. ✅ Maintain **coverage thresholds**
4. ✅ Update this documentation if adding new test types
5. ✅ Follow existing test patterns and conventions

---

## Resources

- [xUnit Documentation](https://xunit.net/)
- [FluentAssertions Documentation](https://fluentassertions.com/)
- [Moq Documentation](https://github.com/moq/moq4)
- [NetArchTest Documentation](https://github.com/BenMorris/NetArchTest)
- [Vitest Documentation](https://vitest.dev/)
- [React Testing Library](https://testing-library.com/react)
- [MSW Documentation](https://mswjs.io/)
