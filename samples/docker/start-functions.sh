#!/bin/bash
# Starts Azure Functions for a given language sample
# Usage: start-functions.sh -l <language> -p <port>

while getopts l:p: flag; do
    case "${flag}" in
        l) language=${OPTARG} ;;
        p) port=${OPTARG} ;;
    esac
done

if [ -z "${language:-}" ] || [ -z "${port:-}" ]; then
    echo "Usage: start-functions.sh -l <language> -p <port>"
    exit 1
fi

echo "Using language: $language & Port: $port"
echo "Running $language functions samples"

SAMPLES_DIR="/src/samples-${language}"
if [ ! -d "$SAMPLES_DIR" ]; then
    echo "Error: Samples directory $SAMPLES_DIR not found"
    exit 1
fi

cd "$SAMPLES_DIR"

case "$language" in
    outofproc)
        echo "Starting out-of-process dotnet-isolated worker"
        cd bin/Debug/net8.0/publish
        FUNCTIONS_WORKER_RUNTIME=dotnet-isolated func start --dotnet-isolated --no-build --verbose --port "$port" >> func-logs.txt &
        ;;
    csharp)
        echo "Starting C# in-process worker"
        cd bin/Debug/net8.0
        FUNCTIONS_WORKER_RUNTIME=dotnet func start --csharp --verbose --port "$port" >> func-logs.txt &
        ;;
    node)
        echo "Starting Node.js (JavaScript) worker"
        FUNCTIONS_WORKER_RUNTIME=node func start --javascript --verbose --port "$port" >> func-logs.txt &
        ;;
    java)
        echo "Starting Java worker"
        cd target/azure-functions/kustojavafunctionssample-20230130111810292
        FUNCTIONS_WORKER_RUNTIME=java func start --java --verbose --port "$port" >> func-logs.txt &
        ;;
    python)
        echo "Starting Python worker"
        FUNCTIONS_WORKER_RUNTIME=python PYTHONPATH="$SAMPLES_DIR" func start --python --verbose --port "$port" >> func-logs.txt &
        ;;
    powershell)
        echo "Starting PowerShell worker"
        FUNCTIONS_WORKER_RUNTIME=powershell func start --powershell --verbose --port "$port" >> func-logs.txt &
        ;;
    *)
        echo "Error: Unsupported language '$language'"
        exit 1
        ;;
esac