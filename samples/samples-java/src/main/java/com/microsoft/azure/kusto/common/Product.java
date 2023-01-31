/**
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License. See License.txt in the project root for
 * license information.
 */

package com.microsoft.azure.kusto.common;

import com.fasterxml.jackson.annotation.JsonProperty;

public class Product {
    @JsonProperty("ProductID")
    public int ProductID;
    @JsonProperty("Name")
    public String Name;
    @JsonProperty("Cost")
    public double Cost;

    public Product() {
    }

    public Product(int ProductID, String name, double Cost) {
        this.ProductID = ProductID;
        this.Name = name;
        this.Cost = Cost;
    }
}
