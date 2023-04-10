// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

// Insert the product, which will insert it into the Products table and ProductsChangeLog table.
module.exports = async function (context, req) {
    context.log('JavaScript HTTP trigger and SQL output binding function processed a request.');
    context.log(req.body);

    if (req.body) {
        var changeLog = {ProductID:req.body.ProductID, CreatedAt: new Date().toISOString()};
        context.bindings.product = req.body;
        context.bindings.productchangelog = changeLog;
        context.res = {
            body: req.body,
            mimetype: "application/json",
            status: 201
        }
    } else {
        context.res = {
            status: 400,
            body: "Error reading request body"
        }
    }
}
