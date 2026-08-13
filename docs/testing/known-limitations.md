# Known limitations and remaining risks

## Security and deployment

- Compose is an HTTP development topology. Production HSTS/HTTPS redirection is present, but TLS termination, forwarded-header trust, certificate renewal, Azure networking, and end-to-end HTTPS were not exercised because Stage 9 must not deploy or create resources.
- PostgreSQL 18 uses the corrected `/var/lib/postgresql` volume target. Any valuable volume created with the former `/var/lib/postgresql/data` target must be backed up and migrated; Stage 9 did not alter or delete the normal project volume.
- CORS is intentionally absent because the supported frontend uses same-origin Nginx proxying. A future separate origin needs a narrow environment-specific allowlist; never combine a wildcard with credentials.
- Rate limits are single-instance, in-memory, IP-partitioned windows. They are not distributed denial-of-service protection, and trusted-proxy client-IP handling must be designed for the selected Azure ingress.
- Local photos validate size/type/signature and require authentication, but are not malware-scanned or re-encoded. Production should use private Blob storage, scanning, and short-lived authorized access.
- The CSP permits inline styles because Angular injects component styles. Scripts remain self-only and frames/objects are denied; nonce/hash style policy would be stronger.
- Secret scanning was a focused source pattern scan, not an enterprise Git-history scanner. The repository currently has no tracked history to audit.

## Test coverage

- No real Stripe Checkout Session or hosted payment journey was called. Tests cover Sandbox enforcement, cryptographic webhook signatures, tampering, idempotency, and persistence locally.
- There is no full browser journey covering registration through payment, attendance, workouts, equipment, parking, booking, and administration. API and focused Angular tests cover boundaries separately.
- Angular coverage is 86.48% lines for source loaded by six specs; many lazy features are build-verified but not component-tested. Backend API line coverage is 28.21%.
- PostgreSQL integration coverage includes generated migrations and is not equivalent to 93.46% behavioral coverage.
- Accessibility review was targeted, not WCAG 2.2 conformance. Screen readers, 400% zoom, high contrast, and more browsers/devices remain untested.
- Concurrency tests cover simultaneous claims and booking capacity in one PostgreSQL environment. Sustained load, multi-instance SignalR, partitions, and chaos recovery remain untested.

## Operational behavior

- SignalR uses one API instance; Azure SignalR Service or a distributed backplane is not configured.
- Development user/device/camera credentials are configuration-driven. Production provisioning, rotation, revocation UI, and hardware secure storage are outside existing stages.
- Password recovery generates an Identity token but does not send email; the endpoint intentionally returns the same response for existing and missing accounts.
- Parking and hardware behavior is synthetic/software-simulated, not computer vision or physical ESP32 validation.
- Monitoring, alerting, backup/restore drills, disaster recovery, performance targets, penetration testing, and formal privacy/retention assessment remain future work.
