// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

/**
    Get a set of products and update the value and send it back in
 */
module.exports = async function (context, req, productget) {
    productget.forEach((product) => product.Name = "js-get-set" + product["ProductID"]);
    context.bindings.productset = productget;
    return {
        status: 201,
        body: productget
    };
}