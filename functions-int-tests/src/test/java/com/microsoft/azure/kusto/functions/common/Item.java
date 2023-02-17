/**
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License. See License.txt in the project root for
 * license information.
 */

package com.microsoft.azure.kusto.functions.common;

import com.fasterxml.jackson.annotation.JsonProperty;

public class Item {
    @JsonProperty("ItemID")
    public long ItemID;
    @JsonProperty("ItemName")
    public String ItemName;
    @JsonProperty("ItemCost")
    public double ItemCost;

    public Item(long itemID, String itemName, double itemCost) {
        ItemID = itemID;
        ItemName = itemName;
        ItemCost = itemCost;
    }
}
