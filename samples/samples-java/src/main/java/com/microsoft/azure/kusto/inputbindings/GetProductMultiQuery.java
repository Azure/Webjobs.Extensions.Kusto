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
import com.microsoft.azure.kusto.common.Constants;
import com.microsoft.azure.kusto.common.Product;

import static com.microsoft.azure.kusto.common.Constants.KUSTOCONNSTR;

import java.util.Optional;

public class GetProductMultiQuery {
    @FunctionName("GetProductsIdOrName")
    public HttpResponseMessage run(@HttpTrigger(name = "req", methods = {
            HttpMethod.GET }, authLevel = AuthorizationLevel.ANONYMOUS, route = "getproductsmq/{productId}/{name}") HttpRequestMessage<Optional<String>> request,
            @KustoInput(name = "getjproductsidorname", kqlCommand = "declare query_parameters (productId:long);Products | where ProductID == productId", kqlParameters = "@productId={productId}", database = Constants.SDKTESTSDB, connection = KUSTOCONNSTR) Product[] productsQuery,
            @KustoInput(name = "getjproductsidornamefn", kqlCommand = "declare query_parameters (name:string);GetProductsByName(name)", kqlParameters = "@name={name}", database = Constants.SDKTESTSDB, connection = KUSTOCONNSTR) Product[] productsFunction) {
        Product[] allProducts = new Product[productsQuery.length + productsFunction.length];
        System.arraycopy(productsQuery, 0, allProducts, 0, productsQuery.length);
        System.arraycopy(productsFunction, 0, allProducts, productsQuery.length, productsFunction.length);
        return request.createResponseBuilder(HttpStatus.OK).header("Content-Type", "application/json").body(allProducts)
                .build();
    }
}
