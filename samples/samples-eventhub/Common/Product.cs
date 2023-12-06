// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

namespace Microsoft.Azure.WebJobs.Extensions.EventHub.Kusto.Samples.Common
{
    public class Product
    {
        public long ProductID { get; set; }

        public string? Name { get; set; }

        public double Cost { get; set; }
    }
}