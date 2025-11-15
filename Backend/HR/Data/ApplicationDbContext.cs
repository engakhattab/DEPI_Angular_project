using HR.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace HR.Data;

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

        builder.Entity<Department>(entity =>
        {
            entity.Property(d => d.Name)
                .HasMaxLength(100)
                .IsRequired();

            entity.HasIndex(d => d.Name)
                .IsUnique();
        });

        builder.Entity<Employee>(entity =>
        {
            entity.Property(e => e.EmployeeNumber)
                .HasMaxLength(20)
                .IsRequired();

            entity.HasIndex(e => e.EmployeeNumber)
                .IsUnique();

            entity.Property(e => e.FullName)
                .HasMaxLength(200)
                .IsRequired();

            entity.Property(e => e.JobTitle)
                .HasMaxLength(150);

            entity.Property(e => e.Email)
                .HasMaxLength(256);

            entity.Property(e => e.PhoneNumber)
                .HasMaxLength(30);

            entity.Property(e => e.Status)
                .HasConversion<string>()
                .HasMaxLength(32);

            entity.HasOne(e => e.Department)
                .WithMany(d => d.Employees)
                .HasForeignKey(e => e.DepartmentId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Manager)
                .WithMany(e => e.DirectReports)
                .HasForeignKey(e => e.ManagerId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.IdentityUser)
                .WithOne(u => u!.Employee)
                .HasForeignKey<Employee>(e => e.ApplicationUserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<Trip>(entity =>
        {
            entity.Property(t => t.ReferenceName)
                .HasMaxLength(200)
                .IsRequired();

            entity.Property(t => t.Project)
                .HasMaxLength(150)
                .IsRequired();

            entity.Property(t => t.Route)
                .HasMaxLength(150)
                .IsRequired();

            entity.Property(t => t.TripType)
                .HasMaxLength(50)
                .IsRequired();

            entity.Property(t => t.TripCode)
                .HasMaxLength(32)
                .IsRequired();

            entity.HasIndex(t => t.TripCode)
                .IsUnique();

            entity.Property(t => t.RequestCode)
                .HasMaxLength(32)
                .IsRequired();

            entity.HasIndex(t => t.RequestCode)
                .IsUnique();

            entity.Property(t => t.CreatedAt)
                .IsRequired();
        });

        builder.Entity<VacationRequest>(entity =>
        {
            entity.Property(v => v.Reason)
                .HasMaxLength(500)
                .IsRequired();

            entity.Property(v => v.Status)
                .HasConversion<string>()
                .HasMaxLength(32);

            entity.Property(v => v.CreatedAt)
                .IsRequired();

            entity.HasOne(v => v.Employee)
                .WithMany()
                .HasForeignKey(v => v.EmployeeId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
