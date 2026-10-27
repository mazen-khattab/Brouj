param([switch]$Smoke)

$ErrorActionPreference = 'Stop'
$repositoryRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$runName = (Get-Date).ToUniversalTime().ToString('yyyyMMddTHHmmssZ')
$artifactPath = Join-Path $repositoryRoot "artifacts/performance/$runName"

Push-Location -LiteralPath $repositoryRoot

try {
    # Build first; timing excludes compilation, schema creation, seeding and readiness checks.
    dotnet build Brouj.slnx --configuration Release --no-restore
    if ($LASTEXITCODE -ne 0) { throw 'Release build failed.' }

    $env:BROUJ_RUN_PERFORMANCE_BASELINE = '1'
    $env:BROUJ_BASELINE_ROOT = $repositoryRoot
    $env:BROUJ_BASELINE_OUTPUT = $artifactPath
    $env:BROUJ_BASELINE_SMOKE = if ($Smoke) { '1' } else { '0' }
    Write-Host "Evidence directory: $artifactPath"
    dotnet test tests/Brouj.Infrastructure.IntegrationTests/Brouj.Infrastructure.IntegrationTests.csproj `
        --configuration Release --no-build --no-restore `
        --filter 'FullyQualifiedName~LocalPerformanceBaseline' `
        --logger 'console;verbosity=normal' --logger "trx;LogFileName=$runName.trx"

    if ($LASTEXITCODE -ne 0) { throw "Baseline failed; inspect the completed artifacts in $artifactPath." }
}
finally {
    Remove-Item Env:BROUJ_RUN_PERFORMANCE_BASELINE -ErrorAction SilentlyContinue
    Remove-Item Env:BROUJ_BASELINE_ROOT -ErrorAction SilentlyContinue
    Remove-Item Env:BROUJ_BASELINE_OUTPUT -ErrorAction SilentlyContinue
    Remove-Item Env:BROUJ_BASELINE_SMOKE -ErrorAction SilentlyContinue
    Pop-Location
}
