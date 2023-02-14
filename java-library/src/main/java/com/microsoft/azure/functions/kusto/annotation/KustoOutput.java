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
@Target({ElementType.PARAMETER, ElementType.METHOD})
@CustomBinding(direction = "out", name = "", type = "Kusto")
public @interface KustoOutput {
    // The database that contains the table to ingest
    String database();

    // The table to which data has to be ingested
    String tableName();

    // The mapping reference for the input data
    String mappingRef() default "";

    // The data format that is supported. Formats currently supported are CSV and JSON
    String dataFormat() default "";

    // The connection string name that is used for referencing the Kusto connection string
    String connection();

    // the name used in the function.json
    String name();

    // An option to set the ManagedServiceIdentity option. If set to "system" will use SystemManagedIdentity
    // else use UserManagedIdentity
    String managedServiceIdentity();
}
