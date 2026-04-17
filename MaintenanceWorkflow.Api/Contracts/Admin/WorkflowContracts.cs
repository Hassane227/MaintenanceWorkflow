namespace MaintenanceWorkflow.Api.Contracts.Admin;

public record CreateWorkflowRequest(string Name);

public record WorkflowDto(int Id, string Name, bool IsActive);
