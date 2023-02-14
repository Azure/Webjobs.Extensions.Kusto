#!/bin/bash
# Takes 3 parameters the path to navigate to , the language and the port to run the func tools on
while getopts l:p: flag
do
    case "${flag}" in
        l) language=${OPTARG};;
        p) port=${OPTARG};;
    esac
done
echo "Using language: $language & Port: $port"
cd /src/samples-$language
if [[ $language  -eq  "node" ]]
then
  echo "Running node functions samples"
  func start --no-build --$language --verbose --port $port >> func-logs.txt &
elif [[ $language  -eq  "node" ]]
then
  echo "Running JAVA functions samples"
  mvn clean package azure-functions:package
  mvn azure-functions:run >> func-logs.txt &
else
  echo "You are not welcome here."
  exit 1;
fi