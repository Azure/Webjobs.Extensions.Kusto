# Check if the cluster name is passed as an argument
param(
    [Parameter(Mandatory=$true)]
    [string]$TestClusterName
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
    
    $Database = "webjobs-e2e"
    
    $env:KustoConnectionString = "Data Source=$TestClusterName;Database=$Database;Fed=True;UserToken=$UserAccessToken"
    $env:KustoConnectionStringNoPermissions = "Data Source=$TestClusterName;Database=$Database-no-perms;Fed=True;UserToken=$UserAccessToken"
    $env:KustoConnectionStringMSI = "Data Source=$TestClusterName;Database=$Database;Fed=True;"
    $env:KustoConnectionStringInvalidAttributes = "Data Source=$TestClusterName;Database=$Database;Fed=True;AppClientId=72f988bf-86f1-41af-91ab-2d7cd011db47"
    $env:Database = $Database
    $env:ProductsTable = "Products"

    Write-Host "--- Setting up dotnet env ---" -ForegroundColor Green
    dotnet restore --force-evaluate
    if ($LASTEXITCODE -ne 0) {
        Write-Error "dotnet restore failed"
        exit 1
    }
    
    dotnet format
    if ($LASTEXITCODE -ne 0) {
        Write-Error "dotnet format failed"
        exit 1
    }
    
    dotnet build
    if ($LASTEXITCODE -ne 0) {
        Write-Error "dotnet build failed"
        exit 1
    }
    
    Write-Host "--- Running E2E tests ---" -ForegroundColor Green
    dotnet test
    if ($LASTEXITCODE -ne 0) {
        Write-Error "dotnet test failed"
        exit 1
    }
    
    Write-Host "E2E tests completed successfully!" -ForegroundColor Green
}
catch {
    Write-Error "Script execution failed: $_"
    exit 1
}
