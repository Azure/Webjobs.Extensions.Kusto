#!/bin/bash
# Takes 3 parameters the path to navigate to , the language and the port to run the func tools on
while getopts l:p: flag
do
    case "${flag}" in
        l) language=${OPTARG};;
        p) port=${OPTARG};;
    esac
done
echo "Language: $language"
echo "Port: $port"
cd /src/samples-$language
func start --no-build --$language --verbose --port $port >> func-logs.txt &