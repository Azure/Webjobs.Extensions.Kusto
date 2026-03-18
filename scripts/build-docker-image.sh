#!/bin/bash
set -euo pipefail

# Linux equivalent of BuildE2ETestImage.ps1
# Re-builds base Docker images for Azure Functions E2E testing.
#
# Usage:
#   ./build-docker-image.sh
#   ./build-docker-image.sh --acr myacr.azurecr.io
#   ./build-docker-image.sh --acr myacr.azurecr.io --push
#   ./build-docker-image.sh --extension-bundle /path/to/bundle.zip

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"
cd "$REPO_ROOT"

EXTENSION_BUNDLE_PATH=""
ACR=""
DOCKER_PUSH=false

while [[ $# -gt 0 ]]; do
    case "$1" in
        --extension-bundle) EXTENSION_BUNDLE_PATH="$2"; shift 2 ;;
        --acr)              ACR="$2"; shift 2 ;;
        --push)             DOCKER_PUSH=true; shift ;;
        *)                  echo "Unknown option: $1"; exit 1 ;;
    esac
done

BASE_IMAGE_TAG="node:4-node22"
TARGET_IMAGE_NAME="func-az-kusto-base"
BUILD_DATE=$(date +"%Y%m%d")
TARGET_FILE_LOCATION="./samples/docker"
TARGET_DOCKER_FILE="${TARGET_FILE_LOCATION}/DockerFile"

echo "------------------------------------------------------------------------------------------------------------------------------"
echo "Using base image mcr.microsoft.com/azure-functions/${BASE_IMAGE_TAG}"
echo "------------------------------------------------------------------------------------------------------------------------------"
echo "Cleaning and building Project"
dotnet clean
dotnet publish src/Microsoft.Azure.WebJobs.Extensions.Kusto.csproj -c Release -o src/bin/Release/publish -p:RunTests=false
echo "------------------------------------------------------------------------------------------------------------------------------"

# Generate DockerFile from template
if [ -z "$EXTENSION_BUNDLE_PATH" ]; then
    echo "Creating docker file to build with tag '${TARGET_IMAGE_NAME}:${BUILD_DATE}'"
    sed \
        -e "s|imagename|mcr.microsoft.com/azure-functions/${BASE_IMAGE_TAG}|g" \
        -e "s|bundlepath|${EXTENSION_BUNDLE_PATH}|g" \
        "${TARGET_FILE_LOCATION}/Docker-template.dockerfile" > "$TARGET_DOCKER_FILE"
else
    ESCAPED_PATH=$(realpath --relative-to=. "$EXTENSION_BUNDLE_PATH")
    cp "$ESCAPED_PATH" "${TARGET_FILE_LOCATION}/Microsoft.Azure.Functions.ExtensionBundle.zip"
    echo "Creating docker file to build with tag '${TARGET_IMAGE_NAME}:${BUILD_DATE}' with extension bundle copy on path ${ESCAPED_PATH}"
    sed \
        -e "s|imagename|mcr.microsoft.com/azure-functions/${BASE_IMAGE_TAG}|g" \
        -e "s|bundlepath|${ESCAPED_PATH}|g" \
        -e "s|#COPY|COPY ${TARGET_FILE_LOCATION}/Microsoft.Azure.Functions.ExtensionBundle.zip |g" \
        "${TARGET_FILE_LOCATION}/Docker-template.dockerfile" > "$TARGET_DOCKER_FILE"
fi

# Build Docker image
IMAGE_PREFIX="${ACR:+${ACR}/}"
TAG_CREATED="${IMAGE_PREFIX}${TARGET_IMAGE_NAME}:${BUILD_DATE}"
LATEST_TAG="${IMAGE_PREFIX}${TARGET_IMAGE_NAME}:latest"
echo "Creating docker tags ${TAG_CREATED} and ${LATEST_TAG}"
if docker build  -t "$TAG_CREATED" -t "$LATEST_TAG" -f "$TARGET_DOCKER_FILE" .; then
    echo "Image build '${TARGET_IMAGE_NAME}:${BUILD_DATE}' complete"
    if [ -n "$ACR" ] && [ "$DOCKER_PUSH" = true ]; then
        docker image push --all-tags "${ACR}/${TARGET_IMAGE_NAME}"
    fi
    for compose_file in "${TARGET_FILE_LOCATION}"/docker-compose*.yml; do
        [ -f "$compose_file" ] && sed -i "s|image:.*func-az-kusto-base.*|image: ${LATEST_TAG}|" "$compose_file" \
            && echo "Updated $(basename "$compose_file") to use image ${LATEST_TAG}"
    done
else
    echo "Image build failed. Is docker running?" >&2
fi

rm -f "${TARGET_FILE_LOCATION}/Microsoft.Azure.Functions.ExtensionBundle.zip"
rm -f "$TARGET_DOCKER_FILE"
