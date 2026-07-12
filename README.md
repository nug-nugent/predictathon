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

### Notes

- Schema changes are picked up automatically — `db-migrate` builds the `Database` project's dacpac from source and publishes it on every `up`, no manual build step needed.
- The frontend container bind-mounts your local `frontend/` source, so edits on your machine hot-reload via Vite exactly like a native `npm run dev` would.
- The API container does not have a database of its own outside Docker — it's talking to the `db` container, not any local/native SQL Server instance you might also have installed.
