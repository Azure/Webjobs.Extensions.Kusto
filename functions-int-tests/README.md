# Testing functions bindings using E2E tests

This readme explains the process of testing function bindings using E2E tests. Azure functions bindings are by default
declarative and makes usage of this easy for developing applications. However, it is important to test the bindings to
validate the application behavior.

This utilizes [TestContainers](https://www.testcontainers.org/) to run the bindings in a containerized environment.

## Prerequisites

- **Docker** installed and running
- **Java 11+** (for running the test harness)
- **Maven 3.8+**
- **.NET SDK** (for building the extension)
- **Azure CLI** (`az`) for obtaining access tokens
- A Kusto (Azure Data Explorer) cluster and database, set up using the KQL script at [`samples/set-up/KQL-Setup.kql`](../samples/set-up/KQL-Setup.kql)

## Scripts

All automation scripts are in the [`scripts/`](../scripts/) directory:

| Script | Description |
|---|---|
| `build-docker-image.sh` | Builds the Docker image with the Kusto extension (Linux) |
| `BuildE2ETestImage.ps1` | Builds the Docker image with the Kusto extension (PowerShell) |
| `run-e2e-tests.sh` / `.ps1` | Sets up environment for performance tests |
| `run-functional-tests-e2e.sh` / `.ps1` | Full pipeline: build image → generate settings → run tests |

## How the tests work

The tests use a custom Docker image built on top of the [Azure Functions base image](https://hub.docker.com/_/microsoft-azure-functions)
(`node:4-node20-core-tools`). The image includes Maven, Java, Python and all runtimes needed to test across languages.

### High-level flow

1. **Build the Docker image** — compiles the Kusto extension, generates a `DockerFile` from
   `samples/docker/Docker-template.dockerfile`, and builds the image with `docker build --no-cache`.
2. **Generate `local.settings.json`** for each language sample with the Kusto connection string and correct
   `FUNCTIONS_WORKER_RUNTIME` value.
3. **Start containers** via docker-compose (function host + Azurite + optional RabbitMQ).
4. **Copy sample function apps** into the container and run them with `func start`.
5. **Execute tests** against the function HTTP endpoints from the host via forwarded ports.
6. **Assert results** by querying Kusto to validate data written/read by the bindings.

### Docker compose

The compose file orchestrates the function host alongside supporting services:

```yaml
services:
  baseimage:
    image: func-az-kusto-base:latest
    hostname: func-az-kusto-base
    ports:
      - "7101:7101"
  azurite:
    image: mcr.microsoft.com/azure-storage/azurite
    hostname: azurite
    ports:
      - "10000:10000"
      - "10001:10001"
      - "10002:10002"
```

### Container initialisation

Inside the container, two scripts handle setup:

- **`init-functions.sh`** — copies the Kusto extension DLL into the extension bundle and registers it in `extensions.json`.
- **`start-functions.sh`** — starts the function app for a given language (`-l node -p 7101`). Maps `node` to `--javascript` for the core tools.

## Running the E2E tests

### Option 1: Full automated pipeline

```bash
# From the repository root
scripts/run-functional-tests-e2e.sh <CLUSTER> <DATABASE>
```

This builds the image, generates `local.settings.json` for every language sample, and runs `mvn clean gatling:test`.

The `CLUSTER` and `DATABASE` parameters are required and identify the Kusto cluster and database to test against.
An access token is obtained automatically via `az account get-access-token`, or you can set the `ACCESS_TOKEN`
environment variable beforehand.

### Option 2: Step by step

```bash
# 1. Build the Docker image
scripts/build-docker-image.sh

# 2. Run the Java test harness directly
cd functions-int-tests
mvn clean test
```

### Option 3: PowerShell

```powershell
# Full pipeline
scripts/run-functional-tests-e2e.ps1 -Cluster <CLUSTER> -Database <DATABASE>

# Build image only
. scripts/BuildE2ETestImage.ps1
BuildE2ETestImage -Acr <acr> -DockerPush $true
```

## Performance / stress tests

The performance tests use [Gatling](https://gatling.io/) with the same containerized setup. They apply load to the
function app and validate results under stress.

```bash
cd functions-int-tests
mvn clean formatter:format gatling:test \
  "-Dport=7105" \
  "-Dlanguage=csharp" \
  "-DrunDescription=.NETFunctions-StressTests" \
  "-DrunTrigger=false"
```

## Building a custom image

To build and optionally push to a container registry:

```bash
# Linux
scripts/build-docker-image.sh --acr myacr.azurecr.io --push

# PowerShell
. scripts/BuildE2ETestImage.ps1
BuildE2ETestImage -Acr myacr.azurecr.io -DockerPush $true
```

The build script generates a `DockerFile` from `samples/docker/Docker-template.dockerfile`, builds with `--no-cache`,
and tags as both `func-az-kusto-base:<date>` and `func-az-kusto-base:latest`.