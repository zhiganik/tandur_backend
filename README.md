# tandur_backend

## Getting started

1. Copy `.env.example` to `.env` and fill in the values.
2. Start infrastructure:

```bash
docker compose up -d postgres redis seq
```

3. Run the API:

```bash
dotnet run --project src/Api
```

Migrations and role seeding run automatically on every API startup.

### Local URLs

| Service | URL | Notes |
|---------|-----|-------|
| Swagger UI | http://localhost:5280/api/swagger | API docs + test console |
| Seq logs | http://localhost:5341 | Structured log viewer |
| Postgres | localhost:**5433** | Mapped to host port 5433 (not default 5432) |
| Redis | localhost:6379 | |

> **Swagger auth:** click **Authorize**, paste your JWT token directly — no `Bearer` prefix needed.

## Commands

```bash
# Stop all infrastructure containers
docker compose down

# Build
dotnet build src/Api/Api.csproj

# Run all tests
dotnet test tests/Api.Tests

# Run a specific test class
dotnet test tests/Api.Tests --filter "FullyQualifiedName~RestaurantServiceTests"

# Add a new EF migration (never run update manually — migrations apply on startup)
dotnet ef migrations add <Name> --project src/Infrastructure --startup-project src/Api

# Create admin / superadmin locally
dotnet run --project src/Cli -- create-admin <email> <username>
dotnet run --project src/Cli -- create-admin <email> <username> --super
```

## Server

### SSH access

Add this to your local `~/.ssh/config`:

```
Host tandur
    HostName 178.104.44.54
    User root
    IdentityFile {your ssh key path}
```

Then connect with:

```bash
ssh tandur
```

### Server structure

```
/home/deploy/app/
├── .env                    ← secrets, NOT in git
├── docker-compose.yml
├── Dockerfile
├── nginx/
│   └── nginx.conf
├── src/
│   ├── Api/
│   ├── Core/
│   ├── Infrastructure/
│   └── Cli/
└── ...
```

The entire repo is cloned on the server. The only file **not** in git is `.env` — create it manually on the server from `.env.example`.

### Deploy

```bash
ssh tandur
cd /home/deploy/app

git pull
docker compose up -d --build
```

If you updated `.env` locally, copy it to the server first:

```bash
scp .env tandur:/home/deploy/app/.env
```

Then redeploy:

```bash
ssh tandur
cd /home/deploy/app
docker compose up -d --build
```

### Creating admins on the server

```bash
ssh tandur
cd /home/deploy/app

# Admin
docker compose run --rm cli create-admin <email> <username>

# SuperAdmin
docker compose run --rm cli create-admin <email> <username> --super
```

The generated password is printed once — save it immediately.

### What is safe to store in git

| File | In git |
|------|--------|
| `docker-compose.yml` | yes |
| `Dockerfile` | yes |
| `nginx/nginx.conf` | yes |
| `.env.example` | yes |
| `.env` | **NO** |
| SSH private keys | **NO** |

## Architecture

Clean Architecture with four projects:

- **`Core`** — domain entities, interfaces, DTOs, validators. No external dependencies except FluentValidation and JWT token models.
- **`Infrastructure`** — EF Core + Postgres (`AppDbContext`), Redis implementations of Core interfaces. Depends only on Core.
- **`Api`** — ASP.NET Core controllers, DI wiring (`DependencyConfig.cs`), Swagger config. Depends on Core + Infrastructure.
- **`Cli`** — standalone console tool for seeding admins. Shares Core + Infrastructure, packed as a Docker service with `profiles: [tools]`.

## Conventions

**Interfaces** live in `Core/Interfaces/Services/`. Implementations live in `Infrastructure/Services/`.

**DTOs** are plain classes with `init` properties in `Core/DTOs/{Feature}/`. Each request DTO has a matching FluentValidation validator in `Core/DTOs/{Feature}/Validators/`. No data annotation validation attributes — FluentValidation only.

**Authorization** uses named policies via `AddAuthorizationBuilder()`. Role constants in `Core/Domain/Constants/TandurRoles.cs`, policy constants in `Core/Domain/Constants/TandurPolicies.cs`. Login/Refresh endpoints evaluate policies manually via `IAuthorizationService` with a constructed `ClaimsPrincipal`.

**Swagger** lock icons are per-endpoint via `AuthorizeOperationFilter` — only `[Authorize]` endpoints get a lock. Paste the raw JWT token in the Authorize dialog (no `Bearer` prefix needed).

**`JwtService`** lives in `Core/Services/` with no interface — inject the concrete class directly.
