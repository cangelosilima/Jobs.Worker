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
        // Map backward-compatible properties if needed
        var executionAssembly = !string.IsNullOrEmpty(request.ExecutionAssembly) ? request.ExecutionAssembly : (request.AssemblyName ?? string.Empty);
        var executionTypeName = !string.IsNullOrEmpty(request.ExecutionTypeName) ? request.ExecutionTypeName : (request.ClassName ?? string.Empty);
        var executionCommand = request.ExecutionCommand ?? request.MethodName;

        var job = new JobDefinition(
            request.Name,
            request.Description,
            request.Category,
            request.AllowedEnvironments,
            request.ExecutionMode,
            executionAssembly,
            executionTypeName,
            request.TimeoutSeconds,
            request.CreatedBy
        );

        job.SetRetryPolicy(new RetryPolicy(
            request.MaxRetries,
            request.RetryStrategy,
            request.BaseDelaySeconds
        ));

        job.SetConcurrency(request.MaxConcurrentExecutions);

        if (!string.IsNullOrEmpty(executionCommand))
        {
            job.SetExecutionCommand(executionCommand, request.ContainerImage);
        }

        try
        {
            await _jobRepository.AddAsync(job, cancellationToken);
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"Error adding job: {ex.Message}");
            System.Console.WriteLine($"Stack trace: {ex.StackTrace}");
            throw;
        }

        // Create ownership (prefer Owner object if provided)
        var ownerName = !string.IsNullOrEmpty(request.OwnerName) ? request.OwnerName : request.Owner?.UserName ?? string.Empty;
        var ownerEmail = !string.IsNullOrEmpty(request.OwnerEmail) ? request.OwnerEmail : request.Owner?.Email ?? string.Empty;
        
        // Only create ownership if we have at least a name
        if (!string.IsNullOrEmpty(ownerName) || !string.IsNullOrEmpty(ownerEmail))
        {
            try
            {
                var ownership = new JobOwnership(
                    job.Id,
                    ownerName,
                    ownerEmail,
                    request.TeamName,
                    request.CreatedBy
                );
                await _jobRepository.AddOwnershipAsync(ownership, cancellationToken);
            }
            catch (Exception ex)
            {
                // Log but don't fail the job creation if ownership fails
                System.Console.WriteLine($"Warning: Failed to create ownership: {ex.Message}");
            }
        }

        // Create audit log
        try
        {
            var audit = JobAudit.CreateJobCreated(
                job.Id,
                request.CreatedBy,
                "127.0.0.1",
                "API",
                System.Text.Json.JsonSerializer.Serialize(request)
            );

            await _auditRepository.AddAsync(audit, cancellationToken);
        }
        catch (Exception ex)
        {
            // Log but don't fail the job creation if audit fails
            System.Console.WriteLine($"Warning: Failed to create audit log: {ex.Message}");
        }

        return job.Id;
    }
}
