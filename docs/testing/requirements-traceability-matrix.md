# FitHub final requirements traceability matrix

This matrix maps the delivered requirements to implementation and at least one verification method. Rows group inseparable sub-requirements that share one implementation boundary. Final release totals are 119 backend unit, 95 PostgreSQL integration, 142 Angular, 2 equipment-simulator and 2 parking-simulator tests; dated Stage 9 totals in historical evidence remain unchanged.

| Requirement | Implementation | Verification |
|---|---|---|
| S1 modular monorepo/dependency direction | solution projects, Angular app, simulator/docs/deploy folders | Release build; project references |
| S1 controller API/OpenAPI/health | API controllers, Swagger, `/health` | `HealthEndpointTests`; Docker health request |
| S1 Angular shell/routes/feature areas | routes, shell, lazy feature components | build; `app.spec.ts`; browser review |
| S1 Docker/config/docs | Dockerfiles, Compose, `.env.example`, README/AGENTS | Compose config/startup smoke |
| S2 PostgreSQL/EF/Identity schema/UUIDs | entities, EF configurations, migrations, context | `PostgreSqlSchemaTests` |
| S2 histories/precision/indexes/concurrency | configurations and aggregates | schema and concurrency tests |
| S2 safe Development seeds | `DevelopmentDataSeeder`, configuration gates | seeder test; secret review |
| S3 registration/login/current user/recovery | auth controller/service and Identity | `AuthenticationApiTests` |
| S3 JWT and rotating/revoked refresh | issuer, hashed refresh entity, HTTP-only cookie | refresh/logout API test; Angular service specs |
| S3 roles/policies | policy definitions, controller/hub attributes | role and metadata tests |
| S3 safe errors/rate limits/audit | safe responses, limiter policies, `AuditLog` | API/metadata tests; review |
| S3 Angular auth/service/interceptor/guards | auth features and core services | Angular tests |
| S4 plans/membership lifecycle | membership entities/services/controllers/pages | membership tests |
| S4 server-priced Stripe Sandbox checkout | gateway and membership service | live-key rejection; code review |
| S4 signed/idempotent webhooks/payment states | gateway parser, processed-event uniqueness | payment and gateway security tests |
| S4 printable encoded receipt/no cards | server receipt builder and payment model | XSS/data-model review |
| S4 expiring one-use QR/metrics | attendance service/token/calculator | attendance and cost tests |
| S5 exercise/templates/sessions/logs/history/assignment | workout services/API/lazy pages | Stage 5 integration; build |
| S5 volume/previous performance/PR | workout rules and queries | analytics tests |
| S5 body measures/BMI/graphs/ranges | progress service/entities/page | BMI theories; integration test |
| S5 dashboard/formulas | dashboard service and analytics documentation | analytics/persistence tests |
| S6 equipment states/one partner/transitions | `Equipment` aggregate/history | exhaustive equipment unit tests |
| S6 sensor grace/final clearance | presence/grace/confirm methods | sensor grace tests |
| S6 heartbeat/offline/history/admin reason | service/monitor/status events | Stage 6 tests |
| S6 checked-in live access | authorization handler/access service | live-access test |
| S6 device hash/API/simulator | authenticator/controller/simulator | device and simulator tests |
| S6 committed SignalR/concurrent claim | notifier after save; optimistic concurrency | event and simultaneous claim tests |
| S6 fault workflow/photo abstraction | fault aggregate/service/storage | fault/upload/photo-access tests |
| S7 parking state/history/confidence/source/correction | parking aggregate/services/controllers | parking unit/integration tests |
| S7 synthetic replaceable simulator | simulator and detection API | simulator tests; API review |
| S7 parking SignalR | hub/notifier/query-token handling | broadcast and negotiation tests |
| S7 rule engine/persisted reason | recommendation rules/service/entities/docs | recommendation tests |
| S7 nutrition disclaimer | nutrition rule and page | unit/integration tests |
| S8 admin metrics/CSV/completed revenue | dashboard service/controller/page | Stage 8 integration tests |
| S8 capacity/duplicate/waitlist/cancel | booking aggregate/service/controller | booking unit/integration tests |
| S8 six notifications | notification entity/service/page | notification tests |
| S8 admin/trainer authorization | controller policies and route guards | metadata/role tests |
| Role-specific navigation and personal-page separation | persistent Angular shell and explicit API policies | layout specs; authentication API role tests |
| Administrator user/card/membership controls | administration management service/controller | domain and authentication integration tests |
| Trainer class approval workflow | booking aggregate, role controllers, PostgreSQL locking | class unit/integration/API tests |
| First-time Member onboarding | profile fields/service/controller and Angular Welcome route | profile API and login destination tests |
| Confirmed net Stripe revenue and refunds | payment fields, webhook, dashboard query | Stripe mapping/webhook and Stage 8 revenue tests |
| Backend unit tests | `FitHub.UnitTests` | 119/119 pass at final executable commit |
| PostgreSQL integration/API validation | `FitHub.IntegrationTests` | 95/95 pass at final executable commit |
| Angular component/service tests | 46 spec files | 142/142 pass at final executable commit |
| S9 authentication/roles | auth API and metadata tests | success/failure/privacy/policy cases |
| S9 Stripe webhooks | payment/gateway security tests | idempotent/signed/tampered/sandbox cases |
| S9 equipment/concurrency/SignalR | equipment and Stage 6 tests | transition/claim/committed-event cases |
| S9 fault upload security | fault service/authenticated file route | type/size/signature/access tests |
| S9 parking/simulator | parking unit/integration/simulator | all suites pass |
| S9 recommendation rules | recommendation unit tests | every documented rule passes |
| Docker startup | PostgreSQL 18, API, frontend and Mailpit | Compose validation, health checks and final CI |
| Secrets/dependencies | ignore/template policy and package pins | tracked-file scan; clean npm/NuGet audits |
| SQL/XSS/CORS/HTTPS/rate/log/errors | EF parameterization, encoding/CSP, narrow hosted CORS, forwarded HTTPS, limiters and generic errors | review, API tests, hosted headers and final acceptance |
| Accessibility/responsive | semantics, focus/reduced motion and responsive shell | Angular build/specs and targeted browser review; complete mobile/WCAG coverage remains documented debt |
