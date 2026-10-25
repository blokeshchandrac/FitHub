# TC-F01 — Account, consent, onboarding, password reset and authorization

- **Requirements:** FR-01–FR-05
- **Environment:** Hosted Render frontend/API with Neon PostgreSQL and Brevo; 24–25 October 2026
- **Release-candidate start:** `afe52dc5639f8afb8a193a9a29a11ee98c3c826f`
- **Synthetic Member:** Sofia Finaltest, tester-controlled mailbox redacted
- **Profile data:** preferred name Sofia; date of birth 1997-06-18; 165 cm; 62 kg; goal Improve strength; activity Moderate; experience Beginner; planned Monday/Wednesday/Friday; branch `DEMO`

## Result

1. Registration without consent was blocked.
2. `/terms` and `/privacy` were real public pages and returned to registration correctly.
3. Consent-enabled registration created the account once and routed to onboarding.
4. The exact profile above persisted and the Member reached the correct membership-gated flow.
5. The Member was denied Administrator routes and no Administrator data was returned.
6. Administrator, Trainer, Active Member and no-membership Member logins reached their role-appropriate destinations; staff were not routed through Member fitness onboarding.
7. Forgot-password returned the same neutral public response.
8. Brevo recorded the earlier hosted reset message as Sent, Delivered and Opened. That link expired before completion.
9. A first fresh hosted link reached the real reset page, but submitting a valid new password failed with the neutral message: `The password could not be reset. Request a new link and try again.` This was recorded as P3-005 before correction.
10. Production logs identified an execution-strategy/transaction incompatibility in the password-reset service. Commit `fb429cd4ff663fc46436f9fb812ec0b1fe879818` corrected the transaction to execute through the configured PostgreSQL retry strategy and added matching integration coverage.
11. After green CI and successful Render deployment, a completely new reset request produced a new Brevo email and public FitHub reset link.
12. The mailbox owner submitted a valid new password and the hosted page displayed `Your password has been reset. You can now sign in.`
13. The mailbox owner then confirmed all three required postconditions: the previous password was rejected, the new password authenticated successfully, and reuse of the same reset link was rejected.
14. The successful reset plus rejected reuse verifies that the server consumed the persisted token. Automated PostgreSQL integration coverage separately verifies hashed storage, expiry and usage timestamps; no raw token or token hash was exposed during hosted testing.

**Decision:** Passed after P3-005 correction and hosted retest.

**Evidence:** `screenshots/TC-F01-public-landing.jpg`, `screenshots/TC-F01-registration-consent-required.jpg`, `screenshots/TC-F01-registration-onboarding-complete.jpg`, `screenshots/TC-F01-password-reset-success.png`; Brevo hosted delivery was observed in the provider dashboard. The mailbox owner's post-reset sign-in and one-time-link checks were confirmed directly. No password, reset token or full mailbox address is recorded here.

