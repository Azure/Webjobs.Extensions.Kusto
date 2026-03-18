<#
.SYNOPSIS
    Build the E2E test Docker image and configure local settings for all language samples.
.DESCRIPTION
    Restores packages, builds the solution, builds the Docker image via build-docker-image.ps1,
    and generates local.settings.json for each language sample directory.
.EXAMPLE
    $env:CLUSTER = "https://mycluster.kusto.windows.net"; $env:DATABASE = "mydb"; .\run-functional-tests-e2e.ps1
.EXAMPLE
    .\run-functional-tests-e2e.ps1 -KustoConnectionString "Data Source=https://mycluster.kusto.windows.net;Database=mydb;Fed=True"
#>
Param(
    [Parameter(Mandatory = $false)][string]$Cluster = $env:CLUSTER,
    [Parameter(Mandatory = $false)][string]$Database = $env:DATABASE,
    [Parameter(Mandatory = $false)][string]$KustoConnectionString = $null,
    [Parameter(Mandatory = $false)][string]$AccessToken = $env:ACCESS_TOKEN
)

$ErrorActionPreference = "Stop"

$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Definition
$RepoRoot = Split-Path -Parent $ScriptDir
Set-Location $RepoRoot

# Build KustoConnectionString from CLUSTER/DATABASE env vars, or use parameter
if ([string]::IsNullOrEmpty($KustoConnectionString)) {
    if (-not [string]::IsNullOrEmpty($Cluster) -and -not [string]::IsNullOrEmpty($Database)) {
        $KustoConnectionString = "Data Source=$Cluster;Database=$Database;Fed=True"
        Write-Host "==> Using KustoConnectionString from Cluster and Database parameters"
    }
    else {
        Write-Error "Error: Either provide -KustoConnectionString, or set -Cluster and -Database (or CLUSTER/DATABASE env vars)"
        exit 1
    }
}

# Fetch access token from az CLI if not set
if ([string]::IsNullOrEmpty($AccessToken)) {
    Write-Host "==> Fetching access token from az CLI..."
    $AccessToken = az account get-access-token --resource https://kusto.kusto.windows.net --query accessToken -o tsv
}

$LangMap = @{
    "samples-csharp"     = "dotnet"
    "samples-java"       = "java"
    "samples-node"       = "javascript"
    "samples-outofproc"  = "dotnet-isolated"
    "samples-powershell" = "powershell"
    "samples-python"     = "python"
}

Write-Host "=== Step 1: Building Docker image ==="
. "$ScriptDir\BuildE2ETestImage.ps1"
BuildE2ETestImage

Write-Host "=== Step 2: Generating local.settings.json for each language sample ==="

$ConnStringWithToken = "${KustoConnectionString};AAD Federated Security=True;AAD User Token=${AccessToken}"

foreach ($entry in $LangMap.GetEnumerator()) {
    $sampleDir = $entry.Key
    $runtime = $entry.Value
    $targetDir = "samples/$sampleDir"
    $target = "$targetDir/local.settings.json"

    if (-not (Test-Path $targetDir)) {
        Write-Host "Warning: $targetDir does not exist, skipping"
        continue
    }

    Write-Host "Creating $target with FUNCTIONS_WORKER_RUNTIME=$runtime"

    $content = Get-Content -Raw local.settings.json.example
    $content = $content -replace '<lang>', $runtime
    $content = $content -replace '<KustoConnectionString>', $ConnStringWithToken
    $content | Set-Content -Path $target
}

Write-Host "=== Step 3: Running functional tests ==="
Set-Location "$RepoRoot/functions-int-tests"
mvn clean gatling:test
