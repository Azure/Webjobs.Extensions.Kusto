# **Kusto bindings for Azure Functions - Preview**

## **Table of Contents**
- [Kusto bindings for Azure Functions - Preview](#kusto-bindings-for-azure-functions---preview)
  - [Table of Contents](#table-of-contents)
  - [Introduction](#introduction)
  - [Input Bindings](#input-binding)
  - [Output Bindings](#output-binding)
  - [Trademarks](#trademarks)

## **Introduction**

This repository contains the Kusto bindings for Azure Functions extension code as well as a quick start tutorial and samples illustrating how to use the binding in different ways. The types of bindings supported are:

### **Input Binding**

Takes a KQL query or KQL function to run (with optional parameters) and returns the output to the function. 
The input binding takes the following attributes

- Database: The database against which the query has to be executed

- ManagedServiceIdentity: A managed identity can be used to connect to Kusto. To use a System managed identity, use "system", any other identity names are interpreted as user managed identity

- KqlCommand: The KqlQuery that has to be executed. Can be a KQL query or a KQL Function call

- KqlParameters: Parameters that act as predicate variables for the KqlCommand. For example "@name={name},@Id={id}" where the parameters {name} and {id} will be substituted at runtime with actual values acting as predicates

- Connection: The _**name**_ of the variable that holds the connection string, resolved through environment variables or through function app settings. Defaults to lookup on the variable _**KustoConnectionString**_, at runtime this variable will be looked up against the environment.
Documentation on connection string can be found at [Kusto connection strings](https://learn.microsoft.com/en-us/azure/data-explorer/kusto/api/connection-strings/kusto)
e.g.:
`"KustoConnectionString": "Data Source=https://_**cluster**_.kusto.windows.net;Database=_**Database**_;Fed=True;AppClientId=_**AppId**_;AppKey=_**AppKey**_;Authority Id=_**TenantId**_`
Note that the application id should **_atleast have viewer privileges_** on the table(s)/function(s) being queried in the KqlCommand

- ClientRequestProperties: Optional attribute to pass [client request properties](https://docs.microsoft.com/en-us/azure/data-explorer/kusto/api/netfx/request-properties) to the Kusto client

Starting versions 1.0.8-Preview there is support for [management commands](https://learn.microsoft.com/en-us/azure/data-explorer/kusto/management/). A sample is available [here](samples/samples-blob-ingest/IngestBlobToKusto.cs)

### **Output Binding**
 
Takes row(s) and inserts them into the Kusto table .

- Database: The database against which the query has to be executed

- TableName: The table to ingest the data into

- ManagedServiceIdentity: A managed identity can be used to connect to Kusto. To use a System managed identity, use "system", any other identity names are interpreted as user managed identity

- Connection: Refer [Connection](#input-binding) attribute above.Note that the application id should have ingest privileges on the table being ingested into

- MappingRef: Optional attribute to pass a [mapping ref](https://learn.microsoft.com/en-us/azure/data-explorer/kusto/management/create-ingestion-mapping-command) that is already defined in the ADX cluster

- DataFormat: The default dataformat is `multijson/json`. This can be set to _**text**_ formats supported in the datasource format [enumeration](https://learn.microsoft.com/en-us/azure/data-explorer/kusto/api/netfx/kusto-ingest-client-reference#enum-datasourceformat). Samples are validated and provided for csv and JSON formats.

### **Samples**

Samples for C# are available and available at the following. This can run with local functions framework. Setup required for the run is available at the [location](samples/set-up)

- [.NET (C# in-process)](samples/samples-csharp)
- [.NET (C# dot command examples)](samples/samples-blob-ingest)
- [.NET (C# isolated)](samples/samples-outofproc)
- [Java](samples/samples-java)
- [Node](samples/samples-node)
- [Python](samples/samples-python)


## Known Issues


## Trademarks

This project may contain trademarks or logos for projects, products, or services. Authorized use of Microsoft trademarks or logos is subject to and must follow [Microsoft’s Trademark & Brand Guidelines](https://www.microsoft.com/legal/intellectualproperty/trademarks/usage/general). Use of Microsoft trademarks or logos in modified versions of this project must not cause confusion or imply Microsoft sponsorship. Any use of third-party trademarks or logos are subject to those third-party’s policies.
