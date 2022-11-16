// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.IO;

namespace Microsoft.Azure.WebJobs.Kusto
{
    internal static class KustoBindingUtilities
    {
        public static Stream StreamFromString(string dataToIngest)
        {
            var stream = new MemoryStream();
            var writer = new StreamWriter(stream);
            writer.Write(dataToIngest);
            writer.Flush();
            stream.Position = 0;
            return stream;
        }
    }
}
