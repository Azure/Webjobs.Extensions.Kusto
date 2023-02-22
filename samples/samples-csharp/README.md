# Kusto bindings for Azure Functions - .NET

## Table of Contents

- [Kusto bindings for Azure Functions - .NET](#kusto-bindings-for-azure-functions---net)
  * [Table of Contents](#table-of-contents)
  * [Setup Function Project](#setup-function-project)
  * [Input Binding](#input-binding)
    + [KustoAttribute for Input Bindings](#kustoattribute-for-input-bindings)
    + [Samples for Input Bindings](#samples-for-input-bindings)
      - [Query String](#query-string)
      - [KQL Functions](#kql-functions)
      - [IAsyncEnumerable](#iasyncenumerable)
    + [KustoAttribute for Output Bindings](#kustoattribute-for-output-bindings)
    + [Samples for Output Bindings](#samples-for-output-bindings)
      - [ICollector&lt;T&gt;/IAsyncCollector&lt;T&gt;](#icollectorlttgtiasynccollectorlttgt)
      - [Array](#array)
      - [Single Row](#single-row)
      - [Ingest CSV / Multiline CSV](#ingest-csv--multiline-csv)
      - [Ingest with mappings](#ingest-with-mappings)


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
5. Reference the [set-up](../set-up/KQL-Setup.kql) to create sample tables, mappings , functions required for the example
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
### KustoAttribute for Output Bindings

See [Output Binding Overview](../../README.md#output-binding) for general information about the Kusto Input binding.

The [KustoAttribute](https://github.com/Azure/Webjobs.Extensions.Kusto/blob/main/src/KustoAttribute.cs) for Output bindings takes the following arguments:

- Database: The database against which the query has to be executed

- ManagedServiceIdentity: A managed identity can be used to connect to Kusto. To use a System managed identity, use "system", any other identity names are interpreted as user managed identity

- TableName: The table to ingest the data into

- MappingRef: Optional attribute to pass a [mapping ref](https://learn.microsoft.com/en-us/azure/data-explorer/kusto/management/create-ingestion-mapping-command) that is already defined in the ADX cluster

- Connection: The _**name**_ of the variable that holds the connection string, resolved through environment variables or through function app settings. Defaults to lookup on the variable _**KustoConnectionString**_, at runtime this variable will be looked up against the environment.
Documentation on connection string can be found at [Kusto connection strings](https://learn.microsoft.com/en-us/azure/data-explorer/kusto/api/connection-strings/kusto)


- DataFormat: The default dataformat is `multijson/json`. This can be set to _**text**_ formats supported in the datasource format [enumeration](https://learn.microsoft.com/en-us/azure/data-explorer/kusto/api/netfx/kusto-ingest-client-reference#enum-datasourceformat). Samples are validated and provided for csv and JSON formats.



The following are valid binding types for the rows to be inserted into the table:

- **ICollector&lt;T&gt;/IAsyncCollector&lt;T&gt;**: Each element is a row represented by `T`, where `T` is a user-defined POCO, or Plain Old C# Object. `T` should follow the structure of a row in the queried table. See the [Query String](#query-string) for an example of what `T` should look like.
- **T**: Used when just one row is to be inserted into the table.
- **T[]**: Each element is again a row of the generic type  `T`. This output binding type requires manual instantiation of the array in the function.
- **string**: When data is not a POCO, rather a raw CSV for example that needs to be ingested.
    ```csv
    19222,prod2,220.22
    19223,prod2,221.22
    ```

The repo contains examples of each of these binding types [here](https://github.com/Azure/Webjobs.Extensions.Kusto/tree/main/samples/samples-csharp/OutputBindingSamples). A few examples are also included [below](#samples-for-output-bindings).


### Samples for Output Bindings

The following are some samples for the above collector types and options

#### ICollector&lt;T&gt;/IAsyncCollector&lt;T&gt;

When using an `ICollector`, it is not necessary to instantiate it. The function can add rows to the `ICollector` directly, and its contents are automatically upserted once the function exits.

 ```csharp
    [FunctionName("AddProductsCollector")]
    public static IActionResult Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "addproductscollector")]
        HttpRequest req, ILogger log,
        [Kusto(Database:SampleConstants.DatabaseName ,
        TableName =SampleConstants.ProductsTable ,
        Connection = "KustoConnectionString")] ICollector<Product> collector)
    {
        log.LogInformation($"AddProducts function started");
        string body = new StreamReader(req.Body).ReadToEnd();
        Product[] products = JsonConvert.DeserializeObject<Product[]>(body);
        products.ForEach(p =>
        {
            collector.Add(p);
        });
        return products != null ? new ObjectResult(products) { StatusCode = StatusCodes.Status201Created } : new BadRequestObjectResult("Please pass a well formed JSON Product array in the body");
    }
```

It is also possible to force an upsert within the function by calling `FlushAsync()` on an `IAsyncCollector`

```csharp
    [FunctionName("AddProductsAsyncCollector")]
    public static IActionResult Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "addproductsasynccollector")]
        HttpRequest req, ILogger log,
        [Kusto(Database:SampleConstants.DatabaseName ,
        TableName =SampleConstants.ProductsTable ,
        Connection = "KustoConnectionString")] IAsyncCollector<Product> collector)
    {
        log.LogInformation($"AddProductsAsyncCollector function started");
        string body = new StreamReader(req.Body).ReadToEnd();
        Product[] products = JsonConvert.DeserializeObject<Product[]>(body);
        products.ForEach(p =>
        {
            collector.AddAsync(p);
        });
        collector.FlushAsync();
        return products != null ? new ObjectResult(products) { StatusCode = StatusCodes.Status201Created } : new BadRequestObjectResult("Please pass a well formed JSON Product array in the body");
    }
```

#### Array

This output binding type requires explicit instantiation within the function body. Note also that the `Product[]` array must be prefixed by `out` when attached to the output binding

``` csharp
    [FunctionName("AddProductsArray")]
    public static IActionResult Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "addproductsarray")]
        HttpRequest req, ILogger log,
        [Kusto(Database:SampleConstants.DatabaseName ,
        TableName =SampleConstants.ProductsTable ,
        Connection = "KustoConnectionString")] out Product[] products)
    {
        log.LogInformation($"AddProducts function started");
        string body = new StreamReader(req.Body).ReadToEnd();
        products = JsonConvert.DeserializeObject<Product[]>(body);
        return products != null ? new ObjectResult(products) { StatusCode = StatusCodes.Status201Created } : new BadRequestObjectResult("Please pass a well formed JSON Product array in the body");
    }
```

#### Single Row

When binding to a single row, it is also necessary to prefix the row with `out`

```csharp
    [FunctionName("AddProductUni")]
    public static IActionResult Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "addproductuni")]
        HttpRequest req, ILogger log,
        [Kusto(Database:SampleConstants.DatabaseName ,
        TableName =SampleConstants.ProductsTable ,
        Connection = "KustoConnectionString")] out Product product)
    {
        log.LogInformation($"AddProduct function started");
        string body = new StreamReader(req.Body).ReadToEnd();
        product = JsonConvert.DeserializeObject<Product>(body);
        string productString = string.Format(CultureInfo.InvariantCulture, "(Name:{0} ID:{1} Cost:{2})",
                    product.Name, product.ProductID, product.Cost);
        log.LogInformation("Ingested product {}", productString);
        return new CreatedResult($"/api/addproductuni", product);
    }
```

#### Ingest CSV / Multiline CSV
A csv row can be bound to an `out` **_string_** and processed as follows. Note the `DataFormat` element used in the binding
```csharp
    [FunctionName("AddProductCsv")]
    public static IActionResult Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "addproductcsv")]
        HttpRequest req, ILogger log,
        [Kusto(Database:SampleConstants.DatabaseName ,
        TableName =SampleConstants.ProductsTable ,
        DataFormat = "csv",
        Connection = "KustoConnectionString")] out string productCsv)
    {
        productCsv = new StreamReader(req.Body).ReadToEnd();
        string productString = string.Format(CultureInfo.InvariantCulture, "(Csv : {0})", productCsv);
        log.LogInformation("Ingested product CSV {}", productString);
        return new CreatedResult($"/api/addproductcsv", productString);
    }
```
#### Ingest with mappings

In the event that we had a POCO of type item
```csharp
        public class Item
        {
            public long ItemID { get; set; }
    #nullable enable
            public string? ItemName { get; set; }
            public double ItemCost { get; set; }
        }
```
and we have to ingest this to the product table which has got different names. An ingestion mapping reference of **_item_to_product_json_** can be created and referenced. For example see mapping reference in the database below
```sql
    .show table Products ingestion mappings
```
| Name | Kind     | Mapping   |
| :----:    | :----:   | :----: |
| item_to_product_json       | Json |```[{"column":"ProductID","path":"$.ItemID","datatype":"","transform":null},{"column":"Name","path":"$.ItemName","datatype":"","transform":null},{"column":"Cost","path":"$.ItemCost","datatype":"","transform":null}]```  |


```csharp
        [FunctionName("AddProductsWithMapping")]
        public static IActionResult Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "addproductswithmapping")]
            HttpRequest req, ILogger log,
            [Kusto(Database:SampleConstants.DatabaseName ,
            TableName =SampleConstants.ProductsTable ,
            MappingRef = "item_to_product_json",
            Connection = "KustoConnectionString")] out Item item)
        {
            log.LogInformation($"AddProductsWithMapping function started");
            string body = new StreamReader(req.Body).ReadToEnd();
            item = JsonConvert.DeserializeObject<Item>(body);
            string productString = string.Format(CultureInfo.InvariantCulture, "(ItemName:{0} ItemID:{1} ItemCost:{2})",
                        item.ItemName, item.ItemID, item.ItemCost);
            log.LogInformation("Ingested item {}", productString);
            return item != null ? new ObjectResult(item) { StatusCode = StatusCodes.Status201Created } : new BadRequestObjectResult("Please pass a well formed JSON Product array in the body");
        }
```