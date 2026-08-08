<#
.SYNOPSIS
    Publishes the API and frontend to a local IIS deployment, for rehearsing the Plesk
    production deployment process (see README.md's "Production deployment" section).

.PARAMETER TargetRoot
    Root folder containing the IIS site's "API" and "frontend" sub-folders.
#>
[CmdletBinding()]
param(
    [string]$TargetRoot = "C:\Dev\IIS\Predictathon"
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

$repoRoot    = Split-Path -Parent $PSScriptRoot
$apiProject  = Join-Path $repoRoot "WebApi\Predictathon.WebApi.csproj"
$frontendSrc = Join-Path $repoRoot "frontend"
$offlinePage = Join-Path $repoRoot "Deployment\app_offline.htm"

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

    Write-Host "Building frontend..." -ForegroundColor Cyan
    Push-Location $frontendSrc
    try {
        Invoke-Native { npm run build } "npm run build"
    }
    finally {
        Pop-Location
    }

    # Bring the API back up before the frontend, so there is no window where a live SPA is talking
    # to an API that is still serving 503s.
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
    Write-Warning "Site left offline. To bring it back up without a full re-publish: copy $frontendSrc\dist\index.html over $frontendIndex, and delete $apiOfflineFile."
    throw
}
