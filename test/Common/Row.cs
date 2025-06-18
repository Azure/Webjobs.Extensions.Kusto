// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;

namespace Microsoft.Azure.WebJobs.Extensions.Kusto.Tests.Common
{
    internal sealed class Row
    {
        public int ID;
        public string Name;
        public double Cost;


        public static Row CreateRandom(string itemname, int counter)
        {
            var rnd = new Random();
            return new Row
            {
                ID = counter,
                Name = itemname,
                Cost = rnd.NextDouble()
            };
        }

        public object[] ToObjectArray()
        {
            return
            [
                this.ID,
                this.Name,
                this.Cost
            ];
        }
    }
}