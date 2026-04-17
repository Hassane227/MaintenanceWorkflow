namespace MaintenanceWorkflow.Api.Domain.Entities;

public class Company
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int? CurativeWorkflowId { get; set; }
    public Workflow? CurativeWorkflow { get; set; }
    public ICollection<NonConformity> NonConformities { get; set; } = new List<NonConformity>();
}
