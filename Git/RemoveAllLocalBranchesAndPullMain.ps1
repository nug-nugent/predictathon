<#
.SYNOPSIS
    Tidies up after merged PRs: switches to main, fast-forwards it, and deletes the local
    branches left behind.

.DESCRIPTION
    The everyday cleanup for this repo's workflow - branch off main, open a PR, GitHub
    squash-merges it and deletes the remote branch, leaving a stale local one behind.

    Squash merging is why this script exists rather than a `git branch -d` one-liner. A squash
    merge replays a branch as a single new commit, so the branch's own commits never appear in
    main's history and `git branch -d` refuses to delete it as "not fully merged" - which is
    precisely the case you most want cleaned up. `git branch -D` would delete it, but it deletes
    genuinely unmerged work just as happily and just as silently.

    So each branch is tested by content rather than by history: its tree is replayed onto its
    merge base with main, and if the resulting patch is already present in main, the branch has
    landed however it was merged. Branches that pass are deleted; branches that don't are kept
    and listed, because those are the ones holding work that exists nowhere else. Use -Force to
    delete those too.

    Refuses to run with uncommitted changes, and only ever fast-forwards main, so it can't
    silently discard local work or invent a merge commit.

.PARAMETER MainBranch
    The branch to keep and update. Defaults to "main".

.PARAMETER Force
    Also delete branches whose work is NOT in the main branch. This discards commits that exist
    nowhere else - the whole point of the default behaviour is to refuse to do this for you.

.PARAMETER SkipPull
    Delete branches but don't fetch or update the main branch. Useful offline.

.EXAMPLE
    .\Git\RemoveAllLocalBranchesAndPullMain.ps1

    Fast-forwards main and deletes every local branch already merged into it, squash merges
    included. Lists anything it kept.

.EXAMPLE
    .\Git\RemoveAllLocalBranchesAndPullMain.ps1 -WhatIf

    Shows exactly which branches would be deleted and which would be kept, changing nothing.

.EXAMPLE
    .\Git\RemoveAllLocalBranchesAndPullMain.ps1 -Force

    As above, but also deletes branches holding unmerged work. There is no undo beyond the reflog.
#>
# ConfirmImpact is deliberately Medium, not High. High would prompt once per branch on the normal
# run, and a script that asks four times to do the thing you just asked it to do is a script you
# learn to run with -Confirm:$false - which then also silences the prompt on the one case that
# warrants it. The safety here is that unmerged branches need an explicit -Force instead.
[CmdletBinding(SupportsShouldProcess, ConfirmImpact = "Medium")]
param(
    [string]$MainBranch = "main",
    [switch]$Force,
    [switch]$SkipPull
)

$ErrorActionPreference = "Stop"

<#
.SYNOPSIS
    Runs a git command, returning its stdout and failing only on a non-zero exit code.

.DESCRIPTION
    git writes plenty of ordinary progress information to stderr ("Switched to branch 'main'",
    fetch progress), which $ErrorActionPreference = "Stop" would otherwise turn into a
    terminating NativeCommandError on a command that actually succeeded. Exit code is the only
    signal worth trusting - see the same reasoning in Deployment/Publish-Local.ps1.

.PARAMETER Arguments
    Arguments to pass to git.

.PARAMETER Description
    Human-readable step name, used in the failure message.

.PARAMETER IgnoreExitCode
    Return output instead of throwing when git exits non-zero - for commands whose failure is a
    meaningful answer rather than an error.
#>
function Invoke-Git {
    param(
        [Parameter(Mandatory)][string[]]$Arguments,
        [Parameter(Mandatory)][string]$Description,
        [switch]$IgnoreExitCode
    )

    $previous = $ErrorActionPreference
    $ErrorActionPreference = "Continue"
    try {
        $output = & git @Arguments 2>&1
    }
    finally {
        $ErrorActionPreference = $previous
    }

    if ($LASTEXITCODE -ne 0 -and -not $IgnoreExitCode) {
        throw "$Description failed with exit code ${LASTEXITCODE}:`n$output"
    }

    return $output
}

<#
.SYNOPSIS
    Reports whether a branch's work is already in the main branch, including via a squash merge.

.DESCRIPTION
    A squash merge rewrites a branch as one new commit, so neither `git branch --merged` nor
    `git cherry` (which matches commits individually by patch id) recognises it. Collapsing the
    branch to a single commit against its merge base first produces the same patch the squash
    merge produced, which `git cherry` then reports as already applied - marking it with "-".

.PARAMETER Branch
    The local branch to test.

.PARAMETER MainBranch
    The branch to test it against.
#>
function Test-BranchIsMerged {
    param(
        [Parameter(Mandatory)][string]$Branch,
        [Parameter(Mandatory)][string]$MainBranch
    )

    # No commits of its own - nothing to lose either way.
    $ownCommits = Invoke-Git -Arguments @("rev-list", "--count", "$MainBranch..$Branch") -Description "Counting commits on '$Branch'"
    if ([int]($ownCommits | Select-Object -First 1) -eq 0) {
        return $true
    }

    $mergeBase = (Invoke-Git -Arguments @("merge-base", $MainBranch, $Branch) -Description "Finding merge base for '$Branch'" | Select-Object -First 1).Trim()
    $tree = (Invoke-Git -Arguments @("rev-parse", "$Branch^{tree}") -Description "Reading tree of '$Branch'" | Select-Object -First 1).Trim()

    # A throwaway commit, never referenced by any branch, that git gc will collect.
    $squashed = (Invoke-Git -Arguments @("commit-tree", $tree, "-p", $mergeBase, "-m", "squash-merge probe") -Description "Building squash probe for '$Branch'" | Select-Object -First 1).Trim()

    $cherry = Invoke-Git -Arguments @("cherry", $MainBranch, $squashed) -Description "Comparing '$Branch' against $MainBranch"

    # "-" means the patch is already in the main branch; "+" means it isn't.
    return ($cherry | Where-Object { $_ -match '^\-' }).Count -gt 0
}

$repoRoot = Split-Path -Parent $PSScriptRoot
Push-Location $repoRoot
try {
    Invoke-Git -Arguments @("rev-parse", "--git-dir") -Description "Checking for a git repository" | Out-Null

    # Deleting branches is destructive enough without also risking whatever is sitting
    # uncommitted in the working tree.
    $dirty = Invoke-Git -Arguments @("status", "--porcelain") -Description "Checking working tree status"
    if ($dirty) {
        throw "The working tree has uncommitted changes. Commit or stash them first:`n$($dirty -join "`n")"
    }

    if (-not $SkipPull) {
        Write-Host "Fetching and pruning remote-tracking branches..."
        Invoke-Git -Arguments @("fetch", "--prune") -Description "git fetch --prune" | Out-Null
    }

    Write-Host "Switching to '$MainBranch'..."
    Invoke-Git -Arguments @("checkout", $MainBranch) -Description "Switching to '$MainBranch'" | Out-Null

    if (-not $SkipPull) {
        # --ff-only so a main that has diverged locally stops the script instead of quietly
        # growing a merge commit.
        Write-Host "Fast-forwarding '$MainBranch'..."
        Invoke-Git -Arguments @("pull", "--ff-only") -Description "git pull --ff-only" | Out-Null
    }

    $branches = Invoke-Git -Arguments @("for-each-ref", "--format=%(refname:short)", "refs/heads/") -Description "Listing local branches" |
        Where-Object { $_ -and $_.Trim() -ne $MainBranch } |
        ForEach-Object { $_.Trim() }

    if (-not $branches) {
        Write-Host "No local branches to remove - nothing to do." -ForegroundColor Green
        return
    }

    $deleted = @()
    $kept = @()

    foreach ($branch in $branches) {
        $merged = Test-BranchIsMerged -Branch $branch -MainBranch $MainBranch

        if (-not $merged -and -not $Force) {
            $kept += $branch
            continue
        }

        $reason = if ($merged) { "merged into $MainBranch" } else { "NOT merged - work will be lost" }
        if ($PSCmdlet.ShouldProcess($branch, "Delete local branch ($reason)")) {
            # -D throughout: -d refuses squash-merged branches, which are the normal case here,
            # so the merged/unmerged decision is made above rather than delegated to git.
            Invoke-Git -Arguments @("branch", "-D", $branch) -Description "Deleting '$branch'" | Out-Null
            $deleted += $branch
        }
    }

    Write-Host ""
    if ($deleted) {
        Write-Host "Deleted $($deleted.Count) branch(es):" -ForegroundColor Green
        $deleted | ForEach-Object { Write-Host "  $_" }
    }
    else {
        Write-Host "No branches deleted." -ForegroundColor Yellow
    }

    if ($kept) {
        Write-Host ""
        Write-Host "Kept $($kept.Count) branch(es) holding work that isn't in ${MainBranch}:" -ForegroundColor Yellow
        $kept | ForEach-Object { Write-Host "  $_" }
        Write-Host "Re-run with -Force to delete these too (their commits exist nowhere else)."
    }
}
finally {
    Pop-Location
}
