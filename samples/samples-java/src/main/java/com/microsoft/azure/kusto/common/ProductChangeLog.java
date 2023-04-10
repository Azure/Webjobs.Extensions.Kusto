/**
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License. See License.txt in the project root for
 * license information.
 */

package com.microsoft.azure.kusto.common;

import com.fasterxml.jackson.annotation.JsonProperty;

public class ProductChangeLog {
    @JsonProperty("ProductID")
    public long ProductID;
    @JsonProperty("CreatedAt")
    public String CreatedAt;

    public ProductChangeLog() {
    }

    public ProductChangeLog(long ProductID, String CreatedAt) {
        this.ProductID = ProductID;
        this.CreatedAt = CreatedAt;
    }
}
