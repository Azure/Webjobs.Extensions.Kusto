/**
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License. See License.txt in the project root for
 * license information.
 */

package com.microsoft.azure.kusto.common;

import java.util.stream.IntStream;

public class ProductUtilities {
    public static Product[] getNewProducts(int num) {
        return IntStream.range(0, num)
                .mapToObj(productId -> new Product(productId, "java-prod" + productId, 99.99 * productId))
                .toArray(Product[]::new);

    }
}
