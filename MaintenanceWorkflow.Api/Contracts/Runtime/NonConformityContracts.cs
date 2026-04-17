namespace MaintenanceWorkflow.Api.Contracts.Runtime;

public record CreateNonConformityRequest(int CompanyId, string Title);

public record NonConformityListItemDto(int Id, string Title, string CompanyName, string WorkflowName, string CurrentStatusName, DateTime CreatedAtUtc);

public record NonConformityDetailsDto(
    int Id,
    string Title,
    int CompanyId,
    string CompanyName,
    int WorkflowId,
    string WorkflowName,
    int CurrentStatusId,
    string CurrentStatusName,
    DateTime CreatedAtUtc,
    IReadOnlyCollection<StatusHistoryDto> History);

public record StatusHistoryDto(
    int Id,
    int FromStatusId,
    string FromStatusName,
    int ToStatusId,
    string ToStatusName,
    string ActionName,
    string RoleUsed,
    string PerformedBy,
    DateTime DateUtc);

public record AvailableActionDto(int TransitionId, string ActionName, int ToStatusId, string ToStatusName);

public record ExecuteTransitionRequest(int TransitionId, string Role);
