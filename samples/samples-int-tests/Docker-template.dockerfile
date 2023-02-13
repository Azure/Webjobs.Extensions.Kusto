# This image additionally contains function core tools – useful when using custom extensions
FROM $$imagename$$ AS installer-env
#FROM bitnami/node:16 AS installer-env
# Copy the DLL to the target use this for the tests
COPY ./src/bin/Release/netstandard2.1/Microsoft.Azure.WebJobs.Extensions.Kusto.dll /src/Microsoft.Azure.WebJobs.Extensions.Kusto.dll
COPY samples/start-functions.sh /src/start-functions.sh
COPY $$bundlepath$$ /src/Microsoft.Azure.Functions.ExtensionBundle.zip
# So that the latest lib is resolved
# RUN bash /src/install-bundle.sh
ENTRYPOINT [ "/src/start-functions.sh" ] 