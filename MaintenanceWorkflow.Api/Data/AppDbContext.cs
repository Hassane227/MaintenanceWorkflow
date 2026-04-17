using MaintenanceWorkflow.Api.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace MaintenanceWorkflow.Api.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Company> Companies => Set<Company>();
    public DbSet<Workflow> Workflows => Set<Workflow>();
    public DbSet<WorkflowStatus> WorkflowStatuses => Set<WorkflowStatus>();
    public DbSet<WorkflowTransition> WorkflowTransitions => Set<WorkflowTransition>();
    public DbSet<NonConformity> NonConformities => Set<NonConformity>();
    public DbSet<StatusHistory> StatusHistories => Set<StatusHistory>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Company>(entity =>
        {
            entity.Property(x => x.Name).HasMaxLength(200).IsRequired();
            entity.HasOne(x => x.CurativeWorkflow)
                .WithMany()
                .HasForeignKey(x => x.CurativeWorkflowId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<Workflow>(entity =>
        {
            entity.Property(x => x.Name).HasMaxLength(200).IsRequired();
        });

        modelBuilder.Entity<WorkflowStatus>(entity =>
        {
            entity.Property(x => x.Name).HasMaxLength(200).IsRequired();
            entity.HasOne(x => x.Workflow)
                .WithMany(x => x.Statuses)
                .HasForeignKey(x => x.WorkflowId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<WorkflowTransition>(entity =>
        {
            entity.Property(x => x.ActionName).HasMaxLength(200).IsRequired();
            entity.Property(x => x.RoleAllowed).HasConversion<string>().HasMaxLength(40);

            entity.HasOne(x => x.Workflow)
                .WithMany(x => x.Transitions)
                .HasForeignKey(x => x.WorkflowId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(x => x.FromStatus)
                .WithMany(x => x.FromTransitions)
                .HasForeignKey(x => x.FromStatusId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.ToStatus)
                .WithMany(x => x.ToTransitions)
                .HasForeignKey(x => x.ToStatusId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(x => new { x.WorkflowId, x.FromStatusId, x.ActionName, x.RoleAllowed }).IsUnique();
        });

        modelBuilder.Entity<NonConformity>(entity =>
        {
            entity.Property(x => x.Title).HasMaxLength(250).IsRequired();
            entity.HasOne(x => x.Company)
                .WithMany(x => x.NonConformities)
                .HasForeignKey(x => x.CompanyId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Workflow)
                .WithMany()
                .HasForeignKey(x => x.WorkflowId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.CurrentStatus)
                .WithMany()
                .HasForeignKey(x => x.CurrentStatusId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<StatusHistory>(entity =>
        {
            entity.Property(x => x.ActionName).HasMaxLength(200).IsRequired();
            entity.Property(x => x.RoleUsed).HasConversion<string>().HasMaxLength(40);
            entity.Property(x => x.PerformedBy).HasMaxLength(200).IsRequired();

            entity.HasOne(x => x.NonConformity)
                .WithMany(x => x.History)
                .HasForeignKey(x => x.NonConformityId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.FromStatus)
                .WithMany()
                .HasForeignKey(x => x.FromStatusId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.ToStatus)
                .WithMany()
                .HasForeignKey(x => x.ToStatusId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
