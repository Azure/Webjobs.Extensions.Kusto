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
echo "Running $language functions samples"
# the compiled functions are in this location
if [[ $language  -eq  "java" ]]
then
  echo "Changing to Java functions directory"
  cd target/azure-functions/kustojavafunctionssample-20230130111810292
fi
func start --$language --verbose --port $port >> func-logs.txt &