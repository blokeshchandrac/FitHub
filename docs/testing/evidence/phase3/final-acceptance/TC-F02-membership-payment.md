# TC-F02 — Membership and Stripe Test Mode payment

- **Requirements:** FR-06–FR-09
- **Account:** Sofia Finaltest (synthetic; mailbox redacted)
- **Plan:** Monthly, EUR 29.90, one month
- **Payment environment:** Stripe Test Mode only; standard Visa test card ending `4242`

## Result

1. Before purchase, active-membership functions were gated.
2. The Monthly plan displayed EUR 29.90 and the Annual plan displayed EUR 499.00.
3. The first Checkout Session was cancelled; membership remained inactive and no revenue was added.
4. A second Checkout Session completed for EUR 29.90.
5. The verified backend reconciliation/webhook path produced one `Succeeded` EUR 29.90 payment and activated one Monthly membership (24 October–24 November 2026).
6. Payment history contained the succeeded record and retained the cancelled/pending attempt without treating it as revenue.
7. Administrator confirmed revenue increased by exactly EUR 29.90; pending/cancelled payment was excluded.
8. Existing webhook idempotency verification confirmed duplicate delivery does not duplicate payment or membership records.
9. FitHub displayed only safe payment references; no full card number or CVC was stored or shown.

**Decision:** Passed.

**Evidence:** `screenshots/TC-F02-payment-confirmation.jpg`, `screenshots/TC-F02-payment-history.jpg`; Stripe Test Mode payment and webhook dashboards; persisted FitHub payment/membership/admin-revenue views.

