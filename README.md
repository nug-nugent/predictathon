# Predictathon

## Docker dev setup

The whole stack (SQL Server, API, frontend) runs in Docker, so you don't need SQL Server, .NET, or Node installed locally to get going. This is the recommended path on macOS, since SQL Server has no native Mac install.

### Prerequisites

- [Docker Desktop](https://www.docker.com/products/docker-desktop/)
- **Apple Silicon (M-series) Macs only:** the SQL Server image has no native ARM64 build, so Docker falls back to x86 emulation via Rosetta. In Docker Desktop, go to **Settings → General** and enable **"Use Rosetta for x86/amd64 emulation on Apple Silicon"** first, or the `db` container will be noticeably slow (or may fail to start).

### First-time setup

1. Copy `.env.docker.example` to `.env.docker` and set a real password for `MSSQL_SA_PASSWORD` (SQL Server requires 8+ characters with at least three of: uppercase, lowercase, digit, symbol).
2. Run the stack:
   ```
   docker compose --env-file .env.docker up --build
   ```
   (On Windows, `.\make.ps1 dev` does the same thing and also opens your browser once it's ready.)

First run pulls the SQL Server image (~1.5GB) and builds the other images, so it'll take a few minutes. After that, only changed layers rebuild.

### URLs

| Service  | URL                          |
|----------|-------------------------------|
| Frontend | http://localhost:5174        |
| API      | http://localhost:7047        |
| Database | `localhost,14330` (SQL auth, `sa` / your `.env.docker` password) |

These are deliberately different from the "native" host-workflow ports (5173/7046/1433) so the Docker stack and a host-based `dotnet run`/`npm run dev` setup can run side by side without colliding.

### Data persistence

The database lives in a named Docker volume, so it survives `docker compose down` and container restarts. To wipe it and start from a genuinely empty (but schema-deployed) database:
```
docker compose --env-file .env.docker down -v
```
(`.\make.ps1 clean` on Windows.)

### Sample data

The stack seeds itself with a starting point so there's something to explore right after `up` -
a `db-seed` service runs `Scripts/Sample/00_RunAll.sql` once `db-migrate` finishes:

- **Sample Cup**: a free, always-open 32-team World Cup-format competition. The group stage and
  Round of 16 are already played, the Quarter-finals onward are still to be predicted.
- **Accounts** (same as David's real dev DB - these are intentionally shared, not newly generated):
  - `DemoAdmin` / `DemoAdmin!2026` - full admin (User/Competition/Match admin), registered into Sample Cup
  - `DemoPredictor` / `DemoPass123!` - regular user, registered into Sample Cup

Re-running `up` re-runs `db-seed` too, but it's idempotent (`MERGE`-based) so it's a safe no-op on
an already-seeded database. To regenerate `Scripts/Sample/01_Teams.sql` after `dbo.Team` changes,
see that file's header comment for the exact `sp_generate_merge` command.

### Notes

- Schema changes are picked up automatically — `db-migrate` builds the `Database` project's dacpac from source and publishes it on every `up`, no manual build step needed.
- The frontend container bind-mounts your local `frontend/` source, so edits on your machine hot-reload via Vite exactly like a native `npm run dev` would.
- The API container does not have a database of its own outside Docker — it's talking to the `db` container, not any local/native SQL Server instance you might also have installed.

## Production deployment (Plesk/IIS shared hosting)

The production host is Plesk on Windows/IIS shared hosting. Nothing here is automated yet (no CI/CD) — both the API and frontend are published and copied up manually.

### One-time Plesk setup

1. In Plesk, under the domain's **Websites & Domains → ASP.NET Settings**, disable classic ASP.NET — this auto-enables .NET Core for the domain (Plesk can't run both at once).
2. `WebApi/Predictathon.WebApi.csproj` uses `Microsoft.NET.Sdk.Web`, so `dotnet publish -c Release` generates a valid IIS `web.config` (ASP.NET Core Module, in-process hosting) automatically alongside the published DLLs — nothing needs to be hand-authored. Publish and copy the output into the domain's app directory.
3. WebSockets must be enabled on the domain for the messageboard's SignalR hub (`/hubs/messageboard`) to work — already confirmed on with the host.

### Required environment variables

None of these are safe to commit, so `appsettings.json` ships them blank. Set each as an environment variable on the app (Plesk's ASP.NET Core panel supports per-app environment variables), using ASP.NET Core's `Section__Key` double-underscore convention:

| Variable | Notes |
|---|---|
| `ConnectionStrings__DefaultConnection` | Production SQL Server connection string. |
| `Jwt__SigningKey` | Required — the app fails to start without it. |
| `Cors__AllowedOrigins__0` (and `__1`, `__2`, … for more) | The real frontend origin(s). Empty by default, so login/CORS is dead until this is set. |
| `Frontend__BaseUrl` | Used to build links in prediction-reminder emails. |
| `Avatars__PublicBaseUrl`, `MessageImages__PublicBaseUrl` | Public base URL the API is served from, used to build absolute links to uploaded avatar/message images. |
| `Smtp__Host`, `Smtp__Port`, `Smtp__EnableSsl`, `Smtp__Username`, `Smtp__Password` | Left unset, `EmailService` logs emails to console/file instead of sending — fine for a dry run, not for real use. |
| `PayPal__Mode`, `PayPal__ClientId`, `PayPal__ClientSecret` | `Mode` must be `live` in production (defaults to `sandbox`). |
| `FootballDataApi__ApiKey` | For automated fixture-change detection. |
| `Health__ApiKey`, `Health__DetailedApiKey` | Leaving these unset leaves `/health` and `/health/detailed` publicly readable. Set them before going live. |
| `ScheduledTasks__ApiKey` | See below — leaving it unset leaves the scheduled-task endpoints open (low risk since they're idempotent, but worth setting). |

### Scheduled tasks

Shared hosting has no "Always On" app-pool option (confirmed with the host) — an in-process timer can't be trusted to run daily, since an idle app pool just stops. Instead, `TasksController` exposes two endpoints meant to be triggered externally, matching how the legacy app did it:

- `GET /api/Tasks/PredictionEmailReminderSend`
- `GET /api/Tasks/UserCompetitionLeagueHistorySet`

Both are safe to call more than once a day (idempotent). Point either an UptimeRobot monitor (configure it as an **API monitor**, not a plain HTTP(s) monitor, so you can set a custom header — this is available on UptimeRobot's free plan) or a Plesk scheduled task at each URL once a day, sending `X-Api-Key: <ScheduledTasks__ApiKey>` if that variable is set. The ping doubles as a way to wake an idled app pool.

### Frontend

`npm run build` produces static files for `frontend/dist/`, including `web.config` (copied automatically from `frontend/public/`), which carries the IIS URL-rewrite rule needed to fall back to `index.html` for client-side routes. `VITE_API_BASE_URL` defaults to `/api` (relative) if unset at build time, which works as long as the API is reverse-proxied under the same domain as the frontend; set it explicitly via `.env.production` or a build-time environment variable if the API is on a different origin.

### Taking the site offline for an upgrade

To do a live upgrade (e.g. over a weekend), drop `Deployment/app_offline.htm` into both:

1. The API's IIS sub-application folder — the ASP.NET Core Module detects `app_offline.htm` automatically and serves it (with a 503) for every request, no config needed.
2. The frontend's site root, alongside `index.html` — the frontend is static files with no ASP.NET Core Module involved, so `web.config`'s "App offline" rewrite rule replicates the same behaviour by checking for the file's presence.

Delete (or rename) both copies to bring the site back up. If the upgrade doesn't go to plan, restoring the previous published output (API + frontend) and removing both `app_offline.htm` copies rolls back to the last known-good state — the database itself isn't touched by the app-offline mechanism, so a rollback of app code alone is safe as long as no schema changes need reverting too (see the SQL backup/restore note in your Plesk control panel before running the `Identity.Users` migration for the first time against production data).
