using HR.Domain.Entities;
using HR.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace HR.Infrastructure.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : IdentityDbContext<ApplicationUser>(options)
{
    public DbSet<Employee> Employees => Set<Employee>();

    public DbSet<Department> Departments => Set<Department>();

    public DbSet<Trip> Trips => Set<Trip>();

    public DbSet<VacationRequest> VacationRequests => Set<VacationRequest>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
    }
}
