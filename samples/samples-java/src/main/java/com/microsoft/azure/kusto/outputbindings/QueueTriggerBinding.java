package com.microsoft.azure.kusto.outputbindings;

import com.microsoft.azure.functions.ExecutionContext;
import com.microsoft.azure.functions.annotation.FunctionName;
import com.microsoft.azure.functions.kusto.annotation.KustoOutput;
import com.microsoft.azure.functions.rabbitmq.annotation.RabbitMQTrigger;
import com.microsoft.azure.kusto.common.Product;

import static com.microsoft.azure.kusto.common.Constants.*;

public class QueueTriggerBinding {
    @FunctionName("QueueTriggerKustoOutputBinding")
    @KustoOutput(name = "product", database = SDKTESTSDB, tableName = PRODUCTS, connection = KUSTOCONNSTR)
    public static Product run(
            @RabbitMQTrigger(connectionStringSetting = "rabbitMQConnectionAppSetting", queueName = "bindings.queue") Product product,
            final ExecutionContext context) {
        context.getLogger().info("Processing message with ID" + product.ProductID);
        return product;
    }
}
