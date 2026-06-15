using HR.Application.DTOs.Transportation;
using HR.Application.Transportation;
using HR.Domain.Enums;
using HR.Tests.TestInfrastructure;

namespace HR.Tests.Transportation;

public class TripAccessScopeTests
{
    // ========== User Story 1: Employee Own Trip Privacy ==========

    [Fact]
    public async Task EmployeeList_ReturnsOwnTripsOnly()
    {
        await using var env = await CreateEnvironmentAsync();
        var emp = await env.AddEmployeeAsync("EMP-E1", "emp-e1@example.com", env.DefaultDepartment!.Id);
        var other = await env.AddEmployeeAsync("EMP-E2", "emp-e2@example.com", env.DefaultDepartment!.Id);
        var ownTrip = await env.AddTripAsync("Own Trip", new DateTimeOffset(2026, 6, 10, 12, 0, 0, TimeSpan.Zero), emp.Id);
        await env.AddTripAsync("Other Trip", new DateTimeOffset(2026, 6, 11, 12, 0, 0, TimeSpan.Zero), other.Id);
        var service = env.GetRequiredService<ITripService>();

        var result = await service.GetTripsAsync(emp.Id, null, 1, 25, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value!.Items);
        Assert.Equal(ownTrip.Id, result.Value.Items[0].Id);
    }

    [Fact]
    public async Task EmployeeDetail_OwnTrip_ReturnsSuccess()
    {
        await using var env = await CreateEnvironmentAsync();
        var emp = await env.AddEmployeeAsync("EMP-E3", "emp-e3@example.com", env.DefaultDepartment!.Id);
        var trip = await env.AddTripAsync("My Trip", new DateTimeOffset(2026, 6, 10, 12, 0, 0, TimeSpan.Zero), emp.Id);
        var service = env.GetRequiredService<ITripService>();

        var result = await service.GetTripByIdAsync(emp.Id, trip.Id, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(trip.Id, result.Value!.Id);
    }

    [Fact]
    public async Task EmployeeDetail_OtherEmployeeTrip_ReturnsForbidden()
    {
        await using var env = await CreateEnvironmentAsync();
        var emp = await env.AddEmployeeAsync("EMP-E4", "emp-e4@example.com", env.DefaultDepartment!.Id);
        var other = await env.AddEmployeeAsync("EMP-E5", "emp-e5@example.com", env.DefaultDepartment!.Id);
        var otherTrip = await env.AddTripAsync("Other's Trip", new DateTimeOffset(2026, 6, 10, 12, 0, 0, TimeSpan.Zero), other.Id);
        var service = env.GetRequiredService<ITripService>();

        var result = await service.GetTripByIdAsync(emp.Id, otherTrip.Id, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("FORBIDDEN", result.Error!.Code);
    }

    [Fact]
    public async Task EmployeeDetail_MissingTrip_ReturnsNotFound()
    {
        await using var env = await CreateEnvironmentAsync();
        var emp = await env.AddEmployeeAsync("EMP-E6", "emp-e6@example.com", env.DefaultDepartment!.Id);
        var service = env.GetRequiredService<ITripService>();

        var result = await service.GetTripByIdAsync(emp.Id, Guid.NewGuid(), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("NOT_FOUND", result.Error!.Code);
    }

    [Fact]
    public async Task EmployeeCreate_Self_Succeeds()
    {
        await using var env = await CreateEnvironmentAsync();
        var emp = await env.AddEmployeeAsync("EMP-E7", "emp-e7@example.com", env.DefaultDepartment!.Id);
        var service = env.GetRequiredService<ITripService>();

        var result = await service.CreateTripAsync(
            emp.Id,
            BuildRequest(emp.Id, new DateOnly(2026, 6, 10)),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(emp.Id, result.Value!.RequestedByEmployeeId);
    }

    [Fact]
    public async Task EmployeeCreate_AnotherEmployee_ReturnsForbidden()
    {
        await using var env = await CreateEnvironmentAsync();
        var emp = await env.AddEmployeeAsync("EMP-E8", "emp-e8@example.com", env.DefaultDepartment!.Id);
        var other = await env.AddEmployeeAsync("EMP-E9", "emp-e9@example.com", env.DefaultDepartment!.Id);
        var service = env.GetRequiredService<ITripService>();

        var result = await service.CreateTripAsync(
            emp.Id,
            BuildRequest(other.Id, new DateOnly(2026, 6, 10)),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("FORBIDDEN", result.Error!.Code);
    }

    [Fact]
    public async Task EmployeeDelete_OwnTrip_Succeeds()
    {
        await using var env = await CreateEnvironmentAsync();
        var emp = await env.AddEmployeeAsync("EMP-E10", "emp-e10@example.com", env.DefaultDepartment!.Id);
        var trip = await env.AddTripAsync("My Trip", new DateTimeOffset(2026, 6, 10, 12, 0, 0, TimeSpan.Zero), emp.Id);
        var service = env.GetRequiredService<ITripService>();

        var result = await service.DeleteTripAsync(emp.Id, trip.Id, CancellationToken.None);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task EmployeeDelete_OtherEmployeeTrip_ReturnsForbidden()
    {
        await using var env = await CreateEnvironmentAsync();
        var emp = await env.AddEmployeeAsync("EMP-E11", "emp-e11@example.com", env.DefaultDepartment!.Id);
        var other = await env.AddEmployeeAsync("EMP-E12", "emp-e12@example.com", env.DefaultDepartment!.Id);
        var otherTrip = await env.AddTripAsync("Other's Trip", new DateTimeOffset(2026, 6, 10, 12, 0, 0, TimeSpan.Zero), other.Id);
        var service = env.GetRequiredService<ITripService>();

        var result = await service.DeleteTripAsync(emp.Id, otherTrip.Id, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("FORBIDDEN", result.Error!.Code);
    }

    [Fact]
    public async Task EmployeeDelete_MissingTrip_ReturnsNotFound()
    {
        await using var env = await CreateEnvironmentAsync();
        var emp = await env.AddEmployeeAsync("EMP-E13", "emp-e13@example.com", env.DefaultDepartment!.Id);
        var service = env.GetRequiredService<ITripService>();

        var result = await service.DeleteTripAsync(emp.Id, Guid.NewGuid(), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("NOT_FOUND", result.Error!.Code);
    }

    [Fact]
    public async Task EmployeeList_OutOfScopeTravelerFilter_ReturnsEmptyPage()
    {
        await using var env = await CreateEnvironmentAsync();
        var emp = await env.AddEmployeeAsync("EMP-E14", "emp-e14@example.com", env.DefaultDepartment!.Id);
        var other = await env.AddEmployeeAsync("EMP-E15", "emp-e15@example.com", env.DefaultDepartment!.Id);
        var service = env.GetRequiredService<ITripService>();

        var result = await service.GetTripsAsync(emp.Id, other.Id, 1, 25, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value!.Items);
        Assert.Equal(0, result.Value.TotalCount);
    }

    // ========== User Story 2: Manager Own and Team Trip Operations ==========

    [Fact]
    public async Task ManagerList_ReturnsOwnAndDirectAndIndirectReportTrips()
    {
        await using var env = await CreateEnvironmentAsync();
        var manager = await env.AddEmployeeAsync("EMP-M1", "emp-m1@example.com", env.DefaultDepartment!.Id, role: EmployeeRole.Manager);
        var direct = await env.AddEmployeeAsync("EMP-M2", "emp-m2@example.com", env.DefaultDepartment!.Id, managerId: manager.Id);
        var indirect = await env.AddEmployeeAsync("EMP-M3", "emp-m3@example.com", env.DefaultDepartment!.Id, managerId: direct.Id);
        var ownTrip = await env.AddTripAsync("Manager Trip", new DateTimeOffset(2026, 6, 10, 12, 0, 0, TimeSpan.Zero), manager.Id);
        var directTrip = await env.AddTripAsync("Direct Trip", new DateTimeOffset(2026, 6, 11, 12, 0, 0, TimeSpan.Zero), direct.Id);
        var indirectTrip = await env.AddTripAsync("Indirect Trip", new DateTimeOffset(2026, 6, 12, 12, 0, 0, TimeSpan.Zero), indirect.Id);
        var service = env.GetRequiredService<ITripService>();

        var result = await service.GetTripsAsync(manager.Id, null, 1, 25, CancellationToken.None);

        Assert.True(result.IsSuccess);
        var tripIds = result.Value!.Items.Select(t => t.Id).ToHashSet();
        Assert.Contains(ownTrip.Id, tripIds);
        Assert.Contains(directTrip.Id, tripIds);
        Assert.Contains(indirectTrip.Id, tripIds);
    }

    [Fact]
    public async Task ManagerList_ExcludesPeerAndUnrelatedTrips()
    {
        await using var env = await CreateEnvironmentAsync();
        var manager = await env.AddEmployeeAsync("EMP-M4", "emp-m4@example.com", env.DefaultDepartment!.Id, role: EmployeeRole.Manager);
        var peer = await env.AddEmployeeAsync("EMP-M5", "emp-m5@example.com", env.DefaultDepartment!.Id, role: EmployeeRole.Manager);
        var unrelated = await env.AddEmployeeAsync("EMP-M6", "emp-m6@example.com", env.DefaultDepartment!.Id);
        await env.AddTripAsync("Peer Trip", new DateTimeOffset(2026, 6, 10, 12, 0, 0, TimeSpan.Zero), peer.Id);
        await env.AddTripAsync("Unrelated Trip", new DateTimeOffset(2026, 6, 11, 12, 0, 0, TimeSpan.Zero), unrelated.Id);
        var service = env.GetRequiredService<ITripService>();

        var result = await service.GetTripsAsync(manager.Id, null, 1, 25, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value!.Items);
    }

    [Fact]
    public async Task ManagerList_ExcludesSoftDeletedAndTerminatedReportTrips()
    {
        await using var env = await CreateEnvironmentAsync();
        var manager = await env.AddEmployeeAsync("EMP-M7", "emp-m7@example.com", env.DefaultDepartment!.Id, role: EmployeeRole.Manager);
        var softDeleted = await env.AddEmployeeAsync("EMP-M8", "emp-m8@example.com", env.DefaultDepartment!.Id, managerId: manager.Id, status: EmployeeStatus.Suspended, isDeleted: true, terminatedAt: new DateTimeOffset(2026, 6, 1, 0, 0, 0, TimeSpan.Zero));
        var terminated = await env.AddEmployeeAsync("EMP-M9", "emp-m9@example.com", env.DefaultDepartment!.Id, managerId: manager.Id, status: EmployeeStatus.Terminated, terminatedAt: new DateTimeOffset(2026, 6, 1, 0, 0, 0, TimeSpan.Zero));
        await env.AddTripAsync("Deleted Report Trip", new DateTimeOffset(2026, 6, 10, 12, 0, 0, TimeSpan.Zero), softDeleted.Id);
        await env.AddTripAsync("Terminated Report Trip", new DateTimeOffset(2026, 6, 11, 12, 0, 0, TimeSpan.Zero), terminated.Id);
        var service = env.GetRequiredService<ITripService>();

        var result = await service.GetTripsAsync(manager.Id, null, 1, 25, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value!.Items);
    }

    [Fact]
    public async Task ManagerDetail_OwnAndTeamTrips_Succeed()
    {
        await using var env = await CreateEnvironmentAsync();
        var manager = await env.AddEmployeeAsync("EMP-M10", "emp-m10@example.com", env.DefaultDepartment!.Id, role: EmployeeRole.Manager);
        var direct = await env.AddEmployeeAsync("EMP-M11", "emp-m11@example.com", env.DefaultDepartment!.Id, managerId: manager.Id);
        var ownTrip = await env.AddTripAsync("Own", new DateTimeOffset(2026, 6, 10, 12, 0, 0, TimeSpan.Zero), manager.Id);
        var teamTrip = await env.AddTripAsync("Team", new DateTimeOffset(2026, 6, 11, 12, 0, 0, TimeSpan.Zero), direct.Id);
        var service = env.GetRequiredService<ITripService>();

        var ownResult = await service.GetTripByIdAsync(manager.Id, ownTrip.Id, CancellationToken.None);
        var teamResult = await service.GetTripByIdAsync(manager.Id, teamTrip.Id, CancellationToken.None);

        Assert.True(ownResult.IsSuccess);
        Assert.True(teamResult.IsSuccess);
    }

    [Fact]
    public async Task ManagerDetail_PeerTrip_ReturnsForbidden()
    {
        await using var env = await CreateEnvironmentAsync();
        var manager = await env.AddEmployeeAsync("EMP-M12", "emp-m12@example.com", env.DefaultDepartment!.Id, role: EmployeeRole.Manager);
        var peer = await env.AddEmployeeAsync("EMP-M13", "emp-m13@example.com", env.DefaultDepartment!.Id, role: EmployeeRole.Manager);
        var peerTrip = await env.AddTripAsync("Peer", new DateTimeOffset(2026, 6, 10, 12, 0, 0, TimeSpan.Zero), peer.Id);
        var service = env.GetRequiredService<ITripService>();

        var result = await service.GetTripByIdAsync(manager.Id, peerTrip.Id, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("FORBIDDEN", result.Error!.Code);
    }

    [Fact]
    public async Task ManagerCreate_SelfAndTeam_Succeed()
    {
        await using var env = await CreateEnvironmentAsync();
        var manager = await env.AddEmployeeAsync("EMP-M14", "emp-m14@example.com", env.DefaultDepartment!.Id, role: EmployeeRole.Manager);
        var direct = await env.AddEmployeeAsync("EMP-M15", "emp-m15@example.com", env.DefaultDepartment!.Id, managerId: manager.Id);
        var service = env.GetRequiredService<ITripService>();

        var selfResult = await service.CreateTripAsync(
            manager.Id,
            BuildRequest(manager.Id, new DateOnly(2026, 6, 10)),
            CancellationToken.None);

        var teamResult = await service.CreateTripAsync(
            manager.Id,
            BuildRequest(direct.Id, new DateOnly(2026, 6, 11)),
            CancellationToken.None);

        Assert.True(selfResult.IsSuccess);
        Assert.True(teamResult.IsSuccess);
    }

    [Fact]
    public async Task ManagerCreate_Peer_ReturnsForbidden()
    {
        await using var env = await CreateEnvironmentAsync();
        var manager = await env.AddEmployeeAsync("EMP-M16", "emp-m16@example.com", env.DefaultDepartment!.Id, role: EmployeeRole.Manager);
        var peer = await env.AddEmployeeAsync("EMP-M17", "emp-m17@example.com", env.DefaultDepartment!.Id, role: EmployeeRole.Manager);
        var service = env.GetRequiredService<ITripService>();

        var result = await service.CreateTripAsync(
            manager.Id,
            BuildRequest(peer.Id, new DateOnly(2026, 6, 10)),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("FORBIDDEN", result.Error!.Code);
    }

    [Fact]
    public async Task ManagerDelete_OwnAndTeamTrips_Succeed()
    {
        await using var env = await CreateEnvironmentAsync();
        var manager = await env.AddEmployeeAsync("EMP-M18", "emp-m18@example.com", env.DefaultDepartment!.Id, role: EmployeeRole.Manager);
        var direct = await env.AddEmployeeAsync("EMP-M19", "emp-m19@example.com", env.DefaultDepartment!.Id, managerId: manager.Id);
        var ownTrip = await env.AddTripAsync("Own", new DateTimeOffset(2026, 6, 10, 12, 0, 0, TimeSpan.Zero), manager.Id);
        var teamTrip = await env.AddTripAsync("Team", new DateTimeOffset(2026, 6, 11, 12, 0, 0, TimeSpan.Zero), direct.Id);
        var service = env.GetRequiredService<ITripService>();

        var ownResult = await service.DeleteTripAsync(manager.Id, ownTrip.Id, CancellationToken.None);
        var teamResult = await service.DeleteTripAsync(manager.Id, teamTrip.Id, CancellationToken.None);

        Assert.True(ownResult.IsSuccess);
        Assert.True(teamResult.IsSuccess);
    }

    [Fact]
    public async Task ManagerDelete_PeerTrip_ReturnsForbidden()
    {
        await using var env = await CreateEnvironmentAsync();
        var manager = await env.AddEmployeeAsync("EMP-M20", "emp-m20@example.com", env.DefaultDepartment!.Id, role: EmployeeRole.Manager);
        var peer = await env.AddEmployeeAsync("EMP-M21", "emp-m21@example.com", env.DefaultDepartment!.Id, role: EmployeeRole.Manager);
        var peerTrip = await env.AddTripAsync("Peer", new DateTimeOffset(2026, 6, 10, 12, 0, 0, TimeSpan.Zero), peer.Id);
        var service = env.GetRequiredService<ITripService>();

        var result = await service.DeleteTripAsync(manager.Id, peerTrip.Id, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("FORBIDDEN", result.Error!.Code);
    }

    [Fact]
    public async Task Manager_WithNoActiveTeam_SeesOnlyOwnTrips()
    {
        await using var env = await CreateEnvironmentAsync();
        var manager = await env.AddEmployeeAsync("EMP-M22", "emp-m22@example.com", env.DefaultDepartment!.Id, role: EmployeeRole.Manager);
        var other = await env.AddEmployeeAsync("EMP-M23", "emp-m23@example.com", env.DefaultDepartment!.Id);
        var ownTrip = await env.AddTripAsync("Own", new DateTimeOffset(2026, 6, 10, 12, 0, 0, TimeSpan.Zero), manager.Id);
        await env.AddTripAsync("Other", new DateTimeOffset(2026, 6, 11, 12, 0, 0, TimeSpan.Zero), other.Id);
        var service = env.GetRequiredService<ITripService>();

        var result = await service.GetTripsAsync(manager.Id, null, 1, 25, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value!.Items);
        Assert.Equal(ownTrip.Id, result.Value.Items[0].Id);
    }

    // ========== User Story 3: HR/System Organization Trip Operations ==========

    [Fact]
    public async Task HRAdminList_ReturnsAllTrips()
    {
        await using var env = await CreateEnvironmentAsync();
        var hr = await env.AddEmployeeAsync("EMP-H1", "emp-h1@example.com", env.DefaultDepartment!.Id, role: EmployeeRole.HRAdministrator);
        var emp1 = await env.AddEmployeeAsync("EMP-H2", "emp-h2@example.com", env.DefaultDepartment!.Id);
        var emp2 = await env.AddEmployeeAsync("EMP-H3", "emp-h3@example.com", env.DefaultDepartment!.Id);
        var trip1 = await env.AddTripAsync("Trip A", new DateTimeOffset(2026, 6, 10, 12, 0, 0, TimeSpan.Zero), emp1.Id);
        var trip2 = await env.AddTripAsync("Trip B", new DateTimeOffset(2026, 6, 11, 12, 0, 0, TimeSpan.Zero), emp2.Id);
        var service = env.GetRequiredService<ITripService>();

        var result = await service.GetTripsAsync(hr.Id, null, 1, 25, CancellationToken.None);

        Assert.True(result.IsSuccess);
        var tripIds = result.Value!.Items.Select(t => t.Id).ToHashSet();
        Assert.Contains(trip1.Id, tripIds);
        Assert.Contains(trip2.Id, tripIds);
    }

    [Fact]
    public async Task HRAdminDetail_AnyTrip_Succeeds()
    {
        await using var env = await CreateEnvironmentAsync();
        var hr = await env.AddEmployeeAsync("EMP-H4", "emp-h4@example.com", env.DefaultDepartment!.Id, role: EmployeeRole.HRAdministrator);
        var emp = await env.AddEmployeeAsync("EMP-H5", "emp-h5@example.com", env.DefaultDepartment!.Id);
        var trip = await env.AddTripAsync("Some Trip", new DateTimeOffset(2026, 6, 10, 12, 0, 0, TimeSpan.Zero), emp.Id);
        var service = env.GetRequiredService<ITripService>();

        var result = await service.GetTripByIdAsync(hr.Id, trip.Id, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(trip.Id, result.Value!.Id);
    }

    [Fact]
    public async Task HRAdminCreate_ForEligibleEmployee_Succeeds()
    {
        await using var env = await CreateEnvironmentAsync();
        var hr = await env.AddEmployeeAsync("EMP-H6", "emp-h6@example.com", env.DefaultDepartment!.Id, role: EmployeeRole.HRAdministrator);
        var emp = await env.AddEmployeeAsync("EMP-H7", "emp-h7@example.com", env.DefaultDepartment!.Id);
        var service = env.GetRequiredService<ITripService>();

        var result = await service.CreateTripAsync(
            hr.Id,
            BuildRequest(emp.Id, new DateOnly(2026, 6, 10)),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(emp.Id, result.Value!.RequestedByEmployeeId);
    }

    [Fact]
    public async Task HRAdminDelete_AnyTrip_Succeeds()
    {
        await using var env = await CreateEnvironmentAsync();
        var hr = await env.AddEmployeeAsync("EMP-H8", "emp-h8@example.com", env.DefaultDepartment!.Id, role: EmployeeRole.HRAdministrator);
        var emp = await env.AddEmployeeAsync("EMP-H9", "emp-h9@example.com", env.DefaultDepartment!.Id);
        var trip = await env.AddTripAsync("Deletable Trip", new DateTimeOffset(2026, 6, 10, 12, 0, 0, TimeSpan.Zero), emp.Id);
        var service = env.GetRequiredService<ITripService>();

        var result = await service.DeleteTripAsync(hr.Id, trip.Id, CancellationToken.None);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task SystemAdminList_ReturnsAllTrips()
    {
        await using var env = await CreateEnvironmentAsync();
        var sysAdmin = await env.AddEmployeeAsync("EMP-S1", "emp-s1@example.com", env.DefaultDepartment!.Id, role: EmployeeRole.SystemAdministrator);
        var emp = await env.AddEmployeeAsync("EMP-S2", "emp-s2@example.com", env.DefaultDepartment!.Id);
        var trip = await env.AddTripAsync("SysAdmin Trip", new DateTimeOffset(2026, 6, 10, 12, 0, 0, TimeSpan.Zero), emp.Id);
        var service = env.GetRequiredService<ITripService>();

        var result = await service.GetTripsAsync(sysAdmin.Id, null, 1, 25, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Contains(trip.Id, result.Value!.Items.Select(t => t.Id));
    }

    [Fact]
    public async Task SystemAdminDetail_AnyTrip_Succeeds()
    {
        await using var env = await CreateEnvironmentAsync();
        var sysAdmin = await env.AddEmployeeAsync("EMP-S3", "emp-s3@example.com", env.DefaultDepartment!.Id, role: EmployeeRole.SystemAdministrator);
        var emp = await env.AddEmployeeAsync("EMP-S4", "emp-s4@example.com", env.DefaultDepartment!.Id);
        var trip = await env.AddTripAsync("Any Trip", new DateTimeOffset(2026, 6, 10, 12, 0, 0, TimeSpan.Zero), emp.Id);
        var service = env.GetRequiredService<ITripService>();

        var result = await service.GetTripByIdAsync(sysAdmin.Id, trip.Id, CancellationToken.None);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task SystemAdminCreate_ForEligibleEmployee_Succeeds()
    {
        await using var env = await CreateEnvironmentAsync();
        var sysAdmin = await env.AddEmployeeAsync("EMP-S5", "emp-s5@example.com", env.DefaultDepartment!.Id, role: EmployeeRole.SystemAdministrator);
        var emp = await env.AddEmployeeAsync("EMP-S6", "emp-s6@example.com", env.DefaultDepartment!.Id);
        var service = env.GetRequiredService<ITripService>();

        var result = await service.CreateTripAsync(
            sysAdmin.Id,
            BuildRequest(emp.Id, new DateOnly(2026, 6, 10)),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task SystemAdminDelete_AnyTrip_Succeeds()
    {
        await using var env = await CreateEnvironmentAsync();
        var sysAdmin = await env.AddEmployeeAsync("EMP-S7", "emp-s7@example.com", env.DefaultDepartment!.Id, role: EmployeeRole.SystemAdministrator);
        var emp = await env.AddEmployeeAsync("EMP-S8", "emp-s8@example.com", env.DefaultDepartment!.Id);
        var trip = await env.AddTripAsync("SysAdmin Delete", new DateTimeOffset(2026, 6, 10, 12, 0, 0, TimeSpan.Zero), emp.Id);
        var service = env.GetRequiredService<ITripService>();

        var result = await service.DeleteTripAsync(sysAdmin.Id, trip.Id, CancellationToken.None);

        Assert.True(result.IsSuccess);
    }

    // ========== Suspended Requester Edge Case ==========

    [Fact]
    public async Task SuspendedRequester_RetainsSelfScope()
    {
        await using var env = await CreateEnvironmentAsync();
        var suspended = await env.AddEmployeeAsync("EMP-SUS", "emp-sus@example.com", env.DefaultDepartment!.Id, status: EmployeeStatus.Suspended);
        var service = env.GetRequiredService<ITripService>();

        var listResult = await service.GetTripsAsync(suspended.Id, null, 1, 25, CancellationToken.None);
        var detailResult = await service.GetTripByIdAsync(suspended.Id, Guid.NewGuid(), CancellationToken.None);
        var createResult = await service.CreateTripAsync(
            suspended.Id,
            BuildRequest(suspended.Id, new DateOnly(2026, 6, 10)),
            CancellationToken.None);
        var deleteResult = await service.DeleteTripAsync(suspended.Id, Guid.NewGuid(), CancellationToken.None);

        Assert.True(listResult.IsSuccess);
        Assert.Empty(listResult.Value!.Items);
        Assert.False(detailResult.IsSuccess);
        Assert.Equal("NOT_FOUND", detailResult.Error!.Code);
        Assert.False(createResult.IsSuccess);
        Assert.Equal("BUSINESS_RULE_VIOLATION", createResult.Error!.Code);
        Assert.False(deleteResult.IsSuccess);
        Assert.Equal("NOT_FOUND", deleteResult.Error!.Code);
    }

    // ========== Missing employee_id claim test ==========

    [Fact]
    public async Task MissingEmployeeIdClaim_ReturnsUnauthorized()
    {
        await using var env = await CreateEnvironmentAsync();
        var service = env.GetRequiredService<ITripService>();

        var listResult = await service.GetTripsAsync(Guid.NewGuid(), null, 1, 25, CancellationToken.None);

        Assert.False(listResult.IsSuccess);
        Assert.Equal("UNAUTHORIZED", listResult.Error!.Code);
    }

    private static async Task<SqliteTestEnvironment> CreateEnvironmentAsync()
    {
        return await SqliteTestEnvironment.CreateAsync(
            seedDefaultDepartment: true,
            timeProvider: new TestTimeProvider(new DateTimeOffset(2026, 6, 3, 12, 0, 0, TimeSpan.Zero)));
    }

    private static TripCreateRequest BuildRequest(Guid requestedByEmployeeId, DateOnly tripDate)
    {
        return new TripCreateRequest
        {
            ReferenceName = "Team Shuttle",
            Project = "HR Revamp",
            Route = "HQ to Client",
            TripType = "Business",
            TripDate = tripDate,
            RequestedByEmployeeId = requestedByEmployeeId
        };
    }
}
