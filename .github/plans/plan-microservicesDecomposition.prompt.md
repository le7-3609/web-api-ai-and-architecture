# Plan: Microservices Decomposition of web-api-shop

## TL;DR

Decompose the current monolithic .NET 9 Web API into **7 microservices + 1 API Gateway**, each owning its domain data (database-per-service). Communication uses **REST for queries** and **RabbitMQ events for side-effects** (e.g., `OrderCreated` triggers cart clearing). Small reference-data domains (Platform, SiteType, MainCategory, SubCategory, Product, BasicSite) merge into a single **Catalog Service**. A new **Auth Service** adds proper JWT authentication. Language is chosen per service based on technical fit. Deployment via **Docker Compose**.

---

## Service Map

| # | Microservice | Language | Justification |
|---|---|---|---|
| 0 | **API Gateway** | **Go** | Lightweight, fast startup, excellent concurrency model for proxying/routing — ideal gateway runtime |
| 1 | **Auth Service** | **C# / .NET 9** | Reuses existing social login code (Google/Microsoft), rich JWT + Identity ecosystem in ASP.NET, existing `User` entity logic |
| 2 | **Catalog Service** | **C# / .NET 9** | Retains existing EF Core CRUD patterns, complex pagination/filtering logic, strong typed entity relationships |
| 3 | **Cart Service** | **TypeScript / Node.js (NestJS)** | Event-driven architecture fits Node's async model, simpler service with clear boundaries, excellent RabbitMQ libraries |
| 4 | **Order Service** | **C# / .NET 9** | Complex transactional logic (price validation, multi-entity creation), existing comprehensive test coverage worth preserving |
| 5 | **Prompt Service** | **Python (FastAPI)** | Python has the strongest AI/ML ecosystem, Google GenAI SDK is Python-first, FastAPI provides async performance + auto-docs |
| 6 | **Analytics Service** | **Go** | High-throughput request logging with minimal overhead, Go's goroutines handle concurrent writes efficiently |

---

## Steps

### Step 0 — Infrastructure Setup

1. Create a root `docker-compose.yml` with services for: **RabbitMQ**, **PostgreSQL** (one instance per service DB, or one instance with separate databases), and all 7 application containers + the API Gateway.
2. Create a shared `proto/` or `contracts/` directory for **event schemas** (JSON schemas for RabbitMQ messages) to ensure cross-language type safety.
3. Define event contracts:
   - `OrderCreated` { orderId, userId, cartId } — published by Order Service
   - `UserRegistered` { userId, email } — published by Auth Service
   - `ProductPriceChanged` { productId, oldPrice, newPrice } — published by Catalog Service
   - `ProductDeleted` { productId } — published by Catalog Service

### Step 1 — API Gateway (Go)

1. Build a lightweight reverse proxy using **Go + standard `net/http/httputil`** or a library like **chi** for routing.
2. Route table mapping (from current unified API):
   - `/api/users/**`, `/api/auth/**`, `/api/password/**` → Auth Service
   - `/api/main-categories/**`, `/api/sub-categories/**`, `/api/products/**`, `/api/platforms/**`, `/api/site-types/**`, `/api/basic-sites/**` → Catalog Service
   - `/api/carts/**` → Cart Service
   - `/api/orders/**` → Order Service
   - `/api/gemini/**` → Prompt Service
   - Analytics is middleware-internal (not routed externally)
3. Add **JWT validation middleware** at the gateway level — validate tokens before forwarding. The Auth Service issues tokens; the Gateway verifies them using a shared secret/public key.
4. Add rate limiting, request logging, and CORS handling centrally here (replaces current `RatingMiddleware` for external tracking and CORS config in `Program.cs`).
5. Forward authenticated `userId` in a header (`X-User-Id`) to downstream services.

### Step 2 — Auth Service (C# / .NET 9)

**Owns DB tables:** `Users`
**Source migration from:** `UsersController`, `UserService`, `UserRepository`, `PasswordValidityService`

1. Migrate `User` entity and repository. Remove the `GetAllOrdersAsync` method from user repo — order history moves to Order Service.
2. **Add JWT token issuance**: on successful login/register, return a signed JWT containing `{ userId, email }`. Use `Microsoft.AspNetCore.Authentication.JwtBearer`.
3. Merge `PasswordValidityService` into this service (it's stateless, no need for a separate microservice).
4. Keep social login logic (Google/Microsoft JWT validation) from current `UserService`.
5. Publish `UserRegistered` event to RabbitMQ after successful registration.
6. Endpoints:
   - `POST /api/auth/register` — register + return JWT
   - `POST /api/auth/login` — login + return JWT
   - `POST /api/auth/social-login` — social login + return JWT
   - `POST /api/auth/password-strength` — password validation
   - `GET /api/users` — list users (admin)
   - `GET /api/users/{id}` — get profile
   - `PUT /api/users/{id}` — update profile
7. **Critical fix**: Hash passwords with bcrypt before storing (current code stores plaintext — direct string comparison in `UserRepository.LoginAsync`).

### Step 3 — Catalog Service (C# / .NET 9)

**Owns DB tables:** `MainCategories`, `SubCategories`, `Products`, `Platforms`, `SiteTypes`, `BasicSites`, `Statuses`
**Source migration from:** `MainCategoriesController`, `SubCategoriesController`, `ProductsController`, `PlatformsController`, `SiteTypeController`, `BasicSiteController`, and all corresponding services/repositories.

1. Combine all 6 controllers into a single ASP.NET Core project with the **same endpoint structure**.
2. Keep existing EF Core repositories and service layer — this is the most CRUD-heavy part of the system with established pagination patterns (`PaginatedResponse<T>` from `CatalogDTO.cs`).
3. Internal FK integrity is preserved (MainCategory → SubCategory → Product, SiteType → BasicSite, Platform → BasicSite).
4. Expose a **product price lookup endpoint** for Order Service's price validation: `GET /api/products/{id}/price` (lightweight, returns just `{ productId, price }`).
5. Expose **SubCategory context endpoint** for Prompt Service: `GET /api/sub-categories/{id}/prompt-context` (returns `{ name, prompt }`).
6. Publish events: `ProductPriceChanged`, `ProductDeleted` when product data changes.
7. All existing unit/integration tests for catalog repos/services migrate here.

### Step 4 — Cart Service (TypeScript / NestJS)

**Owns DB tables:** `Carts`, `CartItems` (with denormalized fields)
**Source migration from:** `CartsController`, `CartService`, `CartRepository`

1. Build with **NestJS** framework (TypeScript), using **TypeORM** or **Prisma** for data access.
2. **Denormalize** `CartItem` — store `productName`, `productPrice`, `platformName`, `promptText` directly in CartItem (no cross-service FK joins). Populate via REST call to Catalog Service when adding items.
3. Re-implement business logic from `CartService`:
   - `EnsureUserCartAsync` — auto-create cart for user
   - Duplicate product check
   - Guest cart import with skip/add logic
4. **Consume `OrderCreated` event** from RabbitMQ → clear the user's cart (replaces current direct `ICartService.ClearCartAsync` call from OrderService).
5. Consume `ProductPriceChanged` event → optionally update cached prices in active carts.
6. REST call to Catalog Service when adding item: validate product exists, fetch current price/name.
7. Endpoints (same as current):
   - `GET /api/carts/items/{id}`
   - `GET /api/carts/{cartId}/items`
   - `POST /api/carts/items`
   - `POST /api/carts/users/{userId}/items`
   - `POST /api/carts/users/{userId}/import-guest`
   - `PUT /api/carts/items`
   - `DELETE /api/carts/items/{id}`
   - `DELETE /api/carts/{cartId}/clear`

### Step 5 — Order Service (C# / .NET 9)

**Owns DB tables:** `Orders`, `OrderItems`, `Reviews`
**Source migration from:** `OrdersController`, `OrderService`, `OrderRepository`

1. Migrate order creation logic from `OrderService.AddOrderFromCartAsync`. Key change: replace direct `IProductRepository` call with a **REST call to Catalog Service** for `CalculateRealSumAsync` price validation.
2. Replace direct `ICartService.ClearCartAsync` call with publishing an **`OrderCreated` event** to RabbitMQ — Cart Service consumes this and clears the cart asynchronously.
3. **Denormalize** `OrderItem` — store `productName`, `productPrice`, `platformName`, `promptText` as snapshot values at order time (prices must not change retroactively).
4. Move user order history here: `GET /api/orders/user/{userId}` (replaces current `/api/Users/{userId}/orders`).
5. `Status` table stays in this service (only used by orders).
6. Review management stays here (1:1 with Order).
7. Preserve existing extensive test coverage from `OrderServiceUnitTests` and `OrderServiceIntegrationTests`.
8. Endpoints:
   - `GET /api/orders/{id}`
   - `GET /api/orders/user/{userId}` (moved from Users)
   - `POST /api/orders`
   - `PUT /api/orders`
   - `POST /api/orders/{orderId}/review`
   - `GET /api/orders/{orderId}/review`
   - `PUT /api/orders/{orderId}/review`
   - `GET /api/orders/{orderId}/orderItems`

### Step 6 — Prompt Service (Python / FastAPI)

**Owns DB tables:** `GeminiPrompts`
**Source migration from:** `GeminiController`, `GeminiService`, `gemini.cs`, `GeminiPromptsRepository`

1. Build with **FastAPI + SQLAlchemy** (async) + **google-genai** Python SDK.
2. Replace direct `ISubCategoryRepository` access with a **REST call to Catalog Service** (`GET /api/sub-categories/{id}/prompt-context`).
3. Port prompt composition logic from `GeminiService.AddGeminiPromptAsync` — builds request string from user input + subcategory context, calls Gemini API, stores result.
4. Use `Pydantic` models for request/response validation (equivalent to current DTOs with `DataAnnotations`).
5. Configuration: `GeminiSettings` (API key, model name) via environment variables.
6. Endpoints:
   - `POST /api/gemini/getUserProduct`
   - `GET /api/gemini/{id}`
   - `PUT /api/gemini/{id}`

### Step 7 — Analytics Service (Go)

**Owns DB tables:** `Ratings`
**Source migration from:** `RatingMiddleware`, `RatingService`, `RatingRepository`

1. Build a lightweight Go service that **consumes request log events** from RabbitMQ (published by the API Gateway on every request).
2. The API Gateway publishes a `RequestReceived` event with `{ host, method, path, referer, userAgent, timestamp }` for every incoming request.
3. Analytics Service consumes these events and bulk-inserts into its own `Ratings` table.
4. Optionally expose `GET /api/analytics/requests` for admin dashboards (not in current API but natural extension).
5. Completely decoupled — no FKs to any other service.

---

## Data Flow Diagrams for Key Scenarios

### Order Creation (most complex cross-service flow)

```
Client → API Gateway → Order Service
                            ├── REST GET → Catalog Service (validate product prices)
                            ├── Create Order + OrderItems in Order DB
                            ├── Publish "OrderCreated" event → RabbitMQ
                            └── Return OrderDetailsDTO to client
                       
RabbitMQ → Cart Service (consume "OrderCreated" → clear user's cart)
```

### Add Cart Item

```
Client → API Gateway → Cart Service
                            ├── REST GET → Catalog Service (fetch product details + price)
                            ├── Create CartItem with denormalized data
                            └── Return CartItemDTO
```

### Generate Prompt

```
Client → API Gateway → Prompt Service
                            ├── REST GET → Catalog Service (fetch subcategory prompt context)
                            ├── Compose prompt string
                            ├── Call Google GenAI API
                            ├── Store result in Prompt DB
                            └── Return GeminiPromptDTO
```

---

## Project Structure

```
web-api-shop-microservices/
├── docker-compose.yml
├── contracts/                    # Shared event schemas (JSON Schema)
│   ├── order-created.json
│   ├── user-registered.json
│   ├── product-price-changed.json
│   └── request-received.json
├── gateway/                      # Go
│   ├── main.go
│   ├── routes.go
│   ├── middleware/
│   ├── go.mod
│   └── Dockerfile
├── auth-service/                 # C# / .NET 9
│   ├── AuthService.sln
│   ├── src/
│   │   ├── AuthService.API/
│   │   ├── AuthService.Domain/
│   │   ├── AuthService.Infrastructure/
│   │   └── AuthService.Application/
│   ├── tests/
│   └── Dockerfile
├── catalog-service/              # C# / .NET 9
│   ├── CatalogService.sln
│   ├── src/
│   │   ├── CatalogService.API/
│   │   ├── CatalogService.Domain/
│   │   ├── CatalogService.Infrastructure/
│   │   └── CatalogService.Application/
│   ├── tests/
│   └── Dockerfile
├── cart-service/                  # TypeScript / NestJS
│   ├── src/
│   │   ├── cart/
│   │   ├── common/
│   │   └── main.ts
│   ├── test/
│   ├── package.json
│   ├── tsconfig.json
│   └── Dockerfile
├── order-service/                # C# / .NET 9
│   ├── OrderService.sln
│   ├── src/
│   │   ├── OrderService.API/
│   │   ├── OrderService.Domain/
│   │   ├── OrderService.Infrastructure/
│   │   └── OrderService.Application/
│   ├── tests/
│   └── Dockerfile
├── prompt-service/               # Python / FastAPI
│   ├── app/
│   │   ├── main.py
│   │   ├── models/
│   │   ├── services/
│   │   ├── repositories/
│   │   └── config.py
│   ├── tests/
│   ├── requirements.txt
│   └── Dockerfile
└── analytics-service/            # Go
    ├── main.go
    ├── consumer/
    ├── repository/
    ├── go.mod
    └── Dockerfile
```

---

## Key Architectural Decisions

| Decision | Choice | Rationale |
|---|---|---|
| Merged small domains | Platform, SiteType, MainCategory, SubCategory, Product, BasicSite, Status → **Catalog Service** | Avoids 6 tiny microservices with excessive operational overhead |
| Password Validity | Merged into **Auth Service** | Stateless utility with no reason to be separate |
| User order history | Moved to **Order Service** | Eliminates cross-aggregate read from User repo into Orders table |
| Denormalized CartItem/OrderItem | Each service stores snapshot data (product name, price) | Avoids cross-service joins; accepts eventual consistency |
| Cart clearing | Via `OrderCreated` event | Replaces synchronous `ICartService.ClearCartAsync` — decouples Order from Cart |
| Go for Gateway + Analytics | Lightweight runtime | I/O-bound proxy and high-throughput logging workloads |
| Python for Prompt | Best AI ecosystem | Google GenAI SDK is Python-first with richer features |
| TypeScript/NestJS for Cart | Clean event-driven architecture | Good RabbitMQ support; domain is simple enough to rewrite cleanly |
| C# for Auth, Catalog, Order | Preserve existing logic & tests | EF Core remains effective for relational data access |
| Password hashing | **bcrypt** in Auth Service | Critical security fix — current plaintext storage must be replaced |

---

## Verification Checklist

1. **Per-service**: Each service has its own test suite runnable in isolation (`dotnet test`, `npm test`, `pytest`, `go test`)
2. **Integration**: `docker-compose up` starts all services + RabbitMQ + databases; end-to-end tests cover:
   - Register user → login → get JWT
   - Browse catalog → add to cart → create order → verify cart cleared (via event)
   - Generate prompt → verify stored
3. **Event flow**: RabbitMQ management UI shows messages flowing for `OrderCreated` → Cart clear
4. **Gateway routing**: All existing API paths from `WebApiShop.http` work through the gateway with JWT auth headers added
