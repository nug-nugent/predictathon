<#
.SYNOPSIS
    Convenience wrapper for the Docker Compose dev stack, mirroring `make <target>`.

.EXAMPLE
    .\make.ps1 dev
    .\make.ps1 down
    .\make.ps1 clean
#>
param(
    [Parameter(Position = 0)]
    [ValidateSet('dev', 'down', 'clean')]
    [string]$Target = 'dev'
)

$FrontendUrl = 'http://localhost:5174'

function Wait-ForFrontend {
    param([int]$TimeoutSeconds = 120)

    $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
    while ((Get-Date) -lt $deadline) {
        try {
            Invoke-WebRequest -Uri $FrontendUrl -UseBasicParsing -TimeoutSec 2 | Out-Null
            return $true
        } catch {
            Start-Sleep -Seconds 2
        }
    }
    return $false
}

switch ($Target) {
    'dev' {
        # Build (if needed) and start the full stack detached, so we can wait for it and open a browser.
        docker compose --env-file .env.docker up --build -d
        if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

        Write-Host "Waiting for frontend at $FrontendUrl..."
        if (Wait-ForFrontend) {
            Start-Process $FrontendUrl
        } else {
            Write-Warning "Frontend didn't respond within the timeout - check 'docker compose logs' for errors."
        }

        # Stream logs so you can still see what's happening. Ctrl+C here only stops watching -
        # the containers keep running. Use '.\make.ps1 down' to actually stop them.
        docker compose --env-file .env.docker logs -f
    }
    'down' {
        # Stop and remove containers, keeping the database volume (data survives).
        docker compose --env-file .env.docker down
    }
    'clean' {
        # Stop and remove containers AND the database volume - next `dev` starts from empty.
        docker compose --env-file .env.docker down -v
    }
}
