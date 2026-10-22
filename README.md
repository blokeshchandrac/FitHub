# FitHub

FitHub is a modular fitness-club platform built as a Master's Software Engineering project. Stages 1-8 deliver the foundation and defined modules; Stage 9 supplies testing, security review, and stabilization evidence.

## Repository layout

- `frontend/fithub-web` — Angular 22 standalone application
- `backend/src` — Domain, Application, Infrastructure, and API projects
- `backend/tests` — unit and integration test projects
- `simulators` — reserved equipment and parking simulators
- `firmware` — reserved equipment-controller firmware
- `docs` — requirements, design, testing, and deployment documentation
- `deploy` — reserved deployment assets

## Prerequisites

- Node.js 22.22.3 (the version pinned in `.nvmrc` and CI)
- npm 10 or later
- .NET SDK 10
- Docker Desktop with Docker Compose

## Start with Docker Compose

Create a local environment file from the safe template and replace the database password, demo password, JWT signing key, and Stripe Sandbox placeholders:

```powershell
Copy-Item .env.example .env
docker compose up --build
```

Then open:

- Web application: `http://localhost:4040`
- API health: `http://localhost:8080/health`
- Swagger UI: `http://localhost:8080/swagger`
- Local email inbox (Mailpit): `http://localhost:8025`

Stop the services with `docker compose down`. The PostgreSQL data volume is preserved.

PostgreSQL 18 uses `/var/lib/postgresql` as the Compose volume target. If you have valuable data in a volume created by an older FitHub Compose file that mounted `/var/lib/postgresql/data`, back it up and migrate it rather than deleting the volume.

## Run locally

Start PostgreSQL through Docker:

```powershell
Copy-Item .env.example .env
docker compose up -d postgres
```

Start the API after setting a matching local connection string:

```powershell
$env:ConnectionStrings__FitHubDatabase='Host=localhost;Port=5432;Database=fithub;Username=fithub;Password=replace_with_your_local_password'
$env:Jwt__SigningKey='replace_with_at_least_32_random_characters'
$env:SeedData__Enabled='true'
$env:SeedData__DemoPassword='replace_with_a_strong_development_password'
$env:EquipmentDevices__CardUidPepper='replace_with_a_random_pepper'
$env:SeedData__EquipmentDeviceCredential='replace_with_a_random_device_key'
$env:SeedData__MemberCardUid='replace_with_a_demo_card_uid'
dotnet run --project backend/src/FitHub.Api
```

Start the frontend in another terminal:

```powershell
Set-Location frontend/fithub-web
npm ci
npm start
```

## Build and test

```powershell
dotnet build backend/FitHub.sln
dotnet test backend/FitHub.sln

Set-Location frontend/fithub-web
npm ci
npm test -- --watch=false
npm run build -- --configuration production
npm audit --omit=dev
npm audit
```

Validate container configuration without starting services:

```powershell
docker compose --env-file .env.example config
```

The production dependency audit is release-blocking at every severity because those packages can reach the shipped browser application. The complete audit is also reported so development and build-tool advisories remain visible. FitHub's frontend Docker image uses Node only in its build stage and serves the compiled application from Nginx; development tooling is not copied into the runtime image. The `anyComponentStyle` production budget is 8 kB warning/10 kB error because the two interactive Three.js laboratories require substantial component-scoped overlay and responsive-control styling; the verified styles remain below that deliberately narrow limit.

## Architecture rules

The Domain project has no project dependencies. Application depends only on Domain. Infrastructure depends on Application and Domain. API references Application and Infrastructure and acts as the composition root.

Secrets must be supplied through local environment variables or the deployment platform. Never commit `.env`, passwords, tokens, or API keys.

## Database foundation

Stage 2 adds PostgreSQL, Entity Framework Core, and ASP.NET Core Identity persistence. Copy `.env.example` to `.env`, replace both password placeholders, and start through Docker Compose to apply the initial migration and load Development demonstration data.

The demo seed process is restricted to the Development environment and reads its password from `SeedData__DemoPassword`; no demo or production password is embedded in source control. See [the database design](docs/design/database-design.md) for the entity model, indexes, history strategy, and concurrency rules.

## Authentication

The Angular client provides registration, login, forgot-password, unauthorized, route-guard, interceptor, and role-aware navigation support. Access tokens remain in memory; rotating refresh tokens use an HTTP-only cookie and are persisted only as SHA-256 hashes.

Password-recovery links are single-use, expire after 30 minutes, and are delivered to the local Mailpit inbox during Docker development. Configure the frontend base URL, expiry, SMTP sender, and published Mailpit ports through `.env`; the raw reset token is sent only in the email link and only its SHA-256 hash is stored in PostgreSQL.

For local development, set `SeedData__Enabled=true`, provide `SeedData__DemoPassword`, and configure `Jwt__SigningKey` with at least 32 random bytes. The configured demonstration emails are:

- `member@fithub.local`
- **Member – No Membership:** `no-membership@fithub.demo` (starts at `/membership/choose`)
- **Member – Active Membership:** `active-member@fithub.demo` (starts at `/dashboard`)
- `trainer@fithub.local`
- `admin@fithub.local`

All accounts use the development password supplied through configuration. See [the authentication flow](docs/design/authentication-flow.md) for endpoints, token rotation, authorization, recovery behavior, and security controls.

## Membership, payments, and attendance

Stage 4 adds plan administration, Member purchase/renewal/upgrade Checkout flows, Stripe-signed idempotent webhooks, payment history, printable receipts, active-membership access control, temporary one-use QR check-in tokens, a demonstration scanner, and attendance metrics.

Authentication and membership entitlement are separate. Members without a database-confirmed active membership use the full-screen plan/renewal flow without the member sidebar. The API considers a membership active only when its status is `Active`, its UTC start has arrived, and its UTC expiry has not passed. `Trainer` and `Administrator` role areas do not require a Member purchase.

Only Stripe Sandbox `sk_test_` keys are accepted. Configure `STRIPE_SECRET_KEY` and `STRIPE_WEBHOOK_SECRET` in the untracked `.env`; never commit them. See the [Stage 4 technical flow](docs/design/stage-4-membership-payments-attendance.md) and [user guide](docs/requirements/stage-4-user-guide.md).

For exact Stripe Test Mode keys, callback URLs, webhook forwarding, test cards, database verification, and development-only cleanup instructions, follow the [Stripe Test Mode setup](docs/deployment/stripe-test-mode-setup.md).

## Workouts, progress, and dashboard

Stage 5 adds a searchable exercise library, personal and trainer-assigned workout templates, live session/set logging, previous performance, personal records, history, body measurements, BMI, server-calculated dashboard metrics, and responsive SVG progress graphs. See [the complete analytics formulas](docs/design/analytics-rules.md).

Exercise equipment is descriptive workout-library metadata only. Equipment monitoring and operational functionality remain outside Stage 5.

## Smart equipment and fault reporting

Stage 6 adds the checked-in equipment list/detail experience, owner/one-partner state machine, sensor clearance grace period, heartbeat/offline detection, administrator overrides, committed SignalR updates, device authentication, and photo-backed fault reporting. Configure the three `EQUIPMENT_*` placeholders in `.env` before enabling development seeds.

Run the hardware simulator with:

```powershell
Set-Location simulators/equipment-simulator
npm test
npm run demo
npm start
```

`npm start` prompts for the API URL, equipment UUID, device ID/key and simulated card UID. See the [state-machine rules](docs/design/equipment-state-machine.md) and [Stage 6 API](docs/design/stage-6-equipment-api.md).

### Smart Equipment Virtual Lab

Open `http://localhost:4040/simulation` to choose a virtual lab. The existing smart-equipment lab is now at `/simulation/equipment`; use **Refresh Members** to load the minimum safe projection of existing development Member accounts, memberships, and card status. Local Mode executes equipment rules in TypeScript; Connected Mode revalidates those development records and sends isolated events through `/api/simulation/events` and `/hubs/simulation`. Session-only status scenarios never update PostgreSQL. The 30-second clearance period can be changed with `SIMULATION_CLEARANCE_SECONDS` (1–60 for testing; use 30 for normal demonstrations).

The interactive smart-parking lab is at `http://localhost:4040/simulation/parking`. Add and drive up to six fictional cars, run the geometry-based virtual camera, test correct parking, two-space obstruction, camera failure and confidence handling. Local Mode never calls the API. Connected Mode requires a signed-in Administrator and persists only the twelve `P-A1`–`P-C4` development spaces plus deduplicated simulation alerts. Normal scans run every 60 seconds; Demo Speed runs every five seconds. The implementation is explicitly a geometry simulator, not real computer vision. See [Smart Parking Virtual Lab](docs/design/smart-parking-virtual-lab.md).

See [the virtual-lab design and demonstration guide](docs/design/smart-equipment-virtual-lab.md) for the state rules, wiring faults, full NFC workflow, and manual verification steps.

## Parking simulation and recommendations

Stage 7 adds authenticated synthetic parking events, confidence/source history, SignalR parking updates, administrator corrections, a visual parking simulator, and transparent rule-based member recommendations. The simulator is not computer vision; a future OpenCV service can replace it through the same API.

Configure `PARKING_CAMERA_CREDENTIAL` in `.env`, then run the simulator from `simulators/parking-simulator` with `npm test` and `npm start`. See [parking simulation](docs/design/parking-simulation.md) and the complete [recommendation rules and formulas](docs/design/recommendation-rules.md).

## Administration, class booking, and notifications

Stage 8 now uses explicit role workflows: Trainers create sessions for Administrator approval, Members book only approved future sessions, and staff inspect participants without entering Member booking pages. Administration includes user, membership-card and membership access controls, Trainer account creation, class review, system activity, and net confirmed-payment revenue. See [Stage 8 administration and module rules](docs/design/stage-8-administration-modules.md).

## Testing and stabilization

Stage 9 provides the complete verification plan, test cases, results, traceability matrix, security review, and residual-risk record in [docs/testing](docs/testing/README.md). The latest role/workflow stabilization verifies 210 backend, Angular, and simulator tests plus a healthy three-service Docker startup; details are in the [test results](docs/testing/test-results.md).
