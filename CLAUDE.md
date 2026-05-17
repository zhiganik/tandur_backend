# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Commands

```bash
# Start infrastructure (Postgres + Redis only)
docker-compose up -d postgres redis

# Stop infrastructure
docker-compose down

# Run API locally (requires .env at repo root)
dotnet run --project src/Api

# Build
dotnet build src/Api/Api.csproj

# Add a new EF migration (never run update manually — migrations apply on startup)
dotnet ef migrations add <Name> --project src/Infrastructure --startup-project src/Api

# Create admin user (runs locally, connects to Postgres via .env)
dotnet run --project src/Cli -- create-admin <email> <username>
dotnet run --project src/Cli -- create-admin <email> <username> --super
```

**Local dev**: copy `.env.example` to `.env` and fill values. The API loads it automatically via `dotenv.net`. Run only `postgres` and `redis` via Docker — the API runs with `dotnet run`. Migrations and role seeding run automatically on every API startup.

## Architecture

Clean Architecture with three layers:

- **`Core`** — domain entities, interfaces, DTOs, validators. No external dependencies except FluentValidation and JWT token models.
- **`Infrastructure`** — EF Core + Postgres (`AppDbContext`), Redis implementations of Core interfaces. Depends only on Core.
- **`Api`** — ASP.NET Core controllers, DI wiring (`DependencyConfig.cs`), Swagger config. Depends on Core + Infrastructure.
- **`Cli`** — standalone console tool for seeding admins. Connects directly to Postgres, shares Core + Infrastructure.

## Key conventions

**Interfaces** live in `Core/Interfaces/Services/`. Implementations live in `Infrastructure/Services/`. Never put interface implementations in Core.

**DTOs** are plain classes with `init` properties in `Core/DTOs/{Feature}/`. Each request DTO has a matching FluentValidation validator in `Core/DTOs/{Feature}/Validators/`. No `[Required]` or other data annotation validation attributes — use FluentValidation only. Validators are auto-registered via `AddValidatorsFromAssemblyContaining<>` and run automatically through `AddFluentValidationAutoValidation()`.

**Authorization** uses named policies defined in `DependencyConfig.AddAuthentication` via `AddAuthorizationBuilder()`. Role constants are in `Core/Domain/Constants/TandurRoles.cs`, policy name constants in `Core/Domain/Constants/TandurPolicies.cs`. In endpoints that authenticate before the JWT pipeline (Login, Refresh), policies are evaluated manually via `IAuthorizationService` with a constructed `ClaimsPrincipal`.

**Swagger** lock icons are per-endpoint via `AuthorizeOperationFilter` — only endpoints with `[Authorize]` get a lock.

**Migrations** are in `Infrastructure/Persistence/Migrations/`. Migrations and role seeding run automatically on API startup.

**`JwtService`** lives in `Core/Services/` (no external I/O, pure config + crypto) and has no interface — inject the concrete class directly.
