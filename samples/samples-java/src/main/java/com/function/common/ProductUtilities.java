/**
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License. See License.txt in the project root for
 * license information.
 */

package com.function.common;

import com.fasterxml.jackson.core.JsonProcessingException;
import com.fasterxml.jackson.databind.ObjectMapper;

import java.util.Random;

public class ProductUtilities {
    private static final ObjectMapper OBJECT_MAPPER = new ObjectMapper();

    private ProductUtilities() {
    }

    public static String getRandomProduct() throws JsonProcessingException {
        Random random = new Random();
        int productId = random.nextInt();
        Product product = new Product(productId, "Prod-" + productId, productId * productId);
        return OBJECT_MAPPER.writeValueAsString(product);
    }
}
