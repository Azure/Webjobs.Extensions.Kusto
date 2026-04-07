// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Microsoft.Azure.WebJobs.Extensions.Kusto.Tests.Config
{
    public class KustoDiagnosticEventTests
    {
        [Fact]
        public void CreateReturnsDiagnosticEventWithCorrectKeys()
        {
            // Arrange & Act
            var evt = KustoDiagnosticEvent.Create(
                KustoConstants.ConnectionErrorCode,
                "Test error message",
                KustoConstants.KustoBindingHelpLink);

            // Assert — the runtime requires exactly these 4 keys
            Assert.Equal(4, evt.Count);

            KeyValuePair<string, object> diagnosticKey = evt[0];
            Assert.Equal(KustoConstants.DiagnosticEventKey, diagnosticKey.Key);
            Assert.Equal(true, diagnosticKey.Value);

            KeyValuePair<string, object> errorCodeKey = evt[1];
            Assert.Equal(KustoConstants.ErrorCodeKey, errorCodeKey.Key);
            Assert.Equal(KustoConstants.ConnectionErrorCode, errorCodeKey.Value);

            KeyValuePair<string, object> helpLinkKey = evt[2];
            Assert.Equal(KustoConstants.HelpLinkKey, helpLinkKey.Key);
            Assert.Equal(KustoConstants.KustoBindingHelpLink, helpLinkKey.Value);

            KeyValuePair<string, object> originalFormatKey = evt[3];
            Assert.Equal("{OriginalFormat}", originalFormatKey.Key);
            Assert.Equal("Test error message", originalFormatKey.Value);
        }

        [Fact]
        public void ToStringReturnsOriginalMessage()
        {
            string message = "Ingestion failed for sourceId abc";
            var evt = KustoDiagnosticEvent.Create(
                KustoConstants.IngestionErrorCode, message, KustoConstants.KustoBindingHelpLink);

            Assert.Equal(message, evt.ToString());
        }

        [Fact]
        public void EnumeratorReturnsAllKeyValuePairs()
        {
            var evt = KustoDiagnosticEvent.Create(
                KustoConstants.QueryErrorCode, "query error", KustoConstants.KustoBindingHelpLink);

            var pairs = evt.ToList();
            Assert.Equal(4, pairs.Count);
            Assert.Contains(pairs, p => p.Key == KustoConstants.DiagnosticEventKey);
            Assert.Contains(pairs, p => p.Key == KustoConstants.ErrorCodeKey);
            Assert.Contains(pairs, p => p.Key == KustoConstants.HelpLinkKey);
            Assert.Contains(pairs, p => p.Key == "{OriginalFormat}");
        }

        [Theory]
        [InlineData(KustoConstants.ConnectionErrorCode)]
        [InlineData(KustoConstants.IngestionErrorCode)]
        [InlineData(KustoConstants.QueryErrorCode)]
        public void CreatePreservesErrorCode(string errorCode)
        {
            var evt = KustoDiagnosticEvent.Create(
                errorCode, "some message", "https://example.com");

            KeyValuePair<string, object> errorCodePair = evt.First(p => p.Key == KustoConstants.ErrorCodeKey);
            Assert.Equal(errorCode, errorCodePair.Value);
        }
    }
}
