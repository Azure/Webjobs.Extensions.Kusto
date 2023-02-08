/**
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License. See License.txt in the project root for
 * license information.
 */

package com.microsoft.azure.kusto.common;

import com.fasterxml.jackson.annotation.JsonProperty;

public class Item {
    @JsonProperty("ItemID")
    public int ItemID;
    @JsonProperty("ItemName")
    public String ItemName;
    @JsonProperty("ItemCost")
    public double ItemCost;
}
