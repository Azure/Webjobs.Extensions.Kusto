// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

module.exports = async function (context, req) {
    const newProduct = {
        "ProductID": req.query?.productId,
        "Name": req.query?.name,
        "Cost": req.query?.cost
    };

    context.bindings.productparams = JSON.stringify(newProduct);

    return {
        status: 201,
        body: context.bindings.productparams
    };
}