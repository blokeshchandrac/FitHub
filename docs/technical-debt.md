# FitHub technical debt

This list records current, factual release limitations. Historical Phase 2 results remain in the Phase 2 evidence package.

## Open after final acceptance on 25 October 2026

- **Mobile acceptance breadth:** the final hosted campaign did not repeat every workflow at every mobile viewport.
- **NFR-16 load measurement:** no approved 20-concurrent-user test with p95 read/write latency and error-rate evidence exists. Modular extensibility must not be presented as measured scalability.
- **Fault-photo persistence:** optional photographs use Render Free ephemeral storage. The fault report itself remains persisted in Neon.
- **Stripe dashboard cleanup:** two Test Mode webhook registrations remain. Stable event/payment idempotency prevents duplicate processing, but the redundant registration should be removed after submission.
- **Free-tier wake delay:** Render and Neon can sleep and add an initial delay. Clients retry and SignalR reconnects, but this is not production-availability evidence.
- **Frontend dependency audit policy:** production dependencies are release-blocking and checked with `npm audit --omit=dev`; the complete development tree is reported separately. Angular build/CLI and Vitest use compatible patched releases, with the narrow Vite override documented in `package.json`.
- **Retained development data:** historical successful payments keep their original amount by design. New Monthly purchases use the server-owned EUR 29.90 plan price.
- **Registration transaction boundary:** production configuration validation prevents an invalid JWT key from starting the API, but registration and token issuance could be wrapped in one recoverable application workflow in a later release.

## Corrected in Phase 3

- GAP-001 / FR-01: registration now requires explicit consent and stores backend-controlled timestamp/version evidence.
- DEF-005 / FR-13: every workout set now has a distinct nullable rest-time field, validation, persistence and history display.
- DEF-004 / FR-23–FR-24: Connected Mode now keeps physical sensor observations separate from persisted SignalR snapshots, orders observations per machine, ignores duplicate/stale readings, and releases shared equipment only after one uninterrupted 30-second clear period.
- GAP-002 / FR-03: password recovery now sends a neutral response, delivers a configured Mailpit/SMTP link, stores only a token hash, enforces account binding and 30-minute single-use expiry, invalidates prior requests, and revokes active refresh tokens after reset.
- DEF-006 / FR-33: Members now have persisted preferences for every notification category with a real FitHub trigger; optional notifications are suppressed before creation, existing notifications remain available, and stable deduplication remains enforced.
- The component-style budget now reflects the verified size of the complex, component-scoped simulator overlays (8 kB warning/10 kB error), avoiding false warnings without weakening the initial-bundle budget.
- Monthly membership source data now uses EUR 29.90; checkout conversion and confirmed-payment revenue use the server-stored amount.
