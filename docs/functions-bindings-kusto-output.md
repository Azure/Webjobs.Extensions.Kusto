# **Kusto output bindings for Azure Functions - Preview**

## **Introduction**

This document explains the usage of the output bindings that are suppported on  Azure functions for Kusto. Output bindings are used to ingest data to Kusto.

For information on setup and configuration details, see the [overview](./functions-bindings-kusto.md).

## Examples
<a id="example"></a>

::: zone pivot="programming-language-csharp"

[!INCLUDE [functions-bindings-csharp-intro](../../includes/functions-bindings-csharp-intro.md)]

# [In-process](#tab/in-process)

More samples for the Kusto output binding are available in the [GitHub repository](https://github.com/Azure/Webjobs.Extensions.Kusto/tree/main/samples/samples-csharp).

This section contains the following examples:

* [HTTP trigger, write one record](#http-trigger-write-one-record-c)
* [HTTP trigger, write to two tables](#http-trigger-write-to-two-tables-c)
* [HTTP trigger, write records using IAsyncCollector](#http-trigger-write-records-using-iasynccollector-c)

The examples refer to `Product` class and a corresponding database table:

```cs
public class Product
{
    [JsonProperty(nameof(ProductID))]
    public long ProductID { get; set; }

    [JsonProperty(nameof(Name))]
    public string Name { get; set; }

    [JsonProperty(nameof(Cost))]
    public double Cost { get; set; }
}
```

```kql
.create-merge table Products (ProductID:long, Name:string, Cost:double)
```


<a id="http-trigger-write-one-record-c"></a>

### HTTP trigger, write one record

The following example shows a [C# function](functions-dotnet-class-library.md) that adds a record to a database, using data provided in an HTTP POST request as a JSON body.

```cs
using System.Globalization;
using System.IO;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Extensions.Kusto.Samples.Common;
using Microsoft.Azure.WebJobs.Kusto;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Microsoft.Azure.WebJobs.Extensions.Kusto.Samples.OutputBindingSamples
{
    public static class AddProduct
    {
        [FunctionName("AddProductUni")]
        public static IActionResult Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "addproductuni")]
            HttpRequest req, ILogger log,
            [Kusto(Database:"productsdb" ,
            TableName ="Products" ,
            Connection = "KustoConnectionString")] out Product product)
        {
            log.LogInformation($"AddProduct function started");
            string body = new StreamReader(req.Body).ReadToEnd();
            product = JsonConvert.DeserializeObject<Product>(body);
            return new CreatedResult($"/api/addproductuni", product);
        }
    }
}
```



<a id="http-trigger-write-to-two-tables-c"></a>

### HTTP trigger, write to two tables

The following example shows a [C# function](functions-dotnet-class-library.md) that adds records to a database in two different tables (`Products` and `ProductsChangeLog`), using data provided in an HTTP POST request as a JSON body and multiple output bindings.

```kql
.create-merge table ProductsChangeLog (ProductID:long, CreatedAt:datetime)
```

```cs
using System;
using Newtonsoft.Json;

namespace Microsoft.Azure.WebJobs.Extensions.Kusto.Samples.Common
{
    public class ProductsChangeLog
    {
        [JsonProperty(nameof(ProductID))]
        public long ProductID { get; set; }

        [JsonProperty(nameof(CreatedAt))]
        public DateTime CreatedAt { get; set; }

    }
}
```

```cs
using System;
using System.IO;
using Kusto.Cloud.Platform.Utils;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Extensions.Kusto.Samples.Common;
using Microsoft.Azure.WebJobs.Kusto;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Microsoft.Azure.WebJobs.Extensions.Kusto.Samples.OutputBindingSamples
{
    public static class AddMultiTable
    {
        [FunctionName("AddMultiTable")]
        public static IActionResult Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "addmulti")]
            HttpRequest req, ILogger log,
            [Kusto(Database:"productsdb" ,
            TableName ="Products" ,
            Connection = "KustoConnectionString")] IAsyncCollector<Product> productsCollector,
                        [Kusto(Database:"productsdb" ,
            TableName =S"ProductsChangeLog" ,
            Connection = "KustoConnectionString")] IAsyncCollector<ProductsChangeLog> changeCollector)
        {
            log.LogInformation($"AddProducts function started");
            string body = new StreamReader(req.Body).ReadToEnd();
            Product[] products = JsonConvert.DeserializeObject<Product[]>(body);
            products.ForEach(p =>
            {
                productsCollector.AddAsync(p);
                changeCollector.AddAsync(new ProductsChangeLog { CreatedAt = DateTime.UtcNow, ProductID = p.ProductID });
            });
            productsCollector.FlushAsync();
            changeCollector.FlushAsync();
            return products != null ? new ObjectResult(products) { StatusCode = StatusCodes.Status201Created } : new BadRequestObjectResult("Please pass a well formed JSON Product array in the body");
        }
    }
}
```

<a id="http-trigger-write-records-using-iasynccollector-c"></a>

### HTTP trigger, write records using IAsyncCollector

The following example shows a [C# function](functions-dotnet-class-library.md) that ingests a set of records to a table, using data provided in an HTTP POST body JSON array.

```cs
using System.IO;
using Kusto.Cloud.Platform.Utils;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Extensions.Kusto.Samples.Common;
using Microsoft.Azure.WebJobs.Kusto;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Microsoft.Azure.WebJobs.Extensions.Kusto.Samples.OutputBindingSamples
{
    public static class AddProductsAsyncCollector
    {
        [FunctionName("AddProductsAsyncCollector")]
        public static IActionResult Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "addproductsasynccollector")]
            HttpRequest req, ILogger log,
            [Kusto(Database:"productsdb" ,
            TableName ="Products" ,
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
    }
}
```


# [Isolated process](#tab/isolated-process)

More samples for the Azure SQL output binding are available in the [GitHub repository](https://github.com/Azure/Webjobs.Extensions.Kusto/tree/main/samples/samples-outofproc).

This section contains the following examples:

* [HTTP trigger, write one record](#http-trigger-write-one-record-c-oop)
* [HTTP trigger, write records with mapping](#http-trigger-write-records-with-mapping-oop)

The examples refer to `Product` class and a corresponding database table:

```cs
public class Product
{
    [JsonProperty(nameof(ProductID))]
    public long ProductID { get; set; }

    [JsonProperty(nameof(Name))]
    public string Name { get; set; }

    [JsonProperty(nameof(Cost))]
    public double Cost { get; set; }
}
```

```kql
.create-merge table Products (ProductID:long, Name:string, Cost:double)
```


<a id="http-trigger-write-one-record-c-oop"></a>

### HTTP trigger, write one record

The following example shows a [C# function](functions-dotnet-class-library.md) that adds a record to a database, using data provided in an HTTP POST request as a JSON body.

```cs
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Extensions.Kusto;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.WebJobs.Extensions.Kusto.SamplesOutOfProc.OutputBindingSamples.Common;

namespace Microsoft.Azure.WebJobs.Extensions.Kusto.SamplesOutOfProc.OutputBindingSamples
{
    public static class AddProduct
    {
        [Function("AddProduct")]
        [KustoOutput(Database: "productsdb", Connection = "KustoConnectionString", TableName = "Products")]
        public static async Task<Product> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "addproductuni")]
            HttpRequestData req)
        {
            Product? prod = await req.ReadFromJsonAsync<Product>();
            return prod ?? new Product { };
        }
    }
}

```

<a id="http-trigger-write-records-with-mapping-oop"></a>

### HTTP trigger, write records with mapping

The following example shows a [C# function](functions-dotnet-class-library.md) that adds a collection of records to a database, using a mapping that transforms a `Product` to `Item`.

To transform data from `Product` to `Item`, the function uses a mapping reference 

```kql
.create-merge table Item (ItemID:long, ItemName:string, ItemCost:float)


-- Create a mapping that transforms an Item to a Product

.create-or-alter table Product ingestion json mapping "item_to_product_json" '[{"column":"ProductID","path":"$.ItemID"},{"column":"Name","path":"$.ItemName"},{"column":"Cost","path":"$.ItemCost"}]'
```

```cs
namespace Microsoft.Azure.WebJobs.Extensions.Kusto.SamplesOutOfProc.OutputBindingSamples.Common
{
    public class Item
    {
        public long ItemID { get; set; }

        public string? ItemName { get; set; }

        public double ItemCost { get; set; }
    }
}
```


```cs
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Extensions.Kusto;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.WebJobs.Extensions.Kusto.SamplesOutOfProc.OutputBindingSamples.Common;

namespace Microsoft.Azure.WebJobs.Extensions.Kusto.SamplesOutOfProc.OutputBindingSamples
{
    public static class AddProductsWithMapping
    {
        [Function("AddProductsWithMapping")]
        [KustoOutput(Database: "productsdb", Connection = "KustoConnectionString", TableName = "Products", MappingRef = "item_to_product_json")]
        public static async Task<Item> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "addproductswithmapping")]
            HttpRequestData req)
        {
            Item? item = await req.ReadFromJsonAsync<Item>();
            return item ?? new Item { };
        }
    }
}
```
::: zone-end

::: zone pivot="programming-language-java"

More samples for the Azure SQL output binding are available in the [GitHub repository](https://github.com/Azure/azure-functions-sql-extension/tree/main/samples/samples-java).

This section contains the following examples:

* [HTTP trigger, write a record to a table](#http-trigger-write-record-to-table-java)
* [HTTP trigger, write to two tables](#http-trigger-write-to-two-tables-java)

The examples refer to a `PrProductoducts` class (in a separate file `Product.java`) and a corresponding database table `Products` (defined earlier):

```java
package com.microsoft.azure.kusto.common;

import com.fasterxml.jackson.annotation.JsonProperty;

public class Product {
    @JsonProperty("ProductID")
    public long ProductID;
    @JsonProperty("Name")
    public String Name;
    @JsonProperty("Cost")
    public double Cost;

    public Product() {
    }

    public Product(long ProductID, String name, double Cost) {
        this.ProductID = ProductID;
        this.Name = name;
        this.Cost = Cost;
    }
}
```

:::code language="sql" source="~/functions-sql-todo-sample/sql/create.sql" range="1-7":::

<a id="http-trigger-write-record-to-table-java"></a>
### HTTP trigger, write a record to a table

The following example shows a Kusto output binding in a Java function that adds a product record to a table, using data provided in an HTTP POST request as a JSON body.  The function takes an additional dependency on the [com.fasterxml.jackson.core](https://github.com/FasterXML/jackson) library to parse the JSON body.

```xml
<dependency>
    <groupId>com.fasterxml.jackson.core</groupId>
    <artifactId>jackson-databind</artifactId>
    <version>2.13.4.1</version>
</dependency>
```

```java
package com.microsoft.azure.kusto.outputbindings;

import com.fasterxml.jackson.databind.ObjectMapper;
import com.microsoft.azure.functions.HttpMethod;
import com.microsoft.azure.functions.HttpRequestMessage;
import com.microsoft.azure.functions.HttpResponseMessage;
import com.microsoft.azure.functions.HttpStatus;
import com.microsoft.azure.functions.OutputBinding;
import com.microsoft.azure.functions.annotation.AuthorizationLevel;
import com.microsoft.azure.functions.annotation.FunctionName;
import com.microsoft.azure.functions.annotation.HttpTrigger;
import com.microsoft.azure.functions.kusto.annotation.KustoOutput;
import com.microsoft.azure.kusto.common.Product;

import java.io.IOException;
import java.util.Optional;

import static com.microsoft.azure.kusto.common.Constants.*;

public class AddProduct {
    @FunctionName("AddProduct")
    public HttpResponseMessage run(@HttpTrigger(name = "req", methods = {
            HttpMethod.POST}, authLevel = AuthorizationLevel.ANONYMOUS, route = "addproductuni") HttpRequestMessage<Optional<String>> request,
            @KustoOutput(name = "product", database = "productsdb", tableName = "Products", connection = KUSTOCONNSTR) OutputBinding<Product> product)
            throws IOException {

        if (request.getBody().isPresent()) {
            String json = request.getBody().get();
            ObjectMapper mapper = new ObjectMapper();
            Product p = mapper.readValue(json, Product.class);
            product.setValue(p);
            return request.createResponseBuilder(HttpStatus.OK).header("Content-Type", "application/json").body(product)
                    .build();
        } else {
            return request.createResponseBuilder(HttpStatus.NO_CONTENT).header("Content-Type", "application/json")
                    .build();
        }
    }
}
```

<a id="http-trigger-write-to-two-tables-java"></a>
### HTTP trigger, write to two tables

The following example shows a SQL output binding in a JavaS function that adds records to a database in two different tables (`dbo.ToDo` and `dbo.RequestLog`), using data provided in an HTTP POST request as a JSON body and multiple output bindings.  The function takes an additional dependency on the [com.fasterxml.jackson.core](https://github.com/FasterXML/jackson) library to parse the JSON body.

```xml
<dependency>
    <groupId>com.fasterxml.jackson.core</groupId>
    <artifactId>jackson-databind</artifactId>
    <version>2.13.4.1</version>
</dependency>
```

The second table, `ProductsChangeLog`, corresponds to the following definition:

```sql
.create-merge table ProductsChangeLog (ProductID:long, CreatedAt:datetime)
```

and Java class in `ProductsChangeLog.java`:

```java
package com.microsoft.azure.kusto.common;

import com.fasterxml.jackson.annotation.JsonProperty;

public class ProductsChangeLog {
    @JsonProperty("ProductID")
    public long ProductID;
    @JsonProperty("CreatedAt")
    public String CreatedAt;

    public ProductsChangeLog() {
    }

    public ProductsChangeLog(long ProductID, String CreatedAt) {
        this.ProductID = ProductID;
        this.CreatedAt = CreatedAt;
    }
}
```


```java
package com.microsoft.azure.kusto.outputbindings;

import com.fasterxml.jackson.databind.ObjectMapper;
import com.microsoft.azure.functions.HttpMethod;
import com.microsoft.azure.functions.HttpRequestMessage;
import com.microsoft.azure.functions.HttpResponseMessage;
import com.microsoft.azure.functions.HttpStatus;
import com.microsoft.azure.functions.OutputBinding;
import com.microsoft.azure.functions.annotation.AuthorizationLevel;
import com.microsoft.azure.functions.annotation.FunctionName;
import com.microsoft.azure.functions.annotation.HttpTrigger;
import com.microsoft.azure.functions.kusto.annotation.KustoOutput;
import com.microsoft.azure.kusto.common.Product;
import com.microsoft.azure.kusto.common.ProductsChangeLog;

import static com.microsoft.azure.kusto.common.Constants.*;

import java.io.IOException;
import java.time.Clock;
import java.time.Instant;
import java.util.Optional;

public class AddMultiTable {
    @FunctionName("AddMultiTable")
    public HttpResponseMessage run(@HttpTrigger(name = "req", methods = {
            HttpMethod.POST}, authLevel = AuthorizationLevel.ANONYMOUS, route = "addmultitable") HttpRequestMessage<Optional<String>> request,
            @KustoOutput(name = "product", database = "productsdb", tableName = "Products", connection = KUSTOCONNSTR) OutputBinding<Product> product,
            @KustoOutput(name = "productChangeLog", database = "productsdb", tableName = "ProductsChangeLog",
                    connection = KUSTOCONNSTR) OutputBinding<ProductsChangeLog> productChangeLog)
            throws IOException {

        if (request.getBody().isPresent()) {
            String json = request.getBody().get();
            ObjectMapper mapper = new ObjectMapper();
            Product p = mapper.readValue(json, Product.class);
            product.setValue(p);
            productChangeLog.setValue(new ProductsChangeLog(p.ProductID, Instant.now(Clock.systemUTC()).toString()));
            return request.createResponseBuilder(HttpStatus.OK).header("Content-Type", "application/json").body(product)
                    .build();
        } else {
            return request.createResponseBuilder(HttpStatus.NO_CONTENT).header("Content-Type", "application/json")
                    .build();
        }
    }
}
```

::: zone-end  
