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
echo "Running $language functions samples"
cd /src/samples-$language
if [ $language == "outofproc" ]; then
  echo "Changing language to c-sharp for out of process worker"
  cd  bin/Debug/net6.0
  func start --csharp --verbose --port $port >> func-logs.txt &
else
  # the compiled functions are in this location
  if [[ $language == "java" ]]; then
    echo "Changing to Java functions directory"
    cd target/azure-functions/kustojavafunctionssample-20230130111810292
  fi 
  func start --$language --verbose --port $port >> func-logs.txt &
fi