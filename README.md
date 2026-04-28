Developed by **Elisheva Ashlag**, as part of *Web API Course*  

# 🏪 Server Side– Store Project

Welcome to the backend repository of the **AI-Driven Website Builder Prompt Store**. This project provides a robust API that empowers users to design websites visually and generate precise, professional AI prompts to bring them to life.

---

## 🚀 Overview
Our platform bridges the gap between imagination and technical execution. Instead of struggling with complex AI prompting, users select their site’s identity, visual components, and features. The server then processes these choices using advanced logic to deliver a perfect technical prompt ready for any AI code generator.

### Key Business Features:
* **Visual Building:** Select site type, design, and plugins via a simple UI.
* **Prompt Generator:** Instant generation of professional, high-fidelity prompts.
* **Developer-Grade Quality:** Ensures clean, efficient output from AI tools.

---

## 🛠 Technical Architecture
The server is built using **ASP.NET Core 9 (Web API)** following modern software engineering principles to ensure scalability and maintainability.

### Core Technologies
* **Language:** C#
* **Framework:** .NET 9 (REST API)
* **ORM:** Entity Framework Core (Database-First approach)
* **Caching:** Redis (via Docker Compose) + StackExchange.Redis
* **Authentication:** JWT (access + refresh tokens) via `Microsoft.IdentityModel.JsonWebTokens`
* **Logging:** nLog
* **Mapping:** AutoMapper

### Structural Patterns
* **3-Layer Architecture:**
    * **Application Layer:** Handles API controllers and request routing.
    * **Service Layer:** Contains business logic and prompt generation algorithms.
    * **Repository Layer:** Manages data persistence and database interaction.
* **Dependency Injection:** Used extensively to achieve **Decoupling** between layers.
* **Asynchronous Programming:** All I/O and database operations are `async/await` based to maximize scalability and thread efficiency.
* **Data Transfer Objects (DTO):** Implemented using **C# Records** for immutable, concise data handling. This prevents circular dependencies and separates internal entities from API contracts.
* **Configuration:** Managed externally via `appsettings.json` for environment flexibility.
* **Security:** JWT secret stored in .NET User Secrets (never committed to source control).

### Authentication & Authorization (JWT)
Authentication is handled via a stateless JWT flow with HttpOnly cookies to prevent XSS:

| Endpoint | Description |
| :--- | :--- |
| `POST /api/Auth/register` | Register a new user; returns access + refresh tokens as HttpOnly cookies |
| `POST /api/Auth/login` | Authenticate with email/password; sets auth cookies |
| `POST /api/Auth/refresh` | Issue a new access token using the refresh token cookie |
| `POST /api/Auth/logout` | Revoke refresh token and clear auth cookies |
| `POST /api/Auth/social-login` | Authenticate via a third-party provider (e.g. Google) |

**Token transport:** Both tokens are set as `HttpOnly + Secure + SameSite=Strict` cookies — never exposed to JavaScript. The refresh token cookie is scoped to `/api/auth/refresh` only.

**Middleware:** `JwtMiddleware` runs on every request, reads the `access_token` cookie, validates it via `IJwtService`, and populates `HttpContext.User` so `[Authorize]` works transparently across all controllers.

**Secret management:** `Jwt:SecretKey` is stored in .NET User Secrets locally and must be provided via environment variable in production. A `secrets.template.json` is included as a reference.

### Caching Strategy (Products)
Product reads are served through a **cache-aside** pattern backed by Redis:

| Operation | Cache behavior |
| :--- | :--- |
| `GET /api/Products/{id}` | Read from `product:{id}` key; on miss, fetch from DB and populate cache (TTL 10 min) |
| `GET /api/Products` | Read from a versioned list key `products:v{n}:{params}`; on miss, fetch and cache (TTL 5 min) |
| `POST /api/Products` | Write to DB, then increment `products:version` counter — all old list keys become unreachable and expire by TTL |
| `PUT /api/Products/{id}` | Write to DB, then delete `product:{id}` and increment version counter |
| `DELETE /api/Products/{id}` | Write to DB, then delete `product:{id}` and increment version counter |

Redis errors are caught and logged — the application always falls back to the database and **never crashes due to a cache failure**.

---

## 🛡️ Reliability & Monitoring

### Quality Assurance (Testing)
The project maintains high code quality through a comprehensive test suite:
* **Unit Tests:** Testing individual services and logic in isolation to ensure core prompt-generation algorithms work as expected.
* **Integration Tests:** Validating the full flow from the API layer down to the database to ensure all layers communicate correctly.

### Monitoring
* **Error Handling Middleware:** A centralized middleware catches all exceptions, providing consistent API responses.
* **Logging:** Integrated with **nLog** for comprehensive monitoring and debugging.
* **Traffic Analytics:** All interactions are tracked in a dedicated **Rating** table for performance analysis.

---

## 💻 Frontend Integration
While this repository contains the Back-end, it is designed to serve a modern **Angular (v19+)** client application. The API follows RESTful principles to ensure seamless data binding and a smooth user experience in the visual builder.

---

## 📂 Getting Started

### Prerequisites
* .NET 9 SDK
* SQL Server
* Docker Desktop (for Redis)
* .NET User Secrets (for `Jwt:SecretKey` — see `secrets.template.json`)

### Installation & Setup
1.  **Clone the repository:**
    ```bash
    git clone https://github.com/le7-3609/web-api-shop
    ```
2.  **Configuration:** Update the connection string in `appsettings.Development.json` to point to your SQL Server instance. Copy `secrets.template.json` as a reference and initialize User Secrets:
    ```bash
    dotnet user-secrets init --project WebApiShop
    dotnet user-secrets set "Jwt:SecretKey" "<your-strong-secret>" --project WebApiShop
    ```
3.  **Start Redis** (requires Docker Desktop running):
    ```bash
    docker compose up -d
    ```
    Redis will be available on `localhost:6380`. The default dev password is in `.env` (not committed to git — copy `.env.example` if provided, or set `REDIS_PASSWORD` directly).
4.  **Restore Dependencies:**
    ```bash
    dotnet restore
    ```
5.  **Run the Project:**
    ```bash
    dotnet run --project WebApiShop
    ```

### Verifying Redis
```bash
# Confirm the container is up
docker ps --filter name=redis

# Open an interactive redis-cli session inside the container
docker exec -it redis redis-cli -a dev-password-change-me

# Useful commands once inside redis-cli:
# KEYS *                  — list all keys
# GET product:1           — read a cached product
# TTL product:1           — seconds until expiry
# GET products:version    — current list cache version counter
# DBSIZE                  — total number of keys
# FLUSHDB                 — clear all keys (dev only)
```

---

## 🌐 Showcase: Real-World Usage

The following websites were generated by non-technical users using prompts created by the **AI-Driven Website Builder Prompt Store**:

| Project Name | Live Link |
| :--- | :--- |
| Big Harmony | [View Site](https://big-harmony.base44.app) |
| Grow Sync Flow | [View Site](https://grow-sync-flow.base44.app) |

> **Note:** These examples demonstrate the effectiveness of the system in translating user requirements into professional, functional web structures.
---

## 📄 License

This project is licensed under the [MIT License](LICENSE).
