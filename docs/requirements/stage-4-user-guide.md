# Membership and attendance user guide

## Members

Open **Membership** to see the current plan, status, start date, expiry date, and available plans. Select Purchase, Renew, or Upgrade to open Stripe Sandbox Checkout. FitHub never asks for or stores card details itself.

Open **Payments** to see pending, successful, and failed transactions. Successful rows provide a **Print** action that opens an HTML receipt in a new window.

Open **Attendance** at the gym entrance and choose **Generate token**. Show the resulting QR code to the scanner within two minutes. Each code works once. The same page shows visit history, weekly and monthly totals, and membership cost per visit.

If membership is inactive, payment and account pages remain available, while gym-only pages redirect to Membership.

## Trainers and administrators

Use **Scanner** to paste the token value produced under a member's QR code and simulate entrance scanning. Expired, replayed, invalid, and inactive-membership tokens are rejected without recording a visit.

Administrators use **Administration → Membership plans** to create or edit a plan and to activate or deactivate it. Plan prices must be positive and use at most two decimal places.

## Sandbox demonstration

Provide Stripe test configuration through environment variables, start the API, and forward Stripe CLI Sandbox events to:

```text
http://localhost:8080/api/payments/stripe/webhook
```

Use Stripe-published test cards only. Never enter real card information during the demonstration.

See [Stripe Test Mode development setup](../deployment/stripe-test-mode-setup.md) for exact local commands and successful, cancelled, and declined-payment checks.
