using MaintenanceWorkflow.Api.Contracts.Admin;
using MaintenanceWorkflow.Api.Data;
using MaintenanceWorkflow.Api.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MaintenanceWorkflow.Api.Controllers.Admin;

[ApiController]
[Route("api/admin/companies")]
public class CompaniesController(AppDbContext dbContext) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<CompanyDto>> Create([FromBody] CreateCompanyRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest("Company name is required.");
        }

        var company = new Company
        {
            Name = request.Name.Trim()
        };

        dbContext.Companies.Add(company);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Created($"/api/admin/companies/{company.Id}", new CompanyDto(company.Id, company.Name, company.CurativeWorkflowId, null));
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyCollection<CompanyDto>>> GetAll(CancellationToken cancellationToken)
    {
        var companies = await dbContext.Companies
            .AsNoTracking()
            .Include(x => x.CurativeWorkflow)
            .OrderBy(x => x.Name)
            .Select(x => new CompanyDto(x.Id, x.Name, x.CurativeWorkflowId, x.CurativeWorkflow != null ? x.CurativeWorkflow.Name : null))
            .ToListAsync(cancellationToken);

        return Ok(companies);
    }

    [HttpPut("{companyId:int}/curative-workflow")]
    public async Task<ActionResult<CompanyDto>> AssignCurativeWorkflow(int companyId, [FromBody] AssignCurativeWorkflowRequest request, CancellationToken cancellationToken)
    {
        var company = await dbContext.Companies.FirstOrDefaultAsync(x => x.Id == companyId, cancellationToken);
        if (company is null)
        {
            return NotFound("Company not found.");
        }

        var workflow = await dbContext.Workflows
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == request.WorkflowId, cancellationToken);

        if (workflow is null)
        {
            return NotFound("Workflow not found.");
        }

        company.CurativeWorkflowId = workflow.Id;
        await dbContext.SaveChangesAsync(cancellationToken);

        return Ok(new CompanyDto(company.Id, company.Name, company.CurativeWorkflowId, workflow.Name));
    }
}
