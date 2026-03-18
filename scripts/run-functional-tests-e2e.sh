#!/bin/bash
set -euo pipefail

# Wrapper script to build the E2E test Docker image and configure local settings.
# Usage: ./run-functional-tests-e2e.sh [KustoConnectionString]
#   KustoConnectionString is built from CLUSTER and DATABASE env vars if set,
#   otherwise falls back to the first positional parameter.

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"
cd "$REPO_ROOT"

# Build KustoConnectionString from CLUSTER/DATABASE env vars, or fall back to $1
if [ -n "${CLUSTER:-}" ] && [ -n "${DATABASE:-}" ]; then
    KUSTO_CONNECTION_STRING="Data Source=${CLUSTER};Database=${DATABASE};Fed=True"
    echo "==> Using KustoConnectionString from CLUSTER and DATABASE env vars"
elif [ $# -ge 1 ]; then
    KUSTO_CONNECTION_STRING="$1"
    echo "==> Using KustoConnectionString from parameter"
else
    echo "Error: Either set CLUSTER and DATABASE env vars, or pass KustoConnectionString as \$1"
    echo "Usage: $0 [KustoConnectionString]"
    echo "  CLUSTER   - Kusto cluster URL (e.g. https://mycluster.kusto.windows.net)"
    echo "  DATABASE  - Kusto database name"
    exit 1
fi

# Source access token from az CLI if not already set
if [ -z "${ACCESS_TOKEN:-}" ]; then
    echo "==> Fetching access token from az CLI..."
    ACCESS_TOKEN="$(az account get-access-token --resource https://kusto.kusto.windows.net --query accessToken -o tsv)"
fi

# Map sample directories to their FUNCTIONS_WORKER_RUNTIME values
declare -A LANG_MAP=(
    ["samples-csharp"]="dotnet"
    ["samples-java"]="java"
    ["samples-node"]="node"
    ["samples-outofproc"]="dotnet-isolated"
    ["samples-powershell"]="powershell"
    ["samples-python"]="python"
)

echo "=== Step 1: Building Docker image ==="
"$SCRIPT_DIR/build-docker-image.sh"

echo "=== Step 2: Generating local.settings.json for each language sample ==="

CONN_STRING_WITH_TOKEN="${KUSTO_CONNECTION_STRING};AAD Federated Security=True;UserToken=${ACCESS_TOKEN}"

for sample_dir in "${!LANG_MAP[@]}"; do
    runtime="${LANG_MAP[$sample_dir]}"
    target_dir="samples/${sample_dir}"
    target="${target_dir}/local.settings.json"

    if [ ! -d "$target_dir" ]; then
        echo "Warning: ${target_dir} does not exist, skipping"
        continue
    fi

    echo "Creating ${target} with FUNCTIONS_WORKER_RUNTIME=${runtime}"

    # Use jq if available for safe JSON manipulation, otherwise fall back to sed
    if command -v jq &>/dev/null; then
        jq \
            --arg runtime "$runtime" \
            --arg connStr "$CONN_STRING_WITH_TOKEN" \
            '.Values.FUNCTIONS_WORKER_RUNTIME = $runtime | .Values.KustoConnectionString = $connStr' \
            local.settings.json.example > "${target}"
    else
        sed \
            -e "s/<lang>/${runtime}/" \
            -e "s|<KustoConnectionString>|${CONN_STRING_WITH_TOKEN//|/\\|}|" \
            local.settings.json.example > "${target}"
    fi
done

echo "=== Step 3: Running functional tests ==="
cd "$REPO_ROOT/functions-int-tests"
mvn clean gatling:test
