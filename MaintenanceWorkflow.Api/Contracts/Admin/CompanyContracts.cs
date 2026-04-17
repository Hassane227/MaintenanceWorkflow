namespace MaintenanceWorkflow.Api.Contracts.Admin;

public record CreateCompanyRequest(string Name);

public record AssignCurativeWorkflowRequest(int WorkflowId);

public record CompanyDto(int Id, string Name, int? CurativeWorkflowId, string? CurativeWorkflowName);
