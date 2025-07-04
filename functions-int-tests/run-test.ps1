param(
    [Parameter(Mandatory=$true)] [string]$TestClusterName,
    [Parameter(Mandatory=$true)] [string]$Database = "webjobs-e2e"
)


if ([string]::IsNullOrEmpty($TestClusterName)) {
    Write-Error "Cluster name is not passed as an argument"
    exit 1
}

Write-Host "--- Setting up environment variables ---" -ForegroundColor Green

try {
    $UserAccessToken = az account get-access-token --scope "$TestClusterName/.default" --query accessToken -o tsv
    if ($LASTEXITCODE -ne 0) {
        Write-Error "Failed to get access token"
        exit 1
    }
    $UniqueId = [System.Guid]::NewGuid().ToString().Substring(0, 8)
    $env:CLUSTER = $TestClusterName
    $env:DATABASE = $Database
    $env:ACCESS_TOKEN = $UserAccessToken
    $env:PRODUCTS_TABLE_NAME = "Products_$UniqueId"
    $env:ITEMS_TABLE_NAME = "Items_$UniqueId"
}
catch {
    Write-Error "Script execution failed: $_"
    exit 1
}

Write-Host "--- Running tests ---" -ForegroundColor Green
mvn clean gatling:test