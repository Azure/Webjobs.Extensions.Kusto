# **Kusto output bindings for Azure Functions - Preview**

## **Introduction**

This document explains the usage of the output bindings that are suppported on  Azure functions for Kusto. Output bindings are used to ingest data to Kusto.



### **Output Binding**
 
Takes row(s) and inserts them into the Kusto table .

- Database: The database against which the query has to be executed

- TableName: The table to ingest the data into

- ManagedServiceIdentity: A managed identity can be used to connect to Kusto. To use a System managed identity, use "system", any other identity names are interpreted as user managed identity

- Connection: Refer [Connection](#input-binding) attribute above.Note that the application id should have ingest privileges on the table being ingested into

- MappingRef: Optional attribute to pass a [mapping ref](https://learn.microsoft.com/en-us/azure/data-explorer/kusto/management/create-ingestion-mapping-command) that is already defined in the ADX cluster

- DataFormat: The default dataformat is `multijson/json`. This can be set to _**text**_ formats supported in the datasource format [enumeration](https://learn.microsoft.com/en-us/azure/data-explorer/kusto/api/netfx/kusto-ingest-client-reference#enum-datasourceformat). Samples are validated and provided for csv and JSON formats.


## Trademarks

This project may contain trademarks or logos for projects, products, or services. Authorized use of Microsoft trademarks or logos is subject to and must follow [Microsoft’s Trademark & Brand Guidelines](https://www.microsoft.com/legal/intellectualproperty/trademarks/usage/general). Use of Microsoft trademarks or logos in modified versions of this project must not cause confusion or imply Microsoft sponsorship. Any use of third-party trademarks or logos are subject to those third-party’s policies.
