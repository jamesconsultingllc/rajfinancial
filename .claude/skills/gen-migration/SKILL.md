---
name: gen-migration
description: Scaffold a new EF Core migration for RajFinancial. Validates the name format, generates the migration via dotnet-ef, and confirms the solution builds. Usage: /gen-migration <MigrationName>
disable-model-invocation: true
---

# Generate EF Core Migration

## Usage

```
/gen-migration <MigrationName>
```

**Example:** `/gen-migration AddPaymentScheduleTable`

---

## Steps

### 1. Validate the migration name

The name must:
- Be PascalCase — e.g., `AddPaymentScheduleTable`, `RemoveObsoleteContactColumn`
- Describe the schema change, not a ticket number
- Contain no spaces or special characters

If the name fails validation, stop and ask the user to provide a corrected name.

### 2. Generate the migration

Run from the repo root:

```bash
cd src/Api && dotnet ef migrations add <MigrationName> --project . --startup-project . && cd ../..
```

The migration file will be created at:
```
src/Api/Data/Migrations/<timestamp>_<MigrationName>.cs
src/Api/Data/Migrations/<timestamp>_<MigrationName>.Designer.cs
```

The snapshot will be updated at:
```
src/Api/Data/Migrations/ApplicationDbContextModelSnapshot.cs
```

### 3. Verify the solution builds

```bash
dotnet build src/RajFinancial.sln --nologo -v:q
```

If the build fails, show the error and stop. Do not continue.

### 4. Review checklist — present to user before committing

Confirm with the user that the generated migration:

- [ ] `Up()` method accurately reflects the intended schema change
- [ ] `Down()` method correctly reverses all changes in `Up()`
- [ ] No data-destructive operations (DROP COLUMN, TRUNCATE) without explicit confirmation
- [ ] Foreign keys include a named constraint (avoids EF auto-naming churn)
- [ ] Indexes are added where new columns are used in queries or joins
- [ ] No raw SQL strings — use `migrationBuilder` fluent API only

### 5. Output

Report:
- Full path to the generated migration file
- Number of operations in `Up()`
- Any warnings from `dotnet ef`
- Build status (pass/fail)

---

## Architecture Notes (RajFinancial-specific)

- DbContext: `src/Api/Data/ApplicationDbContext.cs`
- Entity configurations: `src/Api/Data/Configurations/`
- Entities: `src/Shared/Entities/` (MemoryPackable, EF-mapped)
- Always run `dotnet build` after generating — EF model snapshot must compile
- After adding a migration, run `dotnet test tests/Api.Tests` to confirm unit tests still pass
- The DTO generation step (`npm run gen:dtos`) runs automatically on the next `npm run dev/build/test` via the `predev`/`prebuild`/`pretest` npm scripts — no manual step needed
