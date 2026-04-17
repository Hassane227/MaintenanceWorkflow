namespace MaintenanceWorkflow.Api.Domain.Entities;

public class Workflow
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public ICollection<WorkflowStatus> Statuses { get; set; } = new List<WorkflowStatus>();
    public ICollection<WorkflowTransition> Transitions { get; set; } = new List<WorkflowTransition>();
}
