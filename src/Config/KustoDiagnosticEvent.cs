// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Collections;
using System.Collections.Generic;

namespace Microsoft.Azure.WebJobs.Extensions.Kusto
{
    /// <summary>
    /// Creates structured log state dictionaries that the Azure Functions runtime
    /// recognizes as diagnostic events. These events are aggregated and surfaced
    /// in the Portal Overview section for customers.
    /// </summary>
    internal sealed class KustoDiagnosticEvent : IReadOnlyList<KeyValuePair<string, object>>
    {
        private readonly List<KeyValuePair<string, object>> _state;
        private readonly string _message;

        private KustoDiagnosticEvent(string errorCode, string message, string helpLink)
        {
            this._message = message;
            this._state = new List<KeyValuePair<string, object>>
            {
                new KeyValuePair<string, object>(KustoConstants.DiagnosticEventKey, true),
                new KeyValuePair<string, object>(KustoConstants.ErrorCodeKey, errorCode),
                new KeyValuePair<string, object>(KustoConstants.HelpLinkKey, helpLink),
                new KeyValuePair<string, object>("{OriginalFormat}", message),
            };
        }

        public static KustoDiagnosticEvent Create(string errorCode, string message, string helpLink)
        {
            return new KustoDiagnosticEvent(errorCode, message, helpLink);
        }

        public int Count => this._state.Count;

        public KeyValuePair<string, object> this[int index] => this._state[index];

        public IEnumerator<KeyValuePair<string, object>> GetEnumerator()
        {
            return this._state.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        public override string ToString()
        {
            return this._message;
        }
    }
}
