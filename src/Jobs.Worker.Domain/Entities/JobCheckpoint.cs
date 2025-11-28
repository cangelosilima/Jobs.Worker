namespace Jobs.Worker.Domain.Entities;

public class JobCheckpoint
{
    public Guid Id { get; private set; }
    public Guid JobExecutionId { get; private set; }
    public string CheckpointName { get; private set; } = string.Empty;
    public string CheckpointData { get; private set; } = "{}"; // JSON state
    public DateTime CreatedAtUtc { get; private set; }

    public virtual JobExecution JobExecution { get; private set; } = null!;

    private JobCheckpoint() { }

    public JobCheckpoint(Guid jobExecutionId, string checkpointName, string checkpointData)
    {
        Id = Guid.NewGuid();
        JobExecutionId = jobExecutionId;
        CheckpointName = checkpointName;
        CheckpointData = checkpointData;
        CreatedAtUtc = DateTime.UtcNow;
    }

    public void UpdateData(string checkpointData)
    {
        CheckpointData = checkpointData;
    }
}
