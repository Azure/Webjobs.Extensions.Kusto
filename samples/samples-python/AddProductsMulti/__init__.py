# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.

import json
from datetime import datetime

import azure.functions as func
from Common.product import Product


def main(req: func.HttpRequest, product: func.Out[str],productchangelog: func.Out[str]) -> func.HttpResponse:
    body = str(req.get_body(),'UTF-8')
    # parse x:
    product.set(body)
    id = json.loads(body)["ProductID"]

    changelog = {
        "ProductID": id,
        "CreatedAt": datetime.now().isoformat(),
    }
    productchangelog.set(json.dumps(changelog))
    return func.HttpResponse(
        body=body,
        status_code=201,
        mimetype="application/json"
    )
