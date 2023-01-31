/**
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License. See License.txt in the project root for
 * license information.
 */

package com.microsoft.azure.kusto.outputbindings;

import com.fasterxml.jackson.databind.ObjectMapper;
import com.microsoft.azure.functions.HttpMethod;
import com.microsoft.azure.functions.HttpRequestMessage;
import com.microsoft.azure.functions.annotation.AuthorizationLevel;
import com.microsoft.azure.functions.annotation.FunctionName;
import com.microsoft.azure.functions.annotation.HttpTrigger;
import com.microsoft.azure.functions.kusto.annotation.KustoOutput;
import com.microsoft.azure.kusto.common.Product;

import static com.microsoft.azure.kusto.common.Constants.KUSTOCONNSTR;
import static com.microsoft.azure.kusto.common.Constants.SDKTESTSDB;

import java.io.IOException;
import java.util.Optional;

public class AddProductReturn {
    @FunctionName("AddJProductReturn")
    @KustoOutput(name = "productReturn", database = SDKTESTSDB, tableName = "Products", connection = KUSTOCONNSTR)
    public Product run(@HttpTrigger(name = "req", methods = {
            HttpMethod.POST }, authLevel = AuthorizationLevel.ANONYMOUS, route = "j-addproduct-returnvalue") HttpRequestMessage<Optional<String>> request)
            throws IOException {
        if (request.getBody().isPresent()) {
            String json = request.getBody().get();
            ObjectMapper mapper = new ObjectMapper();
            return mapper.readValue(json, Product.class);
        } else {
            return null;
        }
    }
}
