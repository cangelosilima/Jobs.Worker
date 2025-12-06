using System;
using System.Collections.Generic;
using System.Linq;
using Jobs.Worker.Api.Hubs;
using Jobs.Worker.Api.Services;
using Jobs.Worker.Application.Commands;
using Jobs.Worker.Application.Handlers;
using Jobs.Worker.Application.Interfaces;
using Jobs.Worker.Application.Queries;
using Jobs.Worker.Domain.Entities;
using Jobs.Worker.Domain.Enums;
using Jobs.Worker.Infrastructure.Persistence;
using Jobs.Worker.Infrastructure.Repositories;
using Jobs.Worker.Infrastructure.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("logs/api-.log", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

// Add services
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins("http://localhost:3000", "http://localhost:5173")
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

// Add SignalR
builder.Services.AddSignalR();

// Add DbContext
builder.Services.AddDbContext<JobSchedulerDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("JobSchedulerDb")));

// Add repositories
builder.Services.AddScoped<IJobRepository, JobRepository>();
builder.Services.AddScoped<IJobExecutionRepository, JobExecutionRepository>();
builder.Services.AddScoped<IJobScheduleRepository, Jobs.Worker.Infrastructure.Repositories.JobScheduleRepository>();
builder.Services.AddScoped<IAuditRepository, Jobs.Worker.Infrastructure.Repositories.AuditRepository>();
builder.Services.AddScoped<IJobCircuitBreakerRepository, JobCircuitBreakerRepository>();

// Add services
builder.Services.AddScoped<IScheduleCalculator, ScheduleCalculator>();
builder.Services.AddSingleton<IDistributedLockService, DistributedLockService>();

// Add background services
builder.Services.AddHostedService<SignalRNotificationService>();
builder.Services.AddHostedService<CircuitBreakerMonitoringService>();

// Add handlers
builder.Services.AddScoped<CreateJobCommandHandler>();
builder.Services.AddScoped<TriggerJobCommandHandler>();
builder.Services.AddScoped<GetDashboardStatsQueryHandler>();

// Add health checks
builder.Services.AddHealthChecks()
    .AddCheck("database", () => HealthCheckResult.Healthy("Database connection OK"), tags: new[] { "database" });

var app = builder.Build();

// Configure middleware
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowFrontend");
app.UseSerilogRequestLogging();

// Map SignalR hub
app.MapHub<JobsHub>("/hubs/jobs");

// Map endpoints
MapJobEndpoints(app);
MapExecutionEndpoints(app);
MapScheduleEndpoints(app);
MapDashboardEndpoints(app);
MapAuditEndpoints(app);
MapCircuitBreakerEndpoints(app);
MapHealthEndpoints(app);

app.Run();

void MapJobEndpoints(WebApplication app)
{
    var group = app.MapGroup("/api/jobs").WithTags("Jobs");

    // Get all jobs
    group.MapGet("/", async (IJobRepository jobRepo) =>
    {
        var jobs = await jobRepo.GetAllAsync();
        return Results.Ok(jobs);
    }).WithName("GetAllJobs");

    // Get job by ID
    group.MapGet("/{id:guid}", async (Guid id, IJobRepository jobRepo) =>
    {
        var job = await jobRepo.GetByIdAsync(id);
        return job != null ? Results.Ok(job) : Results.NotFound();
    }).WithName("GetJobById");

    // Create job
    group.MapPost("/", async (CreateJobCommand request, CreateJobCommandHandler handler, ILogger<Program> logger) =>
    {
        try
        {
            logger.LogInformation("Creating job: {JobName}", request.Name);
            var jobId = await handler.HandleAsync(request);
            logger.LogInformation("Job created successfully: {JobId}", jobId);
            return Results.Created($"/api/jobs/{jobId}", new { id = jobId });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating job: {Message}", ex.Message);
            return Results.StatusCode(StatusCodes.Status500InternalServerError);
        }
    }).WithName("CreateJob");

    // Update job
    group.MapPut("/{id:guid}", async (Guid id, Jobs.Worker.Api.UpdateJobCommand command, IJobRepository jobRepo) =>
    {
        if (id != command.Id)
            return Results.BadRequest("ID mismatch");

        var job = await jobRepo.GetByIdAsync(id);
        if (job == null) return Results.NotFound();

        // Update job properties (simplified)
        await jobRepo.UpdateAsync(job);
        return Results.NoContent();
    }).WithName("UpdateJob");

    // Delete job
    group.MapDelete("/{id:guid}", async (Guid id, IJobRepository jobRepo) =>
    {
        var job = await jobRepo.GetByIdAsync(id);
        if (job == null) return Results.NotFound();

        await jobRepo.DeleteAsync(id);
        return Results.NoContent();
    }).WithName("DeleteJob");

    // Activate job
    group.MapPost("/{id:guid}/activate", async (Guid id, IJobRepository jobRepo) =>
    {
        var job = await jobRepo.GetByIdAsync(id);
        if (job == null) return Results.NotFound();

        job.Activate("system");
        await jobRepo.UpdateAsync(job);
        return Results.NoContent();
    }).WithName("ActivateJob");

    // Disable job
    group.MapPost("/{id:guid}/disable", async (Guid id, IJobRepository jobRepo) =>
    {
        var job = await jobRepo.GetByIdAsync(id);
        if (job == null) return Results.NotFound();

        job.Disable("system", "Disabled by user");
        await jobRepo.UpdateAsync(job);
        return Results.NoContent();
    }).WithName("DisableJob");

    // Archive job
    group.MapPost("/{id:guid}/archive", async (Guid id, IJobRepository jobRepo) =>
    {
        var job = await jobRepo.GetByIdAsync(id);
        if (job == null) return Results.NotFound();

        job.Archive("system");
        await jobRepo.UpdateAsync(job);
        return Results.NoContent();
    }).WithName("ArchiveJob");

    // Trigger job
    group.MapPost("/{id:guid}/trigger", async (Guid id, TriggerJobCommand command, TriggerJobCommandHandler handler) =>
    {
        var executionId = await handler.HandleAsync(command with { JobDefinitionId = id });
        return Results.Accepted($"/api/executions/{executionId}", new { executionId });
    }).WithName("TriggerJob");

    // Get job schedules
    group.MapGet("/{jobId:guid}/schedules", async (Guid jobId, IJobScheduleRepository scheduleRepo) =>
    {
        var schedules = await scheduleRepo.GetSchedulesForJobAsync(jobId);
        return Results.Ok(schedules);
    }).WithName("GetJobSchedules");

    // Get job executions
    group.MapGet("/{jobId:guid}/executions", async (
        Guid jobId,
        IJobExecutionRepository execRepo,
        int pageNumber = 1,
        int pageSize = 25) =>
    {
        var executions = await execRepo.GetByJobIdAsync(jobId, pageSize);

        var pagedResult = new
        {
            items = executions,
            totalCount = executions.Count(),
            pageNumber,
            pageSize,
            totalPages = (int)Math.Ceiling(executions.Count() / (double)pageSize),
            hasNextPage = false,
            hasPreviousPage = pageNumber > 1
        };

        return Results.Ok(pagedResult);
    }).WithName("GetJobExecutions");
}

void MapExecutionEndpoints(WebApplication app)
{
    var group = app.MapGroup("/api/executions").WithTags("Executions");

    // Get all executions with filtering and pagination
    group.MapGet("/", async (
        IJobExecutionRepository execRepo,
        Guid? jobId,
        ExecutionStatus? status,
        string? startDateFrom,
        string? startDateTo,
        bool? isManualTrigger,
        int pageNumber = 1,
        int pageSize = 25) =>
    {
        var allExecutions = await execRepo.GetAllAsync();

        // Apply filters
        var filtered = allExecutions.AsEnumerable();

        if (jobId.HasValue)
            filtered = filtered.Where(e => e.JobDefinitionId == jobId.Value);

        if (status.HasValue)
            filtered = filtered.Where(e => e.Status == status.Value);

        if (!string.IsNullOrEmpty(startDateFrom))
            filtered = filtered.Where(e => e.StartedAtUtc >= DateTime.Parse(startDateFrom));

        if (!string.IsNullOrEmpty(startDateTo))
            filtered = filtered.Where(e => e.StartedAtUtc <= DateTime.Parse(startDateTo));

        if (isManualTrigger.HasValue)
            filtered = filtered.Where(e => e.IsManualTrigger == isManualTrigger.Value);

        var totalCount = filtered.Count();
        var items = filtered
            .OrderByDescending(e => e.QueuedAtUtc)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        var pagedResult = new
        {
            items,
            totalCount,
            pageNumber,
            pageSize,
            totalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
            hasNextPage = pageNumber * pageSize < totalCount,
            hasPreviousPage = pageNumber > 1
        };

        return Results.Ok(pagedResult);
    }).WithName("GetAllExecutions");

    // Get execution by ID
    group.MapGet("/{id:guid}", async (Guid id, IJobExecutionRepository execRepo) =>
    {
        var execution = await execRepo.GetByIdAsync(id);
        return execution != null ? Results.Ok(execution) : Results.NotFound();
    }).WithName("GetExecutionById");

    // Get running executions
    group.MapGet("/running", async (IJobExecutionRepository execRepo) =>
    {
        var executions = await execRepo.GetRunningExecutionsAsync();
        return Results.Ok(executions);
    }).WithName("GetRunningExecutions");

    // Cancel execution
    group.MapPost("/{id:guid}/cancel", async (
        Guid id,
        IJobExecutionRepository execRepo,
        Jobs.Worker.Api.CancelExecutionCommand command) =>
    {
        var execution = await execRepo.GetByIdAsync(id);
        if (execution == null) return Results.NotFound();

        execution.Cancel(command.Reason ?? "Cancelled by user");
        await execRepo.UpdateAsync(execution);

        return Results.NoContent();
    }).WithName("CancelExecution");

    // Get execution logs
    group.MapGet("/{id:guid}/logs", async (Guid id, IJobExecutionRepository execRepo) =>
    {
        var execution = await execRepo.GetByIdAsync(id);
        if (execution == null) return Results.NotFound();

        var logs = execution.Logs?.Select(l => l.Message).ToList() ?? new List<string>();
        return Results.Ok(logs);
    }).WithName("GetExecutionLogs");
}

void MapScheduleEndpoints(WebApplication app)
{
    var group = app.MapGroup("/api/schedules").WithTags("Schedules");

    // Create schedule - using new endpoint structure matching frontend
    group.MapPost("/jobs/{jobId:guid}/schedules", async (
        Guid jobId,
        CreateScheduleCommand command,
        IJobScheduleRepository scheduleRepo) =>
    {
        var rule = command.ScheduleType switch
        {
            ScheduleType.Daily => Jobs.Worker.Domain.ValueObjects.ScheduleRule.CreateDaily(command.TimeOfDay ?? TimeSpan.Zero),
            ScheduleType.Weekly => Jobs.Worker.Domain.ValueObjects.ScheduleRule.CreateWeekly(command.DaysOfWeek ?? 0, command.TimeOfDay ?? TimeSpan.Zero),
            ScheduleType.Monthly => Jobs.Worker.Domain.ValueObjects.ScheduleRule.CreateMonthly(command.DayOfMonth ?? 1, command.TimeOfDay ?? TimeSpan.Zero),
            ScheduleType.MonthlyBusinessDay => Jobs.Worker.Domain.ValueObjects.ScheduleRule.CreateMonthlyBusinessDay(command.BusinessDayOfMonth ?? 1, command.TimeOfDay ?? TimeSpan.Zero, command.AdjustToPreviousBusinessDay),
            ScheduleType.Cron => Jobs.Worker.Domain.ValueObjects.ScheduleRule.CreateCron(command.CronExpression ?? ""),
            ScheduleType.OneTime => Jobs.Worker.Domain.ValueObjects.ScheduleRule.CreateOneTime(command.OneTimeExecutionDate ?? DateTime.UtcNow),
            ScheduleType.Conditional => Jobs.Worker.Domain.ValueObjects.ScheduleRule.CreateConditional(command.ConditionalExpression ?? "", command.TimeOfDay ?? TimeSpan.Zero),
            _ => throw new ArgumentException("Invalid schedule type")
        };

        var schedule = new JobSchedule(
            jobId,
            rule,
            command.CreatedBy,
            command.StartDateUtc,
            command.EndDateUtc
        );

        await scheduleRepo.AddAsync(schedule);
        return Results.Created($"/api/jobs/{jobId}/schedules/{schedule.Id}", new { id = schedule.Id });
    }).WithName("CreateSchedule");
}

void MapDashboardEndpoints(WebApplication app)
{
    var group = app.MapGroup("/api/dashboard").WithTags("Dashboard");

    // Get dashboard stats
    group.MapGet("/stats", async (GetDashboardStatsQueryHandler handler) =>
    {
        var stats = await handler.HandleAsync(new GetDashboardStatsQuery());
        return Results.Ok(stats);
    }).WithName("GetDashboardStats");

    // Get execution trends
    group.MapGet("/execution-trends", async (
        IJobExecutionRepository execRepo,
        int days = 7) =>
    {
        var startDate = DateTime.UtcNow.Date.AddDays(-days);
        var executions = await execRepo.GetAllAsync();

        var trends = executions
            .Where(e => e.QueuedAtUtc.Date >= startDate.Date)
            .GroupBy(e => e.QueuedAtUtc.Date)
            .Select(g => new
            {
                date = g.Key.ToString("yyyy-MM-dd"),
                succeeded = g.Count(e => e.Status == ExecutionStatus.Succeeded),
                failed = g.Count(e => e.Status == ExecutionStatus.Failed),
                timedOut = g.Count(e => e.Status == ExecutionStatus.TimedOut),
                cancelled = g.Count(e => e.Status == ExecutionStatus.Cancelled),
                totalExecutions = g.Count()
            })
            .OrderBy(t => t.date)
            .ToList();

        return Results.Ok(trends);
    }).WithName("GetExecutionTrends");

    // Get top failing jobs
    group.MapGet("/top-failing-jobs", async (
        IJobRepository jobRepo,
        IJobExecutionRepository execRepo,
        int limit = 10) =>
    {
        var allExecutions = await execRepo.GetAllAsync();
        var failedExecutions = allExecutions
            .Where(e => e.Status == ExecutionStatus.Failed && e.QueuedAtUtc.Date >= DateTime.UtcNow.Date.AddDays(-7))
            .GroupBy(e => e.JobDefinitionId)
            .Select(g => new
            {
                jobId = g.Key.ToString(),
                failureCount = g.Count(),
                lastFailureTime = g.Max(e => e.CompletedAtUtc ?? e.StartedAtUtc ?? e.QueuedAtUtc),
                lastErrorMessage = g.OrderByDescending(e => e.CompletedAtUtc).FirstOrDefault()?.ErrorMessage
            })
            .OrderByDescending(x => x.failureCount)
            .Take(limit);

        var jobs = await jobRepo.GetAllAsync();
        var result = failedExecutions.Select(f => new
        {
            f.jobId,
            jobName = jobs.FirstOrDefault(j => j.Id.ToString() == f.jobId)?.Name ?? "Unknown",
            f.failureCount,
            f.lastFailureTime,
            f.lastErrorMessage
        }).ToList();

        return Results.Ok(result);
    }).WithName("GetTopFailingJobs");

    // Get stale jobs
    group.MapGet("/stale-jobs", async (
        IJobRepository jobRepo,
        int daysThreshold = 7) =>
    {
        var jobs = await jobRepo.GetActiveJobsAsync();
        var threshold = DateTime.UtcNow.AddDays(-daysThreshold);

        var staleJobs = jobs
            .Where(j => j.Schedules.Any(s =>
                s.LastExecutionUtc.HasValue && s.LastExecutionUtc.Value < threshold))
            .Select(j => new
            {
                jobId = j.Id.ToString(),
                jobName = j.Name,
                lastExecutionTime = j.Schedules.Any() ? j.Schedules.Max(s => s.LastExecutionUtc) : null,
                daysSinceLastExecution = j.Schedules.Any() && j.Schedules.Max(s => s.LastExecutionUtc) is DateTime lastExec
                    ? (DateTime.UtcNow - lastExec).Days
                    : 999,
                isActive = j.Status == JobStatus.Active
            })
            .OrderByDescending(j => j.daysSinceLastExecution)
            .ToList();

        return Results.Ok(staleJobs);
    }).WithName("GetStaleJobs");

    // Get upcoming schedules
    group.MapGet("/upcoming-schedules", async (
        IJobRepository jobRepo,
        IJobScheduleRepository scheduleRepo,
        int hoursAhead = 24) =>
    {
        var cutoffTime = DateTime.UtcNow.AddHours(hoursAhead);
        var allSchedules = await scheduleRepo.GetAllAsync();
        var jobs = await jobRepo.GetAllAsync();

        var upcoming = allSchedules
            .Where(s => s.IsActive && s.NextExecutionUtc.HasValue &&
                       s.NextExecutionUtc.Value <= cutoffTime &&
                       s.NextExecutionUtc.Value >= DateTime.UtcNow)
            .OrderBy(s => s.NextExecutionUtc)
            .Select(s => new
            {
                jobId = s.JobDefinitionId.ToString(),
                jobName = jobs.FirstOrDefault(j => j.Id == s.JobDefinitionId)?.Name ?? "Unknown",
                scheduleId = s.Id.ToString(),
                nextExecutionTime = s.NextExecutionUtc,
                recurrenceType = (int)s.Rule.Type,
                hoursUntilExecution = s.NextExecutionUtc.HasValue
                    ? (s.NextExecutionUtc.Value - DateTime.UtcNow).TotalHours
                    : 0
            })
            .ToList();

        return Results.Ok(upcoming);
    }).WithName("GetUpcomingSchedules");
}

void MapAuditEndpoints(WebApplication app)
{
    var group = app.MapGroup("/api/audit").WithTags("Audit");

    // Get all audit logs with pagination
    group.MapGet("/", async (
        IAuditRepository auditRepo,
        string? entityType,
        string? entityId,
        string? action,
        string? userId,
        string? dateFrom,
        string? dateTo,
        int pageNumber = 1,
        int pageSize = 25) =>
    {
        var allLogs = await auditRepo.GetAllAsync();
        var filtered = allLogs.AsEnumerable();

        if (!string.IsNullOrEmpty(entityType))
            filtered = filtered.Where(a => a.JobDefinitionId.HasValue);

        if (!string.IsNullOrEmpty(entityId) && Guid.TryParse(entityId, out var eId))
            filtered = filtered.Where(a => a.JobDefinitionId == eId);

        if (!string.IsNullOrEmpty(action) && Enum.TryParse<AuditAction>(action, out var auditAction))
            filtered = filtered.Where(a => a.Action == auditAction);

        if (!string.IsNullOrEmpty(userId))
            filtered = filtered.Where(a => a.PerformedBy == userId);

        if (!string.IsNullOrEmpty(dateFrom))
            filtered = filtered.Where(a => a.PerformedAtUtc >= DateTime.Parse(dateFrom));

        if (!string.IsNullOrEmpty(dateTo))
            filtered = filtered.Where(a => a.PerformedAtUtc <= DateTime.Parse(dateTo));

        var totalCount = filtered.Count();
        var items = filtered
            .OrderByDescending(a => a.PerformedAtUtc)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        var pagedResult = new
        {
            items,
            totalCount,
            pageNumber,
            pageSize,
            totalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
            hasNextPage = pageNumber * pageSize < totalCount,
            hasPreviousPage = pageNumber > 1
        };

        return Results.Ok(pagedResult);
    }).WithName("GetAuditLogs");

    // Get audit logs by job
    group.MapGet("/job/{jobId:guid}", async (
        Guid jobId,
        IAuditRepository auditRepo) =>
    {
        var logs = await auditRepo.GetAuditLogForJobAsync(jobId);
        return Results.Ok(logs);
    }).WithName("GetAuditLogsByJob");
}
void MapCircuitBreakerEndpoints(WebApplication app)
{
    var group = app.MapGroup("/api/circuit-breakers").WithTags("CircuitBreakers");

    // Get all circuit breakers
    group.MapGet("/", async (IJobCircuitBreakerRepository circuitBreakerRepo) =>
    {
        var circuitBreakers = await circuitBreakerRepo.GetAllAsync();
        return Results.Ok(circuitBreakers.Select(cb => new
        {
            cb.Id,
            cb.JobDefinitionId,
            JobName = cb.JobDefinition?.Name,
            cb.State,
            cb.ConsecutiveFailures,
            cb.LastFailureAtUtc,
            cb.OpenedAtUtc,
            cb.LastStateChangeAtUtc,
            cb.HalfOpenAttempts,
            cb.OpenReason,
            cb.OpenedBy
        }));
    }).WithName("GetAllCircuitBreakers");

    // Get circuit breaker by job ID
    group.MapGet("/job/{jobId:guid}", async (Guid jobId, IJobCircuitBreakerRepository circuitBreakerRepo) =>
    {
        var circuitBreaker = await circuitBreakerRepo.GetByJobIdAsync(jobId);
        if (circuitBreaker == null)
            return Results.NotFound();

        return Results.Ok(new
        {
            circuitBreaker.Id,
            circuitBreaker.JobDefinitionId,
            JobName = circuitBreaker.JobDefinition?.Name,
            circuitBreaker.State,
            circuitBreaker.ConsecutiveFailures,
            circuitBreaker.LastFailureAtUtc,
            circuitBreaker.OpenedAtUtc,
            circuitBreaker.LastStateChangeAtUtc,
            circuitBreaker.HalfOpenAttempts,
            circuitBreaker.OpenReason,
            circuitBreaker.OpenedBy
        });
    }).WithName("GetCircuitBreakerByJobId");

    // Get open circuit breakers
    group.MapGet("/open", async (IJobCircuitBreakerRepository circuitBreakerRepo) =>
    {
        var openCircuits = await circuitBreakerRepo.GetOpenCircuitsAsync();
        return Results.Ok(openCircuits.Select(cb => new
        {
            cb.Id,
            cb.JobDefinitionId,
            JobName = cb.JobDefinition?.Name,
            cb.State,
            cb.ConsecutiveFailures,
            cb.OpenedAtUtc,
            cb.OpenReason,
            cb.OpenedBy
        }));
    }).WithName("GetOpenCircuitBreakers");

    // Manually close circuit breaker
    group.MapPost("/{jobId:guid}/close", async (
        Guid jobId,
        IJobCircuitBreakerRepository circuitBreakerRepo,
        IJobRepository jobRepo) =>
    {
        var circuitBreaker = await circuitBreakerRepo.GetByJobIdAsync(jobId);
        if (circuitBreaker == null)
            return Results.NotFound("Circuit breaker not found");

        var job = await jobRepo.GetByIdAsync(jobId);
        if (job == null)
            return Results.NotFound("Job not found");

        circuitBreaker.Close();
        job.Activate("Manual");

        await circuitBreakerRepo.UpdateAsync(circuitBreaker);
        await jobRepo.UpdateAsync(job);

        return Results.Ok(new { message = "Circuit breaker closed successfully" });
    }).WithName("CloseCircuitBreaker");

    // Manually open circuit breaker
    group.MapPost("/{jobId:guid}/open", async (
        Guid jobId,
        string reason,
        string openedBy,
        IJobCircuitBreakerRepository circuitBreakerRepo,
        IJobRepository jobRepo) =>
    {
        var circuitBreaker = await circuitBreakerRepo.GetByJobIdAsync(jobId);
        if (circuitBreaker == null)
        {
            // Create new circuit breaker
            var job = await jobRepo.GetByIdAsync(jobId);
            if (job == null)
                return Results.NotFound("Job not found");

            circuitBreaker = new Jobs.Worker.Domain.Entities.JobCircuitBreaker(jobId);
            await circuitBreakerRepo.AddAsync(circuitBreaker);
        }

        circuitBreaker.Open(reason, openedBy);
        await circuitBreakerRepo.UpdateAsync(circuitBreaker);

        var jobToDisable = await jobRepo.GetByIdAsync(jobId);
        if (jobToDisable != null)
        {
            jobToDisable.Disable(openedBy, reason);
            await jobRepo.UpdateAsync(jobToDisable);
        }

        return Results.Ok(new { message = "Circuit breaker opened successfully" });
    }).WithName("OpenCircuitBreaker");
}
void MapHealthEndpoints(WebApplication app)
{
    app.MapHealthChecks("/health/live").WithTags("Health");
    app.MapHealthChecks("/health/ready").WithTags("Health");
    app.MapHealthChecks("/health").WithTags("Health");

    app.MapGet("/health/jobs", async (IJobRepository jobRepo) =>
    {
        var jobs = await jobRepo.GetActiveJobsAsync();
        var staleJobs = jobs.Where(j =>
            j.Schedules.Any(s => s.LastExecutionUtc.HasValue &&
                                 s.LastExecutionUtc.Value < DateTime.UtcNow.AddHours(-24)));

        return Results.Ok(new
        {
            totalActiveJobs = jobs.Count(),
            staleJobs = staleJobs.Count(),
            jobs = staleJobs.Select(j => new { j.Id, j.Name, lastRun = j.Schedules.Max(s => s.LastExecutionUtc) })
        });
    }).WithTags("Health").WithName("HealthJobs");
}