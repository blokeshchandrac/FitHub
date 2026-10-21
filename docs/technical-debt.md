# FitHub technical debt

This list records current, factual release limitations. Historical Phase 2 results remain in the Phase 2 evidence package.

## Open after the 2026-10-21 Phase 3 corrections

- **Hosted Stripe retest:** the Monthly plan, server-side EUR 29.90 amount, 2,990-cent conversion, webhook rules and revenue calculations are covered locally, but the Stripe-hosted checkout was not manually reopened because valid Stripe Test Mode credentials were unavailable in the verification shell.
- **Frontend build-tool advisories:** `npm audit --omit=dev` reports no production vulnerabilities, while the complete audit currently reports one moderate direct advisory in `@angular/build` and one high transitive advisory in Vite. These require a separate supported Angular/build-tool upgrade rather than a forced audit fix.
- **Simulator stylesheet budgets:** the production build passes, but the existing equipment-workbench, equipment-page and parking-page stylesheets remain above the 4 kB component-style warning budget.
- **Retained development data:** historical successful payments keep their original amount by design. A retained demo account may therefore show an older historical cost-per-visit even though new Monthly purchases use EUR 29.90.
- **Registration transaction boundary:** a deliberately invalid local JWT signing configuration demonstrated that Identity user/role creation can complete before access-token issuance fails. Production validation prevents an invalid key from starting the API, but making registration and token issuance a single recoverable application workflow would improve resilience.
- **Evaluator database drift:** previously retained local Identity password hashes can differ from the current development seed password because the safe seeder does not overwrite existing account passwords. A clean seeded evaluator environment avoids this; retained environments require documented reconciliation rather than automatic password replacement.

## Corrected in Phase 3

- GAP-001 / FR-01: registration now requires explicit consent and stores backend-controlled timestamp/version evidence.
- DEF-005 / FR-13: every workout set now has a distinct nullable rest-time field, validation, persistence and history display.
- Monthly membership source data now uses EUR 29.90; checkout conversion and confirmed-payment revenue use the server-stored amount.
