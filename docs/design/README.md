# FitHub design and architecture

FitHub uses an Angular standalone frontend and a modular ASP.NET Core backend with Domain, Application, Infrastructure and API dependency boundaries. EF Core persists to PostgreSQL; SignalR publishes committed equipment and parking changes. The public release uses Render for the Angular static site and API, Neon PostgreSQL, Brevo transactional email over HTTPS, and Stripe Test Mode.

Current design records:

- [Authentication and session flow](authentication-flow.md)
- [Database design](database-design.md)
- [Membership, payment and attendance flow](stage-4-membership-payments-attendance.md)
- [Analytics calculations](analytics-rules.md)
- [Recommendation rules](recommendation-rules.md)
- [Equipment state machine](equipment-state-machine.md)
- [Connected equipment laboratory](smart-equipment-virtual-lab.md)
- [Connected parking laboratory](smart-parking-virtual-lab.md)
- [Administration, classes and notifications](stage-8-administration-modules.md)
- [Final deployment architecture](../deployment/render-neon-deployment.md)

The connected laboratories reproduce synthetic device observations, backend validation, PostgreSQL state transitions and SignalR updates. They do not claim delivery of physical ESP32, NFC, ultrasonic or camera hardware.
