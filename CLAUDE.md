# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What this is

Predictathon is a football score-prediction competition site for a private group (~50 friends/family). Users predict match scores each week and earn 3/2/1/0 points based on accuracy. It's a hobby project, solely maintained by David Huggett, recently rebuilt from a legacy ASP.NET Web Forms app onto a .NET 10 Web API + React 19/Chakra UI v3 frontend (the legacy `UI/Web` project has been fully deleted).

## Coding rules (from `copilot-instructions.md`, keep both in sync)

- Always wrap single-line `if`/`else if`/`else` bodies and loop bodies (`for`, `foreach`, `while`, `do/while`) in curly braces, even for a single statement.
- Document methods with XML doc comments (summary + parameter descriptions).
- Use British English spelling.
- Applies to all C# source in this repo.

## Common commands

### Docker dev stack (recommended — no local SQL Server/.NET/Node needed)

```
docker compose --env-file .env.docker up --build   # first run: copy .env.docker.example -> .env.docker first
.\make.ps1 dev     # same, plus opens the browser once ready (Windows)
.\make.ps1 down    # stop containers, keep DB volume
.\make.ps1 clean   # stop containers AND wipe the DB volume
```
Frontend: http://localhost:5174, API: http://localhost:7047, DB: `localhost,14330`. These ports are deliberately offset from the native host workflow (5173/7046/1433) so both can run side by side. Schema changes are picked up automatically each `up` (dacpac rebuilt/published by `db-migrate`); sample data is seeded idempotently by `db-seed` (`Scripts/Sample/00_RunAll.sql`).

### Native/host workflow

Backend:
```
dotnet build Predictathon.slnx
dotnet run --project WebApi              # https://localhost:7046
dotnet test                               # all tests
dotnet test --filter FullyQualifiedName~CompetitionModelTests   # single test/class
```
Tests use xUnit + FluentAssertions + AutoFixture/AutoFixture.Xunit2 + Bogus (`UnitTests/`, referencing `Application` only).

Frontend (`frontend/`):
```
npm run dev              # Vite dev server, http://localhost:5173
npm run build             # tsc -b && vite build
npm run lint               # ESLint (type-checked + a11y rules are on)
npm run storybook          # Storybook dev server on :6006
```
There is no frontend test runner configured yet (no vitest/jest).

### Database schema changes

Schema is managed via SSDT (`Database/Predictathon.Database.csproj`), **not EF Core Migrations** — every table is DB-first (EF entities under `Domain/Entities` and `Domain/Identity` are reverse-engineered from the DB via EF Core Power Tools, not the other way round). When adding/changing tables: hand-author `.sql` scripts under `Database/dbo/Tables/` or `Database/Identity/Tables/` matching the existing style (bracketed identifiers, `DF_Table_Column`/`PK_Table`/`FK_Table_RefTable_Column` naming), then let the dacpac build/publish pick them up — don't use `dotnet ef migrations add`/`database update`.

## Architecture

Backend is a layered solution (`Predictathon.slnx`):
- **Domain** — EF Core entities only (`Entities/`, `Identity/`), DB-first/reverse-engineered from the SSDT schema, no logic.
- **Application** — business logic: services, interfaces (`Interfaces/`), DTOs (`Models/`), Mapster mapping config, FluentValidation validators, custom `Errors`/`Exceptions`. Services implementing `ICrudService<TPrimaryKey, TCreateModel, TEditModel, TEntity>` (`Interfaces/Base/`) get generic create/read/update/delete via `CrudServiceDependencyAggregate<,>`. Any class marked `[ScopedService]` is auto-registered against its interfaces by `ServiceCollectionExtensions.AddApplication()` (via Scrutor assembly scanning) — no manual DI wiring needed for new services.
- **Infrastructure** — `ApplicationDbContext` (single unified DbContext covering both app data and ASP.NET Core Identity's `Identity` schema — Identity config lives in the clearly-labeled partial `ApplicationDbContext.Identity.cs`, copied from what `IdentityDbContext` would otherwise do invisibly, rather than inheriting a second DbContext base). `GenericDbContext` provides the shared generic-repository plumbing.
- **WebApi** — ASP.NET Core Web API: `Controllers/`, `Program.cs` composition root, a SignalR hub (`Hubs/MessageboardHub.cs`) for the live message board, JWT bearer auth (access token in memory, HttpOnly refresh-token cookie), config-driven CORS allow-list (no permissive dev fallback).
- **Database** — SSDT database project (see schema-change rule above); `Pre-Deployment`/`Post-Deployment` scripts, `Security/Identity.sql`.
- **UnitTests** — xUnit tests against `Application`.

Auth: ASP.NET Core Identity under a dedicated `Identity` SQL schema (`Identity.Users`, `Identity.Roles`), JWT bearer + refresh-token cookie with silent refresh. The legacy `dbo.User` table has been fully retired/dropped — `Identity.Users` is the sole source of user data.

Frontend (`frontend/src/`):
- `pages/public/*` (login/home, registration, password reset) vs `pages/logged-in/*` (predictions, league, board, statistics, hall-of-fame, team, profile, rules, `admin/*`), routed via `routes/Routes.tsx` + `routes/ProtectedRoute.tsx`.
- `services/*` — one file per API resource area (competition, match, prediction, league, statistics, messageboard, users-admin, etc.), plus `messageboard-hub.ts` for the SignalR client.
- `providers/` — `UserProvider`/`CompetitionProvider` for global auth/competition context; `hooks/` for shared data-fetching (`useAsyncData`, `useUser`, `useCompetition`) and UI ticks.
- Chakra UI v3 theme in `theme.ts` (`createSystem`/tokens/recipes) — includes a functional 4-step `points` colour scale (wrong/close/good/perfect) used to read prediction accuracy at a glance; treat that scale as functional, not decorative, when touching styling.
- Shared `Panel` component is the standard card/container wrapper across pages — don't duplicate card styling per-file. A `Panel` wrapping a wide `Table.Root` needs `overflowX="auto"` or content bleeds into neighbouring cards.
