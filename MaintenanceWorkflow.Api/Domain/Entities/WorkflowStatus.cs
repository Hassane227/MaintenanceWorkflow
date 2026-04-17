namespace MaintenanceWorkflow.Api.Domain.Entities;

public class WorkflowStatus
{
    public int Id { get; set; }
    public int WorkflowId { get; set; }
    public Workflow Workflow { get; set; } = null!;
    public string Name { get; set; } = string.Empty;
    public int Order { get; set; }
    public bool IsInitial { get; set; }
    public bool IsFinal { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<WorkflowTransition> FromTransitions { get; set; } = new List<WorkflowTransition>();
    public ICollection<WorkflowTransition> ToTransitions { get; set; } = new List<WorkflowTransition>();
}
