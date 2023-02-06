# Contributing

This project welcomes contributions and suggestions. Most contributions require you to agree to a Contributor License Agreement (CLA) declaring that you have the right to, and actually do, grant us the rights to use your contribution. For details, visit https://cla.opensource.microsoft.com.

When you submit a pull request, a CLA bot will automatically determine whether you need to provide a CLA and decorate the PR appropriately (e.g., status check, comment). Simply follow the instructions provided by the bot. You will only need to do this once across all repos using our CLA.

This project has adopted the Microsoft Open Source Code of Conduct. For more information see the Code of Conduct FAQ or contact opencode@microsoft.com with any additional questions or comments.

## Contributor Getting Started

### Setup

1. [Install Visual Studio](https://visualstudio.microsoft.com/vs/) or  [Install VS Code](https://code.visualstudio.com/Download)

2. Clone the repo

```pwsh
git clone https://github.com/Azure/Webjobs.Extensions.Kusto.git
cd Webjobs.Extensions.Kusto
code .
```
3. Install extensions when prompted after VS Code opens
   - Note: This includes the Azure Functions, C#, and editorconfig extensions

###  KQL set up

> #### Create tables

```sql
.create-merge table kusto_functions_e2e_tests (ID:int, Name:string, Cost:real, Timestamp:datetime)  kusto_functions_e2e_tests
```
    
> ##### Optionally create mappings
 
```sql
.create-or-alter table kusto_functions_e2e_tests ingestion json mapping  "product_to_item_json_mapping" '[{"column":"ID","path":"$.ProductID","datatype":"","transform":""},{"column":"Name","path":"$.ProductName","datatype":"","transform":""},{"column":"Cost","path":"$.UnitCost","datatype":"","transform":""},{"column":"Timestamp","path":"$.Timestamp","datatype":"","transform":""}]'
``` 

### Running End To End Tests

The following environment variables are required for various E2E test scenarios

1. Happy path flow where there exists an application id / key that can ingest and query data from the corresponding Kusto cluster

```pwsh
$env:KustoConnectionString="Data Source=https://<cluster>.<region>.kusto.windows.net;Database=<database>;Fed=True;AppClientId=<app id>;AppKey=<app key>;Authority Id=<tenant>"
```

2. Test scenarios where there are privilege issues in ingest / query. The easiest is to set up a valid app registration but not assigning any privileges so that ingest and queries fail with privilege issues

```pwsh
$env:KustoConnectionStringNoPermissions="Data Source=https://<cluster>.<region>.kusto.windows.net;Database=<database>;Fed=True;AppClientId=<app id>;AppKey=<app key>;Authority Id=<tenant>"
```

3. Extension of scenario where a "System" Managed Identity can be used for tests

```pwsh
$env:AZURE_TENANT_ID="<tenant>"
$env:AZURE_CLIENT_ID="<app id>"
$env:AZURE_CLIENT_SECRET="<app key>"
$env:KustoConnectionStringMSI="Data Source=https://<cluster>.kusto.windows.net;Database=<database>;Fed=True;"
```

4. Test scenarios where a malformed string is used for connection string. In this case, an app id is specified but no app key is mentioned. This is a malformed connection string

```pwsh
$env:KustoConnectionStringInvalidAttributes="Data Source=https://<cluster>.kusto.windows.net;Database=<database>;Fed=True;AppClientId=<app id>"
```

Once the aforementioned the environment variables are set, the tests can be run on the code base

```pwsh
dotnet test --collect:"XPlat Code Coverage"
```

This usually provides the coverage statistics in the format below

```pwsh
Attachments:
  <Path>\<Guid>\coverage.cobertura.xml
Passed!  - Failed:     0, Passed:    18, Skipped:     0, Total:    18, Duration: 1 m 9 s - Microsoft.Azure.WebJobs.Extensions
```

The overall coverage can then be captured as below

```pwsh
reportgenerator -reports:<Path>\<Guid>\coverage.cobertura.xml -targetdir:"coveragereport" -reporttypes:Html
```
