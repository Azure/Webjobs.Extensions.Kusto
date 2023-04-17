# Testing functions bindings using E2E tests

This readme explains the process of testing function bindings using E2E tests. Azure functions bindings are by default 
declarative and makes usage of this easy for developing applications. However, it is important to test the bindings to 
validate the application behavior.

This utilizes [TestContainers](https://www.testcontainers.org/) to run the bindings in a containerized environment.

## Setting up the tests

To run the tests, you need to have Docker installed on your machine. Azure function base images to test the bindings are
published on [DockerHub](https://hub.docker.com/_/microsoft-azure-functions) and on [Github](https://github.com/Azure/azure-functions-docker). 
For ease of running the tests the best option would be to use the base tools image corresponding to the platform. For example to
run the tests for .NET 6.0, the image `4-dotnet6-core-tools` can be used.
The test examples in this repo using Java 11 for tests. This can be easily changed to any other languages by using TestContainers for
the language of choice.

The tests have the following high level steps, this assumes that the function app is already created and there are HTTP triggers configured : 

* Create a compose file using the base image to test with. If additional components are needed they can be orchestrated using a docker-compose file. 
* In the following example, we use the java image and add RabbitMQ (for trigger tests) and Azurite (for saving function state) to the container.

```yaml
version: '3'
services:
    baseimage:
        image: mcr.microsoft.com/azure-functions/java:4-dotnet6-core-tools
        hostname: func-az-kusto-base
        ports:
          - "7101:7101"
    rabbitmq:
      image: rabbitmq:3.11.9-management
      hostname: rabbitmq
      ports:
      - "7000:15672"
      - "7001:5672"
    azurite:
      image: mcr.microsoft.com/azure-storage/azurite
      hostname: azurite
      ports:
      - "10000:10000"
      - "10001:10001"
      - "10002:10002"
```
* The compose environment is instantiated referencing the compose file and then started.
``          
    DockerEnvironment environment = new DockerComposeContainer<>(new File(path));
    environment.start();
``

* Copy the function app to the container or use a volume mount for mounting the function app
```
    containerState.copyFileToContainer(MountableFile.forHostPath(pathToFunctionApp),String.format("/src/samples-%s/", functionAppName));
```

* Run the function app in the container. Use the function core tools to run the app
```
    containerState.execInContainer("func", "start", "--port", "7101", "--java" "--verbose");
```

* Since the ports are forwarded from localhost, the tests can be run against the function app using the localhost url.
```
    String url = String.format("http://localhost:%d/api/%s", port, functionName);
```

* The values inserted or retrieved can then be asserted against the expected values ( by selecting data from Kusto and validating the results)

## Running the tests

In this example , the tests are set up for java and can be run through the maven lifecycle. The tests can be run using the following command

```
mvn clean test
```

## Running the tests for performance tests

This folder contains the performance tests for the bindings. The tests are run using [Gatling](https://gatling.io/). 
The tests are run using the following command. The setup is exactly as described for the E2E tests. The only difference is that the test
runs use the gatling framework for applying load to the function app and validating the results.

```bash
 mvn clean formatter:format gatling:test "-Dport=7105" "-Dlanguage=csharp" "-DrunDescription=.NETFunctions-StressTests" "-DrunTrigger=false"
```

## Building a custom image

This folder contains steps to build a custom Docker image that can be catered to run against all language bindings. If this is the case that a
custom image is needed the following steps can be followed. This assumes that you already have a container registry where the image can be pushed.

```bash
. .\BuildE2ETestImage.ps1
BuildE2ETestImage -Acr <acr/container-registry> -DockerPush $true
```