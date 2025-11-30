using Jobs.Worker.Application.Commands;
using Jobs.Worker.Application.Interfaces;
using Jobs.Worker.Domain.Entities;
using Jobs.Worker.Domain.ValueObjects;

namespace Jobs.Worker.Application.Handlers;

public class TriggerJobCommandHandler
{
    private readonly IJobRepository _jobRepository;
    private readonly IJobExecutionRepository _executionRepository;
    private readonly IAuditRepository _auditRepository;

    public TriggerJobCommandHandler(
        IJobRepository jobRepository,
        IJobExecutionRepository executionRepository,
        IAuditRepository auditRepository)
    {
        _jobRepository = jobRepository;
        _executionRepository = executionRepository;
        _auditRepository = auditRepository;
    }

    public async Task<Guid> HandleAsync(TriggerJobCommand request, CancellationToken cancellationToken = default)
    {
        var job = await _jobRepository.GetByIdAsync(request.JobDefinitionId, cancellationToken)
            ?? throw new InvalidOperationException($"Job {request.JobDefinitionId} not found");

        if (job.Status != Domain.Enums.JobStatus.Active)
        {
            throw new InvalidOperationException($"Job {job.Name} is not active");
        }

        var context = ValueObjects.ExecutionContext.Create(Environment.MachineName);
        var execution = new JobExecution(
            job.Id,
            null,
            context,
            request.InputPayload,
            request.TriggeredBy,
            true,
            job.RetryPolicy.MaxRetries
        );

        await _executionRepository.AddAsync(execution, cancellationToken);

        // Create audit log
        var audit = Domain.Entities.JobAudit.CreateExecutionStarted(
            job.Id,
            execution.Id,
            request.TriggeredBy,
            "127.0.0.1",
            "API"
        );

        await _auditRepository.AddAsync(audit, cancellationToken);

        return execution.Id;
    }
}
