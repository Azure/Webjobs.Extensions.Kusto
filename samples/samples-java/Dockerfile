ARG JAVA_VERSION=8
# This image additionally contains function core tools – useful when using custom extensions
FROM mcr.microsoft.com/azure-functions/java:4-java$JAVA_VERSION-core-tools AS installer-env
# Copy the DLL
COPY ./src/bin/Release/netstandard2.1/Microsoft.Azure.WebJobs.Extensions.Kusto.dll /src/Microsoft.Azure.WebJobs.Extensions.Kusto.dll
COPY samples/install-bundle.sh /src/install-bundle.sh
# Copy the java library
COPY java-library /src/java-library
COPY samples/samples-java /src/samples-java
# So that the latest lib is resolved
RUN cd /src/java-library && mvn clean install
RUN cd /src/samples-java
RUN func extensions sync
RUN bash /src/install-bundle.sh
RUN mvn clean package azure-functions:run