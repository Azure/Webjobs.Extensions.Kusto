#/bin/bash
#check if the cluster name is passed as an argument
if [ -z "$1" ]
then
    echo "Cluster name is not passed as an argument"
    exit 1
fi
TestClusterName=$1
TestDatabaseName=${2:-"webjobs-e2e"}
TestDatabaseNameNoPermissions=${3:-"webjobs-e2e-no-perms"}
echo "--- Setting up environment variables ---"
UserAccessToken=`az account get-access-token --scope "$TestClusterName/.default" --query accessToken -o tsv`
export KustoConnectionString="Data Source=$TestClusterName;Database=$TestDatabaseName;Fed=True;UserToken=$UserAccessToken"
export KustoConnectionStringNoPermissions="Data Source=$TestClusterName;Database=$TestDatabaseNameNoPermissions;Fed=True;UserToken=$UserAccessToken"
export KustoConnectionStringMSI="Data Source=$TestClusterName;Database=$TestDatabaseName;Fed=True;"
export KustoConnectionStringInvalidAttributes="Data Source=$TestClusterName;Database=$TestDatabaseName;Fed=True;AppClientId=72f988bf-86f1-41af-91ab-2d7cd011db47"
export TestDatabaseName
export TestDatabaseNameNoPermissions
echo "--- Setting up dotnet env ---"
dotnet restore --force-evaluate && dotnet format && dotnet build
echo "--- Running E2E tests ---"
dotnet test