#!/bin/bash
ExtensionBundlePath=$(func GetExtensionBundlePath)
cp /src/Microsoft.Azure.WebJobs.Extensions.Kusto.dll $ExtensionBundlePath/bin/Microsoft.Azure.WebJobs.Extensions.Kusto.dll
cp /src/Microsoft.Azure.WebJobs.Extensions.Kusto.dll $ExtensionBundlePath/StaticContent/bin/Microsoft.Azure.WebJobs.Extensions.Kusto.dll
