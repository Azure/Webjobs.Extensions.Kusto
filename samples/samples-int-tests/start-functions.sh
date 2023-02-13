#!/bin/bash
echo "Entering entry point"
cd /src/
ExtensionBundlePath=$(func GetExtensionBundlePath)
unzip -o /src/Microsoft.Azure.Functions.ExtensionBundle.zip -d $ExtensionBundlePath
cp /src/Microsoft.Azure.WebJobs.Extensions.Kusto.dll $ExtensionBundlePath/bin/Microsoft.Azure.WebJobs.Extensions.Kusto.dll