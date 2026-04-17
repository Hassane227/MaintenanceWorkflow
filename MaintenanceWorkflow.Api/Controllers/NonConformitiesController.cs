using MaintenanceWorkflow.Api.Contracts.Runtime;
using MaintenanceWorkflow.Api.Data;
using MaintenanceWorkflow.Api.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MaintenanceWorkflow.Api.Controllers;

[ApiController]
[Route("api/nonconformities")]
public class NonConformitiesController(AppDbContext dbContext) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<NonConformityDetailsDto>> Create([FromBody] CreateNonConformityRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Title))
        {
            return BadRequest("Title is required.");
        }

        var company = await dbContext.Companies
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == request.CompanyId, cancellationToken);

        if (company is null)
        {
            return NotFound("Company not found.");
        }

        if (!company.CurativeWorkflowId.HasValue)
        {
            return BadRequest("Company has no curative workflow configured.");
        }

        var workflowId = company.CurativeWorkflowId.Value;

        var initialStatus = await dbContext.WorkflowStatuses
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.WorkflowId == workflowId && x.IsInitial && x.IsActive, cancellationToken);

        if (initialStatus is null)
        {
            return BadRequest("Workflow has no active initial status.");
        }

        var nc = new NonConformity
        {
            CompanyId = company.Id,
            WorkflowId = workflowId,
            CurrentStatusId = initialStatus.Id,
            Title = request.Title.Trim(),
            CreatedAtUtc = DateTime.UtcNow
        };

        dbContext.NonConformities.Add(nc);
        await dbContext.SaveChangesAsync(cancellationToken);

        var workflowName = await dbContext.Workflows
            .AsNoTracking()
            .Where(x => x.Id == workflowId)
            .Select(x => x.Name)
            .FirstAsync(cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id = nc.Id }, new NonConformityDetailsDto(
            nc.Id,
            nc.Title,
            company.Id,
            company.Name,
            workflowId,
            workflowName,
            initialStatus.Id,
            initialStatus.Name,
            nc.CreatedAtUtc,
            []));
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyCollection<NonConformityListItemDto>>> GetAll(CancellationToken cancellationToken)
    {
        var ncs = await dbContext.NonConformities
            .AsNoTracking()
            .OrderByDescending(x => x.CreatedAtUtc)
            .Select(x => new NonConformityListItemDto(
                x.Id,
                x.Title,
                x.Company.Name,
                x.Workflow.Name,
                x.CurrentStatus.Name,
                x.CreatedAtUtc))
            .ToListAsync(cancellationToken);

        return Ok(ncs);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<NonConformityDetailsDto>> GetById(int id, CancellationToken cancellationToken)
    {
        var nc = await dbContext.NonConformities
            .AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new NonConformityDetailsDto(
                x.Id,
                x.Title,
                x.CompanyId,
                x.Company.Name,
                x.WorkflowId,
                x.Workflow.Name,
                x.CurrentStatusId,
                x.CurrentStatus.Name,
                x.CreatedAtUtc,
                x.History
                    .OrderBy(h => h.DateUtc)
                    .Select(h => new StatusHistoryDto(
                        h.Id,
                        h.FromStatusId,
                        h.FromStatus.Name,
                        h.ToStatusId,
                        h.ToStatus.Name,
                        h.ActionName,
                        h.RoleUsed.ToString(),
                        h.PerformedBy,
                        h.DateUtc))
                    .ToList()))
            .FirstOrDefaultAsync(cancellationToken);

        return nc is null ? NotFound() : Ok(nc);
    }

    [HttpGet("{id:int}/actions")]
    public async Task<ActionResult<IReadOnlyCollection<AvailableActionDto>>> GetActions(int id, [FromQuery] string role, CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<RuntimeRole>(role, true, out var parsedRole))
        {
            return BadRequest("Invalid role.");
        }

        var nc = await dbContext.NonConformities
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (nc is null)
        {
            return NotFound();
        }

        var actions = await dbContext.WorkflowTransitions
            .AsNoTracking()
            .Where(x =>
                x.WorkflowId == nc.WorkflowId
                && x.FromStatusId == nc.CurrentStatusId
                && x.RoleAllowed == parsedRole
                && x.IsActive)
            .OrderBy(x => x.ActionName)
            .Select(x => new AvailableActionDto(x.Id, x.ActionName, x.ToStatusId, x.ToStatus.Name))
            .ToListAsync(cancellationToken);

        return Ok(actions);
    }

    [HttpPost("{id:int}/execute")]
    public async Task<ActionResult<NonConformityDetailsDto>> Execute(int id, [FromBody] ExecuteTransitionRequest request, CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<RuntimeRole>(request.Role, true, out var parsedRole))
        {
            return BadRequest("Invalid role.");
        }

        var nc = await dbContext.NonConformities.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (nc is null)
        {
            return NotFound("Non-conformity not found.");
        }

        var transition = await dbContext.WorkflowTransitions
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == request.TransitionId, cancellationToken);

        if (transition is null)
        {
            return NotFound("Transition not found.");
        }

        if (!transition.IsActive)
        {
            return BadRequest("Transition is not active.");
        }

        if (transition.FromStatusId != nc.CurrentStatusId)
        {
            return BadRequest("Transition does not match current status.");
        }

        if (transition.RoleAllowed != parsedRole)
        {
            return BadRequest("Role is not allowed for this transition.");
        }

        if (transition.WorkflowId != nc.WorkflowId)
        {
            return BadRequest("Transition does not belong to NC workflow.");
        }

        var history = new StatusHistory
        {
            NonConformityId = nc.Id,
            FromStatusId = transition.FromStatusId,
            ToStatusId = transition.ToStatusId,
            ActionName = transition.ActionName,
            RoleUsed = parsedRole,
            PerformedBy = "TEST",
            DateUtc = DateTime.UtcNow
        };

        nc.CurrentStatusId = transition.ToStatusId;
        dbContext.StatusHistories.Add(history);

        await dbContext.SaveChangesAsync(cancellationToken);

        var result = await GetById(id, cancellationToken);
        return result.Result is NotFoundResult ? NotFound() : Ok(((result.Result as OkObjectResult)?.Value as NonConformityDetailsDto) ?? result.Value);
    }
}
