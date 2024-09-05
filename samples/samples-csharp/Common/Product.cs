// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Newtonsoft.Json;

namespace Microsoft.Azure.WebJobs.Extensions.Kusto.Samples.Common
{
    public class Product
    {
        [JsonProperty(nameof(ProductID))]
        public long ProductID { get; set; }

        [JsonProperty(nameof(Name))]
        public string Name { get; set; }

        [JsonProperty(nameof(Cost))]
        public double Cost { get; set; }
    }

    public class Item
    {
        public long ItemID { get; set; }
#nullable enable
        public string? ItemName { get; set; }
        public double ItemCost { get; set; }
    }
}