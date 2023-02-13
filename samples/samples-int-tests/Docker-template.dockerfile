# This image additionally contains function core tools – useful when using custom extensions
FROM imagename AS installer-env
# The host file for all the defaults
COPY ./samples/samples-int-tests/host.json /src/host.json
# Copy the DLL to the target use this for the tests
COPY ./src/bin/Release/netstandard2.1/Microsoft.Azure.WebJobs.Extensions.Kusto.dll /src/Microsoft.Azure.WebJobs.Extensions.Kusto.dll
COPY ./samples/samples-int-tests/start-functions.sh /src/start-functions.sh
#COPY ./samples/samples-int-tests/Microsoft.Azure.Functions.ExtensionBundle.zip /src/Microsoft.Azure.Functions.ExtensionBundle.zip
# So that the latest lib is resolved
ENTRYPOINT [ "/src/start-functions.sh" ] 