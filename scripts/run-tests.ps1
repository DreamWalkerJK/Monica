<#
.SYNOPSIS
    Runs Monica's test projects. The default gate excludes UI (bUnit) test projects.

.DESCRIPTION
    Enumerates test projects dynamically, so new projects are picked up without maintaining a list.
    The default run is the solution's standard verification gate: every tests/Test.Monica.* project
    whose name does not end in .UI. UI test projects run only on explicit request via -UiOnly or
    -IncludeUi; they are also available as the manual ui-tests.yml workflow.

.PARAMETER NoBuild
    Skip the Release solution build and test the existing binaries (--no-build).

.PARAMETER UiOnly
    Run only the UI (bUnit) test projects.

.PARAMETER IncludeUi
    Run the default gate plus the UI test projects (the full suite).

.EXAMPLE
    powershell -File scripts/run-tests.ps1
    pwsh -File scripts/run-tests.ps1 -NoBuild --logger "trx;LogFilePrefix=unit-tests"
#>
[CmdletBinding()]
param(
    [switch]$NoBuild,
    [switch]$UiOnly,
    [switch]$IncludeUi,
    [Parameter(ValueFromRemainingArguments = $true)]
    $RemainingArguments
)

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$solution = Join-Path $root 'Monica.slnx'

if (-not $NoBuild) {
    dotnet build $solution -m -c Release --nologo
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
}

$all = @(Get-ChildItem (Join-Path $root 'tests') -Recurse -Depth 2 -Filter '*.csproj' |
    Where-Object { $_.Directory.Parent.Name -eq 'tests' -and $_.Directory.Name -like 'Test.Monica.*' })
if ($all.Count -eq 0) {
    Write-Error 'No test projects found under tests/.'
    exit 1
}

$ui = @($all | Where-Object { $_.Directory.Name -like '*.UI' })
$core = @($all | Where-Object { $_.Directory.Name -notlike '*.UI' })
if ($UiOnly) {
    $projects = $ui
}
elseif ($IncludeUi) {
    $projects = $all
}
else {
    $projects = $core
}
if ($projects.Count -eq 0) {
    Write-Error 'No test projects matched the selected mode.'
    exit 1
}

$failed = @()
foreach ($project in $projects) {
    Write-Host "==> $($project.Directory.Name)" -ForegroundColor Cyan
    dotnet test $project.FullName -c Release --no-build --nologo @RemainingArguments
    if ($LASTEXITCODE -ne 0) { $failed += $project.Directory.Name }
}

Write-Host ''
if ($failed.Count -gt 0) {
    Write-Host ("Ran {0} test projects: {1} FAILED ({2})." -f $projects.Count, $failed.Count, ($failed -join ', ')) -ForegroundColor Red
    exit 1
}
Write-Host ("Ran {0} test projects: all passed." -f $projects.Count) -ForegroundColor Green
