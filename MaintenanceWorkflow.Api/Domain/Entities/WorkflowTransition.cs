namespace MaintenanceWorkflow.Api.Domain.Entities;

public class WorkflowTransition
{
    public int Id { get; set; }
    public int WorkflowId { get; set; }
    public Workflow Workflow { get; set; } = null!;
    public int FromStatusId { get; set; }
    public WorkflowStatus FromStatus { get; set; } = null!;
    public int ToStatusId { get; set; }
    public WorkflowStatus ToStatus { get; set; } = null!;
    public string ActionName { get; set; } = string.Empty;
    public RuntimeRole RoleAllowed { get; set; }
    public bool IsActive { get; set; } = true;
}
