using Jobs.Worker.Application.Interfaces;
using Jobs.Worker.Infrastructure.Persistence;
using Jobs.Worker.Infrastructure.Repositories;
using Jobs.Worker.Infrastructure.Services;
using Jobs.Worker.Worker;
using Microsoft.EntityFrameworkCore;
using Serilog;

var builder = Host.CreateApplicationBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("logs/worker-.log", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Services.AddSerilog();

// Add DbContext
builder.Services.AddDbContext<JobSchedulerDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("JobSchedulerDb")));

// Add repositories
builder.Services.AddScoped<IJobRepository, JobRepository>();
builder.Services.AddScoped<IJobExecutionRepository, JobExecutionRepository>();
builder.Services.AddScoped<IJobScheduleRepository, JobScheduleRepository>();
builder.Services.AddScoped<IAuditRepository, AuditRepository>();

// Add services
builder.Services.AddScoped<IScheduleCalculator, ScheduleCalculator>();
builder.Services.AddSingleton<IDistributedLockService, DistributedLockService>();

// Add hosted service
builder.Services.AddHostedService<SchedulerWorker>();

var host = builder.Build();
host.Run();
