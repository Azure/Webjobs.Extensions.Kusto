# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.

import azure.functions as func
from Common.product import Product


def main(req: func.HttpRequest, product: func.Out[str]) -> func.HttpResponse:
    body = str(req.get_body(),'UTF-8')
    product.set(body)
    return func.HttpResponse(
        body=body,
        status_code=201,
        mimetype="application/json"
    )
