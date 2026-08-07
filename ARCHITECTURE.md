# Architecture

## Solution overview

A two-project solution (`Backend.slnx`) containing an ASP.NET Core Web API and a Blazor web application. PostgreSQL is the database, accessed through Entity Framework Core.

| Project | Role |
| --- | --- |
| `Backend` | ASP.NET Core Web API. Owns all domain logic, data persistence, and authentication. Exposes a user-scoped REST API only. |
| `Frontend` | Blazor web application (server-side interactive render mode). Owns UI/UX only; calls the Backend API for all data. |

The Frontend and Backend are separate processes. The Frontend never touches the database directly; all persistence and business rules live in the Backend.

## Backend layers

The Backend is organised into layers. Folders reflect a layer per responsibility so code stays easy to navigate and test.

| Layer | Responsibility |
| --- | --- |
| `Controllers` / API | HTTP endpoints. Validate input at the boundary, map request DTOs to domain commands, call application services, and return consistent problem responses. Never contain business logic. |
| `Application` | Use cases and application services. Orchestrate domain operations, enforce authorisation, and return response DTOs. |
| `Domain` | Entities and domain logic such as `JobApplication`, `Company`, `ResumeVersion`, `ApplicationStatusHistory`, `Contact`, and `Tag`. Status-change rules and other invariants live here. |
| `Infrastructure` / Data | Persistence only: EF Core `DbContext`, entity configurations, and migrations. |
| `Auth` / Identity | ASP.NET Core Identity setup: registration, sign-in, and the ownership boundary (`ApplicationUser`). |

Dependencies point inward: controllers depend on application services, application services depend on domain and persistence abstractions, and domain has no external dependencies. Interfaces are introduced only at genuine boundaries where a substitution or testing need exists (per AGENTS.md, not by habit).

## Frontend structure

The Frontend follows the default Blazor project layout:

- `Components/Pages` — routed pages.
- `Components/Layout` — shared layout, navigation, and reconnection UI.
- `Components/` — reusable components.
- `wwwroot/` — static assets.
- `Services` (to be added) — typed HTTP clients for Backend API calls; pages never construct `HttpClient` directly.

Pages use Blazor forms for input and `System.Net.Http.Json` for API calls. Response DTOs from the API are modelled in a dedicated `Frontend` models area, kept separate from Backend DTOs.

## Domain model

The domain model matches the README's planned concepts:

- `ApplicationUser` — account identity and ownership boundary.
- `Company` — a company recorded by one user.
- `JobApplication` — the role, application details, current status, and notes.
- `ResumeVersion` — a user-owned CV name/version, such as `CV-Backend-v3` (names/versions only; no uploads).
- `ApplicationStatusHistory` — immutable record of status changes.
- `Contact` — recruiter or hiring-manager details for an application.
- `Tag` — user-defined categorisation for applications.

Every aggregate is owned by a single user. Queries and mutations are always scoped by the authenticated user ID; owner IDs from the client are never trusted.

## Package choices

| Package | Project | Purpose |
| --- | --- | --- |
| `Microsoft.AspNetCore.OpenApi` | Backend | OpenAPI document generation for the API. |
| `Npgsql.EntityFrameworkCore.PostgreSQL` | Backend | PostgreSQL EF Core provider (to be added). |
| `Microsoft.AspNetCore.Identity.EntityFrameworkCore` | Backend | ASP.NET Core Identity for registration, sign-in, and password hashing (to be added). |

Packages are added only when a concrete need exists, per AGENTS.md. Known issue: the transitive `Microsoft.OpenApi` 2.0.0 package reports a high-severity vulnerability (GHSA-v5pm-xwqc-g5wc); re-check for a patched version when upgrading packages.

## Naming conventions

- Domain names over vague names: `JobApplication`, not `Item` or `Data`.
- Entities, DTOs, and UI models are separate types, each named for its role: `JobApplicationRequest`, `JobApplicationResponse`, `JobApplication`.
- Endpoints use `[Authorize]`, are scoped to the authenticated user, and use parameterized LINQ/EF queries only.
- DTOs in the Frontend model API responses explicitly; no entity or EF types cross the wire.
- Files and folders use PascalCase; projects use the short names `Backend` and `Frontend`.

## Out of scope (current MVP)

Per README and AGENTS.md: no CV/cover-letter uploads, no reminders or notifications, no CSV/PDF export, and no hosting/deployment configuration.
