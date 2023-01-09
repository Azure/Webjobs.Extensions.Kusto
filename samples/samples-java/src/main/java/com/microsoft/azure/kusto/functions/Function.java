package com.microsoft.azure.kusto.functions;

import com.fasterxml.jackson.databind.ObjectMapper;
import com.microsoft.azure.kusto.functions.common.Product;
import com.microsoft.azure.functions.HttpMethod;
import com.microsoft.azure.functions.HttpRequestMessage;
import com.microsoft.azure.functions.HttpResponseMessage;
import com.microsoft.azure.functions.HttpStatus;
import com.microsoft.azure.functions.OutputBinding;
import com.microsoft.azure.functions.annotation.AuthorizationLevel;
import com.microsoft.azure.functions.annotation.FunctionName;
import com.microsoft.azure.functions.annotation.HttpTrigger;
import com.microsoft.azure.functions.kusto.annotation.KustoOutput;
import com.microsoft.azure.kusto.functions.common.ProductUtilities;

import java.io.IOException;
import java.util.Optional;

/**
 * Azure Functions with HTTP Trigger.
 */
public class Function {
    /**
     * This function listens at endpoint "/api/HttpExample". Two ways to invoke it using "curl" command in bash:
     * 1. curl -d "HTTP Body" {your host}/api/HttpExample
     * 2. curl "{your host}/api/HttpExample?name=HTTP%20Query"
     */
    @FunctionName("HttpExample")
    public HttpResponseMessage run(
            @HttpTrigger(
                    name = "req",
                    methods = {HttpMethod.POST},
                    authLevel = AuthorizationLevel.ANONYMOUS,
                    route = "j-addproduct")
                HttpRequestMessage<Optional<String>> request,
            @KustoOutput(
                    name = "product",
                    database = "sdktestsdb",
                    tableName = "Products")
            OutputBinding<Product> product) throws IOException {
        String json = request.getBody().orElse(ProductUtilities.getRandomProduct());
        ObjectMapper mapper = new ObjectMapper();
        Product p = mapper.readValue(json, Product.class);
        product.setValue(p);
        return request.createResponseBuilder(HttpStatus.OK).header("Content-Type", "application/json").body(product)
                .build();
    }
}
