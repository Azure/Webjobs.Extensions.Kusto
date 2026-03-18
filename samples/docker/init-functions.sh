#!/bin/bash
echo "Entering entry point"
cd /src/

ExtensionBundlePath=$(func GetExtensionBundlePath)
BundlePath="/src/Microsoft.Azure.Functions.ExtensionBundle.zip"
if [ -f "$BundlePath" ]; then
    unzip -o "$BundlePath" -d "$ExtensionBundlePath"
fi

cp /src/Microsoft.Azure.WebJobs.Extensions.Kusto.dll $ExtensionBundlePath/bin/Microsoft.Azure.WebJobs.Extensions.Kusto.dll
# Register the Kusto extension in extensions.json
EXTENSIONS_JSON="$BUNDLE_BIN/extensions.json"
KUSTO_TYPE="Microsoft.Azure.WebJobs.Kusto.KustoBindingStartup, Microsoft.Azure.WebJobs.Extensions.Kusto, Version=1.1.0.0, Culture=neutral, PublicKeyToken=7e376be80b905e82"

if [ -f "$EXTENSIONS_JSON" ]; then
    if ! grep -q "KustoBindingStartup" "$EXTENSIONS_JSON"; then
        echo "Registering Kusto extension in extensions.json"
        # Find the max existing name number and add 1
        MAX_NUM=$(grep -oP '"name":\s*"Startup\K\d+' "$EXTENSIONS_JSON" | sort -n | tail -1)
        NEXT_NUM=$(( ${MAX_NUM:-0} + 1 ))
        # Insert new extension entry before the closing array bracket
        sed -i "s|}\s*]|},\n    { \"name\": \"Startup${NEXT_NUM}\", \"typeName\": \"${KUSTO_TYPE}\" }\n  ]|" "$EXTENSIONS_JSON"
    else
        echo "Kusto extension already registered in extensions.json"
    fi
else
    echo "Creating extensions.json with Kusto extension"
    cat > "$EXTENSIONS_JSON" << EJSON
{
  "extensions": [
    { "name": "Startup", "typeName": "${KUSTO_TYPE}" }
  ]
}
EJSON
fi

echo "Extension bundle path: $ExtensionBundlePath"
echo "Init complete"