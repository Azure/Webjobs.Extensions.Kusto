// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using Newtonsoft.Json;

namespace Microsoft.Azure.WebJobs.Extensions.Kusto.Samples.Common
{
    public class ProductsChangeLog
    {
        [JsonProperty(nameof(ProductID))]
        public long ProductID { get; set; }

        [JsonProperty(nameof(CreatedAt))]
        public DateTime CreatedAt { get; set; }

    }
}
