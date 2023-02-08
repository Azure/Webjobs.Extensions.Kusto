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
import com.microsoft.azure.kusto.common.Item;

import static com.microsoft.azure.kusto.common.Constants.KUSTOCONNSTR;
import static com.microsoft.azure.kusto.common.Constants.SDKTESTSDB;

import java.io.IOException;
import java.util.Optional;

public class AddProductWithMapping {
    @FunctionName("AddJProductMapping")
    public HttpResponseMessage run(@HttpTrigger(name = "req", methods = {
            HttpMethod.POST }, authLevel = AuthorizationLevel.ANONYMOUS, route = "j-addproduct-mapping") HttpRequestMessage<Optional<String>> request,
            @KustoOutput(name = "product", database = SDKTESTSDB, tableName = "Products", connection = KUSTOCONNSTR, mappingRef = "item_to_product_json") OutputBinding<Item> item)
            throws IOException {
        if (request.getBody().isPresent()) {
            String json = request.getBody().get();
            ObjectMapper mapper = new ObjectMapper();
            Item p = mapper.readValue(json, Item.class);
            item.setValue(p);
            return request.createResponseBuilder(HttpStatus.OK).header("Content-Type", "application/json").body(item)
                    .build();
        } else {
            return request.createResponseBuilder(HttpStatus.NO_CONTENT).header("Content-Type", "application/json")
                    .build();
        }
    }
}
