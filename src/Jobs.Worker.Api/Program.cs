using Jobs.Worker.Application.Commands;
using Jobs.Worker.Application.Handlers;
using Jobs.Worker.Application.Interfaces;
using Jobs.Worker.Application.Queries;
using Jobs.Worker.Infrastructure.Persistence;
using Jobs.Worker.Infrastructure.Repositories;
using Jobs.Worker.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
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

// Add DbContext
builder.Services.AddDbContext<JobSchedulerDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("JobSchedulerDb")));

// Add repositories
builder.Services.AddScoped<IJobRepository, JobRepository>();
builder.Services.AddScoped<IJobExecutionRepository, JobExecutionRepository>();
builder.Services.AddScoped<IJobScheduleRepository, Jobs.Worker.Infrastructure.Repositories.JobScheduleRepository>();
builder.Services.AddScoped<IAuditRepository, Jobs.Worker.Infrastructure.Repositories.AuditRepository>();

// Add services
builder.Services.AddScoped<IScheduleCalculator, ScheduleCalculator>();
builder.Services.AddSingleton<IDistributedLockService, DistributedLockService>();

// Add handlers
builder.Services.AddScoped<CreateJobCommandHandler>();
builder.Services.AddScoped<TriggerJobCommandHandler>();
builder.Services.AddScoped<GetDashboardStatsQueryHandler>();

// Add health checks
builder.Services.AddHealthChecks()
    .AddDbContextCheck<JobSchedulerDbContext>("database");

var app = builder.Build();

// Configure middleware
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseSerilogRequestLogging();

// Map endpoints
MapJobEndpoints(app);
MapExecutionEndpoints(app);
MapScheduleEndpoints(app);
MapDashboardEndpoints(app);
MapHealthEndpoints(app);

app.Run();

void MapJobEndpoints(WebApplication app)
{
    var group = app.MapGroup("/api/jobs").WithTags("Jobs");

    group.MapGet("/", async (IJobRepository jobRepo) =>
    {
        var jobs = await jobRepo.GetAllAsync();
        return Results.Ok(jobs);
    }).WithName("GetAllJobs");

    group.MapGet("/{id:guid}", async (Guid id, IJobRepository jobRepo) =>
    {
        var job = await jobRepo.GetByIdAsync(id);
        return job != null ? Results.Ok(job) : Results.NotFound();
    }).WithName("GetJobById");

    group.MapPost("/", async (CreateJobCommand command, CreateJobCommandHandler handler) =>
    {
        var jobId = await handler.HandleAsync(command);
        return Results.Created($"/api/jobs/{jobId}", new { id = jobId });
    }).WithName("CreateJob");

    group.MapPost("/{id:guid}/trigger", async (Guid id, TriggerJobCommand command, TriggerJobCommandHandler handler) =>
    {
        var executionId = await handler.HandleAsync(command with { JobDefinitionId = id });
        return Results.Accepted($"/api/executions/{executionId}", new { executionId });
    }).WithName("TriggerJob");

    group.MapPut("/{id:guid}/status", async (Guid id, UpdateJobStatusCommand command, IJobRepository jobRepo) =>
    {
        var job = await jobRepo.GetByIdAsync(id);
        if (job == null) return Results.NotFound();

        if (command.NewStatus == Domain.Enums.JobStatus.Disabled)
            job.Disable(command.UpdatedBy, command.Reason ?? "");
        else if (command.NewStatus == Domain.Enums.JobStatus.Active)
            job.Activate(command.UpdatedBy);

        await jobRepo.UpdateAsync(job);
        return Results.NoContent();
    }).WithName("UpdateJobStatus");
}

void MapExecutionEndpoints(WebApplication app)
{
    var group = app.MapGroup("/api/executions").WithTags("Executions");

    group.MapGet("/running", async (IJobExecutionRepository execRepo) =>
    {
        var executions = await execRepo.GetRunningExecutionsAsync();
        return Results.Ok(executions);
    }).WithName("GetRunningExecutions");

    group.MapGet("/failed-today", async (IJobExecutionRepository execRepo) =>
    {
        var executions = await execRepo.GetFailedExecutionsTodayAsync();
        return Results.Ok(executions);
    }).WithName("GetFailedExecutionsToday");

    group.MapGet("/job/{jobId:guid}", async (Guid jobId, IJobExecutionRepository execRepo, int pageSize = 50) =>
    {
        var executions = await execRepo.GetByJobIdAsync(jobId, pageSize);
        return Results.Ok(executions);
    }).WithName("GetExecutionsByJob");
}

void MapScheduleEndpoints(WebApplication app)
{
    var group = app.MapGroup("/api/schedules").WithTags("Schedules");

    group.MapPost("/", async (CreateScheduleCommand command, IJobScheduleRepository scheduleRepo) =>
    {
        var scheduleRule = command.ScheduleType switch
        {
            Domain.Enums.ScheduleType.Daily => Domain.ValueObjects.ScheduleRule.CreateDaily(command.TimeOfDay ?? TimeSpan.Zero),
            Domain.Enums.ScheduleType.Weekly => Domain.ValueObjects.ScheduleRule.CreateWeekly(command.DaysOfWeek ?? 0, command.TimeOfDay ?? TimeSpan.Zero),
            Domain.Enums.ScheduleType.Monthly => Domain.ValueObjects.ScheduleRule.CreateMonthly(command.DayOfMonth ?? 1, command.TimeOfDay ?? TimeSpan.Zero),
            Domain.Enums.ScheduleType.MonthlyBusinessDay => Domain.ValueObjects.ScheduleRule.CreateMonthlyBusinessDay(command.BusinessDayOfMonth ?? 1, command.TimeOfDay ?? TimeSpan.Zero, command.AdjustToPreviousBusinessDay),
            Domain.Enums.ScheduleType.Cron => Domain.ValueObjects.ScheduleRule.CreateCron(command.CronExpression ?? ""),
            Domain.Enums.ScheduleType.OneTime => Domain.ValueObjects.ScheduleRule.CreateOneTime(command.OneTimeExecutionDate ?? DateTime.UtcNow),
            Domain.Enums.ScheduleType.Conditional => Domain.ValueObjects.ScheduleRule.CreateConditional(command.ConditionalExpression ?? "", command.TimeOfDay ?? TimeSpan.Zero),
            _ => throw new ArgumentException("Invalid schedule type")
        };

        var schedule = new Domain.Entities.JobSchedule(
            command.JobDefinitionId,
            scheduleRule,
            command.CreatedBy,
            command.StartDateUtc,
            command.EndDateUtc
        );

        await scheduleRepo.AddAsync(schedule);
        return Results.Created($"/api/schedules/{schedule.Id}", new { id = schedule.Id });
    }).WithName("CreateSchedule");
}

void MapDashboardEndpoints(WebApplication app)
{
    var group = app.MapGroup("/api/dashboard").WithTags("Dashboard");

    group.MapGet("/stats", async (GetDashboardStatsQueryHandler handler) =>
    {
        var stats = await handler.HandleAsync(new GetDashboardStatsQuery());
        return Results.Ok(stats);
    }).WithName("GetDashboardStats");
}

void MapHealthEndpoints(WebApplication app)
{
    app.MapHealthChecks("/health/live").WithTags("Health");
    app.MapHealthChecks("/health/ready").WithTags("Health");

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
