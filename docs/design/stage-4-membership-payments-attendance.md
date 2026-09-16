# Stage 4 membership, payments, and attendance

## Membership flow

Authenticated users can read active plans and their membership/payment overview. The overview includes the backend-derived effective state: `None`, `PendingPayment`, `Active`, `Expired`, `Cancelled`, or `PastDue`. `Active` additionally requires `StartsAt <= now < EndsAt` in UTC. A Member starts a `Purchase`, `Renewal`, or `Upgrade` by sending only a plan UUID and action. These exact case-sensitive strings are the OpenAPI checkout contract; numeric and unknown enum values are rejected. The API reloads the active plan and current membership from PostgreSQL, validates the transition, creates a pending payment, and sends the database price, currency, and plan name to Stripe Checkout.

Inactive members use `/membership/choose`, `/membership/renew`, or `/membership/confirming` in a full-screen layout with access to `/payments`, `/account`, and sign out. The normal sidebar exists only in the active/staff application layout. Personal gym feature routes require both the Member role and the database-backed `ActiveMembership` policy; staff use explicit Trainer or Administrator endpoints instead of personal Member pages. The centralized Angular account context/guard mirrors that boundary. A membership-status network failure shows a retry page and never silently becomes an inactive decision.

Administrators manage plans through a dedicated API and page. Historical payments keep their selected plan relationship even if the plan is later deactivated.

## Stripe Sandbox

Only secret keys beginning with `sk_test_` are accepted. Configure:

- `Stripe__SecretKey`
- `Stripe__WebhookSecret`
- `Stripe__WebBaseUrl`
- `Stripe__CheckoutSuccessUrl`
- `Stripe__CheckoutCancelUrl`

FitHub intentionally creates a single Checkout `price_data` line item from the selected active PostgreSQL plan. Separate Stripe Price IDs are not used. This keeps price and membership duration under one server-owned contract while Stripe receives EUR amount, plan name, authenticated user ID/email, plan ID, purchase kind, and pending-payment ID. Configuration is validated at API startup; placeholder or live credentials are rejected before the server accepts traffic.

`POST /api/payments/stripe/webhook` verifies `Stripe-Signature` with the official Stripe SDK. Every Stripe event ID is inserted into `ProcessedPaymentWebhooks` under a unique `(Provider, EventId)` index. A repeated event returns success without applying its payment or membership transition again.

Handled Checkout events:

- `checkout.session.completed` and `checkout.session.async_payment_succeeded`: mark payment successful and apply membership.
- `checkout.session.async_payment_failed` and `checkout.session.expired`: mark an incomplete payment failed.
- Other verified events are recorded and ignored.

A Pending payment is persisted only after Stripe successfully creates a Checkout Session. A configuration, network, or Stripe API initialization failure is logged with a correlation ID and creates no payment-history entry; it is not a card failure. Stripe `async_payment_failed` marks an actual failed payment and `checkout.session.expired` marks it Cancelled. FitHub stores no card number, CVC, or payment-method details. Printable receipts contain only FitHub payment, plan, member, and Stripe Checkout reference data.

The Stripe success redirect opens `/membership/checkout/success`, which polls the membership overview for a bounded period. The redirect never activates a membership; only a verified, idempotently recorded webhook can do so. Checkout failures are logged server-side with the request identifier while the browser receives a short retryable message.

## Attendance flow

An active member requests a two-minute random token. PostgreSQL stores only its SHA-256 hash. The Angular attendance page renders the raw value as a QR code for the member; the demonstration scanner accepts that token through a Trainer/Administrator endpoint.

The API checks membership activity, expiry, and prior use. The token's PostgreSQL `xmin` concurrency value prevents two scanners from accepting the same token concurrently. A successful scan marks the token used and creates one attendance record in the same save operation.

Attendance summary calculations use UTC boundaries:

- current ISO week beginning Monday;
- current calendar month;
- visits within the current membership period;
- successful payments for the membership divided by visits in that membership, rounded to two decimals; no visits returns no cost-per-visit value.

## API summary

| Endpoint | Access |
|---|---|
| `GET /api/membership` | Authenticated account/payment page |
| `GET /api/membership/plans` | Authenticated |
| `POST /api/membership/checkout` | Member |
| `GET /api/membership/payments/{id}/receipt` | Payment owner or Administrator |
| `GET/POST/PUT /api/administration/membership-plans` | Administrator |
| `POST /api/payments/stripe/webhook` | Anonymous, Stripe-signed |
| `GET /api/attendance` | Authenticated |
| `POST /api/attendance/check-in-token` | Active membership policy |
| `POST /api/attendance/check-in` | Trainer or Administrator |
