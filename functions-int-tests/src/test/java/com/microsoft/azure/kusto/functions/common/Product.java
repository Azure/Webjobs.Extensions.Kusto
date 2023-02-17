/**
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License. See License.txt in the project root for
 * license information.
 */

package com.microsoft.azure.kusto.functions.common;

import com.fasterxml.jackson.annotation.JsonProperty;

public class Product {
    @JsonProperty("ProductID")
    public long ProductID;
    @JsonProperty("Name")
    public String Name;
    @JsonProperty("Cost")
    public double Cost;

    public Product() {
    }

    public Product(long ProductID, String name, double Cost) {
        this.ProductID = ProductID;
        this.Name = name;
        this.Cost = Cost;
    }
}
