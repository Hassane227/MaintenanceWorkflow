namespace MaintenanceWorkflow.Api.Contracts.Admin;

public record CreateWorkflowStatusRequest(string Name, int Order, bool IsInitial, bool IsFinal, bool IsActive = true);

public record UpdateWorkflowStatusRequest(string Name, int Order, bool IsInitial, bool IsFinal, bool IsActive);

public record WorkflowStatusDto(int Id, int WorkflowId, string Name, int Order, bool IsInitial, bool IsFinal, bool IsActive);
