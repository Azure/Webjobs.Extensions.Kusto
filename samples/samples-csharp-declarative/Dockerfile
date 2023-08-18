# This image additionally contains function core tools – useful when using custom extensions
FROM mcr.microsoft.com/azure-functions/node:4-node16-core-tools AS installer-env
# The host file for all the defaults

ARG USER_HOME_DIR="/root"

RUN apt-get -qq update \
    && apt-get -qqy install curl \
    && apt-get install -y libfreetype6 fontconfig fonts-dejavu \
    && rm -rf /var/lib/apt/lists/*

#Python installation complete
COPY ../docker/host.json /src/host.json
# Copy the DLL to the target use this for the tests
COPY ./src/bin/Release/netstandard2.1/Microsoft.Azure.WebJobs.Extensions.Kusto.dll /src/Microsoft.Azure.WebJobs.Extensions.Kusto.dll
COPY ./../docker/init-functions.sh /src/init-functions.sh
COPY ./../docker/start-functions.sh /src/start-functions.sh
COPY ./../docker/Microsoft.Azure.Functions.ExtensionBundle.zip   /src/Microsoft.Azure.Functions.ExtensionBundle.zip
# So that the latest lib is resolved
ENTRYPOINT ["tail", "-f", "/dev/null"]
