using MaintenanceWorkflow.Api.Contracts.Admin;
using MaintenanceWorkflow.Api.Data;
using MaintenanceWorkflow.Api.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MaintenanceWorkflow.Api.Controllers.Admin;

[ApiController]
[Route("api/admin/workflows")]
public class WorkflowsController(AppDbContext dbContext) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<WorkflowDto>> Create([FromBody] CreateWorkflowRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest("Workflow name is required.");
        }

        var workflow = new Workflow
        {
            Name = request.Name.Trim(),
            IsActive = true
        };

        dbContext.Workflows.Add(workflow);
        await dbContext.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id = workflow.Id }, ToDto(workflow));
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyCollection<WorkflowDto>>> GetAll(CancellationToken cancellationToken)
    {
        var workflows = await dbContext.Workflows
            .AsNoTracking()
            .OrderBy(x => x.Name)
            .Select(x => ToDto(x))
            .ToListAsync(cancellationToken);

        return Ok(workflows);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<WorkflowDto>> GetById(int id, CancellationToken cancellationToken)
    {
        var workflow = await dbContext.Workflows
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        return workflow is null ? NotFound() : Ok(ToDto(workflow));
    }

    private static WorkflowDto ToDto(Workflow workflow) => new(workflow.Id, workflow.Name, workflow.IsActive);
}
