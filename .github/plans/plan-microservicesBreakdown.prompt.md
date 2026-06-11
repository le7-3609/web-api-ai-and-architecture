# Plan: Microservices Decomposition of WebApiShop

**TL;DR:** Decompose the monolithic ASP.NET Core API into **6 microservices + API Gateway**, using polyglot languages (Go, C#, Node.js, Python) and mixed persistence (PostgreSQL, MongoDB, Redis). Kong serves as the API Gateway, handling routing, JWT validation, CORS, rate limiting, and HTTP analytics (absorbing the current `RatingMiddleware`). Communication is synchronous REST between services, plus asynchronous Kafka events for order billing (already implemented). The biggest risk is the **order creation flow** — currently a single transaction touching 5 domains — which will become an HTTP orchestration in the Order service.

> **Last updated:** June 2026 — reflects the current monolith state including JWT authentication, rate limiting, Redis caching, Kafka event-driven billing, Docker Compose infrastructure, and PayPal payment integration.

---

## Current Monolith — What Already Exists

Before decomposition, it's critical to understand what's **already implemented** in the monolith. The microservices plan must preserve all of these capabilities:

### ✅ Authentication & Authorization (JWT) — Fully Implemented
- **JWT access + refresh token flow** via `JwtService`, `AuthService`, `AuthCookieService`
- **HttpOnly + Secure + SameSite cookies** — access token and refresh token are never exposed to JavaScript
- Refresh token cookie scoped to `/api/auth/refresh` only
- **BCrypt password hashing** (work-factor 11, automatic salting) — passwords are **never** stored in plaintext
- **Role-based authorization** — `User.Role` property, claims include `ClaimTypes.Role`
- **Social login** — Google OAuth + Microsoft OAuth token validation and user creation
- `AuthController` endpoints: `POST /login`, `POST /register`, `POST /refresh`, `POST /logout`, `GET /me`, `POST /social-login`
- `JwtMiddleware` reads `access_token` cookie on every request and populates `HttpContext.User`
- JWT config: `Issuer=WebApiShop`, `Audience=WebApiShopAngular`, access token TTL=15min, refresh token TTL=7 days

### ✅ Rate Limiting — Fully Implemented
- **Sliding window rate limiter** via `RateLimitMiddleware` (built-in `Microsoft.AspNetCore.RateLimiting`)
- **100 requests per minute** per IP address, 6 segments per window
- Custom 429 response with JSON body: `{ message, policy, retryAfterSeconds }`
- Applied globally to all controller endpoints via `RequireRateLimiting`

### ✅ Redis Caching — Products & SubCategories
- **Product caching** (`ProductCacheService`): cache-aside pattern, single product TTL=10min, product lists TTL=5min
- **SubCategory caching** (`SubCategoryCacheService`): cache-aside pattern, list TTL=5min
- **Version-based list invalidation**: `products:version` and `subcategories:version` counters — on write, version is bumped, invalidating all list cache keys
- **Graceful degradation**: all Redis errors are caught and logged — the API always falls back to the database

### ✅ Kafka Event-Driven Billing — Already Extracted
- `OrderEventPublisher` in the API publishes to the `orders` topic on order creation (`Acks.All`, idempotent producer)
- `BillingServiceConsumer` is a **standalone .NET Worker Service** (separate project/process) that consumes order events
- Manual offset commit, per-message DI scope, retry ×3 with exponential back-off
- **Dead-letter topic** (`orders.dead-letter`): poison messages forwarded with diagnostic headers (`x-failure-reason`, `x-failed-at`, etc.)
- `KafkaHealthCheck` verifies broker connectivity on `/healthz` probe
- Bill processing: creates `Bill` entity (OrderId, UserId, SiteName, Amount, ItemCount, Status)

### ✅ Docker Compose Infrastructure — 4 Containers
| Container | Image | Purpose | Local Address |
|---|---|---|---|
| `redis` | `redis:7-alpine` | Product + SubCategory read cache | `localhost:6380` |
| `kafka` | `confluentinc/cp-kafka:7.6.1` | Order event message broker (KRaft, no Zookeeper) | `localhost:9093` |
| `kafka-ui` | `provectuslabs/kafka-ui:latest` | Kafka browser UI | `http://localhost:8090` |
| `web-api-shop` | Custom Dockerfile (ASP.NET 9) | API container with full app | `http://localhost:8080` |

### ✅ Additional Features
- **PayPal integration** configured in `secrets.template.json` (ClientId, ClientSecret, sandbox URL)
- **Gemini AI integration** — prompt generation, multi-turn chatbot, prompt CRUD
- **Password strength scoring** via `PasswordValidityService`
- **Error handling middleware** — centralized exception handling with 500 responses
- **Rating/analytics middleware** — logs every HTTP request to the `Rating` table (host, method, path, referer, user-agent, timestamp)
- **nLog logging** — structured logging across all layers
- **AutoMapper** — DTO ↔ Entity mapping
- **Swagger/OpenAPI** — auto-generated API docs in development mode
- **Multi-layer test suite** — Unit tests + Integration tests in `Tests/`

---

## Architecture Overview

```
                  ┌──────────────────────────────────┐
                  │         Kong API Gateway         │
  Angular SPA ──> │  JWT validation, routing,        │
                  │  rate limit, CORS, analytics,    │
                  │  PayPal webhook routing          │
                  └──────┬──┬──┬──┬──┬──┬────────────┘
                         │  │  │  │  │  │
            ┌────────────┘  │  │  │  │  └────────────┐
            ▼               ▼  │  ▼  ▼               ▼
     ┌────────────┐  ┌───────┐ │ ┌──────────┐  ┌────────┐
     │ User/Auth  │  │Catalog│ │ │  Order   │  │   AI   │
     │   (Go)     │  │& Site │ │ │ & Review │  │ Prompt │
     │ PostgreSQL │  │ (C#)  │ │ │  (C#)    │  │(Python)│
     └────────────┘  │Postgre│ │ │PostgreSQL│  │MongoDB │
                     │+Redis │ │ └────┬─────┘  └────────┘
                     └───────┘ │      │
                               ▼      │ Kafka (async)
                        ┌────────────┐ │
                        │   Cart     │ ▼
                        │ (Node.js)  │ ┌─────────────┐
                        │  MongoDB   │ │  Billing    │
                        │  + Redis   │ │  Worker     │
                        └────────────┘ │  (.NET)     │
                                       │  Kafka      │
                                       └─────────────┘
```

---

## Service 1: User & Auth Service — **Go**

| Aspect | Details |
|---|---|
| **Language** | Go (with `gin` or `echo` framework) |
| **Database** | **PostgreSQL** — relational user records, OAuth provider/ID mappings |
| **Why Go** | Ideal for auth: fast, secure, minimal attack surface, mature JWT/OAuth libraries (`golang-jwt`, `golang.org/x/oauth2`), low memory footprint |
| **Entities** | `User` (including `Role`, `RefreshToken`, `RefreshTokenExpiry`) |
| **Current source** | `AuthService`, `AuthCookieService`, `JwtService`, `JwtSettings`, `UserService`, `UserRepository`, `PasswordValidityService`, `AuthController`, `UsersController`, `PasswordValidityController`, `JwtMiddleware` |

**Responsibilities:**
- User registration (email/password) with BCrypt hashing (already implemented — **port as-is**)
- Password strength scoring (port `PasswordValidityService` → Go `zxcvbn-go`)
- **JWT token issuance** — access token (15min) + refresh token (7 days) — **already implemented**
- **HttpOnly cookie management** — set/delete `access_token` and `refresh_token` cookies — **already implemented**
- Refresh token rotation with SHA-256 hashing (already implemented via `AuthService.HashToken`)
- Google OAuth token validation + user creation/login
- Microsoft OAuth token validation + user creation/login
- User profile CRUD
- Role management (`User`, `Admin`)
- Expose `GET /users/{userId}` for other services to resolve user existence

**Endpoints (~10):**
`POST /register`, `POST /login`, `POST /logout`, `POST /social-login`, `POST /password-strength`, `POST /token/refresh`, `GET /me`, `GET /users/{id}`, `GET /users`, `PUT /users/{id}`

**Key change:** The current `UserRepository.GetAllOrdersAsync()` queries the Orders table directly. In the microservice world, the User service will **NOT** own orders. Instead, the Angular SPA calls the Order service directly (with userId from JWT), or the User service proxies via REST to the Order service.

**DB schema (PostgreSQL):**
- `users` table: id, first_name, last_name, email, password_hash, phone, address, provider, provider_id, role, refresh_token_hash, refresh_token_expiry, last_login, created_at

---

## Service 2: Catalog & Site Config Service — **C# / .NET 9**

| Aspect | Details |
|---|---|
| **Language** | C# / ASP.NET Core 9 (reuse majority of existing code) |
| **Database** | **PostgreSQL** — hierarchical categories, relational product data, site type pricing |
| **Cache** | **Redis** — product cache-aside (single TTL=10min, list TTL=5min) + subcategory cache-aside (list TTL=5min) with version-based invalidation |
| **Why C#** | Heaviest CRUD service with complex entity relationships. Direct code reuse from the monolith. EF Core excels at these relational patterns. |
| **Entities** | `MainCategory`, `SubCategory`, `Product`, `Platform`, `SiteType`, `BasicSite` |
| **Current source** | `ProductService/Repository`, `ProductCacheService`, `MainCategoryService/Repository`, `SubCategoryService/Repository`, `SubCategoryCacheService`, `PlatformService/Repository`, `SiteTypeService/Repository`, `BasicSiteService/Repository` + all 6 controllers |

**Responsibilities:**
- Full CRUD for Products, Categories, Platforms, SiteTypes
- BasicSite creation/update (user's website project definition)
- Pricing logic: SiteType base price + Product prices + Platform context
- **Redis caching for Products**: cache-aside with `products:version` counter for list invalidation — **already implemented**
- **Redis caching for SubCategories**: cache-aside with `subcategories:version` counter for list invalidation — **already implemented**
- Graceful Redis degradation (catch all `RedisException`, fallback to DB) — **already implemented**
- Expose read APIs consumed by Cart and Order services

**Endpoints (~25):**
All current endpoints from `ProductsController`, `MainCategoriesController`, `SubCategoriesController`, `PlatformsController`, `SiteTypeController`, `BasicSiteController`

**Key changes:**
- The current `ProductRepository` directly deletes `CartItems` and checks `OrderItems` on product deletion. This becomes: Product service **publishes** a REST call or fires a check to Cart service to remove items with that productId, and checks Order service for existing order items before allowing delete.
- The current `PlatformRepository.ReassignPlatformReferencesAsync()` writes to CartItems, OrderItems, BasicSites. This becomes: Catalog service calls Cart service and Order service to reassign platform references, then updates its own BasicSites.

**DB schema (PostgreSQL):**
- `main_categories`, `sub_categories`, `products`, `platforms`, `site_types`, `basic_sites` — largely mirrors current schema, minus the FKs to Cart/Order which now live in other services.

---

## Service 3: Cart Service — **Node.js / TypeScript**

| Aspect | Details |
|---|---|
| **Language** | Node.js with TypeScript (Express or Fastify) |
| **Database** | **MongoDB** (persistent cart storage) + **Redis** (session cache, fast lookups) |
| **Why Node.js** | Cart operations are I/O-bound, document-like (add/remove/update items). Node's async model is ideal. TypeScript provides safety. |
| **Why MongoDB** | Cart is naturally a document: `{ userId, basicSiteId, items: [{productId, platformId, promptId, qty, price}] }`. No complex joins needed — just store/retrieve per user. |
| **Why Redis** | Cache active carts for sub-millisecond reads. Write-through to MongoDB for persistence. Guest carts (no userId) live in Redis with TTL expiry. |
| **Entities** | `Cart`, `CartItem` |
| **Current source** | `CartService`, `CartRepository`, `CartsController` |

**Responsibilities:**
- Auto-create cart per user (1:1 relationship)
- Cart item CRUD (add, update, remove, clear)
- Guest cart support (Redis-only, TTL-based)
- Guest cart import to authenticated user's cart
- Expose `GET /carts/{userId}` for Order service to read cart at checkout
- Expose `DELETE /carts/{cartId}/clear` for Order service to clear after order placement
- Validate product/platform existence by calling Catalog service via REST

**Endpoints (~8):**
`GET /carts/items/{id}`, `GET /carts/{cartId}/items`, `POST /carts/users/{userId}/items`, `POST /carts/users/{userId}/import-guest`, `PUT /carts/items`, `PUT /carts/{id}`, `DELETE /carts/items/{id}`, `DELETE /carts/{cartId}/clear`

**Key changes:**
- Current `CartService` injects `IBasicSiteService` to get BasicSite price. In microservices: Cart service makes a REST call to Catalog service `GET /api/BasicSite/{id}` to fetch the price.
- Cart items store `productId`, `platformId`, `promptId` as foreign references (validated via REST calls to Catalog and AI services on write).

**MongoDB document schema:**
```json
{
  "_id": "ObjectId",
  "userId": "long",
  "basicSiteId": "long | null",
  "items": [
    {
      "productId": "long",
      "platformId": "long | null",
      "promptId": "long | null",
      "quantity": "int",
      "totalPrice": "decimal",
      "productName": "string"
    }
  ],
  "updatedAt": "ISODate"
}
```

---

## Service 4: Order & Review Service — **C# / .NET 9**

| Aspect | Details |
|---|---|
| **Language** | C# / ASP.NET Core 9 |
| **Database** | **PostgreSQL** — orders are transactional, ACID-critical, need strong consistency |
| **Why C#** | Most complex business logic in the system. Order creation orchestrates multiple services. Prompt assembly uses string templates. Image upload handling. Direct reuse of `OrderService`, `OrderPromptBuilder` logic. |
| **Entities** | `Order`, `OrderItem`, `Review`, `Status` |
| **Current source** | `OrderService`, `OrderRepository`, `OrderPromptBuilder`, `OrderEventPublisher`, `StatusService`, `StatusRepository`, `ReviewService`, `ReviewRepository`, `OrdersController`, `ReviewsController`, `StatusesController` |

**Responsibilities:**
- **Order creation orchestration** (the critical flow — see below)
- Order CRUD, status management
- Order prompt assembly from template (`BasicPrompt.md`)
- **Kafka event publishing** — publish `OrderDetailsDTO` to `orders` topic on order creation — **already implemented**
- Review creation with image upload (file storage)
- Status management (lookup table)
- Expose `GET /orders?userId={id}` for the user's order history

### The Order Creation Flow (orchestrated via REST + Kafka event)

This is the most complex operation. Currently in `OrderService.AddOrderFromCartAsync`, it's a single DB transaction. In the microservice world, it becomes an **HTTP orchestration**:

1. **Read Cart** → `GET Cart Service /carts/{cartId}/items`
2. **Validate Prices** → `GET Catalog Service /products/{id}` for each product (compare prices)
3. **Read BasicSite** → `GET Catalog Service /basicSite/{id}` (for site details + price)
4. **Read Prompts** → `GET AI Service /prompts/{id}` for each cart item with a promptId
5. **Assemble Order Prompt** → Build the Markdown prompt locally using template + fetched data
6. **Create Order** → Insert Order + OrderItems in local PostgreSQL (single transaction)
7. **Publish Kafka event** → `OrderEventPublisher.PublishOrderCreatedAsync()` — triggers billing asynchronously — **already implemented**
8. **Clear Cart** → `DELETE Cart Service /carts/{cartId}/clear`

If step 8 fails after step 6 succeeds, implement a retry with idempotency key or a compensating action.

**Endpoints (~14):**
`GET /orders/{id}`, `GET /orders` (paginated), `GET /orders/statuses`, `GET /orders/{id}/items`, `GET /orders/{id}/prompt`, `GET /orders/reviews`, `GET /orders/{id}/review`, `POST /orders/carts/{cartId}`, `POST /orders/{id}/review`, `PUT /orders`, `PUT /orders/{id}/review`, `GET /statuses`, `GET /statuses/{id}`, `DELETE /orders/{id}`

**DB schema (PostgreSQL):**
- `orders`, `order_items`, `reviews`, `statuses` — mirrors current schema. `order_items` stores `product_id`, `platform_id`, `prompt_id` as reference IDs (not FKs — those entities live in other services).

---

## Service 5: Billing Worker — **.NET Worker Service** (Already Partially Extracted)

| Aspect | Details |
|---|---|
| **Language** | C# / .NET 9 Worker Service |
| **Database** | **PostgreSQL** (new — persist bills and invoices) |
| **Messaging** | **Kafka consumer** on `orders` topic, producer to `orders.dead-letter` |
| **Current source** | `BillingServiceConsumer/` — already a standalone project: `KafkaConsumerService`, `BillingService`, `Bill`, `Invoice`, `KafkaHealthCheck`, `KafkaSettings` |

> ⚠️ **Note:** The `BillingServiceConsumer` is **already extracted** as a separate .NET Worker Service with its own `Program.cs`, `csproj`, and Kafka configuration. However, it currently runs only as a local console process — it needs to be **Dockerized** and added to the `docker-compose.yml`.

**Responsibilities:**
- Consume `orders` topic events (manual offset commit, per-message DI scope) — **already implemented**
- Retry ×3 with exponential back-off — **already implemented**
- Dead-letter topic (`orders.dead-letter`) with diagnostic headers — **already implemented**
- `KafkaHealthCheck` for `/healthz` probes — **already implemented**
- Bill and Invoice entity creation and persistence (**TODO**: add PostgreSQL persistence — currently in-memory only)
- Future: payment processing integration (PayPal webhook handling)

**What needs to happen for full microservice extraction:**
1. Add a `Dockerfile` for the billing worker
2. Add the billing worker to `docker-compose.yml` with Kafka dependency
3. Add PostgreSQL database for persistent bill/invoice storage
4. Implement payment gateway integration (PayPal — credentials already configured in `secrets.template.json`)

---

## Service 6: AI & Prompt Service — **Python**

| Aspect | Details |
|---|---|
| **Language** | Python 3.12+ with **FastAPI** |
| **Database** | **MongoDB** — prompt content varies in structure (JSON `technical_value`, free-text responses), chat history is document-like |
| **Why Python** | The de facto language for AI. Google's `google-generativeai` Python SDK is the most mature and best-documented. FastAPI gives async REST with auto-generated OpenAPI docs. Natural fit for prompt engineering and chat management. |
| **Entities** | `GeminiPrompt`, chat sessions |
| **Current source** | `Gemini`, `GeminiService`, `GeminiChatService`, `ChatBotService`, `GeminiPromptsRepository`, `GeminiController`, `ChatController` |

**Responsibilities:**
- Gemini API integration (product prompt, category prompt, basicSite prompt generation)
- Prompt CRUD (create, read, update, delete)
- Multi-turn chatbot (conversation history, system instructions)
- Expose `GET /prompts/{id}` for Order service to fetch prompt content at order time

**Endpoints (~10):**
`POST /prompts/product`, `POST /prompts/subcategory`, `POST /prompts/basicsite`, `GET /prompts/{id}`, `PUT /prompts/product/{id}`, `PUT /prompts/subcategory/{id}`, `PUT /prompts/basicsite/{id}`, `DELETE /prompts/{id}`, `POST /chat`

**Key changes:**
- Current `GeminiService` injects `ISubCategoryRepository` and `IMainCategoryRepository` to fetch category context for prompt generation. In microservices: AI service calls Catalog service `GET /subcategories/{id}` and `GET /maincategories/{id}` via REST.
- Chat history currently lives in-memory. With MongoDB, it persists across service restarts. Store as documents with TTL expiry.

**MongoDB collections:**
```json
// prompts collection
{
  "_id": "ObjectId",
  "userRequestContent": "string",
  "responseContent": "string",
  "subCategoryId": "long | null",
  "technicalValue": "string (JSON)",
  "createdAt": "ISODate"
}

// chat_sessions collection
{
  "_id": "ObjectId",
  "sessionId": "string",
  "messages": [
    { "role": "user|model", "content": "string", "timestamp": "ISODate" }
  ],
  "systemInstruction": "string",
  "createdAt": "ISODate",
  "expiresAt": "ISODate"
}
```

---

## API Gateway: Kong

| Aspect | Details |
|---|---|
| **Technology** | Kong Gateway (open source) |
| **Absorbs** | `RatingMiddleware` (analytics), `RateLimitMiddleware`, `JwtMiddleware`, CORS configuration, `ErrorMiddleware` (centralized error handling) |

**Responsibilities:**
- **Route all `/api/*` requests** to the correct microservice
- **JWT validation** — validate tokens issued by the User/Auth service on every request (except login/register/social-login). Replaces the current `JwtMiddleware` + `JwtBearerEvents.OnMessageReceived` cookie extraction
- **CORS** — replaces the current `UseCors("AllowAngular")` with `WithOrigins("http://localhost:5000")`, `AllowCredentials`, `AllowAnyHeader`, `AllowAnyMethod`
- **Rate limiting** — replaces the current `RateLimitMiddleware` (sliding window, 100 req/min per IP, 6 segments). Kong's `rate-limiting` plugin provides equivalent functionality with Redis as a backing store for distributed rate limiting across multiple API instances
- **HTTP analytics** — Kong's `http-log` or `file-log` plugin replaces the current `RatingMiddleware` that logs every request (host, method, path, referer, user-agent, timestamp) to the RATING table. Logs can go to **Elasticsearch** or **ClickHouse** for dashboarding
- **Error handling** — replaces the current `ErrorMiddleware` with Kong's standardized error responses
- **Request/response transformation** as needed
- **PayPal webhook routing** — route PayPal IPN/webhook callbacks to the Billing Worker

**Routing table:**

| Route Pattern | Upstream Service |
|---|---|
| `/api/auth/**` | User & Auth (Go) :8001 |
| `/api/users/**`, `/api/passwordvalidity/**` | User & Auth (Go) :8001 |
| `/api/products/**`, `/api/maincategories/**`, `/api/subcategories/**`, `/api/platforms/**`, `/api/sitetype/**`, `/api/basicsite/**` | Catalog & Site Config (C#) :8002 |
| `/api/carts/**` | Cart (Node.js) :8003 |
| `/api/orders/**`, `/api/reviews/**`, `/api/statuses/**` | Order & Review (C#) :8004 |
| `/api/gemini/**`, `/api/chat/**` | AI & Prompt (Python) :8005 |
| `/api/billing/webhooks/**` | Billing Worker (.NET) :8006 |

**Kong plugin configuration:**

| Plugin | Config | Replaces |
|---|---|---|
| `jwt` | Cookie-based token extraction (`cookie_names=access_token`), validate against User/Auth JWKS | `JwtMiddleware` + ASP.NET `JwtBearer` |
| `rate-limiting` | 100 req/min per consumer (IP), sliding window, Redis policy store | `RateLimitMiddleware` (100/min, 6 segments) |
| `cors` | Origins: `http://localhost:5000`, credentials: true, all headers, all methods | `UseCors("AllowAngular")` |
| `http-log` | Forward request metadata (host, method, path, referer, UA, timestamp) to Elasticsearch | `RatingMiddleware` → `Rating` table |
| `request-termination` | Custom 429 response body matching current format | `RateLimitMiddleware.OnRejected` |

---

## Implementation Steps

### Step 1: Infrastructure Foundation
Create a mono-repo or multi-repo structure with Docker Compose for local dev. Extend the existing `docker-compose.yml` with: Kong, PostgreSQL (2–3 instances), MongoDB, and the 6 service containers. Keep existing redis, kafka, kafka-ui containers.

**Docker Compose additions:**
```yaml
services:
  # --- Existing (keep as-is) ---
  redis:        # Already exists — port 6380
  kafka:        # Already exists — port 9093
  kafka-ui:     # Already exists — port 8090
  
  # --- New ---
  kong:                         # API Gateway — port 8000 (proxy), 8001 (admin)
  postgres-user:                # User & Auth DB
  postgres-catalog-order:       # Shared Catalog + Order DB (or separate)
  postgres-billing:             # Billing DB
  mongodb:                      # Cart + AI Prompt DB
  
  user-auth:                    # Go — port 8001
  catalog:                      # C# — port 8002
  cart:                         # Node.js — port 8003
  order:                        # C# — port 8004
  billing-worker:               # .NET Worker (already exists as BillingServiceConsumer)
  ai-prompt:                    # Python — port 8005
```

### Step 2: Dockerize BillingServiceConsumer (Quick Win)
The `BillingServiceConsumer` is **already a standalone project**. Create a `Dockerfile` for it and add it to `docker-compose.yml` alongside the existing containers. This is the lowest-risk extraction since it's already decoupled via Kafka.

### Step 3: Extract User & Auth Service (Go)
Port `AuthService`, `AuthCookieService`, `JwtService`, `JwtSettings`, `UserService`, `UserRepository`, `PasswordValidityService` to Go. **Note:** Unlike the original plan, BCrypt and JWT are already implemented — port the existing logic, don't redesign. Port Google/Microsoft OAuth validation. Set up PostgreSQL `users` table with migrations (golang-migrate).

### Step 4: Extract Catalog & Site Config Service (C#)
Copy `ProductService`, `ProductCacheService`, `MainCategoryService`, `SubCategoryService`, `SubCategoryCacheService`, `PlatformService`, `SiteTypeService`, `BasicSiteService` and their repositories. Create a new `CatalogDbContext` with only catalog+site entities. **Keep the Redis caching layer (ProductCacheService + SubCategoryCacheService) intact** — it's already production-ready. Remove cross-domain writes (product/platform delete logic) and replace with REST calls to Cart/Order services.

### Step 5: Extract Cart Service (Node.js/TS)
Rewrite `CartService` and `CartRepository` in TypeScript. Design MongoDB document schema for carts. Set up Redis for active cart caching. Replace direct `BasicSiteService` dependency with HTTP call to Catalog service. Implement guest cart logic with Redis TTL.

### Step 6: Extract Order & Review Service (C#)
Port `OrderService`, `OrderPromptBuilder`, `OrderRepository`, `OrderEventPublisher`, `StatusService`, `StatusRepository`, `ReviewService`, `ReviewRepository`. Refactor `AddOrderFromCartAsync` into the HTTP orchestration flow (Cart → Catalog → AI → local DB → Kafka publish → clear Cart). Copy `BasicPrompt.md` template. Implement review image upload to object storage (or local filesystem). **Keep `OrderEventPublisher` for Kafka integration** — it's already production-ready.

### Step 7: Extract AI & Prompt Service (Python/FastAPI)
Rewrite `Gemini`, `GeminiService`, `GeminiChatService`, `ChatBotService` in Python. Use `google-generativeai` Python SDK. Set up MongoDB collections for prompts and chat sessions. Replace `ISubCategoryRepository`/`IMainCategoryRepository` with HTTP calls to Catalog service.

### Step 8: Configure Kong Gateway
Set up Kong with declarative config (`kong.yml`). Define services, routes, and plugins:
- `jwt` plugin — validate tokens from User/Auth, extract from `access_token` cookie (matching current `JwtMiddleware` behavior)
- `cors` plugin — matching current `AllowAngular` policy (origin: `http://localhost:5000`, credentials: true)
- `rate-limiting` plugin — 100 req/min per IP with Redis backing (matching current `RateLimitMiddleware`)
- `http-log` plugin — replaces `RatingMiddleware` → log to Elasticsearch
- Configure upstream health checks

### Step 9: Inter-Service Communication
Create HTTP client wrappers in each service for calling other services. Add retry logic with exponential backoff. Add circuit breaker pattern (e.g., Polly in C#, `opossum` in Node.js, `tenacity` in Python). Propagate JWT tokens in service-to-service calls via `Authorization` header.

### Step 10: Data Migration
Write migration scripts to split the monolithic SQL Server database:
- Export `Users` (including `Role`, `RefreshToken`, `RefreshTokenExpiry`) → User service PostgreSQL
- Export `Products/Categories/Platforms/SiteTypes/BasicSites` → Catalog PostgreSQL
- Export `Carts/CartItems` → Cart MongoDB
- Export `Orders/OrderItems/Reviews/Statuses` → Order PostgreSQL
- Export `GeminiPrompts` → AI MongoDB
- Export `Bills/Invoices` → Billing PostgreSQL (new persistence layer)

### Step 11: Testing
Each service gets its own test suite. Port relevant unit tests from `Tests/UnitTests/` to each service's language/framework. Add integration tests per service. Add **contract tests** (e.g., Pact) between services to validate API contracts. Add end-to-end tests for the order creation flow (including Kafka event publishing and billing consumption).

---

## Cross-Domain Dependency Map

```
                   ┌──────────────┐
         ┌─────────│  Catalog     │───────────┐
         │         │ (Products,   │           │
         │         │  Categories) │           │
         │         │  + Redis     │           │
         │         └──────┬───────┘           │
         │                │                   │
    references        references          references
         │                │                   │
         ▼                ▼                   ▼
  ┌──────────┐    ┌──────────────┐    ┌──────────┐
  │  Cart    │───▶│ Site Config  │◀──│  Orders  │
  │          │    │ (BasicSite,  │    │ (Order,  │
  │          │    │  SiteType,   │    │  Review) │
  │          │    │  Platform)   │    │    │     │
  └────┬─────┘    └──────────────┘    └────┼─────┘
       │                                   │
       │          ┌──────────────┐         │ Kafka event
       └─────────▶│  AI/Gemini   │◀───────┘      │
                  │ (Prompts)    │               ▼
                  └──────────────┘       ┌──────────────┐
                         ▲               │   Billing    │
                         │               │   Worker     │
                  ┌──────────────┐       │ (.NET/Kafka) │
                  │  Catalog     │       └──────────────┘
                  │ (SubCategory,│
                  │  MainCategory│
                  └──────────────┘

  ┌──────────┐                     ┌──────────────┐
  │  Users   │─── owns ──────────▶│    Cart       │
  │  & Auth  │─── owns ──────────▶│    Orders     │
  └──────────┘                     └──────────────┘

  ┌──────────┐
  │ Rating   │  (absorbed into Kong API Gateway — http-log plugin)
  └──────────┘
```

### Specific Cross-Domain REST Calls Required

| Caller Service | Calls | Endpoint | Purpose |
|---|---|---|---|
| Cart | Catalog | `GET /api/BasicSite/{id}` | Get BasicSite price |
| Cart | Catalog | `GET /api/Products/{id}` | Validate product exists |
| Order | Cart | `GET /api/Carts/{cartId}/items` | Read cart for checkout |
| Order | Cart | `DELETE /api/Carts/{cartId}/clear` | Clear cart after order |
| Order | Catalog | `GET /api/Products/{id}` | Validate product prices |
| Order | Catalog | `GET /api/BasicSite/{id}` | Get site details for prompt |
| Order | AI | `GET /api/Gemini/{id}` | Fetch prompt content |
| AI | Catalog | `GET /api/SubCategories/{id}` | Category context for prompt gen |
| AI | Catalog | `GET /api/MainCategories/{id}` | Category context for prompt gen |
| Catalog | Cart | `DELETE /api/Carts/items?productId={id}` | Clean up cart items on product delete |
| Catalog | Order | `GET /api/Orders/items?productId={id}` | Check if product is in any order |

### Async Communication (Kafka)

| Publisher | Topic | Consumer | Purpose |
|---|---|---|---|
| Order Service | `orders` | Billing Worker | Bill/invoice generation on order creation |
| Billing Worker | `orders.dead-letter` | — (monitoring) | Poison messages with diagnostic headers |

---

## Security Architecture Summary

The following security features are **already implemented** in the monolith and must be preserved or enhanced in the microservice architecture:

| Feature | Current Implementation | Microservice Target |
|---|---|---|
| Password hashing | BCrypt (work-factor 11, auto-salt) | Port to Go (golang `bcrypt` package) |
| JWT access tokens | HMAC-SHA256, 15min TTL, claims: userId, email, role | User/Auth service issues tokens; Kong validates |
| JWT refresh tokens | SHA-256 hashed, stored in DB, 7-day TTL | User/Auth service manages rotation |
| Token transport | HttpOnly + Secure + SameSite cookies | Kong `jwt` plugin extracts from cookie |
| CORS | Origin whitelist (`http://localhost:5000`), credentials | Kong `cors` plugin |
| Rate limiting | Sliding window, 100/min per IP, 6 segments | Kong `rate-limiting` plugin with Redis |
| Secret management | .NET User Secrets (local), env vars (production) | Per-service secret injection via Docker/K8s |
| Input validation | DTO records, model validation | Per-service validation middleware |
| SQL injection prevention | EF Core parameterized queries | ORM per service (EF Core, Mongoose, etc.) |

---

## Verification Checklist

- [ ] **Per-service:** Each service builds and runs independently via Docker, passes its own unit + integration tests
- [ ] **BillingServiceConsumer:** Dockerized and added to `docker-compose.yml` (currently runs as local console only)
- [ ] **Redis caching:** Product + SubCategory cache-aside patterns preserved in Catalog service with version-based invalidation
- [ ] **Integration:** `docker-compose up` starts all services + Kong; Swagger/OpenAPI docs accessible per service
- [ ] **Auth flow:** Register → login (JWT cookies set) → access protected endpoints → refresh token → logout — all through Kong with cookie-based JWT extraction
- [ ] **End-to-end:** Register user → login (get JWT) → browse catalog (cached via Redis) → add to cart → place order → Kafka event → billing worker processes → verify order prompt assembly → submit review — all through Kong
- [ ] **Contract tests:** Pact or similar verifies that Catalog service responses match what Cart/Order services expect
- [ ] **Rate limiting:** Verify 429 responses after 100 requests within 1 minute, with correct JSON body and `retryAfterSeconds`
- [ ] **Load test:** Verify order creation latency is acceptable with the multi-service REST orchestration (target < 2s)

---

## Key Decisions Made

| Decision | Choice | Rationale |
|---|---|---|
| Service count | 6 (not 5) | Added Billing Worker as standalone service — it's already extracted as `BillingServiceConsumer` with its own project, Kafka consumer, and health check. Just needs Dockerization. |
| Communication | REST (synchronous) + Kafka (async billing) | REST for request-path calls. Kafka for fire-and-forget billing — **already implemented** with idempotent producer, dead-letter topic, and retry logic. |
| Go for Auth | `gin` + `golang-jwt` + `golang-bcrypt` | Highest performance for token validation, smallest container, secure by default. Auth logic is already mature (JWT + BCrypt + refresh rotation + social login) — port existing patterns. |
| Python for AI | FastAPI + `google-generativeai` | Best Gemini SDK, natural for prompt engineering, async REST. |
| Node.js for Cart | Fastify/Express + TypeScript | I/O-bound CRUD on document-like data — Node's sweet spot. |
| C# for Catalog + Order | ASP.NET Core 9 + EF Core | Bulk code reuse from monolith, EF Core for relational data, Redis caching already implemented. |
| MongoDB for Cart + AI | Document store | Cart is a natural document; prompts have variable structure; chat is session-based. |
| PostgreSQL (not SQL Server) | Open source relational | No licensing cost, Docker-friendly, equivalent features. |
| Kong for Gateway | Plugin-based routing | Battle-tested, rich plugin ecosystem. Absorbs existing middleware: JWT validation, rate limiting (100/min sliding window), CORS, analytics (rating), error handling. |
| Redis for rate limiting | Distributed rate limiter via Kong | Current in-memory rate limiter won't work with multiple API instances. Kong + Redis provides distributed rate limiting out of the box. |
| Keep existing Kafka patterns | Reuse `OrderEventPublisher` + `KafkaConsumerService` | Already production-quality: idempotent producer, manual offset commit, exponential back-off, dead-letter with diagnostic headers. Don't redesign. |
