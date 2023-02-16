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
    private DockerComposeContainer<?> environment;

    private final Map<String, Integer> languagePortMap = Stream
            .of(new String[][] { { "java", "7102" }, { "node", "7103" }, { "python", "7104" },
                    { "dotnet-isolated", "7101" }, })
            .collect(Collectors.collectingAndThen(Collectors.toMap(data -> data[0], data -> Integer.parseInt(data[1])),
                    Collections::<String, Integer> unmodifiableMap));
    private static final int hostPort = Integer.getInteger("port", 7103);
    private static final ObjectMapper dataMapper = new ObjectMapper();
    private String language = System.getProperty("language", "node");

    public FunctionsMultiLangTests() throws JsonProcessingException {

    }

    @Override
    public void before() {
        // Set up the tables and may be even clear them
        // Start with a randomly large number
        // Start the test container based on the language passed
        // Copy the project into the container
        // Replace the DLL file
        language = System.getProperty("language", "node");
        if (!languagePortMap.containsKey(language)) {
            logger.warn(
                    "Language " + language + " is not in the list of accepted languages for test. Accepted languages - "
                            + languagePortMap.keySet());
            System.exit(137);
        }
        int hostPort = languagePortMap.get(language);
        String pathToLanguageFolder = String.format("../../samples/samples-%s/docker-compose.yml", language);
        File absoluteFilePath = new File(pathToLanguageFolder).getAbsoluteFile();
        try {
            String path = absoluteFilePath.getCanonicalPath();
            logger.info("Starting compose from file {}", path);
            environment = new DockerComposeContainer<>(new File(path));
            environment.start();
            environment.getContainerByServiceName(language).ifPresent(
                    containerState -> runContainerCommands(pathToLanguageFolder, language, hostPort, containerState));
        } catch (IOException e) {
            throw new RuntimeException(e);
        }
    }

    private static void runContainerCommands(String pathToLanguageFolder, String language, int exposedPort,
            ContainerState containerState) {
        try {
            String pathToCopy = new File(pathToLanguageFolder).getParentFile().getCanonicalPath();
            containerState.copyFileToContainer(MountableFile.forHostPath(pathToCopy),
                    String.format("/src/samples-%s/", language));
            logger.info("Copied folder {} to container", pathToCopy);
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
    String baseUrl = String.format("http://localhost:%d/api", hostPort);
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
                    .body(StringBody(dataMapper.writeValueAsString(addProductsArray))).check(status().in(200, 201)))
                            .pause(5)
                            .exec(http("AddProductWithMapping").post("/addproductswithmapping")
                                    .body(StringBody(dataMapper.writeValueAsString(addItemWithMapping)))
                                    .check(status().in(200, 201)))
                            .pause(5)
                            .exec(http("GetProducts").get("/getproducts/" + itemId).check(status().in(200, 201),
                                    jsonPath("$[*].ProductID").ofLong().find().is(itemId),
                                    jsonPath("$[*].Name").ofString().find()
                                            .is(String.format("Item-%s-%d", language, itemId)),
                                    jsonPath("$[*].Cost").ofDouble().find().shouldBe(itemId / 1000999.999)))
                            .exitHereIfFailed();

    HttpProtocolBuilder httpProtocol = http.baseUrl(baseUrl).acceptHeader("application/json");
    ScenarioBuilder inputAndOutputBindingScenario = scenario("BasicInputAndOutputBindings")
            .exec(inputAndOutputBindings);
    {
        // setUp(inputAndOutputBindingScenario.injectOpen(rampUsers(10).during(10))).protocols(httpProtocol);
        /*
         * setUp(inputAndOutputBindingScenario.injectOpen(nothingFor(Duration.of(10, ChronoUnit.SECONDS)),
         * rampUsers(40).during(20))).protocols(httpProtocol)
         * .assertions(global().successfulRequests().percent().is(100.0));
         *
         */
        setUp(
                // generate a closed workload injection profile
                // with levels of 10, 15, 20, 25 and 30 concurrent users
                // each level lasting 10 seconds
                // separated by linear ramps lasting 10 seconds
                inputAndOutputBindingScenario.injectClosed(
                        incrementConcurrentUsers(5)
                                .times(5)
                                .eachLevelLasting(10)
                                .separatedByRampsLasting(10)
                                .startingFrom(10) // Int
                ).protocols(httpProtocol)
        );
    }

    @Override
    public void after() {
        /*
        try{
            Thread.sleep(300000);
        }catch (Exception ignored){

        }
         */
        environment.stop();
        logger.info("Simulation run finished!");
    }
}
