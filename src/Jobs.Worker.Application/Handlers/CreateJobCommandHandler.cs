using Jobs.Worker.Application.Commands;
using Jobs.Worker.Application.Interfaces;
using Jobs.Worker.Domain.Entities;
using Jobs.Worker.Domain.Enums;
using Jobs.Worker.Domain.ValueObjects;
using MediatR;

namespace Jobs.Worker.Application.Handlers;

public class CreateJobCommandHandler : IRequestHandler<CreateJobCommand, Guid>
{
    private readonly IJobRepository _jobRepository;
    private readonly IAuditRepository _auditRepository;

    public CreateJobCommandHandler(IJobRepository jobRepository, IAuditRepository auditRepository)
    {
        _jobRepository = jobRepository;
        _auditRepository = auditRepository;
    }

    public async Task<Guid> Handle(CreateJobCommand request, CancellationToken cancellationToken)
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

        // Create ownership
        var ownership = new JobOwnership(
            job.Id,
            request.OwnerName,
            request.OwnerEmail,
            request.TeamName,
            request.CreatedBy
        );

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
