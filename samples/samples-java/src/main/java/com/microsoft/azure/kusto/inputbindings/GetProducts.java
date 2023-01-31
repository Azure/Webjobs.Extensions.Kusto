/**
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License. See License.txt in the project root for
 * license information.
 */
package com.microsoft.azure.kusto.inputbindings;

import com.microsoft.azure.functions.HttpMethod;
import com.microsoft.azure.functions.HttpRequestMessage;
import com.microsoft.azure.functions.HttpResponseMessage;
import com.microsoft.azure.functions.HttpStatus;
import com.microsoft.azure.functions.annotation.AuthorizationLevel;
import com.microsoft.azure.functions.annotation.FunctionName;
import com.microsoft.azure.functions.annotation.HttpTrigger;
import com.microsoft.azure.functions.kusto.annotation.KustoInput;
import com.microsoft.azure.kusto.common.Product;

import static com.microsoft.azure.kusto.common.Constants.*;

import java.util.Optional;

public class GetProducts {
    @FunctionName("GetJProducts")
    public HttpResponseMessage run(@HttpTrigger(name = "req", methods = {
            HttpMethod.GET }, authLevel = AuthorizationLevel.ANONYMOUS, route = "j-getproduct/{productId}") HttpRequestMessage<Optional<String>> request,
            @KustoInput(name = "getjproducts", kqlCommand = "declare query_parameters (productId:long);Products | where ProductID == productId", kqlParameters = "@productId={productId}", database = SDKTESTSDB, connection = KUSTOCONNSTR) Product[] products) {
        return request.createResponseBuilder(HttpStatus.OK).header("Content-Type", "application/json").body(products)
                .build();
    }
}
