[CmdletBinding(SupportsShouldProcess = $true)]
param(
    [ValidateSet("Development", "Production")]
    [string]$Environment = "Development",

    [switch]$Force
)

$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
$apiPath = Join-Path $repoRoot "api"
$databaseFileName = if ($Environment -eq "Development") { "starbase.dev.db" } else { "starbase.db" }
$databasePath = Join-Path $apiPath $databaseFileName
$databaseFiles = @(
    $databasePath,
    "$databasePath-shm",
    "$databasePath-wal"
)

Write-Host "Resetting $Environment database to EF seed data: $databaseFileName"
Write-Host "Stop the API first if it is running, otherwise SQLite may keep the database locked."

if (-not $Force -and -not $WhatIfPreference) {
    throw "This deletes the selected SQLite database. Re-run with -Force, or use -WhatIf to preview."
}

foreach ($file in $databaseFiles) {
    if (Test-Path -LiteralPath $file) {
        if ($PSCmdlet.ShouldProcess($file, "Delete SQLite database file")) {
            Remove-Item -LiteralPath $file -Force
            Write-Host "Deleted $file"
        }
    }
}

if ($PSCmdlet.ShouldProcess($databaseFileName, "Recreate database and apply EF migrations")) {
    Push-Location $apiPath
    try {
        $previousEnvironment = $env:ASPNETCORE_ENVIRONMENT
        $env:ASPNETCORE_ENVIRONMENT = $Environment

        dotnet ef database update

        if ($LASTEXITCODE -ne 0) {
            throw "dotnet ef database update failed with exit code $LASTEXITCODE."
        }
    }
    finally {
        $env:ASPNETCORE_ENVIRONMENT = $previousEnvironment
        Pop-Location
    }
}

Write-Host "Database reset complete."
