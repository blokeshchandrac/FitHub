# FitHub technical debt

This list records current, factual release limitations. Historical Phase 2 results remain in the Phase 2 evidence package.

## Open after the 2026-10-21 Phase 3 corrections

- **Hosted Stripe retest:** the Monthly plan, server-side EUR 29.90 amount, 2,990-cent conversion, webhook rules and revenue calculations are covered locally, but the Stripe-hosted checkout was not manually reopened because valid Stripe Test Mode credentials were unavailable in the verification shell.
- **Frontend dependency audit policy:** production dependencies are release-blocking at every severity and are checked with `npm audit --omit=dev`; the complete development tree is reported separately. Angular build/CLI and Vitest use patched compatible releases, while Angular's exact Vite pin is safely constrained to the patched compatible Vite release through the narrow package override documented in `package.json`.
- **Retained development data:** historical successful payments keep their original amount by design. A retained demo account may therefore show an older historical cost-per-visit even though new Monthly purchases use EUR 29.90.
- **Registration transaction boundary:** a deliberately invalid local JWT signing configuration demonstrated that Identity user/role creation can complete before access-token issuance fails. Production validation prevents an invalid key from starting the API, but making registration and token issuance a single recoverable application workflow would improve resilience.
- **Evaluator database drift:** previously retained local Identity password hashes can differ from the current development seed password because the safe seeder does not overwrite existing account passwords. A clean seeded evaluator environment avoids this; retained environments require documented reconciliation rather than automatic password replacement.

## Corrected in Phase 3

- GAP-001 / FR-01: registration now requires explicit consent and stores backend-controlled timestamp/version evidence.
- DEF-005 / FR-13: every workout set now has a distinct nullable rest-time field, validation, persistence and history display.
- DEF-004 / FR-23–FR-24: Connected Mode now keeps physical sensor observations separate from persisted SignalR snapshots, orders observations per machine, ignores duplicate/stale readings, and releases shared equipment only after one uninterrupted 30-second clear period.
- GAP-002 / FR-03: password recovery now sends a neutral response, delivers a configured Mailpit/SMTP link, stores only a token hash, enforces account binding and 30-minute single-use expiry, invalidates prior requests, and revokes active refresh tokens after reset.
- DEF-006 / FR-33: Members now have persisted preferences for every notification category with a real FitHub trigger; optional notifications are suppressed before creation, existing notifications remain available, and stable deduplication remains enforced.
- The component-style budget now reflects the verified size of the complex, component-scoped simulator overlays (8 kB warning/10 kB error), avoiding false warnings without weakening the initial-bundle budget.
- Monthly membership source data now uses EUR 29.90; checkout conversion and confirmed-payment revenue use the server-stored amount.
