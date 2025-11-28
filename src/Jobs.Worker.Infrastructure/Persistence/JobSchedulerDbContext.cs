using Jobs.Worker.Domain.Entities;
using Jobs.Worker.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Jobs.Worker.Infrastructure.Persistence;

public class JobSchedulerDbContext : DbContext
{
    public JobSchedulerDbContext(DbContextOptions<JobSchedulerDbContext> options) : base(options) { }

    public DbSet<JobDefinition> JobDefinitions => Set<JobDefinition>();
    public DbSet<JobSchedule> JobSchedules => Set<JobSchedule>();
    public DbSet<JobExecution> JobExecutions => Set<JobExecution>();
    public DbSet<JobExecutionLog> JobExecutionLogs => Set<JobExecutionLog>();
    public DbSet<JobParameter> JobParameters => Set<JobParameter>();
    public DbSet<JobNotification> JobNotifications => Set<JobNotification>();
    public DbSet<JobDependency> JobDependencies => Set<JobDependency>();
    public DbSet<JobOwnership> JobOwnerships => Set<JobOwnership>();
    public DbSet<JobAudit> JobAudits => Set<JobAudit>();
    public DbSet<JobCircuitBreaker> JobCircuitBreakers => Set<JobCircuitBreaker>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // JobDefinition configuration
        modelBuilder.Entity<JobDefinition>(entity =>
        {
            entity.ToTable("JobDefinition");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.Property(e => e.Category).HasMaxLength(100).IsRequired();
            entity.Property(e => e.ExecutionAssembly).HasMaxLength(500);
            entity.Property(e => e.ExecutionTypeName).HasMaxLength(500);
            entity.Property(e => e.CreatedBy).HasMaxLength(100).IsRequired();
            entity.Property(e => e.UpdatedBy).HasMaxLength(100);
            entity.Property(e => e.DisabledBy).HasMaxLength(100);

            entity.OwnsOne(e => e.RetryPolicy, rp =>
            {
                rp.Property(p => p.MaxRetries).HasColumnName("MaxRetries");
                rp.Property(p => p.Strategy).HasColumnName("RetryStrategy");
                rp.Property(p => p.BaseDelaySeconds).HasColumnName("BaseDelaySeconds");
                rp.Property(p => p.MaxDelaySeconds).HasColumnName("MaxDelaySeconds");
            });

            entity.OwnsOne(e => e.CircuitBreakerPolicy, cbp =>
            {
                cbp.Property(p => p.IsEnabled).HasColumnName("CircuitBreakerEnabled");
                cbp.Property(p => p.FailureThreshold).HasColumnName("CircuitBreakerFailureThreshold");
                cbp.Property(p => p.ConsecutiveFailuresWindow).HasColumnName("CircuitBreakerFailuresWindow");
                cbp.Property(p => p.OpenDurationSeconds).HasColumnName("CircuitBreakerOpenDuration");
                cbp.Property(p => p.AutoRecover).HasColumnName("CircuitBreakerAutoRecover");
                cbp.Property(p => p.HalfOpenMaxAttempts).HasColumnName("CircuitBreakerHalfOpenAttempts");
            });

            entity.HasOne(e => e.Ownership)
                  .WithOne(o => o.JobDefinition)
                  .HasForeignKey<JobOwnership>(o => o.JobDefinitionId);

            entity.HasMany(e => e.Schedules)
                  .WithOne(s => s.JobDefinition)
                  .HasForeignKey(s => s.JobDefinitionId);

            entity.HasMany(e => e.Parameters)
                  .WithOne(p => p.JobDefinition)
                  .HasForeignKey(p => p.JobDefinitionId);

            entity.HasMany(e => e.Notifications)
                  .WithOne(n => n.JobDefinition)
                  .HasForeignKey(n => n.JobDefinitionId);

            entity.HasMany(e => e.Executions)
                  .WithOne(ex => ex.JobDefinition)
                  .HasForeignKey(ex => ex.JobDefinitionId);

            entity.HasOne(e => e.CircuitBreaker)
                  .WithOne(cb => cb.JobDefinition)
                  .HasForeignKey<JobCircuitBreaker>(cb => cb.JobDefinitionId);
        });

        // JobSchedule configuration
        modelBuilder.Entity<JobSchedule>(entity =>
        {
            entity.ToTable("JobSchedule");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.CreatedBy).HasMaxLength(100).IsRequired();
            entity.Property(e => e.UpdatedBy).HasMaxLength(100);

            entity.OwnsOne(e => e.Rule, sr =>
            {
                sr.Property(r => r.Type).HasColumnName("ScheduleType");
                sr.Property(r => r.CronExpression).HasColumnName("CronExpression").HasMaxLength(100);
                sr.Property(r => r.TimeOfDay).HasColumnName("TimeOfDay");
                sr.Property(r => r.DaysOfWeek).HasColumnName("DaysOfWeek");
                sr.Property(r => r.DayOfMonth).HasColumnName("DayOfMonth");
                sr.Property(r => r.BusinessDayOfMonth).HasColumnName("BusinessDayOfMonth");
                sr.Property(r => r.AdjustToPreviousBusinessDay).HasColumnName("AdjustToPreviousBusinessDay");
                sr.Property(r => r.OneTimeExecutionDate).HasColumnName("OneTimeExecutionDate");
                sr.Property(r => r.ConditionalExpression).HasColumnName("ConditionalExpression").HasMaxLength(500);
            });
        });

        // JobExecution configuration
        modelBuilder.Entity<JobExecution>(entity =>
        {
            entity.ToTable("JobExecution");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.TraceId).HasMaxLength(100).IsRequired();
            entity.Property(e => e.HostInstance).HasMaxLength(200).IsRequired();
            entity.Property(e => e.TriggeredBy).HasMaxLength(100);

            entity.HasOne(e => e.JobSchedule)
                  .WithMany()
                  .HasForeignKey(e => e.JobScheduleId)
                  .OnDelete(DeleteBehavior.SetNull);

            entity.HasMany(e => e.Logs)
                  .WithOne(l => l.JobExecution)
                  .HasForeignKey(l => l.JobExecutionId);
        });

        // JobExecutionLog configuration
        modelBuilder.Entity<JobExecutionLog>(entity =>
        {
            entity.ToTable("JobExecutionLog");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.LogLevel).HasMaxLength(20).IsRequired();
            entity.Property(e => e.Message).HasMaxLength(4000);
        });

        // JobParameter configuration
        modelBuilder.Entity<JobParameter>(entity =>
        {
            entity.ToTable("JobParameter");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ParameterName).HasMaxLength(100).IsRequired();
            entity.Property(e => e.ParameterType).HasMaxLength(50);
            entity.Property(e => e.CreatedBy).HasMaxLength(100).IsRequired();
            entity.Property(e => e.UpdatedBy).HasMaxLength(100);
        });

        // JobNotification configuration
        modelBuilder.Entity<JobNotification>(entity =>
        {
            entity.ToTable("JobNotification");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.CreatedBy).HasMaxLength(100).IsRequired();
            entity.Property(e => e.UpdatedBy).HasMaxLength(100);

            entity.OwnsOne(e => e.Rule, nr =>
            {
                nr.Property(r => r.Channels).HasColumnName("Channels");
                nr.Property(r => r.Triggers).HasColumnName("Triggers");
                nr.Property(r => r.TeamsWebhookUrl).HasColumnName("TeamsWebhookUrl").HasMaxLength(500);
                nr.Property(r => r.SlackWebhookUrl).HasColumnName("SlackWebhookUrl").HasMaxLength(500);
                nr.Property(r => r.CustomWebhookUrl).HasColumnName("CustomWebhookUrl").HasMaxLength(500);
                nr.Property(r => r.EmailRecipients)
                  .HasColumnName("EmailRecipients")
                  .HasConversion(
                      v => JsonSerializer.Serialize(v, (JsonSerializerOptions)null!),
                      v => JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions)null!) ?? new List<string>()
                  );
            });
        });

        // JobDependency configuration
        modelBuilder.Entity<JobDependency>(entity =>
        {
            entity.ToTable("JobDependency");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.CreatedBy).HasMaxLength(100).IsRequired();

            entity.HasOne(e => e.JobDefinition)
                  .WithMany(j => j.Dependencies)
                  .HasForeignKey(e => e.JobDefinitionId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.DependsOnJob)
                  .WithMany(j => j.DependentJobs)
                  .HasForeignKey(e => e.DependsOnJobId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // JobOwnership configuration
        modelBuilder.Entity<JobOwnership>(entity =>
        {
            entity.ToTable("JobOwnership");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.OwnerName).HasMaxLength(200).IsRequired();
            entity.Property(e => e.OwnerEmail).HasMaxLength(200).IsRequired();
            entity.Property(e => e.TeamName).HasMaxLength(200).IsRequired();
            entity.Property(e => e.TeamChannel).HasMaxLength(200);
            entity.Property(e => e.EscalationEmail).HasMaxLength(200);
            entity.Property(e => e.CreatedBy).HasMaxLength(100).IsRequired();
            entity.Property(e => e.UpdatedBy).HasMaxLength(100);
        });

        // JobAudit configuration
        modelBuilder.Entity<JobAudit>(entity =>
        {
            entity.ToTable("JobAudit");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.PerformedBy).HasMaxLength(100).IsRequired();
            entity.Property(e => e.IpAddress).HasMaxLength(50);
            entity.Property(e => e.UserAgent).HasMaxLength(500);
        });

        // JobCircuitBreaker configuration
        modelBuilder.Entity<JobCircuitBreaker>(entity =>
        {
            entity.ToTable("JobCircuitBreaker");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.State).IsRequired();
            entity.Property(e => e.OpenReason).HasMaxLength(500);
            entity.Property(e => e.OpenedBy).HasMaxLength(100);
        });
    }
}
