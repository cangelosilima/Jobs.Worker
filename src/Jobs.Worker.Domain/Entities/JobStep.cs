using System;
using System.Collections.Generic;
using Jobs.Worker.Domain.Enums;
using Jobs.Worker.Domain.ValueObjects;

namespace Jobs.Worker.Domain.Entities;

public class JobStep
{
    public Guid Id { get; private set; }
    public Guid JobDefinitionId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public int Order { get; private set; }
    public ExecutionMode ExecutionMode { get; private set; }
    public string ExecutionAssembly { get; private set; } = string.Empty;
    public string ExecutionTypeName { get; private set; } = string.Empty;
    public string? ExecutionCommand { get; private set; }
    public int TimeoutSeconds { get; private set; }
    public RetryPolicy RetryPolicy { get; private set; } = RetryPolicy.NoRetry();
    public bool ContinueOnFailure { get; private set; }
    public string? DependsOnStepIds { get; private set; } // JSON array of step IDs
    public string? InputMappings { get; private set; } // JSON mapping from previous steps
    public string? OutputMappings { get; private set; } // JSON mapping to next steps
    public string CreatedBy { get; private set; } = string.Empty;
    public DateTime CreatedAtUtc { get; private set; }

    // Navigation properties
    public virtual JobDefinition JobDefinition { get; private set; } = null!;
    public virtual ICollection<StepExecution> StepExecutions { get; private set; } = new List<StepExecution>();

    private JobStep() { }

    public JobStep(
        Guid jobDefinitionId,
        string name,
        string description,
        int order,
        ExecutionMode executionMode,
        string executionAssembly,
        string executionTypeName,
        int timeoutSeconds,
        string createdBy)
    {
        Id = Guid.NewGuid();
        JobDefinitionId = jobDefinitionId;
        Name = name;
        Description = description;
        Order = order;
        ExecutionMode = executionMode;
        ExecutionAssembly = executionAssembly;
        ExecutionTypeName = executionTypeName;
        TimeoutSeconds = timeoutSeconds;
        ContinueOnFailure = false;
        CreatedBy = createdBy;
        CreatedAtUtc = DateTime.UtcNow;
    }

    public void SetExecutionCommand(string? command)
    {
        ExecutionCommand = command;
    }

    public void SetRetryPolicy(RetryPolicy policy)
    {
        RetryPolicy = policy;
    }

    public void SetContinueOnFailure(bool continueOnFailure)
    {
        ContinueOnFailure = continueOnFailure;
    }

    public void SetDependencies(List<Guid> dependsOnStepIds)
    {
        DependsOnStepIds = System.Text.Json.JsonSerializer.Serialize(dependsOnStepIds);
    }

    public List<Guid> GetDependencies()
    {
        if (string.IsNullOrEmpty(DependsOnStepIds))
            return new List<Guid>();

        return System.Text.Json.JsonSerializer.Deserialize<List<Guid>>(DependsOnStepIds) ?? new List<Guid>();
    }

    public void SetInputMappings(Dictionary<string, string> mappings)
    {
        InputMappings = System.Text.Json.JsonSerializer.Serialize(mappings);
    }

    public void SetOutputMappings(Dictionary<string, string> mappings)
    {
        OutputMappings = System.Text.Json.JsonSerializer.Serialize(mappings);
    }
}
