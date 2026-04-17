namespace MaintenanceWorkflow.Api.Domain.Entities;

public class NonConformity
{
    public int Id { get; set; }
    public int CompanyId { get; set; }
    public Company Company { get; set; } = null!;
    public int WorkflowId { get; set; }
    public Workflow Workflow { get; set; } = null!;
    public int CurrentStatusId { get; set; }
    public WorkflowStatus CurrentStatus { get; set; } = null!;
    public string Title { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
    public ICollection<StatusHistory> History { get; set; } = new List<StatusHistory>();
}
