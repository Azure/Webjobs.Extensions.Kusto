// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

// Insert the product, which will insert it into the Products table.
module.exports = async function (context, req) {
    // Note that this expects the body to be a JSON object or array of objects which have a property
    // matching each of the columns in the table to insert to.
    context.bindings.product = req.body;

    return {
        status: 201,
        body: req.body
    };
}