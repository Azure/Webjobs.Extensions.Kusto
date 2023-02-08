# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.

import json
import azure.functions as func

def main(req: func.HttpRequest, products: str, productWithRename: func.Out[str]) -> func.HttpResponse:
    listOfProducts = json.loads(products)
    for product in listOfProducts:
        product['Name'] = 'py-get-set-' + str(product['ProductID'])
    productWithRename.set(json.dumps(listOfProducts))
    return func.HttpResponse(
        json.dumps(listOfProducts),
        status_code=200,
        mimetype="application/json"
    )