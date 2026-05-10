# ResponseResults

This sample demonstrates generated endpoint result types and response handling.

## What it demonstrates

| Feature | Operation | Where |
|---------|-----------|-------|
| `Created<T>` result | `createOrder` | `CreateOrderHandler.cs` |
| Schema-less `BadRequestProblem` | `createOrder` | `CreateOrderHandler.cs` |
| `Ok<T>` result | `getOrder` | `GetOrderHandler.cs` |
| Schema-backed `NotFoundProblem` | `getOrder` | `GetOrderHandler.cs` |
| `NoContent` result | `cancelOrder` | `CancelOrderHandler.cs` |
| Schema-less `NotFoundProblem` | `cancelOrder` | `CancelOrderHandler.cs` |

## Generated wrapper types

MinimalOpenAPI generates typed problem wrapper types from `application/problem+json` responses:

| Generated type | Status | Schema |
|----------------|--------|--------|
| `BadRequestProblem` | 400 | Schema-less (wraps `ProblemDetails`) |
| `NotFoundProblem` (in `getOrder`) | 404 | Schema-backed with `OrderId` field |
| `NotFoundProblem` (in `cancelOrder`) | 404 | Schema-less (wraps `ProblemDetails`) |

## Interesting files

| File | What to look at |
|------|----------------|
| `openapi.yaml` | Multiple `application/problem+json` responses, some with schemas |
| `CreateOrderHandler.cs` | `Results<Created<Order>, BadRequestProblem>` usage |
| `GetOrderHandler.cs` | `Results<Ok<Order>, NotFoundProblem>` with typed payload |
| `CancelOrderHandler.cs` | `Results<NoContent, NotFoundProblem>` schema-less problem |

## How to run

```shell
cd sample/ResponseResults
dotnet run
```

Then try:

```shell
# Create an order
curl -X POST http://localhost:5000/orders \
  -H "Content-Type: application/json" \
  -d '{"externalReference":"ORD-001","customerName":"Alice","amount":99.99}'

# Get an order (replace {id} with the id from create)
curl http://localhost:5000/orders/{id}

# Update order status
curl -X PUT http://localhost:5000/orders/{id} \
  -H "Content-Type: application/json" \
  -d '{"status":"confirmed"}'

# Cancel an order
curl -X DELETE http://localhost:5000/orders/{id}
```
