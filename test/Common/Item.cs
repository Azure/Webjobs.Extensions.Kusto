// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;

namespace Microsoft.Azure.WebJobs.Extensions.Kusto.Tests.Common
{
    public class Item
    {
        public int ID { get; set; }

        public string Name { get; set; }

        public double Cost { get; set; }

        public DateTime Timestamp { get; set; }

        public override bool Equals(object obj)
        {
            if (obj is Item otherData)
            {
                return this.ID == otherData.ID && this.Cost == otherData.Cost && ((this.Name == null && otherData.Name == null) ||
                    string.Equals(this.Name, otherData.Name, StringComparison.OrdinalIgnoreCase)) && this.Timestamp.Equals(otherData.Timestamp);
            }
            return false;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(this.ID, this.Name, this.Cost, this.Timestamp);
        }
    }
}