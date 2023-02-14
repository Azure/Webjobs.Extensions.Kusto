#!/bin/bash
echo "Entering entry point"
cd /src/
ExtensionBundlePath=$(func GetExtensionBundlePath)
BundlePath="/src/Microsoft.Azure.Functions.ExtensionBundle.zip"
if [ -f "$BundlePath" ]; then
    unzip -o  $BundlePath -d $ExtensionBundlePath
fi
cp /src/Microsoft.Azure.WebJobs.Extensions.Kusto.dll $ExtensionBundlePath/bin/Microsoft.Azure.WebJobs.Extensions.Kusto.dll