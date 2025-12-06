using System;
using System.Collections.Generic;
using Jobs.Worker.Domain.Enums;
using Jobs.Worker.Domain.ValueObjects;

namespace Jobs.Worker.Domain.Entities;

public class JobDefinition
{
    public Guid Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public string Category { get; private set; } = string.Empty;
    public JobStatus Status { get; private set; }
    public DeploymentEnvironment AllowedEnvironments { get; private set; }
    public ExecutionMode ExecutionMode { get; private set; }
    public string ExecutionAssembly { get; private set; } = string.Empty;
    public string ExecutionTypeName { get; private set; } = string.Empty;
    public string? ExecutionCommand { get; private set; }
    public string? ContainerImage { get; private set; }
    public int TimeoutSeconds { get; private set; }
    public RetryPolicy RetryPolicy { get; private set; } = RetryPolicy.NoRetry();
    public CircuitBreakerPolicy CircuitBreakerPolicy { get; private set; } = CircuitBreakerPolicy.Disabled();
    public SlaPolicy SlaPolicy { get; private set; } = SlaPolicy.Disabled();
    public int MaxConcurrentExecutions { get; private set; }
    public bool AllowManualTrigger { get; private set; }
    public int? ExpectedDurationSeconds { get; private set; }
    public string ParameterSchema { get; private set; } = "{}";
    public int Version { get; private set; }
    public string CreatedBy { get; private set; } = string.Empty;
    public DateTime CreatedAtUtc { get; private set; }
    public string? UpdatedBy { get; private set; }
    public DateTime? UpdatedAtUtc { get; private set; }
    public string? DisabledBy { get; private set; }
    public DateTime? DisabledAtUtc { get; private set; }
    public string? DisabledReason { get; private set; }

    // Navigation properties
    public virtual ICollection<JobSchedule> Schedules { get; private set; } = new List<JobSchedule>();
    public virtual ICollection<JobParameter> Parameters { get; private set; } = new List<JobParameter>();
    public virtual ICollection<JobNotification> Notifications { get; private set; } = new List<JobNotification>();
    public virtual ICollection<JobExecution> Executions { get; private set; } = new List<JobExecution>();
    public virtual ICollection<JobDependency> Dependencies { get; private set; } = new List<JobDependency>();
    public virtual ICollection<JobDependency> DependentJobs { get; private set; } = new List<JobDependency>();
    public virtual JobOwnership? Ownership { get; private set; }
    public virtual JobCircuitBreaker? CircuitBreaker { get; private set; }
    public virtual ICollection<JobStep> Steps { get; private set; } = new List<JobStep>();
    public virtual ICollection<JobFeatureFlag> FeatureFlags { get; private set; } = new List<JobFeatureFlag>();
    public virtual ICollection<BackfillRequest> BackfillRequests { get; private set; } = new List<BackfillRequest>();

    private JobDefinition() { }

    public JobDefinition(
        string name,
        string description,
        string category,
        DeploymentEnvironment allowedEnvironments,
        ExecutionMode executionMode,
        string executionAssembly,
        string executionTypeName,
        int timeoutSeconds,
        string createdBy)
    {
        Id = Guid.NewGuid();
        Name = name;
        Description = description;
        Category = category;
        Status = JobStatus.Draft;
        AllowedEnvironments = allowedEnvironments;
        ExecutionMode = executionMode;
        ExecutionAssembly = executionAssembly;
        ExecutionTypeName = executionTypeName;
        TimeoutSeconds = timeoutSeconds;
        MaxConcurrentExecutions = 1;
        AllowManualTrigger = true;
        Version = 1;
        CreatedBy = createdBy;
        CreatedAtUtc = DateTime.UtcNow;
    }

    public void UpdateDetails(string name, string description, string category, string updatedBy)
    {
        Name = name;
        Description = description;
        Category = category;
        UpdatedBy = updatedBy;
        UpdatedAtUtc = DateTime.UtcNow;
        Version++;
    }

    public void SetRetryPolicy(RetryPolicy policy)
    {
        RetryPolicy = policy;
    }

    public void SetCircuitBreakerPolicy(CircuitBreakerPolicy policy)
    {
        CircuitBreakerPolicy = policy;
    }

    public void SetSlaPolicy(SlaPolicy policy)
    {
        SlaPolicy = policy;
    }

    public void SetExecutionCommand(string? command, string? containerImage = null)
    {
        ExecutionCommand = command;
        ContainerImage = containerImage;
    }

    public void SetTimeout(int timeoutSeconds)
    {
        if (timeoutSeconds < 1)
            throw new ArgumentException("Timeout must be at least 1 second", nameof(timeoutSeconds));
        TimeoutSeconds = timeoutSeconds;
    }

    public void SetConcurrency(int maxConcurrent)
    {
        if (maxConcurrent < 1)
            throw new ArgumentException("Max concurrent must be at least 1", nameof(maxConcurrent));
        MaxConcurrentExecutions = maxConcurrent;
    }

    public void Activate(string activatedBy)
    {
        if (Status == JobStatus.Active)
            return;

        Status = JobStatus.Active;
        UpdatedBy = activatedBy;
        UpdatedAtUtc = DateTime.UtcNow;
        DisabledBy = null;
        DisabledAtUtc = null;
        DisabledReason = null;
    }

    public void Disable(string disabledBy, string reason)
    {
        if (Status == JobStatus.Disabled)
            return;

        Status = JobStatus.Disabled;
        DisabledBy = disabledBy;
        DisabledAtUtc = DateTime.UtcNow;
        DisabledReason = reason;
        UpdatedBy = disabledBy;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void Archive(string archivedBy)
    {
        Status = JobStatus.Archived;
        UpdatedBy = archivedBy;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public bool CanExecuteInEnvironment(DeploymentEnvironment currentEnvironment)
    {
        return AllowedEnvironments.HasFlag(currentEnvironment);
    }

    public void SetParameterSchema(string jsonSchema)
    {
        ParameterSchema = jsonSchema;
    }

    public void SetExpectedDuration(int? seconds)
    {
        ExpectedDurationSeconds = seconds;
    }
}
