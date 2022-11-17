// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Kusto.Ingest;
using Microsoft.Azure.WebJobs.Kusto;

namespace Microsoft.Azure.WebJobs.Extensions.Kusto
{
    internal class KustoIngestContext
    {
        public KustoAttribute ResolvedAttribute { get; set; }

        public IKustoIngestClient IngestService { get; set; }
    }
}
