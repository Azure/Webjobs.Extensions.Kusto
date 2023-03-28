---
title: Kusto bindings for Functions
description: Understand how to use Kusto bindings in Azure Functions.
author: ramacg
ms.topic: reference
ms.custom: event-tier1-build-2022
ms.date: 27/03/2023
ms.author: ramacg
ms.reviewer: 
zone_pivot_groups: programming-languages-set-functions-lang-workers
---

# Kusto bindings for Azure Functions overview (preview)


This set of articles explains how to work with [Kusto](/azure/kusto/index) bindings in Azure Functions. Azure Functions supports input bindings, output bindings for Kusto.

| Action | Type |
|---------|---------|
| Read from Kusto | [Input binding](./functions-bindings-kusto-input.md) |
| Ingest to Kusto |[Output binding](./functions-bindings-kusto-output.md) |

::: zone pivot="programming-language-csharp"

## Install extension

The extension NuGet package you install depends on the C# mode you're using in your function app:

# [In-process](#tab/in-process)

Functions execute in the same process as the Functions host. To learn more, see [Develop C# class library functions using Azure Functions](functions-dotnet-class-library.md).

Add the extension to your project by installing this [NuGet package](https://www.nuget.org/packages/Microsoft.Azure.WebJobs.Extensions.Kusto).

```bash
dotnet add package Microsoft.Azure.WebJobs.Extensions.Kusto --prerelease
```

# [Isolated process](#tab/isolated-process)

Functions execute in an isolated C# worker process. To learn more, see [Guide for running C# Azure Functions in an isolated worker process](dotnet-isolated-process-guide.md).

Add the extension to your project by installing this [NuGet package](https://www.nuget.org/packages/Microsoft.Azure.Functions.Worker.Extensions.Kusto/).

```bash
dotnet add package Microsoft.Azure.Functions.Worker.Extensions.Kusto --prerelease
```

<!-- awaiting bundle support
# [C# script](#tab/csharp-script)

Functions run as C# script, which is supported primarily for C# portal editing. To update existing binding extensions for C# script apps running in the portal without having to republish your function app, see [Update your extensions].

You can install this version of the extension in your function app by registering the [extension bundle], version 4.x, or a later version.
-->

---

::: zone-end


::: zone pivot="programming-language-javascript, programming-language-powershell"


## Install bundle

Kusto bindings extension is part of a preview [extension bundle], which is specified in your host.json project file.


# [Preview Bundle v4.x](#tab/extensionv4)

You can add the preview extension bundle by adding or replacing the following code in your `host.json` file:

```json
{
  "version": "2.0",
  "extensionBundle": {
    "id": "Microsoft.Azure.Functions.ExtensionBundle.Preview",
    "version": "[4.*, 5.0.0)"
  }
}
```

# [Preview Bundle v3.x](#tab/extensionv3)

Kusto bindings for Azure Functions aren't available for the v3 version of the functions runtime.

---

::: zone-end


::: zone pivot="programming-language-python"

## Functions runtime

> [!NOTE]
> Python language support for the Kusto bindings extension is available starting with v4.6.0 of the [functions runtime](./set-runtime-version.md#view-and-update-the-current-runtime-version).  You may need to update your install of Azure Functions [Core Tools](functions-run-local.md) for local development.


## Install bundle

The Kusto bindings extension is part of a preview [extension bundle], which is specified in your host.json project file.

# [Preview Bundle v4.x](#tab/extensionv4)

You can add the preview extension bundle by adding or replacing the following code in your `host.json` file:

```json
{
  "version": "2.0",
  "extensionBundle": {
    "id": "Microsoft.Azure.Functions.ExtensionBundle.Preview",
    "version": "[4.*, 5.0.0)"
  }
}
```

# [Preview Bundle v3.x](#tab/extensionv3)

Kusto bindings for Azure Functions aren't available for the v3 version of the functions runtime.

---

::: zone-end


::: zone pivot="programming-language-java"


## Install bundle

Kusto bindings extension is part of a preview [extension bundle], which is specified in your host.json project file.

# [Preview Bundle v4.x](#tab/extensionv4)

You can add the preview extension bundle by adding or replacing the following code in your `host.json` file:

```json
{
  "version": "2.0",
  "extensionBundle": {
    "id": "Microsoft.Azure.Functions.ExtensionBundle.Preview",
    "version": "[4.*, 5.0.0)"
  }
}
```

# [Preview Bundle v3.x](#tab/extensionv3)

Kusto bindings for Azure Functions aren't available for the v3 version of the functions runtime.

---

## Update packages

Add the Java library for Kusto bindings to your functions project with an update to the `pom.xml` file in your Python Azure Functions project as seen in the following snippet:

```xml
<dependency>
    <groupId>com.microsoft.azure.functions</groupId>
    <artifactId>azure-functions-java-library-kusto</artifactId>
    <version>1.0.4-Preview</version>
</dependency>
```

::: zone-end

## Kusto connection string

Kusto bindings for Azure Functions have a required property for the connection string on all bindings. The connection string is documented at [Kusto connection strings](https://learn.microsoft.com/en-us/azure/data-explorer/kusto/api/connection-strings/kusto). 

## Considerations

- Kusto binding supports version 4.x and later of the Functions runtime.
- Source code for the Kusto bindings can be found in [this GitHub repository](https://github.com/Azure/Webjobs.Extensions.Kusto).
- This binding requires connectivity to Kusto. For input bindings, the user should have ingest privileges and for output bindings the user should have read/viewer privileges

## Next steps

- [Read data from a database (Input binding)](./functions-bindings-kusto-input.md)
- [Save data to a database (Output binding)](./functions-bindings-kusto-output.md)
- [Learn how to connect Azure Function to Kusto with managed identity](./functions-bindings-kusto-managed-identity.md)

[preview NuGet package]: https://www.nuget.org/packages/Microsoft.Azure.WebJobs.Extensions.Kusto
[maven coordinates]: https://mvnrepository.com/artifact/com.microsoft.azure.functions/azure-functions-java-library-kusto
[core tools]: ./functions-run-local.md
[extension bundle]: ./functions-bindings-register.md#extension-bundles
[Azure Tools extension]: https://marketplace.visualstudio.com/items?itemName=ms-vscode.vscode-node-azure-pack