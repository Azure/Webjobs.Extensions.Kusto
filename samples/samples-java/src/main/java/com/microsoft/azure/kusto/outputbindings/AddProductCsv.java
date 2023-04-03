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

import java.io.IOException;
import java.util.Optional;

import static com.microsoft.azure.kusto.common.Constants.*;

//TODO needs revisiting as there are mapping issues and the ingestion of CSV is not working!
public class AddProductCsv {
    @FunctionName("AddProductCsv")
    public HttpResponseMessage run(@HttpTrigger(name = "req", methods = {
            HttpMethod.POST}, authLevel = AuthorizationLevel.ANONYMOUS, route = "addproductcsv") HttpRequestMessage<Optional<String>> request,
            @KustoOutput(name = "productCsv", database = SDKTESTSDB, tableName = PRODUCTS, connection = KUSTOCONNSTR,
                    dataFormat = "csv") OutputBinding<String> productString)
            throws IOException {
        if (request.getBody().isPresent()) {
            String json = request.getBody().get();
            ObjectMapper mapper = new ObjectMapper();
            Product p = mapper.readValue(json, Product.class);
            String productCsv = p.ProductID + "," + p.Name + "," + p.Cost;
            productString.setValue(productCsv);
            return request.createResponseBuilder(HttpStatus.OK).header("Content-Type", "application/text")
                    .body(productString).build();
        } else {
            return request.createResponseBuilder(HttpStatus.NO_CONTENT).header("Content-Type", "application/json")
                    .build();
        }
    }
}
