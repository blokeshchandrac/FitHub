# Stripe Test Mode development setup

FitHub accepts Stripe Test Mode only. Never enter a real card and never commit `sk_test_...` or `whsec_...` values. Membership plan price and duration are authoritative in PostgreSQL. Checkout uses Stripe `price_data` generated from the selected active plan, so separate Stripe Price IDs are deliberately not required; the seeded Monthly plan is EUR 29.90 for one month and Annual is EUR 499.00 for twelve months.

## 1. Get Test Mode credentials

1. Create or open a Stripe account and enable **Test mode** in the Stripe Dashboard.
2. Open **Developers → API keys** and reveal the test secret key beginning with `sk_test_`.
3. Install and authenticate the Stripe CLI:

```powershell
stripe login
```

Do not put either secret in `appsettings*.json`. For Docker, copy the placeholder file and edit only the ignored `.env` file:

```powershell
Copy-Item .env.example .env
```

Set these values in `.env`:

```dotenv
STRIPE_SECRET_KEY=sk_test_your_real_test_secret
FRONTEND_BASE_URL=http://localhost:4040
STRIPE_CHECKOUT_SUCCESS_URL=http://localhost:4040/membership/checkout/success?session_id={CHECKOUT_SESSION_ID}
STRIPE_CHECKOUT_CANCEL_URL=http://localhost:4040/membership/checkout/cancelled
```

## 2. Configure the local webhook

In a separate terminal, forward Stripe Test Mode events to the existing API port and copy the printed `whsec_...` value into `.env` as `STRIPE_WEBHOOK_SECRET`:

```powershell
stripe listen --events checkout.session.completed,checkout.session.async_payment_succeeded,checkout.session.async_payment_failed,checkout.session.expired,charge.refunded --forward-to http://localhost:8080/api/payments/stripe/webhook
```

Restart the API after changing the secret. The API validates all Stripe settings during startup and stops with a developer-facing configuration error for placeholders, missing values, live keys, or invalid callback URLs.

## 3. Start FitHub

Docker Compose uses PostgreSQL 5432, API 8080, and Angular/nginx 4200:

```powershell
docker compose --env-file .env up --build
```

For separate local processes, start PostgreSQL first, then bind the API to the port used by Angular's proxy:

```powershell
docker compose --env-file .env up -d postgres
$env:ASPNETCORE_URLS='http://localhost:8080'
$env:ConnectionStrings__FitHubDatabase='Host=localhost;Port=5432;Database=fithub;Username=fithub;Password=<value from .env>'
$env:Jwt__SigningKey='<local key of at least 32 characters>'
$env:SeedData__Enabled='true'
$env:SeedData__DemoPassword='<local development demo password>'
$env:Stripe__SecretKey='sk_test_your_real_test_secret'
$env:Stripe__WebhookSecret='whsec_from_stripe_listen'
$env:Stripe__WebBaseUrl='http://localhost:4040'
$env:Stripe__CheckoutSuccessUrl='http://localhost:4040/membership/checkout/success?session_id={CHECKOUT_SESSION_ID}'
$env:Stripe__CheckoutCancelUrl='http://localhost:4040/membership/checkout/cancelled'
dotnet run --project backend/src/FitHub.Api --no-launch-profile
```

In another terminal:

```powershell
Set-Location frontend/fithub-web
npm ci
npm start
```

## 4. Manual purchase checks

1. Open `http://localhost:4040` and sign in with **Member – No Membership**.
2. Choose Monthly or Annual. Confirm the browser opens a Stripe-hosted URL beginning with `https://checkout.stripe.com/`.
3. Successful test: card `4242 4242 4242 4242`, any future expiry such as `12/34`, any three-digit CVC such as `123`, and valid-looking test name/postal details.
4. Wait for the verified webhook. FitHub first shows **Confirming your payment**, then **Payment successful! Your FitHub membership is now active.**, refreshes membership, and opens Dashboard. Payment History must contain one successful row and its printable receipt.
5. Cancellation test: start another Checkout and use Stripe's back/cancel action. FitHub shows **Payment was cancelled. No charge was made.** Membership is not activated by the redirect.
6. Decline test: use `4000 0000 0000 9995`. Stripe declines the payment; FitHub does not activate membership.

The webhook is the primary confirmation path. Local Stripe CLI forwarding only receives events while `stripe listen` is running. If a synchronous card payment completed while the listener was stopped, the success page uses its Stripe Checkout session identifier to request an authenticated server-side reconciliation. When onboarding is resumed after sign-in, the API can instead select that user's latest stored pending Checkout session. The API retrieves the session directly from Stripe Test Mode and verifies the payment owner, FitHub payment ID, plan, purchase kind, completed/paid state, EUR amount and Test Mode status before activating the existing pending membership. The browser redirect by itself never activates membership. This recovery is idempotent, and a later webhook delivery records normally without duplicating the payment or membership.

Inspect the development records without changing them:

```powershell
docker compose --env-file .env exec postgres psql -U fithub -d fithub -c 'SELECT "Id", "Amount", "Currency", "Status", "ProviderReference", "PaidAt" FROM "Payments" ORDER BY "CreatedAt" DESC;'
docker compose --env-file .env exec postgres psql -U fithub -d fithub -c 'SELECT "UserId", "MembershipPlanId", "Status", "StartsAt", "EndsAt" FROM "Memberships" ORDER BY "CreatedAt" DESC;'
```

## Existing development-only failed initialization rows

The correction does not silently delete old data. Review candidates inside a transaction; this command deliberately rolls back:

```powershell
docker compose --env-file .env exec postgres psql -U fithub -d fithub -c 'BEGIN; SELECT "Id", "CreatedAt", "FailureReason" FROM "Payments" WHERE "Status" = ''Failed'' AND "ProviderReference" IS NULL; ROLLBACK;'
```

If those rows are confirmed to be local checkout-initialization artifacts, back up the database and replace `SELECT ...` with a narrowly targeted deletion and `ROLLBACK` with `COMMIT`. Never run development cleanup against production.
