#!/bin/bash
echo "Entering entry point"
cd /src/
ExtensionBundlePath=$(func GetExtensionBundlePath)
unzip -o /src/Microsoft.Azure.Functions.ExtensionBundle.zip -d $ExtensionBundlePath
func extensions sync
func start --no-build --node --verbose --port 7103