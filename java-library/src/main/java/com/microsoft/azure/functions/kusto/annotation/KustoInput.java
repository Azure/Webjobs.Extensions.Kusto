/**
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License. See License.txt in the project root for
 * license information.
 */

package com.microsoft.azure.functions.kusto.annotation;

import com.microsoft.azure.functions.annotation.CustomBinding;

import java.lang.annotation.ElementType;
import java.lang.annotation.Retention;
import java.lang.annotation.RetentionPolicy;
import java.lang.annotation.Target;

@Retention(RetentionPolicy.RUNTIME)
@Target(ElementType.PARAMETER)
@CustomBinding(direction = "in", name = "", type = "Kusto")
public @interface KustoInput {
    // The database that contains the table to ingest
    String database();

    // The connection string name that is used for resolving connection to Kusto
    String connection();

    // the name used in the function.json
    String name();

    // The KQL Command that is to be used for the query
    String kqlCommand();

    // The KQL Query parameters
    String kqlParameters();

    // An option to set the ManagedServiceIdentity option. If set to "system" will use SystemManagedIdentity
    // else use UserManagedIdentity
    String managedServiceIdentity() default "";

    // An option to set the ClientRequestProperties.
    // Refer https://learn.microsoft.com/en-us/azure/data-explorer/kusto/api/netfx/request-properties#clientrequestproperties for details on properties    
    String clientRequestProperties() default "";

}
