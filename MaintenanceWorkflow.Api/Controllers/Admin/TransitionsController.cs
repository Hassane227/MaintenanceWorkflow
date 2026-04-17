using MaintenanceWorkflow.Api.Contracts.Admin;
using MaintenanceWorkflow.Api.Data;
using MaintenanceWorkflow.Api.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MaintenanceWorkflow.Api.Controllers.Admin;

[ApiController]
[Route("api/admin/transitions")]
public class TransitionsController(AppDbContext dbContext) : ControllerBase
{
    [HttpPut("{transitionId:int}")]
    public async Task<ActionResult<WorkflowTransitionDto>> Update(int transitionId, [FromBody] UpdateWorkflowTransitionRequest request, CancellationToken cancellationToken)
    {
        var transition = await dbContext.WorkflowTransitions
            .Include(x => x.FromStatus)
            .Include(x => x.ToStatus)
            .FirstOrDefaultAsync(x => x.Id == transitionId, cancellationToken);

        if (transition is null)
        {
            return NotFound();
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
            .Where(x => x.WorkflowId == transition.WorkflowId && (x.Id == request.FromStatusId || x.Id == request.ToStatusId))
            .Select(x => new { x.Id, x.Name })
            .ToListAsync(cancellationToken);

        var fromStatus = statuses.FirstOrDefault(x => x.Id == request.FromStatusId);
        var toStatus = statuses.FirstOrDefault(x => x.Id == request.ToStatusId);

        if (fromStatus is null || toStatus is null)
        {
            return BadRequest("FromStatusId and ToStatusId must belong to workflow.");
        }

        transition.FromStatusId = request.FromStatusId;
        transition.ToStatusId = request.ToStatusId;
        transition.ActionName = request.ActionName.Trim();
        transition.RoleAllowed = roleAllowed;
        transition.IsActive = request.IsActive;

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            return Conflict("A transition with same workflow/from/action/role already exists.");
        }

        return Ok(new WorkflowTransitionDto(
            transition.Id,
            transition.WorkflowId,
            transition.FromStatusId,
            fromStatus.Name,
            transition.ToStatusId,
            toStatus.Name,
            transition.ActionName,
            transition.RoleAllowed.ToString(),
            transition.IsActive));
    }

    [HttpDelete("{transitionId:int}")]
    public async Task<IActionResult> Delete(int transitionId, CancellationToken cancellationToken)
    {
        var transition = await dbContext.WorkflowTransitions.FirstOrDefaultAsync(x => x.Id == transitionId, cancellationToken);

        if (transition is null)
        {
            return NotFound();
        }

        dbContext.WorkflowTransitions.Remove(transition);
        await dbContext.SaveChangesAsync(cancellationToken);

        return NoContent();
    }
}
