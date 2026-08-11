<#
.SYNOPSIS
    Publishes the API and frontend to a local IIS deployment, for rehearsing the Plesk
    production deployment process against the Systest environment (see README.md's
    "Production deployment" / "Local Systest rehearsal" sections).

.DESCRIPTION
    Builds the frontend in Vite's "systest" mode (frontend/.env.systest), not the default
    "production" mode - otherwise it bakes predictathon.co.uk into the bundle instead of the
    local Systest domain. ASPNETCORE_ENVIRONMENT and the API's secrets (connection string, JWT
    signing key) are deliberately NOT handled here - they must be set once as IIS Application
    Pool-level environment variables (see README.md's "Local Systest rehearsal" section),
    because dotnet publish regenerates web.config from scratch on every run and would silently
    wipe anything written there instead.

    Also publishes the database schema (sqlpackage, same Database/Predictathon.publish.xml
    profile the production deploy workflow uses) against the local Systest database, while both
    apps are offline - mirrors deploy.yml's ordering so this rehearsal actually exercises the
    same offline-window guarantee, not just the app code.

.PARAMETER TargetRoot
    Root folder containing the IIS site's "API" and "frontend" sub-folders.

.PARAMETER DatabaseConnectionString
    Connection string for the local Systest database. Defaults to the "Predictathon.Systest"
    database on (local) - a separate database from the "Predictathon" one native dev/Docker use,
    so rehearsing a schema change here can't collide with your everyday dev data.

.PARAMETER SkipDatabaseDeploy
    Skip the sqlpackage step - useful when iterating on app code only and the schema hasn't
    changed, to avoid the dacpac rebuild/publish round-trip on every run.
#>
[CmdletBinding()]
param(
    [string]$TargetRoot = "C:\Dev\IIS\Predictathon",
    [string]$DatabaseConnectionString = "Server=(local);Database=Predictathon.Systest;Trusted_Connection=true;MultipleActiveResultSets=true;TrustServerCertificate=true",
    [switch]$SkipDatabaseDeploy
)

$ErrorActionPreference = "Stop"

<#
.SYNOPSIS
    Runs a native command, failing only on a non-zero exit code.

.DESCRIPTION
    With $ErrorActionPreference = "Stop", PowerShell can turn anything a native command writes to
    stderr into a terminating NativeCommandError - even when the command succeeded. npm/vite emit
    build warnings on stderr routinely, which aborted this script mid-deploy and left the site
    offline. Exit code is the only signal worth trusting here, so check just that.

.PARAMETER Command
    The native command to invoke.

.PARAMETER Description
    Human-readable step name, used in the failure message.
#>
function Invoke-Native {
    param(
        [Parameter(Mandatory)][scriptblock]$Command,
        [Parameter(Mandatory)][string]$Description
    )

    $previous = $ErrorActionPreference
    $ErrorActionPreference = "Continue"
    try {
        & $Command
    }
    finally {
        $ErrorActionPreference = $previous
    }

    if ($LASTEXITCODE -ne 0) {
        throw "$Description failed with exit code $LASTEXITCODE"
    }
}

$repoRoot        = Split-Path -Parent $PSScriptRoot
$apiProject      = Join-Path $repoRoot "WebApi\Predictathon.WebApi.csproj"
$frontendSrc     = Join-Path $repoRoot "frontend"
$offlinePage     = Join-Path $repoRoot "Deployment\app_offline.htm"
$databaseProject = Join-Path $repoRoot "Database\Predictathon.Database.csproj"
$dacpacPath      = Join-Path $repoRoot "Database\bin\Release\Predictathon.Database.dacpac"
$publishProfile  = Join-Path $repoRoot "Database\Predictathon.publish.xml"

$apiTarget       = Join-Path $TargetRoot "API"
$frontendTarget  = Join-Path $TargetRoot "frontend"
$frontendIndex   = Join-Path $frontendTarget "index.html"
$apiOfflineFile  = Join-Path $apiTarget "app_offline.htm"

<#
.SYNOPSIS
    Takes both applications offline for the duration of the upgrade.

.DESCRIPTION
    The API uses app_offline.htm, which the ASP.NET Core Module detects natively - that mechanism
    is reliable and needs no help. The frontend is static files with no such module, and the
    obvious equivalent (a URL Rewrite rule keyed on app_offline.htm's presence) is NOT reliable:
    IIS caches the response for "/" and only invalidates it when the file it actually served -
    index.html - changes, so toggling a separate app_offline.htm leaves "/" serving a stale answer.
    That surfaced as a persistent 404 on the site root after deploying, among other things.
    Overwriting index.html itself is deterministic, because it is the file IIS is watching.
#>
function Enter-Offline {
    Copy-Item $offlinePage $apiOfflineFile -Force
    Copy-Item $offlinePage $frontendIndex -Force
    # In-process hosting: ANCM needs a moment to unload the app and release its file locks
    # on the published DLLs before `dotnet publish` can overwrite them.
    Start-Sleep -Seconds 3
}

Write-Host "Taking site offline..." -ForegroundColor Cyan
Enter-Offline

try {
    Write-Host "Publishing API to $apiTarget..." -ForegroundColor Cyan
    Invoke-Native { dotnet publish $apiProject -c Release -o $apiTarget } "dotnet publish"

    Write-Host "Building frontend (systest mode)..." -ForegroundColor Cyan
    Push-Location $frontendSrc
    try {
        Invoke-Native { npm run build:systest } "npm run build:systest"
    }
    finally {
        Pop-Location
    }

    if ($SkipDatabaseDeploy) {
        Write-Host "Skipping database deploy (-SkipDatabaseDeploy)." -ForegroundColor Yellow
    }
    else {
        # Runs while both apps are still offline, same reasoning as deploy.yml: the API's new
        # code shouldn't come back up against a schema it wasn't built against.
        Write-Host "Building database dacpac..." -ForegroundColor Cyan
        Invoke-Native { dotnet build $databaseProject -c Release } "dotnet build (database)"

        Write-Host "Publishing database schema to Systest..." -ForegroundColor Cyan
        Invoke-Native {
            sqlpackage /Action:Publish `
                /SourceFile:"$dacpacPath" `
                /TargetConnectionString:"$DatabaseConnectionString" `
                /Profile:"$publishProfile" `
                /p:ExcludeObjectTypes="Users;RoleMembership;DatabaseRoles;Permissions"
        } "sqlpackage /Action:Publish"
    }

    # Bring the API back up before the frontend, so there is no window where a live SPA is talking
    # to an API that is still serving 503s - and, now, no window where it's talking to a schema
    # that doesn't match what it was just built against either.
    Write-Host "Bringing API online..." -ForegroundColor Cyan
    Remove-Item $apiOfflineFile -ErrorAction SilentlyContinue

    Write-Host "Copying frontend build to $frontendTarget..." -ForegroundColor Cyan
    # robocopy uses exit codes 0-7 for success (1 = files copied, 3 = copied + extras, etc.);
    # only 8+ is a genuine failure, so it can't go through Invoke-Native's non-zero check.
    $previousPreference = $ErrorActionPreference
    $ErrorActionPreference = "Continue"
    try {
        robocopy (Join-Path $frontendSrc "dist") $frontendTarget /MIR
    }
    finally {
        $ErrorActionPreference = $previousPreference
    }
    if ($LASTEXITCODE -ge 8) {
        throw "robocopy failed with exit code $LASTEXITCODE"
    }

    # The mirror above should already have restored index.html (the offline copy differs in both
    # size and timestamp), but copy it explicitly rather than depend on robocopy's same-file
    # heuristics - this single file is the difference between the site being up and being down.
    Copy-Item (Join-Path $frontendSrc "dist\index.html") $frontendIndex -Force

    Write-Host "Publish complete, site is back online." -ForegroundColor Green
}
catch {
    Write-Warning "Publish failed: $_"
    Write-Warning "Site left offline. To bring it back up without a full re-publish: copy $frontendSrc\dist\index.html over $frontendIndex, and delete $apiOfflineFile - but only if the database publish step above (if it was reached) actually succeeded. If it failed or wasn't reached yet, bringing the new API online against the old schema is exactly what this offline window exists to prevent - fix the schema first, or restore the previous API output instead."
    throw
}
