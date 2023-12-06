// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Azure.Messaging.EventHubs;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace Microsoft.Azure.Webjobs.EventHub.Kusto.Sample
{
    public class EventHubToKustoFunction
    {
        private readonly ILogger<EventHubToKustoFunction> _logger;

        public EventHubToKustoFunction(ILogger<EventHubToKustoFunction> logger)
        {
            this._logger = logger;
        }

        [Function(nameof(EventHubToKustoFunction))]
        public void Run([EventHubTrigger("samples-workitems", Connection = "AzureEHConnectionString")] EventData[] events)
        {
            foreach (EventData @event in events)
            {
            }
        }
    }
}
