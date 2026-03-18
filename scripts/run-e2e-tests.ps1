<#
.SYNOPSIS
    Run E2E performance tests against a Kusto cluster.
.DESCRIPTION
    Sets up environment variables (connection strings, access token) and builds the solution.
    Performance tests can then be run via the functions-int-tests directory.
.EXAMPLE
    $env:CLUSTER = "https://mycluster.kusto.windows.net"; $env:DATABASE = "mydb"; .\run-e2e-tests.ps1
.EXAMPLE
    .\run-e2e-tests.ps1 -Cluster "https://mycluster.kusto.windows.net" -Database "mydb"
#>
Param(
    [Parameter(Mandatory = $false)][string]$Cluster = $env:CLUSTER,
    [Parameter(Mandatory = $false)][string]$Database = $env:DATABASE
)

$ErrorActionPreference = "Stop"

$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Definition
$RepoRoot = Split-Path -Parent $ScriptDir
Set-Location $RepoRoot

if ([string]::IsNullOrEmpty($Cluster)) {
    Write-Error "Error: Either set CLUSTER env var or pass -Cluster parameter"
    exit 1
}
if ([string]::IsNullOrEmpty($Database)) {
    Write-Error "Error: Either set DATABASE env var or pass -Database parameter"
    exit 1
}

Write-Host "--- Setting up environment variables ---"
Write-Host "Cluster:  $Cluster"
Write-Host "Database: $Database"

$UserAccessToken = az account get-access-token --scope "$Cluster/.default" --query accessToken -o tsv
$env:KustoConnectionString = "Data Source=$Cluster;Database=$Database;Fed=True;UserToken=$UserAccessToken"
$env:KustoConnectionStringNoPermissions = "Data Source=$Cluster;Database=${Database}-no-perms;Fed=True;UserToken=$UserAccessToken"
$env:KustoConnectionStringMSI = "Data Source=$Cluster;Database=$Database;Fed=True;"
$env:KustoConnectionStringInvalidAttributes = "Data Source=$Cluster;Database=$Database;Fed=True;AppClientId=72f988bf-86f1-41af-91ab-2d7cd011db47"

Write-Host "--- Setting up dotnet env ---"
dotnet restore --force-evaluate
dotnet format
dotnet build --no-restore /p:RunTests=false

Write-Host "--- Build complete. Run performance tests via the functions-int-tests directory ---"
