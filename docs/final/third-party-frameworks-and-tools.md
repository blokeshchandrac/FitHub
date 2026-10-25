# Third-party frameworks, libraries and tools

## Application

- Angular 22 and TypeScript - standalone responsive frontend, routing, forms and role-aware UI.
- ASP.NET Core 10 and C# - REST API, Identity authentication, authorization, rate limiting, health checks and SignalR hubs.
- Entity Framework Core with Npgsql - PostgreSQL mapping, migrations, transactional persistence and retry-aware operations.
- PostgreSQL - authoritative application data, histories, idempotency records and operational state.
- SignalR - committed equipment, parking and notification updates.
- Stripe .NET SDK in Test Mode - Checkout Sessions, signed webhooks, reconciliation and server-owned pricing.
- Brevo transactional HTTP API - hosted password-reset email without SMTP from Render.
- Mailpit - inspectable local SMTP inbox for Docker development.
- Three.js - route-lazy virtual equipment and parking laboratory scenes.
- Vitest and Angular testing utilities - frontend regression tests.
- xUnit, ASP.NET Core test hosting and Testcontainers-compatible PostgreSQL infrastructure - backend unit and integration verification.

## Delivery and operation

- Docker and Docker Compose - reproducible local PostgreSQL, API, frontend and Mailpit environment.
- Git and GitHub - public version control and GitHub Actions CI.
- Render - public Angular static site and ASP.NET Core container service.
- Neon - persistent TLS PostgreSQL for the public deployment.
- Repsona Gantt - editable project schedule source.

Third-party services are configured through server-side environment variables. No Stripe, Brevo, JWT or database secret belongs in the Angular bundle or tracked source.
