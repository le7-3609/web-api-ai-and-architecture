# Plan: Microservices Decomposition of WebApiShop

**TL;DR:** Decompose the monolithic ASP.NET Core API into **5 microservices + API Gateway**, using polyglot languages (Go, C#, Node.js, Python) and mixed persistence (PostgreSQL, MongoDB, Redis). Kong serves as the API Gateway, handling routing, JWT auth, CORS, rate limiting, and HTTP analytics (absorbing the current `RatingMiddleware`). Communication is synchronous REST between services. The biggest risk is the **order creation flow** — currently a single transaction touching 5 domains — which will become an HTTP orchestration in the Order service.

---

## Architecture Overview

```
                  ┌─────────────────────────────┐
                  │        Kong API Gateway     │
  Angular SPA ──> │  JWT validation, routing,   │
                  │  rate limit, CORS, analytics│
                  └──────┬──┬──┬──┬──┬──────────┘
                         │  │  │  │  │
            ┌────────────┘  │  │  │  └────────────┐
            ▼               ▼  │  ▼               ▼
     ┌────────────┐  ┌───────┐ │ ┌──────────┐  ┌────────┐
     │ User/Auth  │  │Catalog│ │ │  Order   │  │   AI   │
     │   (Go)     │  │& Site │ │ │ & Review │  │ Prompt │
     │ PostgreSQL │  │ (C#)  │ │ │  (C#)    │  │(Python)│
     └────────────┘  │Postgre│ │ │PostgreSQL│  │MongoDB │
                     └───────┘ │ └──────────┘  └────────┘
                               ▼
                        ┌────────────┐
                        │   Cart     │
                        │ (Node.js)  │
                        │  MongoDB   │
                        │  + Redis   │
                        └────────────┘
```

---

## Service 1: User & Auth Service — **Go**

| Aspect | Details |
|---|---|
| **Language** | Go (with `gin` or `echo` framework) |
| **Database** | **PostgreSQL** — relational user records, OAuth provider/ID mappings |
| **Why Go** | Ideal for auth: fast, secure, minimal attack surface, mature JWT/OAuth libraries (`golang-jwt`, `golang.org/x/oauth2`), low memory footprint |
| **Entities** | `User` |
| **Current source** | `UserService`, `UserRepository`, `PasswordValidityService`, `UsersController`, `PasswordValidityController` |

**Responsibilities:**
- User registration (email/password) with password strength scoring (port `zxcvbn` → Go `zxcvbn-go`)
- Local login + **JWT token issuance** (new — the monolith has no real auth)
- Google OAuth token validation + user creation/login
- Microsoft OAuth token validation + user creation/login
- User profile CRUD
- Expose `GET /users/{userId}` for other services to resolve user existence

**Endpoints (~8):**
`POST /register`, `POST /login`, `POST /social-login`, `POST /password-strength`, `GET /users/{id}`, `GET /users`, `PUT /users/{id}`, `POST /token/refresh`

**Key change:** The current `UserRepository.GetAllOrdersAsync()` queries the Orders table directly. In the microservice world, the User service will **NOT** own orders. Instead, the Angular SPA calls the Order service directly (with userId from JWT), or the User service proxies via REST to the Order service.

**DB schema (PostgreSQL):**
- `users` table: id, first_name, last_name, email, password_hash (upgrade from plaintext!), phone, address, provider, provider_id, last_login, created_at

---

## Service 2: Catalog & Site Config Service — **C# / .NET 9**

| Aspect | Details |
|---|---|
| **Language** | C# / ASP.NET Core 9 (reuse majority of existing code) |
| **Database** | **PostgreSQL** — hierarchical categories, relational product data, site type pricing |
| **Why C#** | Heaviest CRUD service with complex entity relationships. Direct code reuse from the monolith. EF Core excels at these relational patterns. |
| **Entities** | `MainCategory`, `SubCategory`, `Product`, `Platform`, `SiteType`, `BasicSite` |
| **Current source** | `ProductService/Repository`, `MainCategoryService/Repository`, `SubCategoryService/Repository`, `PlatformService/Repository`, `SiteTypeService/Repository`, `BasicSiteService/Repository` + all 6 controllers |

**Responsibilities:**
- Full CRUD for Products, Categories, Platforms, SiteTypes
- BasicSite creation/update (user's website project definition)
- Pricing logic: SiteType base price + Product prices + Platform context
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
| **Current source** | `OrderService`, `OrderRepository`, `OrderPromptBuilder`, `StatusService`, `StatusRepository`, `OrdersController`, `StatusesController` |

**Responsibilities:**
- **Order creation orchestration** (the critical flow — see below)
- Order CRUD, status management
- Order prompt assembly from template (`BasicPrompt.md`)
- Review creation with image upload (file storage)
- Status management (lookup table)
- Expose `GET /orders?userId={id}` for the user's order history

### The Order Creation Flow (orchestrated via REST)

This is the most complex operation. Currently in `OrderService.AddOrderFromCartAsync`, it's a single DB transaction. In the microservice world, it becomes an **HTTP orchestration**:

1. **Read Cart** → `GET Cart Service /carts/{cartId}/items`
2. **Validate Prices** → `GET Catalog Service /products/{id}` for each product (compare prices)
3. **Read BasicSite** → `GET Catalog Service /basicSite/{id}` (for site details + price)
4. **Read Prompts** → `GET AI Service /prompts/{id}` for each cart item with a promptId
5. **Assemble Order Prompt** → Build the Markdown prompt locally using template + fetched data
6. **Create Order** → Insert Order + OrderItems in local PostgreSQL (single transaction)
7. **Clear Cart** → `DELETE Cart Service /carts/{cartId}/clear`

If step 7 fails after step 6 succeeds, implement a retry with idempotency key or a compensating action.

**Endpoints (~12):**
`GET /orders/{id}`, `GET /orders` (paginated), `GET /orders/statuses`, `GET /orders/{id}/items`, `GET /orders/{id}/prompt`, `GET /orders/reviews`, `GET /orders/{id}/review`, `POST /orders/carts/{cartId}`, `POST /orders/{id}/review`, `PUT /orders`, `PUT /orders/{id}/review`, `GET /statuses`, `GET /statuses/{id}`

**DB schema (PostgreSQL):**
- `orders`, `order_items`, `reviews`, `statuses` — mirrors current schema. `order_items` stores `product_id`, `platform_id`, `prompt_id` as reference IDs (not FKs — those entities live in other services).

---

## Service 5: AI & Prompt Service — **Python**

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
| **Absorbs** | Analytics/Rating (replaces `RatingMiddleware`) |

**Responsibilities:**
- **Route all `/api/*` requests** to the correct microservice
- **JWT validation** — validate tokens issued by the User/Auth service on every request (except login/register)
- **CORS** — replaces the current `UseCors("AllowAngular")` in Program.cs
- **Rate limiting** — protect services from abuse
- **HTTP analytics** — Kong's `http-log` or `file-log` plugin replaces the current `RatingMiddleware` that logs every request to the RATING table. Logs can go to **Elasticsearch** or **ClickHouse** for dashboarding.
- **Request/response transformation** as needed

**Routing table:**

| Route Pattern | Upstream Service |
|---|---|
| `/api/users/**`, `/api/passwordvalidity/**` | User & Auth (Go) :8001 |
| `/api/products/**`, `/api/maincategories/**`, `/api/subcategories/**`, `/api/platforms/**`, `/api/sitetype/**`, `/api/basicsite/**` | Catalog & Site Config (C#) :8002 |
| `/api/carts/**` | Cart (Node.js) :8003 |
| `/api/orders/**`, `/api/statuses/**` | Order & Review (C#) :8004 |
| `/api/gemini/**`, `/api/chat/**` | AI & Prompt (Python) :8005 |

---

## Implementation Steps

### Step 1: Infrastructure Foundation
Create a mono-repo or multi-repo structure with Docker Compose for local dev. Define a `docker-compose.yml` with: Kong, PostgreSQL (2 instances for User and Catalog+Order can share, or 3 separate), MongoDB, Redis, and the 5 service containers.

### Step 2: Extract User & Auth Service (Go)
Port `UserService`, `UserRepository`, `PasswordValidityService` to Go. Implement JWT issuance (access + refresh tokens). **Upgrade password storage from plaintext to bcrypt.** Port Google/Microsoft OAuth validation. Set up PostgreSQL `users` table with migrations (golang-migrate).

### Step 3: Extract Catalog & Site Config Service (C#)
Copy `ProductService`, `MainCategoryService`, `SubCategoryService`, `PlatformService`, `SiteTypeService`, `BasicSiteService` and their repositories. Create a new `CatalogDbContext` with only catalog+site entities. Remove cross-domain writes (product/platform delete logic) and replace with REST calls to Cart/Order services.

### Step 4: Extract Cart Service (Node.js/TS)
Rewrite `CartService` and `CartRepository` in TypeScript. Design MongoDB document schema for carts. Set up Redis for active cart caching. Replace direct `BasicSiteService` dependency with HTTP call to Catalog service. Implement guest cart logic with Redis TTL.

### Step 5: Extract Order & Review Service (C#)
Port `OrderService`, `OrderPromptBuilder`, `OrderRepository`, `StatusService`, `StatusRepository`. Refactor `AddOrderFromCartAsync` into the HTTP orchestration flow (Cart → Catalog → AI → local DB → clear Cart). Copy `BasicPrompt.md` template. Implement review image upload to object storage (or local filesystem).

### Step 6: Extract AI & Prompt Service (Python/FastAPI)
Rewrite `Gemini`, `GeminiService`, `GeminiChatService`, `ChatBotService` in Python. Use `google-generativeai` Python SDK. Set up MongoDB collections for prompts and chat sessions. Replace `ISubCategoryRepository`/`IMainCategoryRepository` with HTTP calls to Catalog service.

### Step 7: Configure Kong Gateway
Set up Kong with declarative config (`kong.yml`). Define services, routes, and plugins: `jwt` plugin (validate tokens from User/Auth), `cors` plugin, `rate-limiting` plugin, `http-log` plugin (replaces RatingMiddleware → send to Elasticsearch). Configure upstream health checks.

### Step 8: Inter-Service Communication
Create HTTP client wrappers in each service for calling other services. Add retry logic with exponential backoff. Add circuit breaker pattern (e.g., Polly in C#, `opossum` in Node.js, `tenacity` in Python). Propagate JWT tokens in service-to-service calls via `Authorization` header.

### Step 9: Data Migration
Write migration scripts to split the monolithic SQL Server database: export `Users` → User service PostgreSQL, export `Products/Categories/Platforms/SiteTypes/BasicSites` → Catalog PostgreSQL, export `Carts/CartItems` → Cart MongoDB, export `Orders/OrderItems/Reviews/Statuses` → Order PostgreSQL, export `GeminiPrompts` → AI MongoDB.

### Step 10: Testing
Each service gets its own test suite. Port relevant unit tests from `Tests/UnitTests/` to each service's language/framework. Add integration tests per service. Add **contract tests** (e.g., Pact) between services to validate API contracts. Add end-to-end tests for the order creation flow.

---

## Cross-Domain Dependency Map

```
                   ┌──────────────┐
         ┌─────────│  Catalog     │───────────┐
         │         │ (Products,   │           │
         │         │  Categories) │           │
         │         └──────┬───────┘           │
         │                │                   │
    references        references          references
         │                │                   │
         ▼                ▼                   ▼
  ┌──────────┐    ┌──────────────┐    ┌──────────┐
  │  Cart    │───▶│ Site Config  │◀──│  Orders  │
  │          │    │ (BasicSite,  │    │ (Order,  │
  │          │    │  SiteType,   │    │  Review) │
  │          │    │  Platform)   │    │          │
  └────┬─────┘    └──────────────┘    └────┬─────┘
       │                                   │
       │          ┌──────────────┐         │
       └─────────▶│  AI/Gemini   │◀───────┘
                  │ (Prompts)    │
                  └──────────────┘
                         ▲
                         │
                  ┌──────────────┐
                  │  Catalog     │
                  │ (SubCategory,│
                  │  MainCategory│
                  └──────────────┘

  ┌──────────┐                     ┌──────────────┐
  │  Users   │─── owns ──────────▶│    Cart       │
  │  & Auth  │─── owns ──────────▶│    Orders     │
  └──────────┘                     └──────────────┘

  ┌──────────┐
  │ Rating   │  (absorbed into Kong API Gateway)
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

---

## Verification Checklist

- [ ] **Per-service:** Each service builds and runs independently via Docker, passes its own unit + integration tests
- [ ] **Integration:** `docker-compose up` starts all services + Kong; Swagger/OpenAPI docs accessible per service
- [ ] **End-to-end:** Register user → login (get JWT) → browse catalog → add to cart → place order → verify order prompt assembly → submit review — all through Kong
- [ ] **Contract tests:** Pact or similar verifies that Catalog service responses match what Cart/Order services expect
- [ ] **Load test:** Verify order creation latency is acceptable with the multi-service REST orchestration (target < 2s)

---

## Key Decisions Made

| Decision | Choice | Rationale |
|---|---|---|
| Service count | 5 (not 7) | Merged SiteConfig into Catalog (shared `Platform`/`BasicSite` coupling). Absorbed Analytics into Kong. |
| Communication | REST (synchronous) | Simpler to implement and debug. Order flow is HTTP orchestration with retries. |
| Go for Auth | `gin` + `golang-jwt` | Highest performance for token validation, smallest container, secure by default. |
| Python for AI | FastAPI + `google-generativeai` | Best Gemini SDK, natural for prompt engineering, async REST. |
| Node.js for Cart | Fastify/Express + TypeScript | I/O-bound CRUD on document-like data — Node's sweet spot. |
| C# for Catalog + Order | ASP.NET Core 9 + EF Core | Bulk code reuse from monolith, EF Core for relational data. |
| MongoDB for Cart + AI | Document store | Cart is a natural document; prompts have variable structure; chat is session-based. |
| PostgreSQL (not SQL Server) | Open source relational | No licensing cost, Docker-friendly, equivalent features. |
| Kong for Gateway | Plugin-based routing | Battle-tested, rich plugin ecosystem (JWT, CORS, rate-limit, logging). |
| Password storage | Upgrade to bcrypt | Current plaintext storage is a critical security risk. |
