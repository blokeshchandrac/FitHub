# FitHub deployment: Render, Neon, Brevo and Stripe Test Mode

## Architecture

FitHub deploys from the final `master` branch as:

```text
Render Angular static site (HTTPS)
        |
        | REST + SignalR/WebSockets
        v
Render ASP.NET Core web service (HTTPS)
        |
        +-- Neon PostgreSQL (TLS, pooled endpoint)
        +-- Brevo transactional email API (HTTPS)
        +-- Stripe Test Mode API and webhook (HTTPS)
```

The Blueprint is [`render.yaml`](../../render.yaml). PostgreSQL is not run in the Render container. Neon is used because its free database persists independently of Render application deploys; an expiring application-host database would not meet the evaluator requirement.

## Required accounts

1. GitHub with access to `blokeshchandrac/FitHub`.
2. Render connected to that GitHub account.
3. Neon with a free PostgreSQL project.
4. Brevo with a verified transactional sender.
5. Stripe with **Test mode** enabled.

No provider secret belongs in Git, Angular, documentation, screenshots, or chat.

## Deployed service names

The Blueprint requests and the verified public deployment uses `fithub-api-10252426` and `fithub-web-10252426`. A future deployment that uses different names must also update the API URL, frontend URL, CORS origin, reset URL, Stripe URLs and static-site CSP.

- Frontend: `https://fithub-web-10252426.onrender.com`
- API: `https://fithub-api-10252426.onrender.com`
- Health: `https://fithub-api-10252426.onrender.com/health`
- Stripe webhook: `https://fithub-api-10252426.onrender.com/api/payments/stripe/webhook`

These URLs were verified on 24 October 2026. See the [public-deployment evidence](../testing/evidence/phase3/public-deployment-verification.log) for the deployed commit, database, authentication, SignalR and Stripe results.

## Create Neon PostgreSQL

1. Create a free Neon project and database in a nearby European region where available.
2. In **Connect**, select the pooled endpoint and retain `sslmode=require`.
3. Use an Npgsql key/value connection string containing host, database, username, password and `SSL Mode=Require`. Keep the application pool small for the free service (for example, maximum 10 connections).
4. Paste the complete value directly into Render as `ConnectionStrings__FitHubDatabase`.
5. Never put this value in `.env.example` or another tracked file.

`Database__ApplyMigrations=true` applies committed EF Core migrations during API startup. The operation retries a waking database, is idempotent, and never calls `EnsureCreated`, drops the database, or rewrites migration history.

## Deploy the Render Blueprint

1. In Render choose **New > Blueprint**.
2. Connect `https://github.com/blokeshchandrac/FitHub`.
3. Select `master` and `render.yaml` for the final release. The `phase3-finalization` branch was used only while verifying the release candidate.
4. Confirm the API is a free Docker web service in Frankfurt and the frontend is a static site.
5. Confirm both service names are available; correct all candidate URLs first if a name changes.
6. Enter every `sync: false` value directly in Render.
7. Create the Blueprint. Automatic deployment uses `checksPass`, so branch updates wait for successful GitHub checks.

The API reads Render's `PORT` and binds to `0.0.0.0`. Render terminates TLS and forwards the original scheme; FitHub handles forwarded headers before HTTPS middleware. Swagger is disabled in Production. `/health` exposes only a healthy status.

The static site runs `npm ci` and `npm run build:render`. This writes only the non-secret `PUBLIC_API_BASE_URL` to `runtime-config.js`. Angular REST and SignalR calls use that API origin. The catch-all rewrite serves `index.html` for refreshed Angular routes, including login, registration, reset, dashboards and both simulations.

## API environment variables

The names-only template is [`deploy/render-api.env.example`](../../deploy/render-api.env.example). The Blueprint supplies non-secret values. Enter these controlled or secret values directly in Render:

| Variable | Secret | Source / purpose |
| --- | --- | --- |
| `ConnectionStrings__FitHubDatabase` | Yes | Neon pooled TLS connection string |
| `Jwt__SigningKey` | Yes | Render-generated 256-bit value |
| `SeedData__DemoPassword` | Yes | Shared evaluator password |
| `SeedData__EquipmentDeviceCredential` | Yes | Synthetic simulator device credential |
| `SeedData__MemberCardUid` | Yes | Synthetic evaluator card UID |
| `SeedData__ParkingCameraCredential` | Yes | Synthetic simulator camera credential |
| `EquipmentDevices__CardUidPepper` | Yes | Random server-side card hashing pepper |
| `Email__Brevo__ApiKey` | Yes | Brevo transactional API key |
| `Email__SenderAddress` | Controlled | Verified Brevo sender address |
| `Stripe__SecretKey` | Yes | Stripe **Test Mode** `sk_test_...` key |
| `Stripe__WebhookSecret` | Yes | Hosted Test Mode endpoint signing secret |

Keep `SeedData__Enabled=true` and `SeedData__Mode=Evaluation` only for this academic evaluator deployment. Seeding creates missing synthetic records and does not reset passwords, notification choices, or an existing evaluation membership on restart.

## Brevo password-reset email

1. Add and verify a sender in Brevo.
2. Create an API key with transactional-email access.
3. Enter it as `Email__Brevo__ApiKey`; enter the verified sender as `Email__SenderAddress`.
4. Keep `Email__Provider=Brevo` and `Email__Brevo__ApiBaseUrl=https://api.brevo.com`.
5. After deployment, request a reset only for a disposable synthetic account.

Production sends `POST /v3/smtp/email` over HTTPS and puts the key only in the server-side `api-key` header. The public recovery response remains neutral when an account is unknown or delivery fails. Local Docker continues to use SMTP and Mailpit at `http://localhost:8025`.

## Stripe Test Mode

1. Enable **Test mode** in Stripe.
2. Add the actual API URL plus `/api/payments/stripe/webhook` as a webhook endpoint.
3. Subscribe to `checkout.session.completed`, `checkout.session.async_payment_succeeded`, `checkout.session.async_payment_failed`, `checkout.session.expired`, and `charge.refunded`.
4. Put the endpoint's `whsec_...` value directly in Render as `Stripe__WebhookSecret`.
5. Put a Test Mode `sk_test_...` key directly in `Stripe__SecretKey`.
6. Verify success and cancel URLs use the deployed Angular origin.

FitHub reads the authoritative plan price from PostgreSQL. Monthly is EUR 29.90 (2,990 cents). A redirect alone never activates membership; only verified successful payment processing does. Processed event IDs make webhook handling idempotent, and failed, cancelled or pending payments do not count as revenue.

## CORS, authentication and SignalR

`Cors__AllowedOrigins__0` must equal the exact HTTPS frontend origin without a trailing slash. The API permits credentials only for that origin. The refresh token is an HttpOnly, Secure, SameSite=None API cookie; access tokens remain in browser memory. Do not put tokens in URLs or browser storage.

Render supports WebSocket upgrades on the web service port. FitHub's SignalR clients reconnect automatically and detach handlers before reconnecting. After a cold start, the UI may briefly reconnect while the free API and Neon compute wake.

## Evaluation data and migrations

On first successful startup, committed migrations run and the explicit Evaluation seed creates only missing synthetic accounts and operational records. Restart the API once and confirm records are not duplicated. Check the API log for `Database migrations are current.` Never run destructive reset commands against Neon.

## Ephemeral photograph storage

Fault reports are stored in Neon and photographs are optional. The current photo provider writes to `/tmp/fithub-fault-images`, which is ephemeral on a free Render service. A photograph can disappear after restart or deploy, but its fault report remains intact. Persistent object storage remains documented technical debt and is not provisioned by this free Blueprint.

## Deployment verification

1. Open the frontend over HTTPS and directly refresh public, dashboard and simulator routes.
2. Confirm API `/health` returns HTTP 200 without configuration data.
3. Confirm both Render deploys use the intended Git commit.
4. Check Neon migration history and verify seed records remain unique after an API restart.
5. Smoke-test Administrator, Trainer, active Member and no-membership Member destinations.
6. Verify a standard equipment update crosses SignalR after an API wake.
7. Use a disposable synthetic account for password reset and token-reuse rejection.
8. If Stripe is configured, complete one controlled EUR 29.90 Test Mode payment and verify one payment and membership record.
9. Open both Administrator simulations and verify Connected Mode reaches the API and feature hubs.

Create `docs/testing/evidence/phase3/public-deployment-verification.log` only after these public checks have factual results.

## Operate, redeploy and roll back

- **Logs:** open the Render service's **Logs** and inspect startup, migrations, HTTP 5xx responses and reconnect loops. Provider errors must never contain credentials.
- **Manual deploy:** use **Manual Deploy > Deploy latest commit** only after GitHub checks pass.
- **Redeploy:** retain the same Neon database and environment values; migrations and evaluation seeds are idempotent.
- **Rollback:** select the last known-good deploy in Render **Events**. Do not delete or roll back the database without assessing migration compatibility.
- **Environment changes:** edit values in Render and redeploy. Changing the public API URL also requires rebuilding the static site with the new `PUBLIC_API_BASE_URL`.

## Free-tier limitations

- The Render API can sleep, making its first request slower.
- A sleeping Neon compute adds connection latency; bounded retries handle normal wake-up delays.
- WebSockets close during deploys or instance replacement and reconnect afterward.
- Fault-photo files are ephemeral while reports and database state persist.
- Brevo and Stripe have their own free/Test Mode limits and require verified dashboard configuration.
- These free tiers are suitable for academic evaluation, not a production service-level commitment.
