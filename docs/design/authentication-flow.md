# FitHub authentication flow

## Scope

FitHub implements local email/password authentication with ASP.NET Core Identity, short-lived JWT access tokens, rotating refresh tokens, role policies, and a separate database-backed membership entitlement check. Google login is not implemented.

## Session flow

1. Registration validates name, email, and password. Identity normalizes the email, prevents duplicates, hashes the password, and assigns the `Member` role.
2. Login verifies the password through Identity. Successful registration and login return a short-lived JWT and place a random refresh token in the `HttpOnly`, `SameSite=Strict` `fithub.refresh` cookie.
3. Angular keeps the JWT only in memory. Its interceptor adds the bearer token to FitHub API requests; neither token is written to local or session storage.
4. After a reload, Angular calls `POST /api/auth/refresh`. The API compares the cookie token's SHA-256 hash with PostgreSQL, revokes the old record, and issues a replacement pair.
5. A newly registered Member whose profile setup is incomplete enters `/welcome`. After profile completion, the centralized Angular account context reads `GET /api/membership`. `Active` Members enter `/dashboard`; `None`, `PendingPayment`, `Expired`, `Cancelled`, and `PastDue` states use the membership onboarding layout. Trainer and Administrator destinations are role-specific and never enter Member fitness onboarding.
6. Logout revokes the presented refresh token, clears its cookie, and clears the in-memory client session. Revoked or expired refresh tokens cannot create another session.

## Endpoints

| Method and path | Purpose | Protection |
|---|---|---|
| `POST /api/auth/register` | Create a Member account and session | Anonymous |
| `POST /api/auth/login` | Verify credentials and create a session | Anonymous; rate limited |
| `POST /api/auth/refresh` | Rotate a valid refresh token | Refresh cookie |
| `POST /api/auth/logout` | Revoke the token and clear the cookie | Anonymous so expired JWTs can log out |
| `GET /api/auth/me` | Return the current user and roles | Bearer token |
| `POST /api/auth/forgot-password` | Request recovery without disclosing account existence | Anonymous; rate limited |
| `GET/PUT /api/profile` | Read or complete a Member fitness profile | Member |

Password recovery generates an Identity reset token only for an eligible user and always returns the same public response. Delivery remains behind a future email-provider boundary; reset tokens are never returned by the API or logged.

## Authorization and security

The API defines `Member`, `Trainer`, `Administrator`, and `Staff` policies backed by Identity role claims. The `ActiveMembership` requirement checks PostgreSQL for `Active`, start-inclusive, expiry-exclusive membership using UTC on every protected Member request; it is not a JWT claim. Personal Member endpoints require both `Member` and `ActiveMembership`. Trainer and Administrator workflows use explicit role-specific endpoints instead of receiving a blanket bypass into Member APIs. Angular's centralized context and guards provide matching navigation behavior, but the API policy remains the security boundary. Inactive Member denials are a safe 403 ProblemDetails document with code `membership_required`; wrong-role access returns 403.

- Identity owns password hashing, validation, normalization, lockout, and reset tokens.
- Login permits five requests per IP per minute; password recovery permits three per IP per ten minutes.
- Login, registration, refresh, logout, and recovery events write account-safe `AuditLog` records.
- Public errors do not reveal whether an email exists.
- PostgreSQL stores only refresh-token hashes; raw refresh tokens exist only in the protected cookie.
- Every bearer-token validation confirms that the database user is still enabled. Blocking a user therefore invalidates an already issued access token as well as revoking refresh tokens.
- Production must provide a secret `Jwt__SigningKey` of at least 32 bytes and HTTPS.

## Development accounts

Development seeding reads accounts from `appsettings.Development.json` and their shared password from `SeedData__DemoPassword`:

- `member@fithub.local` - Member
- `no-membership@fithub.demo` - **Member – No Membership**; no membership/payment seed
- `active-member@fithub.demo` - **Member – Active Membership**; one active plan and completed development payment seed
- `trainer@fithub.local` - Trainer
- `admin@fithub.local` - Administrator

Seeding runs only in `Development` when `SeedData__Enabled=true`. No demonstration password is embedded in code or migrations.
