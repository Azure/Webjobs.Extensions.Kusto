/**
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License. See License.txt in the project root for
 * license information.
 */

package com.microsoft.azure.kusto.outputbindings;

import com.fasterxml.jackson.databind.ObjectMapper;
import com.microsoft.azure.functions.HttpMethod;
import com.microsoft.azure.functions.HttpRequestMessage;
import com.microsoft.azure.functions.HttpResponseMessage;
import com.microsoft.azure.functions.HttpStatus;
import com.microsoft.azure.functions.OutputBinding;
import com.microsoft.azure.functions.annotation.AuthorizationLevel;
import com.microsoft.azure.functions.annotation.FunctionName;
import com.microsoft.azure.functions.annotation.HttpTrigger;
import com.microsoft.azure.functions.kusto.annotation.KustoOutput;
import com.microsoft.azure.kusto.common.Product;
import com.microsoft.azure.kusto.common.ProductChangeLog;

import static com.microsoft.azure.kusto.common.Constants.*;

import java.io.IOException;
import java.time.Clock;
import java.time.Instant;
import java.util.Optional;

public class AddMultiTable {
    @FunctionName("AddMultiTable")
    public HttpResponseMessage run(@HttpTrigger(name = "req", methods = {
            HttpMethod.POST}, authLevel = AuthorizationLevel.ANONYMOUS, route = "addmultitable") HttpRequestMessage<Optional<String>> request,
            @KustoOutput(name = "product", database = SDKTESTSDB, tableName = PRODUCTS, connection = KUSTOCONNSTR) OutputBinding<Product> product,
            @KustoOutput(name = "productChangeLog", database = SDKTESTSDB, tableName = PRODUCTSCHANGELOG,
                    connection = KUSTOCONNSTR) OutputBinding<ProductChangeLog> productChangeLog)
            throws IOException {

        if (request.getBody().isPresent()) {
            String json = request.getBody().get();
            ObjectMapper mapper = new ObjectMapper();
            Product p = mapper.readValue(json, Product.class);
            product.setValue(p);
            productChangeLog.setValue(new ProductChangeLog(p.ProductID, Instant.now(Clock.systemUTC()).toString()));
            return request.createResponseBuilder(HttpStatus.OK).header("Content-Type", "application/json").body(product)
                    .build();
        } else {
            return request.createResponseBuilder(HttpStatus.NO_CONTENT).header("Content-Type", "application/json")
                    .build();
        }
    }
}
