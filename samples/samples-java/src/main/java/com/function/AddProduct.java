/**
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License. See License.txt in the project root for
 * license information.
 */

package com.function;

import com.fasterxml.jackson.databind.ObjectMapper;
import com.function.common.Product;
import com.microsoft.azure.functions.HttpMethod;
import com.microsoft.azure.functions.HttpRequestMessage;
import com.microsoft.azure.functions.HttpResponseMessage;
import com.microsoft.azure.functions.HttpStatus;
import com.microsoft.azure.functions.OutputBinding;
import com.microsoft.azure.functions.annotation.AuthorizationLevel;
import com.microsoft.azure.functions.annotation.FunctionName;
import com.microsoft.azure.functions.annotation.HttpTrigger;
import com.microsoft.azure.functions.kusto.annotation.KustoOutput;

import static com.function.common.ProductUtilities.getRandomProduct;

import java.io.IOException;
import java.util.Optional;

public class AddProduct {
    @FunctionName("AddProduct")
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
                    connection = "KustoConnectionString", tableName = "Products")
            OutputBinding<Product> product) throws IOException {
        String json = request.getBody().orElse(getRandomProduct());
        ObjectMapper mapper = new ObjectMapper();
        Product p = mapper.readValue(json, Product.class);
        product.setValue(p);
        return request.createResponseBuilder(HttpStatus.OK).header("Content-Type", "application/json").body(product)
                .build();
    }
}
