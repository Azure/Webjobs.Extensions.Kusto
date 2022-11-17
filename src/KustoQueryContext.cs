// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Kusto.Data.Common;
using Microsoft.Azure.WebJobs.Kusto;

namespace Microsoft.Azure.WebJobs.Extensions.Kusto
{
    internal class KustoQueryContext
    {
        public KustoAttribute ResolvedAttribute { get; set; }

        public ICslQueryProvider QueryProvider { get; set; }
    }
}
