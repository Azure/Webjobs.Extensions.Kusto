package com.microsoft.azure.kusto.functions;

import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.testcontainers.containers.Container;
import org.testcontainers.containers.ContainerState;
import org.testcontainers.containers.DockerComposeContainer;
import org.testcontainers.utility.MountableFile;

import static io.gatling.javaapi.core.CoreDsl.*;
import static io.gatling.javaapi.http.HttpDsl.http;
import static io.gatling.javaapi.http.HttpDsl.status;
import static java.lang.System.getProperty;

import io.gatling.javaapi.core.ChainBuilder;
import io.gatling.javaapi.core.ScenarioBuilder;
import io.gatling.javaapi.core.Simulation;
import io.gatling.javaapi.http.HttpProtocolBuilder;
import java.io.File;
import java.io.IOException;
import java.time.Duration;
import java.time.Instant;
import java.time.temporal.ChronoUnit;
import java.util.Collections;
import java.util.List;
import java.util.Map;
import java.util.stream.Collectors;
import java.util.stream.IntStream;
import java.util.stream.Stream;

import com.fasterxml.jackson.core.JsonProcessingException;
import com.fasterxml.jackson.databind.ObjectMapper;
import com.microsoft.azure.kusto.functions.common.Item;
import com.microsoft.azure.kusto.functions.common.Product;

public class FunctionsMultiLangTests extends Simulation {
    private static final Logger logger = LoggerFactory.getLogger(FunctionsMultiLangTests.class);
    // File name in docker compose file
    private static final String BASE_IMAGE = "baseimage";

    private static final int HOST_PORT = Integer.getInteger("port", 7103);
    private static final ObjectMapper JSON_MAPPER = new ObjectMapper();

    private static final String PATH_TO_DOCKER_COMPOSE = "../samples/docker/docker-compose.yml";

    private DockerComposeContainer<?> environment;

    private final Map<String, Integer> languagePortMap = Stream.of(
            new String[][] { { "outofproc", "7101" }, { "java", "7102" }, { "node", "7103" }, { "python", "7104" }, })
            .collect(Collectors.collectingAndThen(Collectors.toMap(data -> data[0], data -> Integer.parseInt(data[1])),
                    Collections::<String, Integer> unmodifiableMap));
    private String language = getProperty("language", "node");

    public FunctionsMultiLangTests() throws JsonProcessingException {

    }

    @Override
    public void before() {
        // Set up the tables and may be even clear them
        // Start with a randomly large number
        // Start the test container based on the language passed
        // Copy the project into the container
        // Replace the DLL file
        language = getProperty("language", "node");
        if (!languagePortMap.containsKey(language)) {
            logger.warn(
                    "Language " + language + " is not in the list of accepted languages for test. Accepted languages - "
                            + languagePortMap.keySet());
            System.exit(137);
        }
        int hostPort = languagePortMap.get(language);

        File absoluteFilePath = new File(PATH_TO_DOCKER_COMPOSE).getAbsoluteFile();
        try {
            String path = absoluteFilePath.getCanonicalPath();
            logger.info("Starting compose from file {}", path);
            environment = new DockerComposeContainer<>(new File(path));
            environment.start();
            environment.getContainerByServiceName(BASE_IMAGE)
                    .ifPresent(containerState -> runContainerCommands(language, hostPort, containerState));
        } catch (IOException e) {
            throw new RuntimeException(e);
        }
    }

    private static void runContainerCommands(String language, int exposedPort, ContainerState containerState) {
        try {
            // Goes to the samples folder
            String pathToSamplesDirectory = new File(PATH_TO_DOCKER_COMPOSE).getParentFile().getParentFile()
                    .getCanonicalPath();
            String pathToLanguageSample = String.format("%s%ssamples-%s", pathToSamplesDirectory, File.separator,
                    language);
            containerState.copyFileToContainer(MountableFile.forHostPath(pathToLanguageSample),
                    String.format("/src/samples-%s/", language));
            logger.info("Copied folder {} to container", pathToLanguageSample);
            // Since the file is copied now move over
            Container.ExecResult initFunctionsResult = containerState.execInContainer("bash", "/src/init-functions.sh");
            logger.debug("Init function for language binding {} returned {}.StdErr {} and StdOut {}", language,
                    initFunctionsResult.getExitCode(), initFunctionsResult.getStderr(),
                    initFunctionsResult.getStdout());
            // Once in the folder start the function tools after navigating to the folder
            // Since the file is copied now move over
            Container.ExecResult startFunctionsResult = containerState.execInContainer("bash",
                    "/src/start-functions.sh", "-l", language, "-p", String.valueOf(exposedPort));
            logger.info("Starting function on port {} for language binding {} returned {}. StdErr {} and StdOut {}",
                    exposedPort, language, startFunctionsResult.getExitCode(), startFunctionsResult.getStderr(),
                    startFunctionsResult.getStdout());
        } catch (IOException | InterruptedException e) {
            throw new RuntimeException(e);
        }
    }

    // Start func inside the container
    // Run the tests
    String baseUrl = String.format("http://localhost:%d/api", HOST_PORT);
    long seconds = Instant.now().toEpochMilli();
    List<Product> addProductsArray = IntStream
            .range(1, 10).mapToObj(count -> new Product(seconds - count,
                    String.format("Product-%s-%d", language, seconds - count), (seconds - count) / 1000999.999))
            .collect(Collectors.toList());
    long itemId = seconds + 10;
    Item addItemWithMapping = new Item(itemId, String.format("Item-%s-%d", language, itemId), itemId / 1000999.999);

    ChainBuilder inputAndOutputBindings =
            // let's give proper names, as they are displayed in the reports
            exec(http("AddProduct").post("/addproduct")
                    .body(StringBody(JSON_MAPPER.writeValueAsString(addProductsArray))).check(status().in(200, 201)))
                            .pause(5)
                            .exec(http("AddProductWithMapping").post("/addproductswithmapping")
                                    .body(StringBody(JSON_MAPPER.writeValueAsString(addItemWithMapping)))
                                    .check(status().in(200, 201)))
                            .pause(5)
                            .exec(http("GetProducts").get("/getproducts/" + itemId).check(status().in(200, 201),
                                    jsonPath("$[*].ProductID").ofLong().find().is(itemId),
                                    jsonPath("$[*].Name").ofString().find()
                                            .is(String.format("Item-%s-%d", language, itemId)),
                                    jsonPath("$[*].Cost").ofDouble().find().shouldBe(itemId / 1000999.999)))
                            .exitHereIfFailed();

    HttpProtocolBuilder httpProtocol = http.baseUrl(baseUrl).acceptHeader("application/json");
    // Open systems, where you control the arrival rate of users
    ScenarioBuilder inputAndOutputBindingOpenScenario = scenario("BasicInputAndOutputBindings-Open")
            .exec(inputAndOutputBindings);
    {
        /*
         * setUp(inputAndOutputBindingOpenScenario.injectOpen( nothingFor(Duration.of(10,ChronoUnit.SECONDS)), // warm
         * up and functions start time incrementUsersPerSec(5) .times(10)
         * .eachLevelLasting(Duration.of(10,ChronoUnit.SECONDS))
         * .separatedByRampsLasting(Duration.of(10,ChronoUnit.SECONDS)) .startingFrom(10)
         * ).protocols(httpProtocol)).assertions(global().successfulRequests().percent().is(100.0));
         */
        setUp(inputAndOutputBindingOpenScenario.injectOpen(nothingFor(Duration.of(10, ChronoUnit.SECONDS)),
                rampUsers(50).during(25))).protocols(httpProtocol)
                        .assertions(global().successfulRequests().percent().gt(90.0));

    }

    @Override
    public void after() {
        try {
            Thread.sleep(150000);
        } catch (Exception ignored) {

        }
        String containerPath;
        if ("java".equalsIgnoreCase(language)) {
            containerPath = String.format(
                    "/src/samples-%s/target/azure-functions/kustojavafunctionssample-20230130111810292/func-logs.txt",
                    language);
        } else if ("outofproc".equalsIgnoreCase(language)) {
            containerPath = String.format("/src/samples-%s/bin/Debug/net6.0/func-logs.txt", language);
        } else {
            containerPath = String.format("/src/samples-%s/func-logs.txt", language);
        }
        final String currentTargetLogPath = String.format("%s%s%s-%s-%d.log", System.getProperty("user.dir"),
                File.separator, "func-logs", language, Instant.now().toEpochMilli());
        logger.info("Copying log runs to {}", currentTargetLogPath);
        environment.getContainerByServiceName(BASE_IMAGE).ifPresent(containerState -> {
            try {
                containerState.copyFileFromContainer(containerPath, currentTargetLogPath);
            } catch (IOException | InterruptedException e) {
                logger.warn("Could not copy run logs, this should not affect the run", e);
            }
        });
        environment.stop();
        logger.info("Simulation run finished!");
    }
}
