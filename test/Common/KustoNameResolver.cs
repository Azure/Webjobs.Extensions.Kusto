// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace Microsoft.Azure.WebJobs.Extensions.Kusto.Tests.Common
{
    // Resolved from : https://github.com/Azure/azure-webjobs-sdk-extensions/blob/f60ddaa9da1065fca896a1ef98ab62dd46ffd97d/test/WebJobs.Extensions.Tests.Common/TestNameResolver.cs
    public class KustoNameResolver : INameResolver
    {
        private readonly bool _throwException;
        public KustoNameResolver(bool throwNotImplementedException = false)
        {
            // DefaultNameResolver throws so this helps simulate that for testing
            this._throwException = throwNotImplementedException;
        }
        public Dictionary<string, string> Values { get; } = new Dictionary<string, string>();
        public string Resolve(string name)
        {
            if (this._throwException)
            {
                throw new NotImplementedException("INameResolver must be supplied to resolve '%" + name + "%'.");
            }
            this.Values.TryGetValue(name, out string value);
            return value;
        }
    }
}