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

---

## 🔗 API Endpoints

| Category | Method | Endpoint | Description |
| :--- | :--- | :--- | :--- |
| **Carts** | `GET` | `/api/Carts/{cartId}/items` | Retrieve all items in a specific cart |
| | `POST` | `/api/Carts/users/{userId}/items` | Preferred: create/reuse user cart and add item if not already in cart |
| | `POST` | `/api/Carts/users/{userId}/import-guest` | Import guest cart items into user cart (skip duplicates) |
| | `DELETE` | `/api/Carts/{cartId}/clear` | Remove all items from the cart |
| **Users** | `POST` | `/api/Users/register` | Register a new user |
| | `POST` | `/api/Users/login` | Secure user authentication |
| | `GET` | `/api/Users/{userId}/orders` | Fetch full order history for a user |
| **Products** | `GET` | `/api/Products` | Get all available website components/prompts |
| | `GET` | `/api/Products/{id}` | Get detailed information for a specific product |
| **Orders** | `POST` | `/api/Orders` | Process and finalize a new order |
| | `POST` | `/api/Orders/{id}/review` | Submit a rating/review for a purchase |
| **Security** | `POST` | `/api/PasswordValidity/passwordStrength` | Server-side password strength validation |

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

### Installation & Setup
1.  **Clone the repository:**
    ```bash
    git clone https://github.com/le7-3609/web-api-shop
    ```
2.  **Configuration:** Update the connection string in `appsettings.json` to point to your SQL Server instance.
3.  **Restore Dependencies:**
    ```bash
    dotnet restore
    ```
4.  **Run the Project:**
    ```bash
    dotnet run
    ```
