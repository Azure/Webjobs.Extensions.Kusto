// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Microsoft.Azure.WebJobs.Kusto;

namespace Microsoft.Azure.WebJobs.Extensions.Kusto.Tests
{
    public class PublicSurfaceTests
    {
        [Xunit.Fact]
        public void WebJobsExtensionsKustoVerifyPublicSurfaceArea()
        {
            System.Reflection.Assembly assembly = typeof(KustoAttribute).Assembly;

            string[] expected = new[]
            {
                "KustoBindingExtension",
                "KustoBindingStartup",
                "KustoAttribute"
            };
            Host.TestCommon.TestHelpers.AssertPublicTypes(expected, assembly);
        }
    }
}
