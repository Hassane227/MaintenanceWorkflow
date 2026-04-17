using MaintenanceWorkflow.Api.Contracts.Admin;
using MaintenanceWorkflow.Api.Data;
using MaintenanceWorkflow.Api.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MaintenanceWorkflow.Api.Controllers.Admin;

[ApiController]
[Route("api/admin/statuses")]
public class StatusesController(AppDbContext dbContext) : ControllerBase
{
    [HttpPut("{statusId:int}")]
    public async Task<ActionResult<WorkflowStatusDto>> Update(int statusId, [FromBody] UpdateWorkflowStatusRequest request, CancellationToken cancellationToken)
    {
        var status = await dbContext.WorkflowStatuses.FirstOrDefaultAsync(x => x.Id == statusId, cancellationToken);

        if (status is null)
        {
            return NotFound();
        }

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest("Status name is required.");
        }

        if (request.IsInitial)
        {
            await dbContext.WorkflowStatuses
                .Where(x => x.WorkflowId == status.WorkflowId && x.Id != status.Id && x.IsInitial)
                .ExecuteUpdateAsync(setters => setters.SetProperty(x => x.IsInitial, false), cancellationToken);
        }

        status.Name = request.Name.Trim();
        status.Order = request.Order;
        status.IsInitial = request.IsInitial;
        status.IsFinal = request.IsFinal;
        status.IsActive = request.IsActive;

        await dbContext.SaveChangesAsync(cancellationToken);

        return Ok(new WorkflowStatusDto(status.Id, status.WorkflowId, status.Name, status.Order, status.IsInitial, status.IsFinal, status.IsActive));
    }

    [HttpDelete("{statusId:int}")]
    public async Task<IActionResult> Delete(int statusId, CancellationToken cancellationToken)
    {
        var status = await dbContext.WorkflowStatuses.FirstOrDefaultAsync(x => x.Id == statusId, cancellationToken);

        if (status is null)
        {
            return NotFound();
        }

        if (status.IsInitial)
        {
            var hasAnotherStatus = await dbContext.WorkflowStatuses
                .AsNoTracking()
                .AnyAsync(x => x.WorkflowId == status.WorkflowId && x.Id != status.Id, cancellationToken);

            if (hasAnotherStatus)
            {
                return BadRequest("Cannot delete initial status before assigning another initial status.");
            }
        }

        var usedAsCurrent = await dbContext.NonConformities
            .AsNoTracking()
            .AnyAsync(x => x.CurrentStatusId == statusId, cancellationToken);

        if (usedAsCurrent)
        {
            return BadRequest("Status is in use by non-conformities.");
        }

        dbContext.WorkflowStatuses.Remove(status);
        await dbContext.SaveChangesAsync(cancellationToken);

        return NoContent();
    }
}
