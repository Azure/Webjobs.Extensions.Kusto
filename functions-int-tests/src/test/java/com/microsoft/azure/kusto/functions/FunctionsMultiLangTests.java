package com.microsoft.azure.kusto.functions;

import static io.gatling.javaapi.core.CoreDsl.StringBody;
import static io.gatling.javaapi.core.CoreDsl.exec;
import static io.gatling.javaapi.core.CoreDsl.global;
import static io.gatling.javaapi.core.CoreDsl.jsonPath;
import static io.gatling.javaapi.core.CoreDsl.nothingFor;
import static io.gatling.javaapi.core.CoreDsl.rampUsers;
import static io.gatling.javaapi.core.CoreDsl.scenario;
import static io.gatling.javaapi.http.HttpDsl.http;
import static io.gatling.javaapi.http.HttpDsl.status;
import static java.lang.System.getProperty;

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

import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.testcontainers.containers.Container;
import org.testcontainers.containers.ContainerState;
import org.testcontainers.containers.DockerComposeContainer;
import org.testcontainers.utility.MountableFile;

import com.fasterxml.jackson.core.JsonProcessingException;
import com.fasterxml.jackson.databind.ObjectMapper;
import com.microsoft.azure.kusto.functions.common.Item;
import com.microsoft.azure.kusto.functions.common.Product;

import io.gatling.javaapi.core.ChainBuilder;
import io.gatling.javaapi.core.ScenarioBuilder;
import io.gatling.javaapi.core.Simulation;
import io.gatling.javaapi.http.HttpProtocolBuilder;

public class FunctionsMultiLangTests extends Simulation {
    private static final Logger logger = LoggerFactory.getLogger(FunctionsMultiLangTests.class);
    // File name in docker compose file
    private static final String BASE_IMAGE = "baseimage";

    private static final int HOST_PORT = Integer.getInteger("port", 7103);
    private static final Boolean HOLD_CONTAINER = Boolean.getBoolean("debug");
    private static final Boolean RUN_TRIGGER = Boolean.getBoolean("runTrigger");
    private static final ObjectMapper JSON_MAPPER = new ObjectMapper();

    private static final String PATH_TO_DOCKER_COMPOSE = "../samples/docker/docker-compose.yml";
    private static final String PATH_TO_DOCKER_COMPOSE_WITH_NO_RMQ = "../samples/docker/docker-compose-no-rmq.yml";
    private static final String CREATE_QUEUE = "../samples/docker/create-queue.sh";

    private DockerComposeContainer<?> environment;

    private final Map<String, Integer> languagePortMap = Stream
            .of(new String[][] { { "outofproc", "7101" }, { "java", "7102" }, { "node", "7103" }, { "python", "7104" },
                    { "csharp", "7105" } })
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
        String dockerComposeFile = RUN_TRIGGER ? PATH_TO_DOCKER_COMPOSE : PATH_TO_DOCKER_COMPOSE_WITH_NO_RMQ;
        File absoluteFilePath = new File(dockerComposeFile).getAbsoluteFile();
        try {
            String path = absoluteFilePath.getCanonicalPath();
            logger.info("Starting compose from file {}", path);
            environment = new DockerComposeContainer<>(new File(path));
            environment.start();
            environment.getContainerByServiceName("rabbitmq").ifPresent(FunctionsMultiLangTests::createQueue);
            environment.getContainerByServiceName(BASE_IMAGE)
                    .ifPresent(containerState -> runContainerCommands(language, hostPort, containerState));
        } catch (IOException e) {
            throw new RuntimeException(e);
        }
    }

    private static void createQueue(ContainerState containerState) {
        try {
            containerState.copyFileToContainer(MountableFile.forHostPath(CREATE_QUEUE), "/tmp/create-queue.sh");
            Container.ExecResult createQueue = containerState.execInContainer("bash", "/tmp/create-queue.sh");
            // .execInContainer("rabbitmqadmin -u guest -p guest -V / declare queue name=bindings.test.queue");
            logger.info("Create Queue returned {}.StdErr {} and StdOut {}", createQueue.getExitCode(),
                    createQueue.getStderr(), createQueue.getStdout());
        } catch (IOException | InterruptedException e) {
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
    int noActionTime = 60;// RUN_TRIGGER ? 60 : 20;
    long itemId = seconds + 10;
    Item addItemWithMapping = new Item(itemId, String.format("Item-%s-%d", language, itemId), itemId / 1000999.999);
    ChainBuilder inputAndOutputBindings = exec(session ->
    // return a new session instance and set the language in it
    session.set("runTrigger", RUN_TRIGGER)).
    // let's give proper names, as they are displayed in the reports
            exec(http("AddProduct").post("/addproduct")
                    .body(StringBody(JSON_MAPPER.writeValueAsString(addProductsArray))).check(status().in(200, 201)))
            .pause(5)
            .exec(http("AddProductWithMapping").post("/addproductswithmapping")
                    .body(StringBody(JSON_MAPPER.writeValueAsString(addItemWithMapping))).check(status().in(200, 201)))
            .pause(5)
            .exec(http("GetProducts").get("/getproducts/" + itemId).check(status().in(200, 201),
                    jsonPath("$[*].ProductID").ofLong().find().is(itemId),
                    jsonPath("$[*].Name").ofString().find().is(String.format("Item-%s-%d", language, itemId)),
                    jsonPath("$[*].Cost").ofDouble().find().shouldBe(itemId / 1000999.999)))
            .exec(http("GetProductsFunction").get(String.format("/getproductsfn/Item-%s-%d", language, itemId)).check(
                    status().in(200), jsonPath("$[*].ProductID").ofLong().find().is(itemId),
                    jsonPath("$[*].Name").ofString().find().is(String.format("Item-%s-%d", language, itemId)),
                    jsonPath("$[*].Cost").ofDouble().find().shouldBe(itemId / 1000999.999)))
            .pause(Duration.of(10, ChronoUnit.SECONDS)).doIf(session -> session.getBoolean("runTrigger"))
            .then(exec(http("RetrieveTriggerMessages").get(String.format("/getproductsfn/R-MQ-%d", itemId)).check(
                    status().in(200), jsonPath("$[*].ProductID").ofLong().find().is(itemId),
                    jsonPath("$[*].Name").ofString().find().is(String.format("R-MQ-%d", itemId)),
                    jsonPath("$[*].Cost").ofDouble().find().shouldBe(itemId / 1000999.999))).exitHereIfFailed());

    HttpProtocolBuilder httpProtocol = http.baseUrl(baseUrl).acceptHeader("application/json");
    // Open systems, where you control the arrival rate of users
    ScenarioBuilder inputAndOutputScenario = scenario("BasicInputAndOutputBindings-Open").exec(inputAndOutputBindings);
    {
        setUp(inputAndOutputScenario.injectOpen(nothingFor(Duration.of(noActionTime, ChronoUnit.SECONDS)),
                rampUsers(50).during(40))).protocols(httpProtocol)
                        .assertions(global().successfulRequests().percent().shouldBe(100.0));
    }

    @Override
    public void after() {
        String containerPath;
        if ("java".equalsIgnoreCase(language)) {
            containerPath = String.format(
                    "/src/samples-%s/target/azure-functions/kustojavafunctionssample-20230130111810292/func-logs.txt",
                    language);
        } else if ("outofproc".equalsIgnoreCase(language)) {
            containerPath = String.format("/src/samples-%s/bin/Debug/net9.0/func-logs.txt", language);
        } else if ("csharp".equalsIgnoreCase(language)) {
            containerPath = String.format("/src/samples-%s/bin/Debug/net6/func-logs.txt", language);
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
        if (HOLD_CONTAINER) {
            try {
                Thread.sleep(600000);
            } catch (Exception ignored) {

            }
        }
        environment.stop();
        logger.info("Simulation run finished!");
    }
}

/*
 * { if ("csharp".equalsIgnoreCase(language)) { AmqpQueue triggerQueue = new AmqpQueue("trigger.bindings.queue", false,
 * false, false, Collections.emptyMap()); AmqpQueue outQueue = new AmqpQueue("output.bindings.queue", false, false,
 * false, Collections.emptyMap()); final AmqpProtocolBuilder amqpConf = amqp()
 * .connectionFactory(rabbitmq().host("rabbitmq").port(7001).username("guest").password("guest") .vhost("/").build())
 * .replyTimeout(60000L).consumerThreadsCount(8).matchByMessage((amqpMessage) -> { try { return
 * JSON_MAPPER.readValue(amqpMessage.payload(), Product.class).Name; } catch (Exception ex) { return ""; }
 * }).usePersistentDeliveryMode().declare(triggerQueue).declare(outQueue); final AtomicInteger counter = new
 * AtomicInteger(1000); Iterator<Map<String, Object>> productFeeder = Stream.generate((Supplier<Map<String, Object>>) ()
 * -> { try { return Collections.singletonMap("id", JSON_MAPPER.writeValueAsString(new Product(seconds -
 * counter.incrementAndGet(), String.format("RMQ-Product-%s-%d", language, seconds - counter.get()), (seconds -
 * counter.get()) / 1000999.999))); } catch (JsonProcessingException e) { throw new RuntimeException(e); }
 * }).iterator();
 *
 * ScenarioBuilder scn = scenario("MQ Trigger Tests").feed(productFeeder)
 * .exec(amqp("Request Reply exchange test").requestReply().queueExchange("trigger.bindings.queue")
 * .textMessage("#{id}").priority(0).contentType("application/json") .check(bodyString().exists()));
 *
 * { setUp(scn.injectOpen(rampUsersPerSec(1).to(5).during(60))).protocols(amqpConf).maxDuration(10 * 60); }
 *
 * } }
 */