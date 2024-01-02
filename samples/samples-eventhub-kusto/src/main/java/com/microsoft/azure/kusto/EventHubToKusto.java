package com.microsoft.azure.kusto;

import com.fasterxml.jackson.core.JsonProcessingException;
import com.fasterxml.jackson.databind.ObjectMapper;
import com.microsoft.azure.functions.ExecutionContext;
import com.microsoft.azure.functions.OutputBinding;
import com.microsoft.azure.functions.annotation.*;
import com.microsoft.azure.functions.kusto.annotation.KustoOutput;

import java.util.List;
import java.util.Map;
import java.util.stream.Collectors;

/**
 * Azure Functions with EventHub Trigger.
 */
public class EventHubToKusto {
    @FunctionName("EventHubToKusto")
    public void run(
            @EventHubTrigger(name = "ProductsToKusto", eventHubName = "AzureWebJobsEventHubPath",
                    consumerGroup = "FunctionsCG",
                    connection = "AzureWebJobsEventHubSender", cardinality = Cardinality.MANY) List<String> messages,
            @BindingName("PartitionContext") Map<String, Object> partitionContext,
            @KustoOutput(name = "KustoProductsEH", database = "sdktests", tableName = "ProductsEH", connection = "FabricKQLDbConnectionString")
            OutputBinding<List<ProductWithEHContext>> productWithContext,
            final ExecutionContext context) {
        final ObjectMapper mapper = new ObjectMapper();
        List<ProductWithEHContext> products = messages.stream().map(message -> {
            try {
                context.getLogger().info("** Processing ** " + message);
                ProductWithEHContext p =  mapper.readValue(message, ProductWithEHContext.class);
                p.partitionContext = partitionContext;
                context.getLogger().info("** PostProcess ** " + mapper.writeValueAsString(p));
                return p;
            } catch (JsonProcessingException e) {
                throw new RuntimeException(e);
            }
        }).collect(Collectors.toList());
        productWithContext.setValue(products);
        context.getLogger().info("Processed " + messages.size() + " messages.");
    }
}
