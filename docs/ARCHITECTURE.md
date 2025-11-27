# Job Scheduler Worker - Architecture Documentation

## System Architecture

### High-Level Overview

The Job Scheduler Worker is built using **Clean Architecture** principles, ensuring separation of concerns, testability, and maintainability.

```
┌──────────────────────────────────────────────────────────────┐
│                         Clients                               │
│              (UI, CLI, Other Services)                        │
└────────────────────────┬─────────────────────────────────────┘
                         │
                         ▼
┌──────────────────────────────────────────────────────────────┐
│                    API Gateway / LB                           │
└────────────────────────┬─────────────────────────────────────┘
                         │
         ┌───────────────┴───────────────┐
         ▼                               ▼
┌─────────────────┐            ┌─────────────────┐
│   API Layer     │            │  Worker Layer   │
│  (REST APIs)    │            │ (Job Execution) │
└────────┬────────┘            └────────┬────────┘
         │                               │
         └───────────────┬───────────────┘
                         ▼
         ┌───────────────────────────────┐
         │   Application Layer           │
         │  (Commands, Queries)          │
         └───────────────┬───────────────┘
                         │
         ┌───────────────┴───────────────┐
         ▼                               ▼
┌─────────────────┐            ┌─────────────────┐
│  Domain Layer   │            │ Infrastructure  │
│   (Entities)    │            │  (Persistence)  │
└─────────────────┘            └────────┬────────┘
                                        │
                    ┌───────────────────┼───────────────────┐
                    ▼                   ▼                   ▼
            ┌──────────────┐   ┌──────────────┐   ┌──────────────┐
            │  SQL Server  │   │    Redis     │   │ Elasticsearch│
            └──────────────┘   └──────────────┘   └──────────────┘
```

## Layer Responsibilities

### 1. Domain Layer (Core)

**Purpose**: Contains all business logic and domain entities

**Components**:
- **Entities**: JobDefinition, JobExecution, JobSchedule, etc.
- **Value Objects**: RetryPolicy, ScheduleRule, NotificationRule
- **Enums**: JobStatus, ExecutionStatus, ScheduleType

**Key Principles**:
- No external dependencies
- Pure business logic
- Framework-agnostic
- Highly testable

### 2. Application Layer

**Purpose**: Orchestrates use cases and business workflows

**Components**:
- **Commands**: CreateJobCommand, TriggerJobCommand
- **Queries**: GetDashboardStatsQuery, GetRunningExecutionsQuery
- **Handlers**: MediatR handlers for commands and queries
- **DTOs**: Data Transfer Objects for API responses
- **Interfaces**: Repository and service contracts

**Key Principles**:
- CQRS pattern (Command Query Responsibility Segregation)
- MediatR for request/response pipeline
- FluentValidation for input validation
- No infrastructure concerns

### 3. Infrastructure Layer

**Purpose**: Implements external concerns (database, messaging, etc.)

**Components**:
- **Persistence**: EF Core DbContext, repositories
- **Services**: ScheduleCalculator, DistributedLockService
- **External Integrations**: Notifications, logging

**Key Patterns**:
- Repository pattern
- Unit of Work pattern
- Dependency Inversion

### 4. Presentation Layer

#### API Layer
- Minimal APIs with ASP.NET Core 8
- Swagger/OpenAPI documentation
- Health check endpoints
- MediatR integration

#### Worker Layer
- Background service for job execution
- Schedule monitoring
- Queue processing
- Retry handling

## Data Flow

### Job Creation Flow

```
1. Client → POST /api/jobs
2. API → CreateJobCommand
3. Handler → Validate input
4. Handler → Create JobDefinition entity
5. Handler → Save to repository
6. Repository → Call usp_JobDefinition_Upsert
7. Database → Insert record
8. Database → Create audit entry
9. Handler → Return JobId
10. API → Return 201 Created
```

### Job Execution Flow

```
1. Worker → Check due schedules (every 10s)
2. Worker → Query usp_JobSchedule_GetDue
3. Worker → Acquire distributed lock
4. Worker → Create JobExecution entity
5. Worker → Save to repository
6. Worker → Start execution in background
7. Executor → Load job implementation
8. Executor → Execute with timeout
9. Executor → Update execution status
10. Executor → Release lock
11. Executor → Send notifications
```

## Scheduling Engine

### Schedule Resolution

The scheduling engine uses a pull-based model:

1. **Scheduler Worker** runs continuously
2. Every 10 seconds, queries for due schedules
3. For each due schedule:
   - Acquire distributed lock
   - Check concurrency limits
   - Create execution record
   - Calculate next execution time
   - Update schedule
   - Release lock

### Schedule Types

#### Daily
```csharp
Rule = ScheduleRule.CreateDaily(new TimeSpan(6, 0, 0))
// Runs every day at 6:00 AM
```

#### Weekly
```csharp
Rule = ScheduleRule.CreateWeekly(
    DayOfWeekFlags.Monday | DayOfWeekFlags.Friday,
    new TimeSpan(9, 0, 0)
)
// Runs Monday and Friday at 9:00 AM
```

#### Cron
```csharp
Rule = ScheduleRule.CreateCron("0 */4 * * *")
// Runs every 4 hours
```

#### Business Day
```csharp
Rule = ScheduleRule.CreateMonthlyBusinessDay(
    10,
    new TimeSpan(18, 0, 0),
    adjustToPrevious: true
)
// Runs on 10th business day at 6:00 PM
```

## Execution Engine

### Execution Lifecycle

```
Queued → Running → [Succeeded | Failed | TimedOut | Cancelled]
                        ↓
                     Retrying (if configured)
                        ↓
                     Running (retry)
```

### Isolation Strategies

#### 1. In-Process
- Runs in same process as worker
- Fast, low overhead
- Risk: Can crash worker
- Use for: Trusted, lightweight jobs

#### 2. Out-of-Process
- Runs as separate process
- Isolated memory space
- Use for: Untrusted code

#### 3. Container
- Runs in Docker container
- Complete isolation
- Use for: Resource-intensive jobs

### Retry Strategies

#### Linear
```
Attempt 1: Wait 30s
Attempt 2: Wait 60s
Attempt 3: Wait 90s
```

#### Exponential
```
Attempt 1: Wait 30s
Attempt 2: Wait 60s
Attempt 3: Wait 120s
```

#### Exponential with Jitter
```
Attempt 1: Wait 15-45s (random)
Attempt 2: Wait 30-90s (random)
Attempt 3: Wait 60-180s (random)
```

## Distributed Architecture

### Multi-Instance Deployment

The system supports horizontal scaling with multiple worker instances:

```
┌─────────────┐    ┌─────────────┐    ┌─────────────┐
│  Worker 1   │    │  Worker 2   │    │  Worker 3   │
└──────┬──────┘    └──────┬──────┘    └──────┬──────┘
       │                  │                  │
       └──────────────────┼──────────────────┘
                          │
                    ┌─────▼──────┐
                    │   Redis    │
                    │ (Locking)  │
                    └────────────┘
```

### Distributed Locking

Uses Redis for distributed locks:
- Lock key: `schedule:{scheduleId}`
- Lock duration: 5 minutes
- Lock owner: Host instance name
- Prevents duplicate execution

### Lock Acquisition Flow

```csharp
if (await lockService.TryAcquireLockAsync(lockKey, hostInstance, TimeSpan.FromMinutes(5)))
{
    try
    {
        // Process schedule
        // Create execution
        // Update next run time
    }
    finally
    {
        await lockService.ReleaseLockAsync(lockKey, hostInstance);
    }
}
```

## Database Architecture

### Table Relationships

```
JobDefinition (1) ──┬──< (N) JobSchedule
                    │
                    ├──< (N) JobParameter
                    │
                    ├──< (N) JobNotification
                    │
                    ├──< (N) JobExecution
                    │         │
                    │         └──< (N) JobExecutionLog
                    │
                    ├──< (N) JobDependency
                    │
                    └──< (1) JobOwnership

JobAudit (independent audit table)
```

### Stored Procedure Architecture

All data modifications go through stored procedures:

**Benefits**:
- Consistent business logic
- Automatic audit logging
- Better performance (query plan caching)
- Security (grant execute only)
- Version control

**Upsert Pattern**:
```sql
IF EXISTS (SELECT 1 FROM Table WHERE Id = @Id)
    UPDATE Table SET ...
ELSE
    INSERT INTO Table ...
```

### Indexing Strategy

**High-Selectivity Indexes**:
- Primary keys (clustered)
- Foreign keys
- Status + timestamp columns

**Filtered Indexes**:
- Active schedules only
- Running executions only

**Covering Indexes**:
- Include frequently queried columns

## Observability

### Logging

**Structured Logging with Serilog**:
```csharp
_logger.LogInformation(
    "Starting execution {ExecutionId} for job {JobName}",
    executionId,
    jobName
);
```

**Log Sinks**:
- Console (JSON format)
- File (rolling daily)
- Elasticsearch (searchable)

### Tracing

**OpenTelemetry Integration**:
- ExecutionId: Unique per execution
- CorrelationId: Links related executions
- TraceId: Distributed tracing ID

### Metrics

**Key Metrics**:
- Jobs per status (active, disabled)
- Executions per status (running, failed)
- Success rate percentage
- Average execution duration
- Queue depth
- Retry counts

## Security Considerations

### Authentication
- Integrate with Azure AD / OAuth
- API key support for service accounts

### Authorization
- Role-based access control
- Job-level permissions
- Environment restrictions

### Data Protection
- Encrypted parameters
- Sensitive data masking in logs
- SQL injection prevention (parameterized queries)

### Audit
- Every operation logged
- Immutable audit table
- User tracking (who, when, what)

## Performance Optimization

### Database
- Optimized indexes
- Stored procedure execution plans
- Connection pooling
- Async operations

### Caching
- Redis for distributed cache
- In-memory cache for reference data

### Async/Await
- Non-blocking I/O throughout
- Parallel execution where possible

### Batch Processing
- Bulk inserts for logs
- Batch notification sending

## Failure Handling

### Transient Failures
- Automatic retry with backoff
- Exponential backoff with jitter
- Circuit breaker pattern

### Permanent Failures
- Dead letter queue
- Manual intervention required
- Escalation to job owner

### System Failures
- Health checks
- Auto-restart containers
- Graceful shutdown

## Scaling Considerations

### Horizontal Scaling
- Multiple worker instances
- Distributed locking
- Stateless design

### Vertical Scaling
- CPU for compute-intensive jobs
- Memory for data-intensive jobs
- I/O for database operations

### Database Scaling
- Read replicas for queries
- Write master for updates
- Table partitioning for large tables

---

**Version**: 1.0
**Last Updated**: 2025-11-27
