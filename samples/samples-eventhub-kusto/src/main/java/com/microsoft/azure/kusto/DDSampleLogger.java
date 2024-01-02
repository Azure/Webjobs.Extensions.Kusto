package com.microsoft.azure.kusto;


import com.microsoft.azure.functions.ExecutionContext;
import com.microsoft.azure.functions.annotation.FunctionName;
import com.microsoft.azure.functions.annotation.TimerTrigger;

import java.util.Random;

import org.slf4j.Logger;
import org.slf4j.LoggerFactory;

public class DDSampleLogger {
    private static final Logger logger = LoggerFactory.getLogger(DDSampleLogger.class);
    private final Random random = new Random();
    @FunctionName("keepAlive")
    public void keepAlive(
    @TimerTrigger(name = "loggingTrigger", schedule = "*/15 * * * * *") String timerInfo,
        ExecutionContext context
    ) {
        int rand = random.nextInt();
        if(rand%2 == 0 ){
            logger.info("Emitted a log with level INFO {}", rand);
        }
        if(rand%3 == 0 ){
            logger.warn("Emitted a log with level WARN {}", rand);
        }

        if(rand%5 == 0 ){
            logger.error("Emitted a log with level Error {}", new RuntimeException(timerInfo, new RuntimeException("A random Inner Exception" + rand)));
        }
        // timeInfo is a JSON string, you can deserialize it to an object using your favorite JSON library
        context.getLogger().info("Timer is triggered: " + timerInfo);
    }

}
