#!/bin/bash
set -euo pipefail

# Wrapper script to build the E2E test Docker image and configure local settings.
# Usage: ./run-functional-tests-e2e.sh <Cluster> <Database> [Language]
#
#   Cluster and Database can also be set via CLUSTER and DATABASE env vars.
#   Language selects which sample to test (must be a LANG_MAP key, e.g.
#   samples-csharp, samples-java, etc.). Can also be set via SAMPLE_LANG env var.
#   Defaults to samples-node.

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"
cd "$REPO_ROOT"

# Build KustoConnectionString from positional args or CLUSTER/DATABASE env vars.
# Positional args take precedence over env vars.
LANG_ARG=""
if [ $# -ge 2 ]; then
    CLUSTER="$1"
    DATABASE="$2"
    LANG_ARG="${3:-}"
    echo "==> Using Cluster and Database from positional parameters"
elif [ -n "${CLUSTER:-}" ] && [ -n "${DATABASE:-}" ]; then
    LANG_ARG="${1:-}"
    echo "==> Using Cluster and Database from env vars"
else
    echo "Error: Provide Cluster and Database as positional args, or set CLUSTER and DATABASE env vars"
    echo "Usage: $0 <Cluster> <Database> [Language]"
    echo "  Cluster   - Kusto cluster URL (e.g. https://mycluster.kusto.windows.net)"
    echo "  Database  - Kusto database name"
    echo "  Language  - LANG_MAP key (e.g. samples-csharp, samples-java). Default: samples-node"
    exit 1
fi

KUSTO_CONNECTION_STRING="Data Source=${CLUSTER};Database=${DATABASE};Fed=True"

# Source access token from az CLI if not already set
if [ -z "${ACCESS_TOKEN:-}" ]; then
    echo "==> Fetching access token from az CLI..."
    ACCESS_TOKEN="$(az account get-access-token --resource https://kusto.kusto.windows.net --query accessToken -o tsv)"
fi

# Map sample directories to their FUNCTIONS_WORKER_RUNTIME values
declare -A LANG_MAP=(
    ["samples-csharp"]="csharp"
    ["samples-java"]="java"
    ["samples-node"]="node"
    ["samples-outofproc"]="dotnet-isolated"
    ["samples-powershell"]="powershell"
    ["samples-python"]="python"
)

FUNC_PORT=7071

# Resolve language: positional arg > SAMPLE_LANG env var > default (samples-node)
if [ -n "$LANG_ARG" ]; then
    SAMPLE_LANG="$LANG_ARG"
else
    SAMPLE_LANG="${SAMPLE_LANG:-samples-node}"
fi

# Validate SAMPLE_LANG against LANG_MAP
if [ -z "${LANG_MAP[$SAMPLE_LANG]+_}" ]; then
    echo "Error: '${SAMPLE_LANG}' is not a valid language key."
    echo "Valid keys: ${!LANG_MAP[*]}"
    exit 1
fi
echo "==> Running tests for language: ${SAMPLE_LANG} (runtime: ${LANG_MAP[$SAMPLE_LANG]})"

runtime="${LANG_MAP[$SAMPLE_LANG]}"

echo "=== Step 1: Building Docker image ==="
"$SCRIPT_DIR/build-docker-image.sh"

echo "=== Step 2: Generating local.settings.json for ${SAMPLE_LANG} ==="

CONN_STRING_WITH_TOKEN="${KUSTO_CONNECTION_STRING};AAD Federated Security=True;UserToken=${ACCESS_TOKEN}"

target_dir="samples/${SAMPLE_LANG}"
target="${target_dir}/local.settings.json"

if [ ! -d "$target_dir" ]; then
    echo "Error: ${target_dir} does not exist"
    exit 1
fi

rm -f "${target}"
echo "Creating ${target} with FUNCTIONS_WORKER_RUNTIME=${runtime}"

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

if [ "$SAMPLE_LANG" = "samples-outofproc" ]; then
    echo "=== Step 3: Building ${SAMPLE_LANG} ==="
    dotnet publish "${target_dir}" -c Debug -o "${target_dir}/bin/Debug/net8.0/publish"
    cp "${target}" "${target_dir}/bin/Debug/net8.0/publish/local.settings.json"
fi

echo "=== Step 4: Running functional tests ==="
cd "$REPO_ROOT/functions-int-tests"
test_language="${SAMPLE_LANG#samples-}"
mvn clean gatling:test -Dlanguage="${test_language}" -Dport="${FUNC_PORT}"
