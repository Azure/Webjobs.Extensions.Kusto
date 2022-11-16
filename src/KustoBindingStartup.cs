// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Microsoft.Azure.WebJobs.Extensions.Kusto;
using Microsoft.Azure.WebJobs.Hosting;
using Microsoft.Azure.WebJobs.Kusto;
using Microsoft.Extensions.DependencyInjection;

[assembly: WebJobsStartup(typeof(KustoBindingStartup))]

namespace Microsoft.Azure.WebJobs.Kusto
{
    public class KustoBindingStartup : IWebJobsStartup
    {
        public void Configure(IWebJobsBuilder builder)
        {
            builder.Services.AddSingleton<IKustoClientFactory>(new KustoManagedStreamingClientFactory());
            builder.AddKusto();
        }
    }
}
