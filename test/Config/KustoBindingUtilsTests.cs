// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using Microsoft.Azure.WebJobs.Extensions.Kusto.Config;
using Xunit;

namespace Microsoft.Azure.WebJobs.Extensions.Kusto.Tests.Config
{
    public class KustoBindingUtilsTests
    {
        [Theory]
        [InlineData
            ("Data Source=https://randomcluster.eastus.dev.kusto.windows.net;Database=testdb;Fed=True;AppClientId=214dd011-44fd-4893-fc2f-6f6c3c85b90c;AppKey=areallyfakekey;Authority Id=11f111bf-11f2-11af-11ab-1d1cd111db11",
             "Data Source=https://randomcluster.eastus.dev.kusto.windows.net;Database=testdb;Fed=True;AppClientId=*;AppKey=*;Authority Id=*")]
        [InlineData("Data Source=https://randomcluster.eastus.dev.kusto.windows.net;Database=testdb;Fed=True;User ID=UserId;AppKey=areallyfakekey!;Authority Id=11f111bf-11f2-11af-11ab-1d1cd111db11;Application Token=apptoken11",
             "Data Source=https://randomcluster.eastus.dev.kusto.windows.net;Database=testdb;Fed=True;User ID=*;AppKey=*;Authority Id=*;Application Token=*")]
        [InlineData("Data Source=https://randomcluster.eastus.dev.kusto.windows.net;Database=testdb;Fed=True",
             "Data Source=https://randomcluster.eastus.dev.kusto.windows.net;Database=testdb;Fed=True")]
        public void ToSecureStringTest(string connectionStringInput, string expectedString)
        {
            string actualString = KustoBindingUtils.ToSecureString(connectionStringInput);
            Assert.True(string.Equals(expectedString, actualString, StringComparison.OrdinalIgnoreCase));
        }
    }
}
