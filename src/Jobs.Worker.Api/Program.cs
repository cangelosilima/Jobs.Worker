using Jobs.Worker.Application.Commands;
using Jobs.Worker.Application.Interfaces;
using Jobs.Worker.Application.Queries;
using Jobs.Worker.Infrastructure.Persistence;
using Jobs.Worker.Infrastructure.Repositories;
using Jobs.Worker.Infrastructure.Services;
using MediatR;
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

// Add MediatR
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(CreateJobCommand).Assembly));

// Add repositories
builder.Services.AddScoped<IJobRepository, JobRepository>();
builder.Services.AddScoped<IJobExecutionRepository, JobExecutionRepository>();
builder.Services.AddScoped<IJobScheduleRepository, Jobs.Worker.Infrastructure.Repositories.JobScheduleRepository>();
builder.Services.AddScoped<IAuditRepository, Jobs.Worker.Infrastructure.Repositories.AuditRepository>();

// Add services
builder.Services.AddScoped<IScheduleCalculator, ScheduleCalculator>();
builder.Services.AddSingleton<IDistributedLockService, DistributedLockService>();

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

    group.MapGet("/", async (IMediator mediator) =>
    {
        var jobs = await mediator.Send(new GetAllJobsQuery());
        return Results.Ok(jobs);
    }).WithName("GetAllJobs");

    group.MapGet("/{id:guid}", async (Guid id, IMediator mediator) =>
    {
        var job = await mediator.Send(new GetJobByIdQuery(id));
        return job != null ? Results.Ok(job) : Results.NotFound();
    }).WithName("GetJobById");

    group.MapPost("/", async (CreateJobCommand command, IMediator mediator) =>
    {
        var jobId = await mediator.Send(command);
        return Results.Created($"/api/jobs/{jobId}", new { id = jobId });
    }).WithName("CreateJob");

    group.MapPost("/{id:guid}/trigger", async (Guid id, TriggerJobCommand command, IMediator mediator) =>
    {
        var executionId = await mediator.Send(command with { JobDefinitionId = id });
        return Results.Accepted($"/api/executions/{executionId}", new { executionId });
    }).WithName("TriggerJob");

    group.MapPut("/{id:guid}/status", async (Guid id, UpdateJobStatusCommand command, IMediator mediator) =>
    {
        await mediator.Send(command with { JobDefinitionId = id });
        return Results.NoContent();
    }).WithName("UpdateJobStatus");
}

void MapExecutionEndpoints(WebApplication app)
{
    var group = app.MapGroup("/api/executions").WithTags("Executions");

    group.MapGet("/running", async (IMediator mediator) =>
    {
        var executions = await mediator.Send(new GetRunningExecutionsQuery());
        return Results.Ok(executions);
    }).WithName("GetRunningExecutions");

    group.MapGet("/failed-today", async (IMediator mediator) =>
    {
        var executions = await mediator.Send(new GetFailedExecutionsTodayQuery());
        return Results.Ok(executions);
    }).WithName("GetFailedExecutionsToday");

    group.MapGet("/job/{jobId:guid}", async (Guid jobId, IMediator mediator, int pageSize = 50) =>
    {
        var executions = await mediator.Send(new GetExecutionsByJobIdQuery(jobId, pageSize));
        return Results.Ok(executions);
    }).WithName("GetExecutionsByJob");
}

void MapScheduleEndpoints(WebApplication app)
{
    var group = app.MapGroup("/api/schedules").WithTags("Schedules");

    group.MapPost("/", async (CreateScheduleCommand command, IMediator mediator) =>
    {
        var scheduleId = await mediator.Send(command);
        return Results.Created($"/api/schedules/{scheduleId}", new { id = scheduleId });
    }).WithName("CreateSchedule");
}

void MapDashboardEndpoints(WebApplication app)
{
    var group = app.MapGroup("/api/dashboard").WithTags("Dashboard");

    group.MapGet("/stats", async (IMediator mediator) =>
    {
        var stats = await mediator.Send(new GetDashboardStatsQuery());
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
