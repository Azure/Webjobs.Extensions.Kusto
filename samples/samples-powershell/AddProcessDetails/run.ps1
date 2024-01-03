# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License. See License.txt in the project root for license information.

using namespace System.Net

# Trigger binding data passed in via param block
param($Request, $TriggerMetadata)

# Write to the Azure Functions log stream.
Write-Host "PowerShell function with Kusto Output Binding processed a request."

# A sample where we can execute a Powershell command and pass the output to the Kusto Output binding
# Just to show what is possible from a native Powershell perspective

$process_query = Get-ChildItem | Sort-Object -Descending |  ConvertTo-Json

# Assign the value we want to pass to the Kusto Output binding.
# The -Name value corresponds to the name property in the function.json for the binding
Push-OutputBinding -Name processdetails -Value $process_query

# Assign the value to return as the HTTP response.
# The -Name value matches the name property in the function.json for the binding
Push-OutputBinding -Name response -Value ([HttpResponseContext]@{
    StatusCode = [HttpStatusCode]::Created
    Body = $process_query
})