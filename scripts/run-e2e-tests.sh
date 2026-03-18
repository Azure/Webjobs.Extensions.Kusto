#!/bin/bash
set -euo pipefail

# Run E2E performance tests against a Kusto cluster.
# Usage:
#   CLUSTER=https://mycluster.kusto.windows.net DATABASE=mydb ./run-e2e-tests.sh
#   ./run-e2e-tests.sh <ClusterURL> <Database>

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"
cd "$REPO_ROOT"

# Resolve cluster and database from env vars or positional parameters
if [ -n "${CLUSTER:-}" ]; then
    TestClusterName="$CLUSTER"
elif [ $# -ge 1 ]; then
    TestClusterName="$1"
else
    echo "Error: Either set CLUSTER env var or pass cluster URL as \$1"
    echo "Usage: CLUSTER=<url> DATABASE=<db> $0"
    echo "       $0 <ClusterURL> <Database>"
    exit 1
fi

if [ -n "${DATABASE:-}" ]; then
    TestDatabase="$DATABASE"
elif [ $# -ge 2 ]; then
    TestDatabase="$2"
else
    echo "Error: Either set DATABASE env var or pass database name as \$2"
    echo "Usage: CLUSTER=<url> DATABASE=<db> $0"
    echo "       $0 <ClusterURL> <Database>"
    exit 1
fi

echo "--- Setting up environment variables ---"
echo "Cluster:  $TestClusterName"
echo "Database: $TestDatabase"

UserAccessToken=$(az account get-access-token --scope "$TestClusterName/.default" --query accessToken -o tsv)
export KustoConnectionString="Data Source=$TestClusterName;Database=$TestDatabase;Fed=True;UserToken=$UserAccessToken"
export KustoConnectionStringNoPermissions="Data Source=$TestClusterName;Database=${TestDatabase}-no-perms;Fed=True;UserToken=$UserAccessToken"
export KustoConnectionStringMSI="Data Source=$TestClusterName;Database=$TestDatabase;Fed=True;"
export KustoConnectionStringInvalidAttributes="Data Source=$TestClusterName;Database=$TestDatabase;Fed=True;AppClientId=72f988bf-86f1-41af-91ab-2d7cd011db47"
echo "--- Setting up dotnet env ---"
dotnet restore --force-evaluate && dotnet format && dotnet build --no-restore -p:RunTests=false
echo "--- Build complete. Run performance tests via the functions-int-tests directory ---"