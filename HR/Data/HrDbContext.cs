using Microsoft.EntityFrameworkCore;
using HR.Entities;
namespace HR.Data
{
    public class HrDbContext : DbContext
    {
        public HrDbContext(DbContextOptions<HrDbContext> options) : base(options) { }

        public DbSet<Department> Departments => Set<Department>();
        public DbSet<Position> Positions => Set<Position>();
        public DbSet<Employee> Employees => Set<Employee>();
        public DbSet<LeaveType> LeaveTypes => Set<LeaveType>();
        public DbSet<LeaveRequest> LeaveRequests => Set<LeaveRequest>();
        public DbSet<AttendanceRecord> AttendanceRecords => Set<AttendanceRecord>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Department
            modelBuilder.Entity<Department>()
                .HasIndex(d => d.Code).IsUnique();
            modelBuilder.Entity<Department>()
                .HasOne(d => d.Parent)
                .WithMany(p => p.Children)
                .HasForeignKey(d => d.ParentDepartmentId)
                .OnDelete(DeleteBehavior.Restrict);

            // Position
            modelBuilder.Entity<Position>()
                .HasIndex(p => new { p.DepartmentId, p.Code }).IsUnique();
            modelBuilder.Entity<Position>()
                .HasOne(p => p.Department)
                .WithMany(d => d.Positions)
                .HasForeignKey(p => p.DepartmentId)
                .OnDelete(DeleteBehavior.Cascade);

            // Employee
            modelBuilder.Entity<Employee>()
                .HasIndex(e => e.EmployeeNumber).IsUnique();
            modelBuilder.Entity<Employee>()
                .HasOne(e => e.Department)
                .WithMany(d => d.Employees)
                .HasForeignKey(e => e.DepartmentId)
                .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Employee>()
                .HasOne(e => e.Position)
                .WithMany(p => p.Employees)
                .HasForeignKey(e => e.PositionId)
                .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Employee>()
                .HasOne(e => e.Manager)
                .WithMany(m => m!.DirectReports)
                .HasForeignKey(e => e.ManagerId)
                .OnDelete(DeleteBehavior.Restrict);

            // Leave
            modelBuilder.Entity<LeaveType>()
                .HasIndex(t => t.Code).IsUnique();

            modelBuilder.Entity<LeaveRequest>()
                .HasOne(l => l.Employee)
                .WithMany(e => e.LeaveRequests)
                .HasForeignKey(l => l.EmployeeId);

            // Attendance
            modelBuilder.Entity<AttendanceRecord>()
                .HasIndex(a => new { a.EmployeeId, a.WorkDate }).IsUnique();

            // Seed minimal lookup data
            modelBuilder.Entity<LeaveType>().HasData(
                new LeaveType { Id = Guid.Parse("11111111-1111-1111-1111-111111111111"), Name = "Annual Leave", Code = "ANL", IsPaid = true, MaxPerYearDays = 21 },
                new LeaveType { Id = Guid.Parse("22222222-2222-2222-2222-222222222222"), Name = "Sick Leave", Code = "SICK", IsPaid = true, MaxPerYearDays = 10 }
            );
        }

    }
}
