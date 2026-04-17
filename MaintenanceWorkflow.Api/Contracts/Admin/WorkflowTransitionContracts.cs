namespace MaintenanceWorkflow.Api.Contracts.Admin;

public record CreateWorkflowTransitionRequest(int FromStatusId, int ToStatusId, string ActionName, string RoleAllowed, bool IsActive = true);

public record UpdateWorkflowTransitionRequest(int FromStatusId, int ToStatusId, string ActionName, string RoleAllowed, bool IsActive);

public record WorkflowTransitionDto(int Id, int WorkflowId, int FromStatusId, string FromStatusName, int ToStatusId, string ToStatusName, string ActionName, string RoleAllowed, bool IsActive);
