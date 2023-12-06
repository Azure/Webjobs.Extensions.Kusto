using System;
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
            _logger = logger;
        }

        [Function(nameof(EventHubToKustoFunction))]
        public void Run([EventHubTrigger("samples-workitems", Connection = "AzureEHConnectionString")] EventData[] events)
        {
            foreach (EventData @event in events)
            {
                _logger.LogInformation("Event Body: {body}", @event.Body);
                _logger.LogInformation("Event Content-Type: {contentType}", @event.ContentType);
            }
        }
    }
}
