namespace MaintenanceWorkflow.Api.Domain.Entities;

public class StatusHistory
{
    public int Id { get; set; }
    public int NonConformityId { get; set; }
    public NonConformity NonConformity { get; set; } = null!;
    public int FromStatusId { get; set; }
    public WorkflowStatus FromStatus { get; set; } = null!;
    public int ToStatusId { get; set; }
    public WorkflowStatus ToStatus { get; set; } = null!;
    public string ActionName { get; set; } = string.Empty;
    public RuntimeRole RoleUsed { get; set; }
    public string PerformedBy { get; set; } = string.Empty;
    public DateTime DateUtc { get; set; }
}
