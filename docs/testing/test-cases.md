# FitHub test cases

| ID | Verification | Expected result | Automated evidence |
|---|---|---|---|
| FND-01 | Backend Release build | Zero warnings/errors | `dotnet build` |
| FND-02 | Angular production build | Optimized lazy build within budgets | `npm run build` |
| DB-01 | Apply all migrations to PostgreSQL | Schema and migration history created | `PostgreSqlSchemaTests` |
| DB-02 | Seed Development data | Roles, users, plans, exercises, equipment, parking exist | `PostgreSqlSchemaTests` |
| AUTH-01 | Register valid then duplicate email | First succeeds; duplicate fails without disclosure | `AuthenticationApiTests` |
| AUTH-02 | Login valid, invalid, and missing account | Correct status; common safe failure text | `AuthenticationApiTests` |
| AUTH-03 | Refresh rotation and logout | Replacement issued; revoked token rejected | `AuthenticationApiTests` |
| AUTH-04 | Role endpoint | Member forbidden; administrator allowed | `AuthenticationApiTests` |
| AUTH-05 | Forgot password known/missing user | Identical accepted response | `AuthenticationApiTests` |
| AUTH-06 | Invalid registration model | 400 validation problem without stack data | `AuthenticationApiTests` |
| AUTH-07 | Sensitive endpoint throttling metadata | Login and recovery policies present | `ApiSecurityMetadataTests` |
| AUTH-08 | Controller authorization audit | Every action has explicit decision | `ApiSecurityMetadataTests` |
| PAY-01 | Duplicate Stripe event | Payment/membership updated once | `Stage4PaymentTests` |
| PAY-02 | Signed/tampered Stripe payload | Valid accepted; tampered rejected | `StripeGatewaySecurityTests` |
| PAY-03 | Live Stripe key | Configuration rejected | `StripeGatewaySecurityTests` |
| PAY-04 | Invalid webhook response | Generic error; no payload/config leak | `Stage4PaymentTests` |
| ATT-01 | Expired/replayed QR token | Check-in rejected and not persisted | attendance unit/integration tests |
| WRK-01 | Volume, PR, streak, consistency, BMI | Documented formulas and boundaries | `AnalyticsRulesTests` |
| WRK-02 | Persisted workout/progress/dashboard | Queries use saved PostgreSQL data | `Stage5WorkoutProgressTests` |
| EQ-01 | All valid/invalid equipment transitions | Domain state machine enforced | `EquipmentTests` |
| EQ-02 | Sensor absence grace rule | No automatic Available transition | `EquipmentTests` |
| EQ-03 | Simultaneous claim | Exactly one owner succeeds | `Stage6EquipmentTests` |
| EQ-04 | Device authentication/access | Hash verified; wrong key/ineligible card rejected | `Stage6EquipmentTests` |
| EQ-05 | Committed SignalR update | Broadcast observes committed state | `Stage6EquipmentTests` |
| EQ-06 | Parking hub negotiation | Query JWT accepted for SignalR | `AuthenticationApiTests` |
| FLT-01 | Other/safety rules | Comment and safety category rules enforced | `FaultReportTests` |
| FLT-02 | Upload validation | Type, size, signature, and spoof checks enforced | `Stage6EquipmentTests` |
| FLT-03 | Fault-photo retrieval | Anonymous request returns 401 | `AuthenticationApiTests` |
| PRK-01 | Parking transitions/corrections | State, confidence, source, order, reason enforced | `ParkingSpaceTests` |
| PRK-02 | Camera/hash/broadcast | Authenticated event persists before broadcast | Stage 7 integration tests |
| PRK-03 | Parking simulator | Synthetic click mapping/confidence preserved | simulator tests |
| REC-01 | Every recommendation rule | Result, reason, estimates, disclaimer verified | `RecommendationRulesTests` |
| ADM-01 | Booking capacity/duplicates/waitlist | Capacity safe; cancellation promotes | booking unit/integration tests |
| ADM-02 | Trainer creates class | Status is PendingApproval and Members cannot see it | `AuthenticationApiTests`, `ClassBookingTests` |
| ADM-03 | Administrator reviews class | Approved future class becomes Member-visible; rejection needs a reason | `AuthenticationApiTests`, `ClassBookingTests` |
| ADM-04 | Trainer account creation | Only Administrator can create a Trainer login | `AuthenticationApiTests` |
| ADM-05 | Block user/card | Existing user token is rejected; card validation returns an explicit denial | `AuthenticationApiTests`, `RoleWorkflowDomainTests` |
| ADM-06 | Role navigation/API boundary | Admin/Trainer personal Member endpoints forbidden; role navigation excludes unrelated pages | `AuthenticationApiTests`, `ApiSecurityMetadataTests`, `app.spec.ts` |
| ADM-07 | Confirmed net revenue | Pending/cancelled/failed/demo excluded and refunds deducted | `Stage8AdministrationModulesTests` |
| PROFILE-01 | First Member login | Incomplete profile enters Welcome; valid profile completes onboarding | `AuthenticationApiTests`, `login-page.spec.ts` |
| ADM-02 | Revenue/CSV | Only succeeded payments counted | Stage 8 integration tests |
| ADM-03 | Six notification types | Generation and deduplication verified | notification tests |
| UI-01 | Unauthenticated protected route | Redirects to login with return URL | Angular guard spec |
| UI-02 | Inactive membership route | Redirects to payment-capable page | Angular guard spec |
| UI-03 | Authentication service/interceptor | Memory token, refresh, coalescing, cleanup, API-only bearer | Angular service specs |
| UI-04 | Login component | Invalid form blocks request; API error uses alert | login component spec |
| UI-05 | Desktop/mobile shell | No overflow; labeled controls; menu state correct | targeted browser review |
| UI-06 | Membership-aware navigation | Inactive Member sees only Membership, Payments, and Account; active Member sees gym features | `app.spec.ts` |
| SEC-01 | Dependency advisories | Zero known NuGet/npm vulnerabilities | package audits |
| SEC-02 | Secret patterns | No production credential/private-key pattern | repository scan |
| SEC-03 | SQL/XSS/error review | Parameterized EF, encoded output, safe boundary errors | review plus API tests |
| CTR-01 | Compose configuration | Configuration resolves | `docker compose config --quiet` |
| CTR-02 | Full startup | PostgreSQL healthy; API/frontend return 200 | isolated Compose smoke test |
