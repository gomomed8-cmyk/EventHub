# EventHub — Event Booking & Ticketing API

EventHub is an event management and ticket booking RESTful API built with **ASP.NET Core 8**. It provides backend functionality for organizers to manage events and for users to discover events, create bookings, complete payments, and receive digital tickets.

The project demonstrates practical backend engineering concepts such as Clean Architecture, CQRS, authentication and authorization, booking expiration, payment webhooks, concurrency handling, Redis caching, and background processing.

**Repository:** [EventHub on GitHub](https://github.com/gomomed8-cmyk/EventHub)

---

## Table of Contents

- [Features](#features)
- [Architecture](#architecture)
- [Technology Stack](#technology-stack)
- [Booking and Payment Workflow](#booking-and-payment-workflow)
- [Background Processing](#background-processing)
- [API Reference](#api-reference)
- [Database and Storage](#database-and-storage)
- [Getting Started](#getting-started)
- [Configuration](#configuration)
- [Database Migrations](#database-migrations)
- [Running the Application](#running-the-application)
- [Security Notes](#security-notes)

---

## Features

### Authentication and Authorization

- User registration and login using ASP.NET Core Identity.
- JWT access-token authentication and refresh-token support.
- Role-based authorization with `User`, `Organizer`, and `Admin` roles.
- Roles and the configured administrator account are seeded during startup.
- Refresh tokens are stored using Redis.
- Only an Admin can promote a user to the Organizer role.

### Event Management

- Create, update, publish, cancel, and retrieve events.
- General Admission and Categorized ticket modes.
- Event categorization, searching, filtering, sorting, and pagination.
- Organizer-specific event listing.
- Event booking lists and booking summary statistics.
- Ticket availability tracking.
- Duplicate-event prevention using a SHA-256 fingerprint protected by a unique database index.

### Booking Management

- Authenticated users can create, view, and cancel their bookings.
- A booking can contain multiple ticket items.
- Ticket availability and requested quantities are validated.
- Booking totals are calculated by the application.
- Temporary booking holds expire automatically.
- Idempotency keys help prevent duplicate booking creation requests.
- SQL Server RowVersion concurrency tokens help protect updates from conflicting writes.

### Payment Integration

- Payment checkout initiation through Paymob.
- Paymob webhook endpoint for payment notifications.
- HMAC validation before processing incoming webhook notifications.
- Idempotency support for payment initiation.
- Booking and payment status updates after payment processing.
- SQL Server concurrency handling for payment updates.

### Digital Tickets and Email

- Individual tickets are generated after successful payment.
- PDF tickets are generated using QuestPDF.
- Each ticket includes a QR code generated using QRCoder.
- Booking confirmation emails are sent using SMTP and MailKit.
- HTML email templates are rendered with Scriban.
- PDF tickets are attached to confirmation emails.
- Hangfire is used to process confirmation emails in the background.

### Caching and Background Jobs

- Redis caching for public event data.
- Cache versioning and invalidation after event changes.
- Redis-backed refresh-token storage.
- Hangfire recurring jobs for expired events and expired bookings.
- SQL Server-backed Hangfire job storage.

### API Reliability

- CQRS request handling through MediatR.
- Result Pattern for application operation outcomes.
- Centralized exception handling.
- Consistent API error responses using Problem Details.
- Validation and conflict responses.
- CancellationToken support across API operations and application handlers.

---

## Architecture

The solution follows **Clean Architecture** principles and separates responsibilities across four projects.

```text
EventHub
│
├── EventHub.API
│   ├── Controllers
│   ├── Exceptions
│   └── Program.cs
│
├── EventHub.Application
│   ├── Common
│   ├── Contracts
│   ├── DTOs
│   ├── Features
│   ├── Mappings
│   ├── Settings
│   └── Specifications
│
├── EventHub.Domain
│   ├── Common
│   ├── Entities
│   └── Enums
│
└── EventHub.Infrastructure
    ├── BackgroundJobs
    ├── Identity
    ├── Options
    ├── Persistence
    ├── Redis
    ├── Services
    └── Settings
```

### Project Responsibilities

| Project | Responsibility |
|---|---|
| **EventHub.Domain** | Core business entities, enums, and domain models. |
| **EventHub.Application** | Use cases, CQRS handlers, DTOs, contracts, specifications, and application logic. |
| **EventHub.Infrastructure** | Database access, Identity persistence, Redis, payment integration, email, PDF generation, and background jobs. |
| **EventHub.API** | HTTP endpoints, middleware, authentication configuration, and application startup. |

### Application Design

- **CQRS:** Separates commands that change state from queries that retrieve data.
- **MediatR:** Dispatches application requests to their handlers.
- **Repository and Unit of Work:** Encapsulate data access and persistence operations.
- **Specification Pattern:** Encapsulates reusable query conditions.
- **Result Pattern:** Represents successful and failed application operations.
- **Dependency Injection:** Registers and manages application and infrastructure services.

---

## Technology Stack

| Category | Technologies |
|---|---|
| Framework | .NET 8, ASP.NET Core Web API |
| Language | C# |
| ORM | Entity Framework Core 8 |
| Database | SQL Server |
| Authentication | ASP.NET Core Identity, JWT |
| Architecture | Clean Architecture, CQRS |
| Mediator | MediatR |
| Object Mapping | AutoMapper |
| Caching | Redis, StackExchange.Redis |
| Background Jobs | Hangfire |
| Payment Gateway | Paymob |
| Email | MailKit, SMTP |
| Email Templates | Scriban |
| PDF Generation | QuestPDF |
| QR Codes | QRCoder |
| API Documentation | Swagger / OpenAPI |
| Version Control | Git, GitHub |

---

## Booking and Payment Workflow

The main booking and payment flow is:

1. An authenticated user submits a booking request containing the event and requested ticket items.
2. The API validates the event, ticket mode, requested quantities, and availability.
3. The application calculates the booking total.
4. A temporary booking hold is created with an expiration time.
5. The user initiates payment through the Paymob integration.
6. Paymob processes the payment and sends a webhook notification.
7. The API validates the webhook HMAC before processing the notification.
8. After successful payment processing, the booking is confirmed and individual tickets are generated.
9. A background job generates the PDF ticket document and sends the confirmation email.

### Booking Protection

- Idempotency keys for booking creation and payment initiation.
- SQL Server RowVersion concurrency tokens.
- Ticket availability validation.
- Temporary booking expiration.
- Paymob webhook HMAC validation.
- Background processing for expired bookings and email delivery.

---

## Background Processing

Hangfire is used for recurring jobs and fire-and-forget background processing.

| Job | Trigger | Responsibility |
|---|---|---|
| Expired Events Processing | Every 5 minutes | Processes events whose end date has passed. |
| Expired Bookings Processing | Every 1 minute | Processes bookings whose temporary hold has expired. |
| Booking Confirmation Email | After successful booking confirmation | Generates PDF tickets and sends the confirmation email. |

Hangfire uses SQL Server for persistent job storage.

---

## API Reference

The following endpoints are implemented by the API controllers. Run the application in the Development environment to inspect detailed request and response schemas in Swagger.

### Authentication

**Base route:** `/api/Auth`

| Method | Endpoint | Description | Authorization |
|---|---|---|---|
| POST | `/api/Auth/register` | Register a new user. | Public |
| POST | `/api/Auth/login` | Authenticate a user. | Public |
| POST | `/api/Auth/refresh` | Refresh authentication tokens. | Public |

### Events

**Base route:** `/api/Event`

| Method | Endpoint | Description | Authorization |
|---|---|---|---|
| GET | `/api/Event` | Retrieve events with filtering and pagination. | Public |
| GET | `/api/Event/{eventId}` | Retrieve event details. | Public |
| POST | `/api/Event` | Create a new event. | Organizer |
| GET | `/api/Event/my-events` | Retrieve the organizer's events. | Organizer |
| POST | `/api/Event/{eventId}/publish` | Publish an event. | Organizer |
| PATCH | `/api/Event/{eventId}` | Update an event. | Organizer |
| POST | `/api/Event/{eventId}/cancel` | Cancel an event. | Organizer |
| GET | `/api/Event/{eventId}/bookings` | Retrieve bookings for an event. | Organizer |
| GET | `/api/Event/{eventId}/booking-summary` | Retrieve event booking statistics. | Organizer |

### Bookings

**Base route:** `/api/Booking`

| Method | Endpoint | Description | Authorization |
|---|---|---|---|
| POST | `/api/Booking` | Create a booking. | Authenticated |
| GET | `/api/Booking` | Retrieve the current user's bookings. | Authenticated |
| GET | `/api/Booking/{bookingId}` | Retrieve booking details. | Authenticated |
| POST | `/api/Booking/{bookingId}/cancel` | Cancel a booking. | Authenticated |

Booking creation requires the following request header:

```http
Idempotency-Key: unique-request-key
```

### Payments

| Method | Endpoint | Description | Authorization |
|---|---|---|---|
| POST | `/api/bookings/{bookingId}/payments` | Initiate a payment for a booking. | Authenticated |
| POST | `/api/payments/paymob/webhook` | Receive Paymob payment notifications. | Public webhook; HMAC is validated by the application. |

Payment initiation also accepts the `Idempotency-Key` request header.

### Roles

**Base route:** `/api/Roles`

| Method | Endpoint | Description | Authorization |
|---|---|---|---|
| POST | `/api/Roles/promote-to-organizer` | Promote a user to the Organizer role. | Admin |

---

## Database and Storage

EventHub separates application data, Identity data, and Hangfire job storage.

| Context / Service | Configuration key | Responsibility |
|---|---|---|
| HubDbContext | `ConnectionStrings:DefaultConnection` | Events, ticket categories, bookings, booking items, payments, and tickets. |
| EventHubIdentityDbContext | `ConnectionStrings:IdentityConnection` | ASP.NET Core Identity users and roles. |
| Hangfire | `ConnectionStrings:HangfireConnection` | Persistent background-job storage and processing state. |
| Redis | `ConnectionStrings:Redis` | Event caching and refresh-token storage. |

Redis is a separate service and is not an Entity Framework database context.

---

## Getting Started

### Prerequisites

Make sure the following are installed and available:

- .NET 8 SDK
- SQL Server
- Redis
- Visual Studio 2022 or another compatible IDE
- Paymob credentials for payment integration
- SMTP credentials for email delivery
- Entity Framework Core tools if you use the `dotnet ef` CLI

### 1. Clone the Repository

```bash
git clone https://github.com/gomomed8-cmyk/EventHub.git
cd EventHub
```

### 2. Configure Application Settings

The application uses these configuration sections:

- `ConnectionStrings`
- `Jwt`
- `RefreshTokenSettings`
- `AdminSeed`
- `Paymob`
- `EmailSettings`

Supply sensitive values through **.NET User Secrets** or environment variables. Do not commit real credentials to source control.

### 3. Example Configuration Structure

The following example shows the expected configuration structure. Replace all placeholders with your own values.

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "YOUR_APPLICATION_DATABASE_CONNECTION",
    "IdentityConnection": "YOUR_IDENTITY_DATABASE_CONNECTION",
    "HangfireConnection": "YOUR_HANGFIRE_DATABASE_CONNECTION",
    "Redis": "localhost:6379,defaultDatabase=1"
  },
  "Jwt": {
    "Issuer": "EventHub",
    "Audience": "EventHub.Client",
    "SecretKey": "YOUR_JWT_SECRET",
    "ExpirationMinutes": 15
  },
  "RefreshTokenSettings": {
    "ExpirationDays": 7
  },
  "AdminSeed": {
    "UserName": "admin",
    "Email": "admin@example.com",
    "Password": "YOUR_ADMIN_PASSWORD",
    "DisplayName": "Admin User",
    "PhoneNumber": "YOUR_ADMIN_PHONE"
  },
  "Paymob": {
    "BaseUrl": "https://accept.paymob.com",
    "SecretKey": "YOUR_PAYMOB_SECRET_KEY",
    "PublicKey": "YOUR_PAYMOB_PUBLIC_KEY",
    "ApiKey": "YOUR_PAYMOB_API_KEY",
    "HmacSecret": "YOUR_PAYMOB_HMAC_SECRET",
    "IntegrationId": 0,
    "CheckoutBaseUrl": "https://eg.checkout.paymob.com/",
    "NotificationUrl": "YOUR_PUBLIC_WEBHOOK_URL",
    "Currency": "EGP"
  },
  "EmailSettings": {
    "Host": "smtp.gmail.com",
    "Port": 587,
    "Username": "YOUR_EMAIL",
    "Password": "YOUR_EMAIL_APP_PASSWORD",
    "FromEmail": "YOUR_EMAIL",
    "FromName": "EventHub"
  }
}
```

Replace every placeholder with a valid value. `IntegrationId` must be a valid numeric Paymob integration ID. The notification URL must be publicly reachable when testing Paymob webhooks.

### 4. Configure the Databases and Redis

Provide separate connection strings for the application database, Identity database, and Hangfire database.

Make sure SQL Server is running and the configured databases can be created or accessed by the application.

Also make sure Redis is running and reachable using the configured Redis connection string.

### 5. Apply Database Migrations

From Visual Studio Package Manager Console, run:

```powershell
Update-Database -Context HubDbContext
Update-Database -Context EventHubIdentityDbContext
```

Alternatively, from the solution directory, use the Entity Framework Core CLI:

```bash
dotnet ef database update --project EventHub.Infrastructure --startup-project EventHub.API --context HubDbContext

dotnet ef database update --project EventHub.Infrastructure --startup-project EventHub.API --context EventHubIdentityDbContext
```

### 6. Restore and Build

```bash
dotnet restore
dotnet build
```

### 7. Run the API

```bash
dotnet run --project EventHub.API
```

When running in the Development environment, Swagger UI is enabled at:

```text
https://localhost:<configured-port>/swagger
```

The Hangfire Dashboard is available at:

```text
https://localhost:<configured-port>/hangfire
```

---

## Security Notes

- The User, Organizer, and Admin roles are seeded during application startup.
- The administrator account is created using the `AdminSeed` configuration section.
- Paymob and email options are validated when the application starts.
- SQL Server and Redis must be reachable for the application features that depend on them.
- Use a publicly reachable webhook URL when testing Paymob callbacks.
- Never commit database credentials, JWT signing keys, Paymob secrets, SMTP passwords, or administrator passwords.
- Before exposing the application publicly, configure appropriate authorization for the Hangfire Dashboard. The dashboard should not be left publicly accessible without protection.

---

## Project Status

EventHub is a backend portfolio project demonstrating event management, booking and payment workflows, digital ticket generation, caching, concurrency handling, and background processing with ASP.NET Core 8.
