# Job Scheduler Worker

Enterprise-grade Job Scheduling and Execution Platform built with .NET 8

## Overview

A highly-reliable, auditable, and enterprise-ready job scheduling and execution platform designed for internal engineering teams. Built following Clean Architecture principles and SOLID design patterns.

## Features

### Scheduling
- **Recurring Schedules**: Daily, Weekly, Monthly, Cron expressions
- **Business Day Rules**: Execute on Nth business day of month with holiday awareness
- **One-Time Executions**: Run once at specific time or after delay
- **Conditional Schedules**: Run based on custom conditions

### Execution Control
- **Timeout Management**: Per-job timeout configuration
- **Retry Policies**: Linear, Exponential, Exponential with Jitter
- **Concurrency Control**: Job-level and global throttling
- **Execution Isolation**: In-process, out-of-process, and container execution modes
- **Distributed Locking**: Redis-based distributed locks for multi-instance deployments

### Notifications
- **Email**: SMTP notifications on job events
- **Microsoft Teams**: Adaptive cards via webhook
- **Slack**: Webhook notifications
- **Custom Webhooks**: HTTP callbacks to custom endpoints
- **Trigger Events**: OnStart, OnRetry, OnSuccess, OnFailure, OnTimeout, OnSkip, OnCancel

### Governance & Audit
- **Complete Audit Trail**: Immutable audit logs for all operations
- **Ownership Tracking**: Job owner, team, escalation contacts
- **Environment Controls**: DEV/HOM/PROD environment restrictions
- **Parameter Schemas**: Typed, versioned parameter definitions
- **Version Control**: Job definition versioning

### Observability
- **OpenTelemetry**: Distributed tracing with correlation IDs
- **Serilog**: Structured logging to Elasticsearch
- **Execution Metrics**: Duration, success rate, retry counts
- **Health Checks**: Live, ready, and job-specific health endpoints
- **Dashboard**: Real-time monitoring and analytics

### Dependency Management
- **Job Dependencies**: DAG (Directed Acyclic Graph) scheduling
- **Parallel Execution**: Support for parallel branches
- **Fail-Fast Semantics**: Stop dependent jobs on failure
- **Delay Configuration**: Configurable delays between dependent jobs

## Architecture

### Clean Architecture Layers

```
┌─────────────────────────────────────────────────────┐
│               Presentation Layer                     │
│         (API, Worker Service)                        │
├─────────────────────────────────────────────────────┤
│            Application Layer                         │
│    (Commands, Queries, Handlers, DTOs)              │
├─────────────────────────────────────────────────────┤
│              Domain Layer                            │
│   (Entities, Value Objects, Enums)                  │
├─────────────────────────────────────────────────────┤
│           Infrastructure Layer                       │
│  (EF Core, Repositories, Services)                  │
└─────────────────────────────────────────────────────┘
```

### Projects

- **Jobs.Worker.Domain**: Core business entities and logic
- **Jobs.Worker.Application**: Use cases, commands, queries
- **Jobs.Worker.Infrastructure**: Data access, external services
- **Jobs.Worker.Api**: REST API endpoints
- **Jobs.Worker.Worker**: Background worker service

## Prerequisites

- .NET 8 SDK
- SQL Server 2019+ or Azure SQL Database
- Redis (optional, for distributed locking)
- Elasticsearch (optional, for logging)

## Getting Started

### 1. Database Setup

Execute the SQL scripts in order:

```bash
# From the scripts/database directory
sqlcmd -S localhost -U sa -P YourPassword -i 001_CreateTables.sql
sqlcmd -S localhost -U sa -P YourPassword -i 002_CreateStoredProcedures_JobDefinition.sql
sqlcmd -S localhost -U sa -P YourPassword -i 003_CreateStoredProcedures_JobExecution.sql
sqlcmd -S localhost -U sa -P YourPassword -i 004_CreateStoredProcedures_JobSchedule.sql
sqlcmd -S localhost -U sa -P YourPassword -i 005_CreateStoredProcedures_Dashboard.sql
sqlcmd -S localhost -U sa -P YourPassword -i 006_CreateIndexesAndViews.sql
```

### 2. Configuration

Update connection strings in:
- `src/Jobs.Worker.Api/appsettings.json`
- `src/Jobs.Worker.Worker/appsettings.json`

```json
{
  "ConnectionStrings": {
    "JobSchedulerDb": "Server=localhost;Database=JobScheduler;User Id=sa;Password=YourPassword;TrustServerCertificate=True"
  }
}
```

### 3. Build and Run

```bash
# Restore dependencies
dotnet restore

# Build solution
dotnet build

# Run API
cd src/Jobs.Worker.Api
dotnet run

# Run Worker (in separate terminal)
cd src/Jobs.Worker.Worker
dotnet run
```

## API Endpoints

### Jobs

- `GET /api/jobs` - Get all jobs
- `GET /api/jobs/{id}` - Get job by ID
- `POST /api/jobs` - Create new job
- `POST /api/jobs/{id}/trigger` - Manually trigger job
- `PUT /api/jobs/{id}/status` - Update job status (enable/disable)

### Executions

- `GET /api/executions/running` - Get currently running executions
- `GET /api/executions/failed-today` - Get failed executions today
- `GET /api/executions/job/{jobId}` - Get executions for specific job

### Schedules

- `POST /api/schedules` - Create new schedule for job

### Dashboard

- `GET /api/dashboard/stats` - Get dashboard statistics

### Health

- `GET /health/live` - Liveness probe
- `GET /health/ready` - Readiness probe
- `GET /health/jobs` - Job health status

## Database Schema

### Core Tables

- **JobDefinition**: Job configurations and metadata
- **JobSchedule**: Scheduling rules and next execution times
- **JobExecution**: Execution records and results
- **JobExecutionLog**: Detailed execution logs
- **JobParameter**: Job parameters and configurations
- **JobNotification**: Notification configurations
- **JobDependency**: Job dependency relationships (DAG)
- **JobOwnership**: Job ownership and team information
- **JobAudit**: Immutable audit trail

### Stored Procedures

All DML operations (INSERT/UPDATE/DELETE) are performed via stored procedures:

- `usp_JobDefinition_Upsert`: Insert or update job definition
- `usp_JobExecution_Upsert`: Insert or update job execution
- `usp_JobSchedule_Upsert`: Insert or update job schedule
- `usp_Dashboard_GetStats`: Get dashboard statistics
- And many more...

## Dashboard Mockups

Sample JSON responses are available in `docs/mockups/`:

- `dashboard-stats.json` - Dashboard statistics
- `all-jobs.json` - List of all jobs
- `running-executions.json` - Currently running executions
- `failed-executions-today.json` - Failed executions
- `execution-trends.json` - 7-day execution trends
- `top-failing-jobs.json` - Most failing jobs
- `upcoming-jobs.json` - Upcoming scheduled jobs

## Example: Creating a Job

```bash
curl -X POST https://localhost:5001/api/jobs \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Daily Sales Report",
    "description": "Generates daily sales reports",
    "category": "Reporting",
    "allowedEnvironments": 7,
    "executionMode": 1,
    "executionAssembly": "SalesReports.dll",
    "executionTypeName": "SalesReports.DailyReportJob",
    "timeoutSeconds": 300,
    "maxRetries": 3,
    "retryStrategy": 3,
    "baseDelaySeconds": 30,
    "maxConcurrentExecutions": 1,
    "ownerName": "John Smith",
    "ownerEmail": "john.smith@company.com",
    "teamName": "Data Analytics",
    "createdBy": "admin@company.com"
  }'
```

## Example: Creating a Schedule

```bash
curl -X POST https://localhost:5001/api/schedules \
  -H "Content-Type: application/json" \
  -d '{
    "jobDefinitionId": "a1b2c3d4-e5f6-4a5b-8c9d-0e1f2a3b4c5d",
    "scheduleType": 2,
    "timeOfDay": "06:00:00",
    "createdBy": "admin@company.com"
  }'
```

## Monitoring and Observability

### Logs

Logs are written to:
- Console (structured JSON)
- Files (`logs/` directory)
- Elasticsearch (if configured)

### Metrics

Key metrics tracked:
- Execution duration
- Success/failure rates
- Retry attempts
- Concurrent executions
- Queue depth

### Traces

OpenTelemetry traces include:
- ExecutionId (unique per execution)
- CorrelationId (links related operations)
- TraceId (distributed tracing)

## Security

- **Authentication**: Integrate with your identity provider
- **Authorization**: Role-based access control (implement as needed)
- **Secrets**: Use Azure Key Vault or environment variables
- **Encryption**: Parameter values can be encrypted
- **Audit**: All operations are logged

## Deployment

### Docker

Build and run with Docker:

```bash
# Build API
docker build -f src/Jobs.Worker.Api/Dockerfile -t jobs-worker-api .

# Build Worker
docker build -f src/Jobs.Worker.Worker/Dockerfile -t jobs-worker .

# Run with Docker Compose (see docker-compose.yml)
docker-compose up -d
```

### Kubernetes

Helm charts available in `deploy/kubernetes/` (to be created).

## Performance

- Handles 1000+ jobs with complex schedules
- Sub-second scheduling resolution
- Horizontal scaling with distributed locks
- Optimized database indexes for fast queries

## Contributing

1. Follow Clean Architecture principles
2. Write unit tests for new features
3. Update stored procedures for data operations
4. Add audit logging for all changes
5. Document API changes

## License

Internal use only - Proprietary

## Support

For issues or questions:
- Email: platform-team@company.com
- Slack: #job-scheduler-support
- Wiki: https://wiki.company.com/job-scheduler

---

**Built with .NET 8 | Clean Architecture | Enterprise-Ready**
