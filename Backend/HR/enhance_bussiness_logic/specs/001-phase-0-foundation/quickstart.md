# Quickstart: Phase 0 Foundation

## Getting Started

Because this phase reorganizes the physical solution structure into 5 projects, you must ensure your IDE and CLI are aware of the new solution structure.

### 1. Restore and Build

From the root directory of the repository (where the `.sln` file resides), run:

```bash
dotnet restore
dotnet build
```

Verify that there are zero compilation errors.

### 2. Run the Application

The startup project is still `HR.API`. Run it as normal:

```bash
cd HR.API
dotnet run
```

### 3. Verify Functionality

Navigate to the Swagger UI (typically `https://localhost:xxxx/swagger`) and verify that all endpoints are present and return the expected data. 

Because `ApplicationDbContext` and the Controllers remain fundamentally identical (just in different assemblies), the runtime behavior should be exactly as it was before this branch.
