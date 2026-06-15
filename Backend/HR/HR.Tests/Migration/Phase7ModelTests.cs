using HR.Domain.Entities;
using HR.Domain.Enums;
using HR.Infrastructure.Data;
using HR.Tests.TestInfrastructure;
using Microsoft.EntityFrameworkCore;

namespace HR.Tests.Migration;

public class Phase7ModelTests
{
    [Fact]
    public async Task Model_ContainsPhase7DbSetsAndEmployeeRoleShape()
    {
        await using var environment = await SqliteTestEnvironment.CreateAsync();
        var contextType = typeof(ApplicationDbContext);
        var model = environment.Context.Model;
        var employeeEntity = model.FindEntityType(typeof(Employee));

        Assert.NotNull(contextType.GetProperty(nameof(ApplicationDbContext.AttendanceRecords)));
        Assert.NotNull(contextType.GetProperty(nameof(ApplicationDbContext.EmployeeCompensations)));
        Assert.NotNull(contextType.GetProperty(nameof(ApplicationDbContext.SalaryHistoryEntries)));
        Assert.NotNull(contextType.GetProperty(nameof(ApplicationDbContext.EmployeeDocuments)));
        Assert.NotNull(contextType.GetProperty(nameof(ApplicationDbContext.AuditLogEntries)));
        Assert.NotNull(model.FindEntityType(typeof(AttendanceRecord)));
        Assert.NotNull(model.FindEntityType(typeof(EmployeeCompensation)));
        Assert.NotNull(model.FindEntityType(typeof(SalaryHistoryEntry)));
        Assert.NotNull(model.FindEntityType(typeof(EmployeeDocument)));
        Assert.NotNull(model.FindEntityType(typeof(AuditLogEntry)));

        var role = employeeEntity!.FindProperty(nameof(Employee.Role));
        Assert.NotNull(role);
        Assert.Equal(typeof(EmployeeRole), role!.ClrType);
        Assert.Equal(32, role.GetMaxLength());
        Assert.Equal(EmployeeRole.Employee, role.GetDefaultValue());
        Assert.False(role.IsNullable);
    }

    [Fact]
    public async Task Model_ContainsPhase7RequiredIndexes()
    {
        await using var environment = await SqliteTestEnvironment.CreateAsync();
        var model = environment.Context.Model;

        var attendance = model.FindEntityType(typeof(AttendanceRecord))!;
        Assert.True(attendance.FindIndex(
            [
                attendance.FindProperty(nameof(AttendanceRecord.EmployeeId))!,
                attendance.FindProperty(nameof(AttendanceRecord.AttendanceDate))!
            ])!.IsUnique);
        Assert.NotNull(attendance.FindIndex(attendance.FindProperty(nameof(AttendanceRecord.AttendanceDate))!));

        var compensation = model.FindEntityType(typeof(EmployeeCompensation))!;
        Assert.True(compensation.FindIndex(compensation.FindProperty(nameof(EmployeeCompensation.EmployeeId))!)!.IsUnique);

        var salaryHistory = model.FindEntityType(typeof(SalaryHistoryEntry))!;
        Assert.NotNull(salaryHistory.FindIndex(
            [
                salaryHistory.FindProperty(nameof(SalaryHistoryEntry.EmployeeId))!,
                salaryHistory.FindProperty(nameof(SalaryHistoryEntry.ChangedAt))!
            ]));
        Assert.NotNull(salaryHistory.FindIndex(
            [
                salaryHistory.FindProperty(nameof(SalaryHistoryEntry.ChangedByEmployeeId))!,
                salaryHistory.FindProperty(nameof(SalaryHistoryEntry.ChangedAt))!
            ]));

        var documents = model.FindEntityType(typeof(EmployeeDocument))!;
        Assert.NotNull(documents.FindIndex(
            [
                documents.FindProperty(nameof(EmployeeDocument.EmployeeId))!,
                documents.FindProperty(nameof(EmployeeDocument.RemovedAt))!,
                documents.FindProperty(nameof(EmployeeDocument.UploadedAt))!
            ]));
        Assert.True(documents.FindIndex(documents.FindProperty(nameof(EmployeeDocument.StoredFileName))!)!.IsUnique);

        var audit = model.FindEntityType(typeof(AuditLogEntry))!;
        Assert.NotNull(audit.FindIndex(
            [
                audit.FindProperty(nameof(AuditLogEntry.EntityType))!,
                audit.FindProperty(nameof(AuditLogEntry.EntityId))!,
                audit.FindProperty(nameof(AuditLogEntry.PerformedAt))!
            ]));
        Assert.NotNull(audit.FindIndex(
            [
                audit.FindProperty(nameof(AuditLogEntry.ActorEmployeeId))!,
                audit.FindProperty(nameof(AuditLogEntry.PerformedAt))!
            ]));
        Assert.NotNull(audit.FindIndex(
            [
                audit.FindProperty(nameof(AuditLogEntry.ActionType))!,
                audit.FindProperty(nameof(AuditLogEntry.PerformedAt))!
            ]));
    }

    [Fact]
    public void Phase7TablesAreIntroducedOnlyByPhase7Migration()
    {
        var migrationsPath = GetRepositoryPath("HR.Infrastructure", "Data", "Migrations");
        const string phase7MigrationId = "20260606235241";
        var phase7 = File.ReadAllText(Path.Combine(migrationsPath, "20260606235241_Phase7AdvancedHrFeatures.cs"));
        var earlierMigrationFiles = Directory.GetFiles(migrationsPath, "*.cs")
            .Where(path =>
            {
                var fileName = Path.GetFileName(path);
                return !fileName.EndsWith(".Designer.cs", StringComparison.Ordinal)
                    && !fileName.Equals("ApplicationDbContextModelSnapshot.cs", StringComparison.Ordinal)
                    && string.CompareOrdinal(fileName[..14], phase7MigrationId) < 0;
            })
            .ToList();
        var phase7Tables = new[]
        {
            "AttendanceRecords",
            "EmployeeCompensations",
            "SalaryHistoryEntries",
            "EmployeeDocuments",
            "AuditLogEntries"
        };

        foreach (var table in phase7Tables)
        {
            Assert.Contains(table, phase7);
        }

        foreach (var earlier in earlierMigrationFiles)
        {
            var content = File.ReadAllText(earlier);
            foreach (var table in phase7Tables)
            {
                Assert.DoesNotContain(table, content);
            }
        }
    }

    private static string GetRepositoryPath(params string[] relativePath)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "HR.slnx")))
        {
            directory = directory.Parent;
        }

        Assert.NotNull(directory);
        return Path.Combine(new[] { directory!.FullName }.Concat(relativePath).ToArray());
    }
}
