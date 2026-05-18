# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Commands

```bash
# Start required infrastructure (Postgres + Redis + Seq)
docker compose up -d postgres redis seq

# Run API
dotnet run --project src/Api

# Build
dotnet build src/Api/Api.csproj

# Run all tests
dotnet test tests/Api.Tests

# Run a single test class or method
dotnet test tests/Api.Tests --filter "FullyQualifiedName~RestaurantServiceTests"
dotnet test tests/Api.Tests --filter "FullyQualifiedName~RestaurantServiceTests.DeleteAsync_NonExistingId_ReturnsFalse"

# Add EF migration (migrations apply automatically on API startup — never run update manually)
dotnet ef migrations add <Name> --project src/Infrastructure --startup-project src/Api

# Create admin user locally
dotnet run --project src/Cli -- create-admin <email> <username>
dotnet run --project src/Cli -- create-admin <email> <username> --super
```

Copy `.env.example` to `.env` and fill in values before running locally.

## Local URLs

| Service | URL |
|---------|-----|
| Swagger UI | http://localhost:5280/api/swagger |
| Seq logs | http://localhost:5341 |
| Postgres | localhost:5433 |
| Redis | localhost:6379 |

## Architecture

Four projects with strict dependency flow: `Api` → `Core` ← `Infrastructure`, `Cli` → `Core` + `Infrastructure`.

- **`Core`** — domain entities, interfaces, DTOs, validators, services with no infrastructure dependencies (`JwtService`, `RestaurantService`). Zero external dependencies except FluentValidation and JWT libs.
- **`Infrastructure`** — EF Core + Postgres (`AppDbContext`), Redis-backed service implementations, repository implementations.
- **`Api`** — controllers, DI wiring (`DependencyConfig.cs`), Swagger. Depends on both Core and Infrastructure.
- **`Cli`** — standalone console tool for seeding admins. Runs as a Docker service with `profiles: [tools]`.

### Domain model

Entities are **rich domain models** — business logic lives on the entity, not in services. For example `Restaurant.IsOpenNow()` and `Restaurant.DistanceTo(lat, lng)` are entity methods. Services handle orchestration and DTO mapping only.

### Layering conventions

**Interfaces** split by type:
- `Core/Interfaces/` — service contracts live flat here (`IRestaurantService`, `IRefreshTokenService`, `IOtpService`, etc.)
- `Core/Interfaces/Repository/` — repository contracts (data access only, e.g. `IRestaurantRepository`)

**Implementations**:
- `Core/Services/` — services with no infrastructure dependencies (no EF, no Redis). `JwtService`, `RestaurantService` live here.
- `Infrastructure/Services/` — services that require infrastructure (Redis-backed OTP, refresh tokens).
- `Infrastructure/Persistence/Repositories/` — repository implementations.

The rule: if a service only depends on Core interfaces and DTOs, it belongs in `Core/Services/`. If it needs EF, Redis, or any external client, it belongs in `Infrastructure/Services/`.

**Services** depend on repository interfaces, not on `AppDbContext` directly.

**DTOs** — plain classes with `init` properties in `Core/DTOs/{Feature}/`. Request DTOs always have a matching FluentValidation validator in `Core/DTOs/{Feature}/Validators/`. No data annotation attributes — FluentValidation only. Validators are auto-registered via `AddValidatorsFromAssemblyContaining<ICoreReference>()`.

**EF entity configuration** — each entity gets its own `IEntityTypeConfiguration<T>` class in `Infrastructure/Persistence/Configurations/`. `AppDbContext.OnModelCreating` calls `ApplyConfigurationsFromAssembly` to pick them all up automatically.

### Auth

Two separate JWT flows share the same bearer scheme:

- **Mobile** (`/api/auth/*`) — passwordless OTP via phone or email. Three-step: send OTP → verify OTP (get session token) → register/login (exchange session token for JWT pair). `[Authorize]` on mobile endpoints.
- **Admin** (`/api/admin/auth/*`) — email + password. `[Authorize(Policy = TandurPolicies.AdminPanel)]` on admin endpoints.

Roles: `User`, `Admin`, `SuperAdmin` (enum in `Core/Domain/Enums/TandurRole.cs`, string constants in `TandurRoles.cs`). The `AdminPanel` policy accepts `Admin` or `SuperAdmin`. Role seeding happens automatically on API startup.

`JwtService` lives in `Core/Services/` with no interface — inject the concrete class directly.

Refresh tokens are stored in Redis (not the database). Token keys are namespaced as `tandur:`.

### Soft delete

Entities like `Restaurant` use soft delete — `DELETE` endpoints set `IsActive = false` rather than removing the row. Mobile read endpoints filter to `IsActive = true`; admin endpoints return all records.

## Testing

NUnit (`[TestFixture]`, `[Test]`, `[SetUp]`), Moq for dependencies, EF InMemory for repository tests.

- **Controller tests** — instantiate the controller directly with a mocked service interface. No `WebApplicationFactory`.
- **Service tests** — mock the repository interface; test orchestration logic (e.g. distance sorting, DTO field mapping).
- **Repository tests** — use `DbContextOptionsBuilder.UseInMemoryDatabase(Guid.NewGuid().ToString())` for an isolated DB per test; test actual query logic (filters, soft delete, persistence).
