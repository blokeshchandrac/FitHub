# FitHub Stage 9 test plan

## Purpose and scope

Stage 9 verifies and stabilizes the requirements delivered in Stages 1-8. It does not introduce product functionality. The plan covers builds, domain rules, PostgreSQL persistence, HTTP authentication and authorization, Stripe Sandbox webhook handling, simulators, container startup, dependency/security review, and targeted accessibility/responsive checks.

## Test environment

- Verification date: 13 August 2026
- Host: Windows, PowerShell, Docker Desktop
- Runtime: .NET 10, Node.js 22.22.3, Angular 22, PostgreSQL 18 container
- Browser check: Codex in-app Chromium at 1280x800 and 375x812
- Stripe: local cryptographic webhook verification and fake checkout gateway only; no live or paid resources

## Test levels

| Level | Purpose | Location |
|---|---|---|
| Unit | Domain transitions, calculations, validation, recommendations | `backend/tests/FitHub.UnitTests` |
| Integration | PostgreSQL schema, API, Identity/JWT, Stripe, SignalR, concurrency, modules | `backend/tests/FitHub.IntegrationTests` |
| Angular | Components, services, interceptors, guards | `frontend/fithub-web/src/**/*.spec.ts` |
| Simulator | API mapping and synthetic-event behavior | `simulators/*/simulator.test.mjs` |
| Smoke | Build images, start three-service stack, health and rendering | `docker-compose.yml` |
| Review | Authorization metadata, secrets, dependencies, code patterns, browser layout | repository-wide |

## Entry and exit criteria

Entry requires restored dependencies, Docker Desktop, and no production credentials. Exit requires zero failed automated tests, clean Release builds, valid Compose configuration, healthy PostgreSQL/API/frontend containers, no known vulnerable NuGet/npm packages, and documentation of residual risks. A full WCAG audit, penetration test, live Stripe transaction, and Azure deployment are explicitly outside this stage.

## Reproduction commands

```powershell
dotnet build backend/FitHub.sln -c Release
dotnet test backend/tests/FitHub.UnitTests/FitHub.UnitTests.csproj -c Release --collect:"XPlat Code Coverage"
dotnet test backend/tests/FitHub.IntegrationTests/FitHub.IntegrationTests.csproj -c Release --collect:"XPlat Code Coverage"
dotnet list backend/FitHub.sln package --vulnerable --include-transitive

Set-Location frontend/fithub-web
npm ci
npm test -- --watch=false --coverage
npm run build -- --configuration production
npm audit

Set-Location ../../simulators/equipment-simulator
npm test
Set-Location ../parking-simulator
npm test

Set-Location ../..
docker compose --env-file .env.example config
docker compose up -d --build
Invoke-WebRequest http://localhost:8080/health
Invoke-WebRequest http://localhost:4200/
docker compose down
```

Use strong local values in an untracked `.env` before `up`; `.env.example` contains placeholders only.

## Security review method

- Search source/configuration for private keys, live payment keys, cloud credentials, connection URLs, and non-placeholder secrets.
- Reflect over every controller action to require an explicit `[Authorize]` or `[AllowAnonymous]` decision.
- Inspect database calls for raw string-concatenated SQL; exercise schema and concurrency on PostgreSQL.
- Search Angular for unsafe HTML/sanitizer bypass/evaluation and verify server receipt encoding.
- Test upload size, MIME allowlist, magic bytes, authenticated retrieval, random filenames, and traversal restrictions.
- Inspect CORS, HTTPS/HSTS, cookies, JWT validation, rate limiting, logging, safe errors, security headers, and dependency advisories.
- Inspect landmarks, labels, accessible names, focus styles, reduced motion, and desktop/mobile overflow in the built container.

