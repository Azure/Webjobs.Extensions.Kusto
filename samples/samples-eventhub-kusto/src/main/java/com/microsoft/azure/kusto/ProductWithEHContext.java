/**
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License. See License.txt in the project root for
 * license information.
 */

package com.microsoft.azure.kusto;

import com.fasterxml.jackson.annotation.JsonProperty;

import java.util.Map;

public class ProductWithEHContext {
    @JsonProperty("ProductID")
    public long productId;
    @JsonProperty("Name")
    public String name;
    @JsonProperty("Cost")
    public double cost;
    @JsonProperty("EHPartitionContext")
    public Map<String,Object> partitionContext;

    public ProductWithEHContext() {
    }

    public ProductWithEHContext(long productId, String name, double cost, Map<String,Object> partitionContext) {
        this.productId = productId;
        this.name = name;
        this.cost = cost;
        this.partitionContext = partitionContext;
    }
}
