namespace Jobs.Worker.Domain.Entities;

public class JobOwnership
{
    public Guid Id { get; private set; }
    public Guid JobDefinitionId { get; private set; }
    public string OwnerName { get; private set; } = string.Empty;
    public string OwnerEmail { get; private set; } = string.Empty;
    public string TeamName { get; private set; } = string.Empty;
    public string? TeamChannel { get; private set; }
    public string? EscalationEmail { get; private set; }
    public string CreatedBy { get; private set; } = string.Empty;
    public DateTime CreatedAtUtc { get; private set; }
    public string? UpdatedBy { get; private set; }
    public DateTime? UpdatedAtUtc { get; private set; }

    // Navigation properties
    public virtual JobDefinition JobDefinition { get; private set; } = null!;

    private JobOwnership() { }

    public JobOwnership(
        Guid jobDefinitionId,
        string ownerName,
        string ownerEmail,
        string teamName,
        string createdBy,
        string? teamChannel = null,
        string? escalationEmail = null)
    {
        Id = Guid.NewGuid();
        JobDefinitionId = jobDefinitionId;
        OwnerName = ownerName;
        OwnerEmail = ownerEmail;
        TeamName = teamName;
        TeamChannel = teamChannel;
        EscalationEmail = escalationEmail;
        CreatedBy = createdBy;
        CreatedAtUtc = DateTime.UtcNow;
    }

    public void UpdateOwnership(
        string ownerName,
        string ownerEmail,
        string teamName,
        string updatedBy,
        string? teamChannel = null,
        string? escalationEmail = null)
    {
        OwnerName = ownerName;
        OwnerEmail = ownerEmail;
        TeamName = teamName;
        TeamChannel = teamChannel;
        EscalationEmail = escalationEmail;
        UpdatedBy = updatedBy;
        UpdatedAtUtc = DateTime.UtcNow;
    }
}
