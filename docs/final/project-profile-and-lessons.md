# FitHub final project profile and lessons learned

## Purpose and customer value

FitHub is a responsive smart-gym application for Members, Trainers and Administrators. It reduces fragmented gym visits by connecting membership, Stripe Test Mode payments, attendance, workouts, progress, recommendations, classes, notifications, equipment status, fault reporting and parking availability. Members can plan and record training with fewer separate tools; Trainers manage assigned Members and approved class workflows; Administrators manage operational and safety information without receiving Member-only services.

## Method and delivered scope

The project followed a Waterfall structure with MoSCoW prioritisation. Requirements and architecture were established before implementation, followed by staged development, concrete Phase 2 manual testing, bounded Phase 3 corrections, deployment and final acceptance. Must Have workflows were completed first. Selected Should Have and Could Have capabilities were included when they supported a coherent end-to-end product; unsupported ideas were deferred rather than represented as finished.

## Architecture and deployment

The Angular frontend calls an ASP.NET Core Web API. Domain rules are separated from Application contracts, Infrastructure adapters and the API composition root. EF Core persists authoritative state in PostgreSQL. SignalR publishes committed equipment and parking changes. Stripe Test Mode confirms payments through signed, idempotent webhooks. Password-reset mail uses Mailpit locally and Brevo's HTTPS transactional API when hosted.

Azure App Service was considered during Phase 1. The delivered free-tier architecture uses a Render Angular static site, a Render ASP.NET Core service, Neon PostgreSQL and Brevo. This choice provided a public HTTPS frontend, persistent external PostgreSQL, WebSocket-capable SignalR and transactional email without creating paid Azure resources. The original Azure planning decision remains historical context, while current operational documentation describes the deployed platform.

The equipment and parking laboratories are connected software prototypes. They generate synthetic NFC, presence and parking observations, submit them through authenticated APIs, apply normal backend rules, persist accepted transitions and publish them through the standard SignalR hubs. They validate software integration and repeatable state behaviour; they do not claim physical ESP32, NFC, ultrasonic or camera hardware delivery.

## Verification

The final release passed 119 backend unit tests, 95 PostgreSQL integration tests, 142 Angular tests, and two tests in each simulator suite. Nine hosted acceptance cases passed with synthetic data and no failed or blocked case. The tested workflows covered registration and consent, password reset, membership and payment, attendance, workout rest time and progress, guidance, shared-equipment clearance, fault safety, parking, notifications, administration, Trainer access and classes.

## Lessons learned

- A working screen is not proof of an integrated requirement; persistence, authorization and downstream updates must be observed.
- A Stripe redirect cannot replace verified webhook or server-side reconciliation.
- PostgreSQL retry strategies must wrap complete transactional units, not individual commands inside an unmanaged transaction.
- Real-time features need one authoritative backend state so SignalR reconciliation cannot invent physical sensor events.
- Concrete synthetic manual data exposes routing, validation and state defects that isolated unit tests may not reveal.
- Connected virtual laboratories made smart-system rules repeatable without pretending that physical hardware was validated.
- MoSCoW prioritisation protected complete core workflows when optional features competed for time.
- Hosted behaviour, proxying, free-tier wake delays and email/payment providers require verification separate from localhost.

## Remaining limitations

The final campaign did not repeat every workflow at every mobile viewport. NFR-16 lacks the approved 20-user p95 load measurement. Optional fault photographs are ephemeral on Render Free. Two Stripe Test Mode webhook registrations remain for dashboard cleanup, although backend idempotency prevents duplicate processing. Render and Neon may add a cold-start delay. These limitations are recorded as technical debt and do not change the verified core results.
