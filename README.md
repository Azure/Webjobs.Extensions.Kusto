﻿# **Kusto bindings for Azure Functions - Preview**

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

- KqlCommand: The KqlQuery that has to be executed. Can be a KQL query or a KQL Function call

- KqlParameters: Parameters that act as predicate variables for the KqlCommand. For example "@name={name},@Id={id}" where the parameters {name} and {id} will be substituted at runtime with actual values acting as predicates

- Connection: The _**name**_ of the variable that holds the connection string, resolved through environment variables or through function app settings. Defaults to lookup on the variable _**KustoConnectionString**_, at runtime this variable will be looked up against the environment.
Documentation on connection string can be found at [Kusto connection strings](https://learn.microsoft.com/en-us/azure/data-explorer/kusto/api/connection-strings/kusto)
e.g.:
"KustoConnectionString": "Data Source=https://_**cluster**_.kusto.windows.net;Database=_**Database**_;Fed=True;AppClientId=_**AppId**_;AppKey=_**AppKey**_;Authority Id=_**TenantId**_
Note that the application id should atleast have viewer privileges on the table(s)/function(s) being queried in the KqlCommand


### **Output Binding**
 
Takes row(s) and inserts them into the Kusto table (i.e. If a row doesn't already exist, it is added. If it does, it is updated).

- Database: The database against which the query has to be executed

- TableName: The table to ingest the data into

- Connection: Refer [Connection](#input-binding) attribute above.Note that the application id should have ingest privileges on the table being ingested into


### **Samples**

Samples for C# are available and available at the following. This can run with local functions framework. Set up and run samples are available at the following [location](samples/postman/samples-csharp)

- [.NET (C# in-process)](samples/samples-csharp)


## Known Issues


## Trademarks

This project may contain trademarks or logos for projects, products, or services. Authorized use of Microsoft trademarks or logos is subject to and must follow [Microsoft’s Trademark & Brand Guidelines](https://www.microsoft.com/legal/intellectualproperty/trademarks/usage/general). Use of Microsoft trademarks or logos in modified versions of this project must not cause confusion or imply Microsoft sponsorship. Any use of third-party trademarks or logos are subject to those third-party’s policies.
