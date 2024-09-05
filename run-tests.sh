#/bin/bash
# Check the first parameter it should be cluster name
clusterName=''
if [ -z "$1" ]
  then
    echo "No cluster name supplied"
    exit 1
fi
clusterName = $1
userAccessToken=`az account get-access-token --scope "$clusterName/.default" --query accessToken -o tsv`
export KustoConnectionString: Data Source=$clusterName;Database=e2e;Fed=True;UserToken=$userAccessToken
export KustoConnectionStringNoPermissions: Data Source=$clusterName;Database=webjobs;Fed=True;UserToken=$userAccessToken
export KustoConnectionStringMSI: Data Source=$clusterName;Database=e2e;Fed=True;
export KustoConnectionStringInvalidAttributes: Data Source=$clusterName;Database=e2e;Fed=True;AppClientId=134
dotnet clean
dotnet test --collect:"XPlat Code Coverage"

