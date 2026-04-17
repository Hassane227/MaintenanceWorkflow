using MaintenanceWorkflow.Api.Contracts.Admin;
using MaintenanceWorkflow.Api.Data;
using MaintenanceWorkflow.Api.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MaintenanceWorkflow.Api.Controllers.Admin;

[ApiController]
[Route("api/admin/workflows/{workflowId:int}/transitions")]
public class WorkflowTransitionsController(AppDbContext dbContext) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<WorkflowTransitionDto>> Create(int workflowId, [FromBody] CreateWorkflowTransitionRequest request, CancellationToken cancellationToken)
    {
        var workflowExists = await dbContext.Workflows.AsNoTracking().AnyAsync(x => x.Id == workflowId, cancellationToken);
        if (!workflowExists)
        {
            return NotFound("Workflow not found.");
        }

        if (!Enum.TryParse<RuntimeRole>(request.RoleAllowed, true, out var roleAllowed))
        {
            return BadRequest("Invalid role.");
        }

        if (string.IsNullOrWhiteSpace(request.ActionName))
        {
            return BadRequest("ActionName is required.");
        }

        var statuses = await dbContext.WorkflowStatuses
            .AsNoTracking()
            .Where(x => x.WorkflowId == workflowId && (x.Id == request.FromStatusId || x.Id == request.ToStatusId))
            .Select(x => new { x.Id, x.Name })
            .ToListAsync(cancellationToken);

        var fromStatus = statuses.FirstOrDefault(x => x.Id == request.FromStatusId);
        var toStatus = statuses.FirstOrDefault(x => x.Id == request.ToStatusId);

        if (fromStatus is null || toStatus is null)
        {
            return BadRequest("FromStatusId and ToStatusId must belong to workflow.");
        }

        var transition = new WorkflowTransition
        {
            WorkflowId = workflowId,
            FromStatusId = request.FromStatusId,
            ToStatusId = request.ToStatusId,
            ActionName = request.ActionName.Trim(),
            RoleAllowed = roleAllowed,
            IsActive = request.IsActive
        };

        dbContext.WorkflowTransitions.Add(transition);

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            return Conflict("A transition with same workflow/from/action/role already exists.");
        }

        return Created($"/api/admin/transitions/{transition.Id}", ToDto(transition, fromStatus.Name, toStatus.Name));
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyCollection<WorkflowTransitionDto>>> GetAll(int workflowId, CancellationToken cancellationToken)
    {
        var transitions = await dbContext.WorkflowTransitions
            .AsNoTracking()
            .Where(x => x.WorkflowId == workflowId)
            .OrderBy(x => x.Id)
            .Select(x => new WorkflowTransitionDto(
                x.Id,
                x.WorkflowId,
                x.FromStatusId,
                x.FromStatus.Name,
                x.ToStatusId,
                x.ToStatus.Name,
                x.ActionName,
                x.RoleAllowed.ToString(),
                x.IsActive))
            .ToListAsync(cancellationToken);

        return Ok(transitions);
    }

    private static WorkflowTransitionDto ToDto(WorkflowTransition transition, string fromStatusName, string toStatusName) =>
        new(
            transition.Id,
            transition.WorkflowId,
            transition.FromStatusId,
            fromStatusName,
            transition.ToStatusId,
            toStatusName,
            transition.ActionName,
            transition.RoleAllowed.ToString(),
            transition.IsActive);
}
