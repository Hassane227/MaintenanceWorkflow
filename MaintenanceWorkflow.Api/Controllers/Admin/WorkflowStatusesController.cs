using MaintenanceWorkflow.Api.Contracts.Admin;
using MaintenanceWorkflow.Api.Data;
using MaintenanceWorkflow.Api.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MaintenanceWorkflow.Api.Controllers.Admin;

[ApiController]
[Route("api/admin/workflows/{workflowId:int}/statuses")]
public class WorkflowStatusesController(AppDbContext dbContext) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<WorkflowStatusDto>> Create(int workflowId, [FromBody] CreateWorkflowStatusRequest request, CancellationToken cancellationToken)
    {
        var workflowExists = await dbContext.Workflows
            .AsNoTracking()
            .AnyAsync(x => x.Id == workflowId, cancellationToken);

        if (!workflowExists)
        {
            return NotFound("Workflow not found.");
        }

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest("Status name is required.");
        }

        if (request.IsInitial)
        {
            await dbContext.WorkflowStatuses
                .Where(x => x.WorkflowId == workflowId && x.IsInitial)
                .ExecuteUpdateAsync(setters => setters.SetProperty(x => x.IsInitial, false), cancellationToken);
        }

        var status = new WorkflowStatus
        {
            WorkflowId = workflowId,
            Name = request.Name.Trim(),
            Order = request.Order,
            IsInitial = request.IsInitial,
            IsFinal = request.IsFinal,
            IsActive = request.IsActive
        };

        dbContext.WorkflowStatuses.Add(status);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Created($"/api/admin/statuses/{status.Id}", ToDto(status));
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyCollection<WorkflowStatusDto>>> GetAll(int workflowId, CancellationToken cancellationToken)
    {
        var statuses = await dbContext.WorkflowStatuses
            .AsNoTracking()
            .Where(x => x.WorkflowId == workflowId)
            .OrderBy(x => x.Order)
            .ThenBy(x => x.Id)
            .Select(x => ToDto(x))
            .ToListAsync(cancellationToken);

        return Ok(statuses);
    }

    private static WorkflowStatusDto ToDto(WorkflowStatus status) =>
        new(status.Id, status.WorkflowId, status.Name, status.Order, status.IsInitial, status.IsFinal, status.IsActive);
}
