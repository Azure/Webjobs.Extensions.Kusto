// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

namespace Microsoft.Azure.WebJobs.Extensions.Kusto.SamplesOutOfProc.OutputBindingSamples.Common
{
    public class Product
    {
        public long ProductID { get; set; }

        public string? Name { get; set; }

        public double Cost { get; set; }
    }

    public class Item
    {
        public long ItemID { get; set; }

        public string? ItemName { get; set; }

        public double ItemCost { get; set; }
    }
}