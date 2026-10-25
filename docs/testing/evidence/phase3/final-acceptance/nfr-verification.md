# Phase 3 non-functional verification

Environment: hosted Render HTTPS frontend/API, Neon PostgreSQL, Brevo HTTPS API, Stripe Test Mode; 24–25 October 2026. This is lightweight evaluation evidence, not penetration testing, a WCAG certification or a production load test.

| Area | Evidence | Result |
| --- | --- | --- |
| Authorization | Member, Trainer and Administrator navigation/API boundaries were exercised; Member administration access was denied; inactive membership gating worked. | Passed for tested roles |
| Password/reset protection | Tokens are hashed, expiring and single-use in automated/PostgreSQL integration coverage. A fresh Brevo message delivered the hosted link; the reset completed after P3-005, the old password failed, the new password succeeded and reuse of the link was rejected. | Passed |
| Payment secrecy/integrity | Stripe Test Mode only; browser did not control the plan amount; webhook confirmation activated membership; pending/cancelled payment was excluded; no full card/CVC was stored. | Passed |
| Validation | Consent, workout/rest limits, QR reuse, fault comment, class capacity, parking confidence and equipment state validation were exercised. | Passed |
| HTTPS/security headers | Public frontend and API health returned HTTPS 200 with HSTS, CSP and `X-Content-Type-Options: nosniff`. | Passed |
| Availability/recovery | During a controlled Render restart, repeated health checks stayed HTTP 200 for approximately 92 seconds (rolling service behaviour). Session-visible data and Neon records remained. This was not a measured natural free-tier sleep/wake. | Passed for controlled restart |
| Normal response time | One observed frontend request was approximately 293 ms and API health approximately 299 ms. No statistical percentile sample was run. | Informational only |
| SignalR | Connected equipment and parking updates reached operational pages. After P3-004, an Active Member page received committed Available → Maintenance → Available equipment updates without refresh. | Passed |
| Accessibility basics | Semantic headings, labels, keyboard-operable links/buttons and visible validation/focus treatment were observed on the principal public/authentication forms. | Basic checks passed; not a WCAG audit |
| Responsive layout | Desktop/laptop flows were exercised. A reliable emulated mobile viewport was not available in the final hosted session, so no complete mobile acceptance claim is made. | Incomplete |
| Maintainability | Final CI build/tests are green; modular Domain/Application/Infrastructure/API separation remained; dependency and Compose checks passed. | Passed |
| Persistence | Membership, payment, attendance, workouts/rest values, measurements, preferences, class bookings, equipment and parking survived refresh/redeploy/restart checks. | Passed |
| Error handling/audit | Neutral password-reset response, safe authorization problems, explicit simulator errors, payment idempotency, administrative actor/timestamp evidence and defect/retest records were observed. | Passed for tested workflows |
| NFR-16 scalability/extensibility | No approved 20-concurrent-authenticated-user run, p95 read/write measurement or error-rate dataset was executed against the free hosted services. Architecture/extensibility is not evidence of load capacity. | Not demonstrated |

