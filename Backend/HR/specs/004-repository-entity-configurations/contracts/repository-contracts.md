# Internal Repository Contracts: Phase 4

## Scope

These contracts are internal infrastructure boundaries. Public HTTP routes, DTOs, pagination envelopes, authentication behavior, and structured errors remain unchanged.

Repository implementations use the same scoped persistence session through dependency injection. Service implementations orchestrate business behavior through these contracts and do not reference `ApplicationDbContext`.

Repository page methods delegate EF Core query execution to the single infrastructure-owned `PagedQueryExecutor`. `HR.Shared.Pagination.PagedList<T>` remains an EF-free result container with constants and normalization only.

## Department Repository

```csharp
public interface IDepartmentRepository
{
    Task<PagedList<Department>> GetPageAsync(int page, int pageSize, CancellationToken ct);
    Task<Department?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<Department?> GetByIdWithEmployeesAsync(Guid id, CancellationToken ct);
    Task<bool> ExistsByNameAsync(string name, Guid? excludingId, CancellationToken ct);
    Task AddAsync(Department department, CancellationToken ct);
    void Remove(Department department);
}
```

## Vacation Request Repository

```csharp
public interface IVacationRequestRepository
{
    Task<PagedList<VacationRequest>> GetPageWithEmployeeAsync(
        VacationRequestStatus? status,
        Guid? employeeId,
        int page,
        int pageSize,
        CancellationToken ct);

    Task<VacationRequest?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<VacationRequest?> GetByIdWithEmployeeAsync(Guid id, CancellationToken ct);
    Task<IReadOnlyList<VacationRequest>> GetByEmployeeIdAsync(Guid employeeId, CancellationToken ct);
    Task AddAsync(VacationRequest request, CancellationToken ct);
    void Remove(VacationRequest request);
    void RemoveRange(IEnumerable<VacationRequest> requests);
}
```

## Trip Repository

```csharp
public interface ITripRepository
{
    Task<PagedList<Trip>> GetPageAsync(int page, int pageSize, CancellationToken ct);
    Task<Trip?> GetByIdAsync(Guid id, CancellationToken ct);
    Task AddAsync(Trip trip, CancellationToken ct);
    void Remove(Trip trip);
}
```

## Employee Repository

```csharp
public interface IEmployeeRepository
{
    Task<PagedList<Employee>> GetPageWithDetailsAsync(
        EmployeeStatus? status,
        int page,
        int pageSize,
        CancellationToken ct);

    Task<Employee?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<Employee?> GetByIdWithDetailsAsync(Guid id, CancellationToken ct);
    Task<Employee?> GetByApplicationUserIdWithDetailsAsync(string applicationUserId, CancellationToken ct);
    Task<Employee?> GetByEmployeeNumberWithDetailsAsync(string employeeNumber, CancellationToken ct);
    Task<IReadOnlyList<Employee>> GetDirectReportsAsync(Guid managerId, CancellationToken ct);
    Task<bool> ExistsAsync(Guid id, CancellationToken ct);
    Task<bool> ExistsByNumberAsync(string employeeNumber, CancellationToken ct);
    Task AddAsync(Employee employee, CancellationToken ct);
    void Remove(Employee employee);
}
```

## Identity User Lookup

```csharp
public interface IIdentityUserLookup
{
    Task<ApplicationUser?> GetByIdAsync(string id, CancellationToken ct);
    Task<IReadOnlyDictionary<string, ApplicationUser>> GetByIdsAsync(
        IReadOnlyCollection<string> ids,
        CancellationToken ct);
}
```

This is a read-only mapping helper. Password checks, user creation, user updates, and user deletion remain `UserManager<ApplicationUser>` responsibilities.

## Unit of Work

```csharp
public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken ct);
    Task ExecuteWithStrategyAsync(Func<CancellationToken, Task> operation, CancellationToken ct);
    Task<IDataTransaction> BeginTransactionAsync(CancellationToken ct);
}

public interface IDataTransaction : IAsyncDisposable
{
    Task CommitAsync(CancellationToken ct);
    Task RollbackAsync(CancellationToken ct);
}
```

`EmployeeService` uses `ExecuteWithStrategyAsync` and `BeginTransactionAsync` for creation. It uses `BeginTransactionAsync` for deletion. Expected Identity failures roll back and retain the current structured validation response. Unexpected exceptions unwind to the existing global middleware.

## Authentication Result

```csharp
public sealed record AuthenticatedEmployee(
    EmployeeResponse Employee,
    string UserId,
    string? UserName,
    string? UserEmail);
```

`AuthService` maps repository entities into `EmployeeResponse`. `AuthController` uses the DTO for claims and login response construction. Public login JSON, cookies, claims, HTTP statuses, and error codes remain unchanged.

## Tracking Rules

- Page and read-only detail methods use no-tracking queries.
- Mutation lookup methods return tracked entities.
- Repository methods apply current filters, ordering, and related-data loading.
- All asynchronous repository and unit-of-work methods forward `CancellationToken`.
