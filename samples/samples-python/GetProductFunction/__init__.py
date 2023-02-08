# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.

import azure.functions as func
from Common.product import Product


def main(req: func.HttpRequest, products: str) -> func.HttpResponse:
    return func.HttpResponse(
        products,
        status_code=200,
        mimetype="application/json"
    )
