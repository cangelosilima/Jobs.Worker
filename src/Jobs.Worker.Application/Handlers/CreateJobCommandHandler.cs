using Jobs.Worker.Application.Commands;
using Jobs.Worker.Application.Interfaces;
using Jobs.Worker.Domain.Entities;
using Jobs.Worker.Domain.Enums;
using Jobs.Worker.Domain.ValueObjects;

namespace Jobs.Worker.Application.Handlers;

public class CreateJobCommandHandler
{
    private readonly IJobRepository _jobRepository;
    private readonly IAuditRepository _auditRepository;

    public CreateJobCommandHandler(IJobRepository jobRepository, IAuditRepository auditRepository)
    {
        _jobRepository = jobRepository;
        _auditRepository = auditRepository;
    }

    public async Task<Guid> HandleAsync(CreateJobCommand request, CancellationToken cancellationToken = default)
    {
        var job = new JobDefinition(
            request.Name,
            request.Description,
            request.Category,
            request.AllowedEnvironments,
            request.ExecutionMode,
            request.ExecutionAssembly,
            request.ExecutionTypeName,
            request.TimeoutSeconds,
            request.CreatedBy
        );

        job.SetRetryPolicy(new RetryPolicy(
            request.MaxRetries,
            request.RetryStrategy,
            request.BaseDelaySeconds
        ));

        job.SetConcurrency(request.MaxConcurrentExecutions);

        if (!string.IsNullOrEmpty(request.ExecutionCommand))
        {
            job.SetExecutionCommand(request.ExecutionCommand, request.ContainerImage);
        }

        await _jobRepository.AddAsync(job, cancellationToken);

        // Create ownership (prefer Owner object if provided)
        var ownerName = !string.IsNullOrEmpty(request.OwnerName) ? request.OwnerName : request.Owner?.UserName ?? string.Empty;
        var ownerEmail = !string.IsNullOrEmpty(request.OwnerEmail) ? request.OwnerEmail : request.Owner?.Email ?? string.Empty;
        var ownership = new JobOwnership(
            job.Id,
            ownerName,
            ownerEmail,
            request.TeamName,
            request.CreatedBy
        );
        await _jobRepository.AddOwnershipAsync(ownership, cancellationToken);

        // Create audit log
        var audit = JobAudit.CreateJobCreated(
            job.Id,
            request.CreatedBy,
            "127.0.0.1",
            "API",
            System.Text.Json.JsonSerializer.Serialize(request)
        );

        await _auditRepository.AddAsync(audit, cancellationToken);

        return job.Id;
    }
}
