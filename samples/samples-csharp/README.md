# Kusto bindings for Azure Functions - .NET

## Table of Contents

- [Kusto bindings for Azure Functions - .NET](#kusto-bindings-for-azure-functions---net)
  - [Table of Contents](#table-of-contents)
  - [Setup Function Project](#setup-function-project)
  - [Input Binding](#input-binding)
    - [KustoAttribute for Input Bindings](#KustoAttribute-for-input-bindings)
    - [Setup for Input Bindings](#setup-for-input-bindings)
    - [Samples for Input Bindings](#samples-for-input-bindings)
      - [Query String](#query-string)
      - [KQL Functions](#kql-functions)
      - [IAsyncEnumerable](#iasyncenumerable)

## Setup Function Project

These instructions will guide you through creating your Function Project and adding the Kusto binding extension. This only needs to be done once for every function project you create. If you have one created already you can skip this step.

1. Install [Azure Functions Core Tools](https://docs.microsoft.com/azure/azure-functions/functions-run-local)

2. Create a function project for .NET:

    ```powershell
    mkdir SampleApp
    cd SampleApp
    func init --worker-runtime dotnet
    ```

3. Enable Kusto bindings on the function project. 

    Install the extension.

    ```powershell
    dotnet add package Microsoft.Azure.WebJobs.Extensions.Kusto --prerelease
    ```

4. Use the `local.settings.json` to provide the [KustoConnectionString](https://learn.microsoft.com/en-us/azure/data-explorer/kusto/api/connection-strings/kusto)

    ```json
    {
        "IsEncrypted": false,
        "Values": {
            "AzureWebJobsStorage": "UseDevelopmentStorage=true",
            "FUNCTIONS_WORKER_RUNTIME": "dotnet",
            "AzureWebJobsDashboard": "",
            "KustoConnectionString": "Data Source=https://<kusto-cluster>.kusto.windows.net;Database=sdktestsdb;Fed=True;AppClientId=<app-id>;AppKey=<app-key>;Authority Id=<tenant-id>"
        },
        "ConnectionStrings": {
            "rabbitMQConnectionAppSetting": "amqp://guest:guest@rabbitmq:5672"
        }
    }
    ```
    and `host.json`
    ```json
    
        {
            "version": "2.0",
            "logging": {
                "applicationInsights": {
                    "samplingSettings": {
                        "isEnabled": true,
                        "excludedTypes": "Request"
                    }
                }
            }
        }
    ```
## Input Binding
See [Input Binding Overview](../../README.md#input-binding) for general information about the Kusto Input binding.

### KustoAttribute for Input Bindings

The [KustoAttribute](https://github.com/Azure/Webjobs.Extensions.Kusto/blob/main/src/KustoAttribute.cs) for Input bindings takes four arguments:

Takes a KQL query or KQL function to run (with optional parameters) and returns the output to the function. 
The input binding takes the following attributes

- Database: The database against which the query has to be executed

- ManagedServiceIdentity: A managed identity can be used to connect to Kusto. To use a System managed identity, use "system", any other identity names are interpreted as user managed identity

- KqlCommand: The KqlQuery that has to be executed. Can be a KQL query or a KQL Function call

- KqlParameters: Parameters that act as predicate variables for the KqlCommand. For example "@name={name},@Id={id}" where the parameters {name} and {id} will be substituted at runtime with actual values acting as predicates

- Connection: The _**name**_ of the variable that holds the connection string, resolved through environment variables or through function app settings. Defaults to lookup on the variable _**KustoConnectionString**_, at runtime this variable will be looked up against the environment.
Documentation on connection string can be found at [Kusto connection strings](https://learn.microsoft.com/en-us/azure/data-explorer/kusto/api/connection-strings/kusto)
e.g.:
"KustoConnectionString": "Data Source=https://_**cluster**_.kusto.windows.net;Database=_**Database**_;Fed=True;AppClientId=_**AppId**_;AppKey=_**AppKey**_;Authority Id=_**TenantId**_
Note that the application id should atleast have viewer privileges on the table(s)/function(s) being queried in the KqlCommand


The following are valid binding types for the result of the query/stored procedure execution:

- **IEnumerable&lt;T&gt;**: Each element is a row of the generic type  `T`, where `T` is a user-defined POCO, or Plain Old C# Object. `T` should follow the structure of a row in the queried table. See the [Query String](#query-string) section for an example of what `T` should look like. An example is provided [here](https://github.com/Azure/Webjobs.Extensions.Kusto/blob/main/samples/samples-csharp/InputBindingSamples/GetProductsFunction.cs)

- **IAsyncEnumerable&lt;T&gt;**: Each element is again a row of the generic type  `T`, but the rows are retrieved "lazily". A row of the result is only retrieved when `MoveNextAsync` is called on the enumerator. This is useful in the case that the query and predicate return a lot of rows. An example is provided [here](https://github.com/Azure/Webjobs.Extensions.Kusto/blob/main/samples/samples-csharp/InputBindingSamples/GetProductsAsyncEnumerable.cs)

- **String**: A JSON string representation of the rows of the result (an example is provided [here](https://github.com/Azure/Webjobs.Extensions.Kusto/blob/main/samples/samples-csharp/InputBindingSamples/GetProductsString.cs). Note that as a generic representation, returns are a JSONArray with 1 row in case of 1 row being selected

- **JArray**: A [JSONArray](https://www.newtonsoft.com/json/help/html/T_Newtonsoft_Json_Linq_JArray.htm) type of the rows of the result (an example is provided [here](https://github.com/Azure/Webjobs.Extensions.Kusto/blob/main/samples/samples-csharp/InputBindingSamples/GetProductsJson.cs). Note that as a generic representation, returns are a JSONArray with 1 row in case of 1 row being selected


### Samples for Input Bindings

The repo contains examples of each of these binding types [here](https://github.com/Azure/Webjobs.Extensions.Kusto/tree/main/samples/samples-csharp/InputBindingSamples).

#### Query String

The input binding executes the `declare query_parameters (productId:long);Products | where ProductID == productId` query, returning the result as an `IEnumerable<Product>`, where Product is a user-defined POCO. The *Parameters* argument passes the `{productId}` specified in the URL that triggers the function, `getproducts/{productId}`, as the value of the `@productId` parameter in the query.


| ProductID | Name     | Cost   |
| :----:    | :----:   | :----: |
| 104       | Prod-104 | 11.01  |
| 104       | Prod-104 | 11.11  |

The corresponding POCO for `Product` is as follows

```csharp
    public class Product
    {
        [JsonProperty("ProductID")]
        public long ProductID { get; set; }

        [JsonProperty("Name")]
        public string Name { get; set; }

        [JsonProperty("Cost")]
        public double Cost { get; set; }
    }
```

```csharp
    [FunctionName("GetProductsList")]
    public static async Task<IActionResult> RunAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "getproducts/{productId}")]
        HttpRequest req,
        [Kusto(Database:"functionsdb" ,
        KqlCommand = "declare query_parameters (productId:long,rmqPrefix:string);Products | where ProductID == productId and Name !has rmqPrefix" ,
        KqlParameters = "@productId={productId},@rmqPrefix=R-MQ", // Exclude any parameters that have this prefix
        Connection = "KustoConnectionString")]
        IEnumerable<Product> products)
    {
        return new OkObjectResult(products);
    }
```

#### KQL Functions

`GetProductsByName` is the name of a KQL Function in the database functionsdb. The parameter value of the `@name` parameter in the procedure is in the `{name}` specified in the `getproductsfn/{name}` URL.

```csharp
    [FunctionName("GetProductsFunction")]
    public static IActionResult Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "getproductsfn/{name}")]
        HttpRequest req,
        [Kusto(Database:"functionsdb" ,
        KqlCommand = "declare query_parameters (name:string);GetProductsByName(name)" ,
        KqlParameters = "@name={name}",
        Connection = "KustoConnectionString")]
        IEnumerable<Product> products)
    {
        return new OkObjectResult(products);
    }
```

#### IAsyncEnumerable

Using the `IAsyncEnumerable` binding generally requires that the `Run` function be `async`. It is also important to call `DisposeAsync` at the end of function execution to make sure all resources used by the enumerator are freed.

```csharp
    [FunctionName("GetProductsAsyncEnumerable")]
    public static async Task<IActionResult> RunAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "getproducts-ae?name={name}")]
        HttpRequest req,
        [Kusto(Database:"functionsdb" ,
        KqlCommand = "declare query_parameters (name:string);Products | where Name == name" ,
        KqlParameters = "@name={name}",
        Connection = "KustoConnectionString")]
        IAsyncEnumerable<Product> products)
    {
        IAsyncEnumerator<Product> enumerator = products.GetAsyncEnumerator();
        var productList = new List<Product>();
        while (await enumerator.MoveNextAsync())
        {
            productList.Add(enumerator.Current);
        }
        await enumerator.DisposeAsync();
        return new OkObjectResult(productList);
    }
```
