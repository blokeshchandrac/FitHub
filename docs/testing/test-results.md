# FitHub Stage 9 test results

## Role and workflow stabilization — 1 September 2026

The role/workflow correction verified **210 automated tests** with no failures: 91 backend unit, 71 PostgreSQL integration/API, 44 Angular, 2 equipment-simulator, and 2 parking-simulator tests. After the final class-audit and capacity safeguard, the affected Stage 8 integration group was rerun separately (3/3 passed). Backend and Angular Release/production builds, Docker image builds, Compose validation, PostgreSQL readiness, API health, frontend HTTP 200, role-route HTTP checks, and the tracked-file credential scan passed.

Live checks confirmed that Administrator management/class/equipment/parking routes succeed while personal Member routes return 403; Trainer class routes succeed while Administrator, membership, equipment, parking, and personal Member routes return 403; and an active Member reaches the dashboard but cannot reach staff routes.

## Final result

Verified on 13 August 2026: **134 automated tests passed, 0 failed, 0 skipped**. Both production builds and the isolated Docker startup smoke test passed.

| Suite | Passed | Failed | Skipped | Notes |
|---|---:|---:|---:|---|
| Backend unit | 83 | 0 | 0 | Domain and calculation rules |
| Backend PostgreSQL integration | 36 | 0 | 0 | PostgreSQL 18, API, Identity, Stripe, SignalR |
| Angular | 11 | 0 | 0 | 6 spec files |
| Equipment simulator | 2 | 0 | 0 | Node test runner |
| Parking simulator | 2 | 0 | 0 | Node test runner |
| **Total** | **134** | **0** | **0** | |

## Build and container evidence

- Backend Release build: passed, 0 warnings, 0 errors.
- Angular production build: passed; initial bundle 134.18 kB raw / 34.87 kB estimated transfer.
- Compose configuration: passed.
- Isolated project `fithub-stage9-smoke`: PostgreSQL healthy, API 200 with `{"status":"Healthy"}`, frontend 200 with Angular root, all three containers running.
- Browser verification: 1280x800 and 375x812, no horizontal overflow, responsive menu state correct, labeled controls and main/navigation/banner landmarks present.
- The disposable smoke containers, network, and volume were removed after verification.

## Coverage

| Scope | Lines | Branches | Important caveat |
|---|---:|---:|---|
| Backend unit run | 41.57% | 42.74% | Domain + Application assemblies only |
| Backend integration run | 93.46% | 31.73% | Includes generated EF migrations; API line coverage is 28.21% |
| Angular exercised source set | 86.48% | 89.15% | Includes files loaded by six specs, not every lazy feature |

Coverage is evidence, not an exit threshold. Gaps are recorded in `known-limitations.md`.

## Security review results

| Area | Result |
|---|---|
| Secrets | No live key, private key, cloud credential, or credential-bearing URL pattern found; `.env*` ignored except template |
| Authorization | Reflection test covers all controller actions; admin policies and SignalR policies verified |
| SQL injection | EF LINQ and parameterized `FromSql` used; no concatenated runtime SQL found |
| XSS | Angular interpolation; no sanitizer bypass/`innerHTML`; receipt values encoded; strict web CSP |
| Uploads | JPG/PNG/WebP only, 5 MB, magic bytes, random filename, authenticated retrieval, traversal-safe route |
| CORS | No cross-origin policy enabled; supported design is same-origin through Nginx |
| HTTPS | Production HSTS/redirection; refresh cookie always Secure outside Development |
| Rate limiting | Fixed-window IP policies verified for login and password recovery |
| Logging/errors | Secrets not logged; EF values masked; webhook/authorization errors generalized |
| Dependencies | NuGet and npm scans report zero known vulnerabilities |
| Accessibility/layout | Focus/reduced-motion rules; labels/landmarks/names and desktop/mobile overflow checked |

## Confirmed defects fixed

1. Parking SignalR JWT query tokens were ignored; both hub paths are supported.
2. Production refresh-cookie security depended on proxy recognition; it is always Secure outside Development.
3. Stripe configuration failures disclosed state; anonymous webhook failures now share a safe response.
4. Authorization-domain exception details could escape; the API now returns a generic denial.
5. A high-severity transitive `SSH.NET` advisory entered through test tooling; version 2026.0.0 is pinned.
6. PostgreSQL 18 refused the old Compose data mount; it now mounts `/var/lib/postgresql`.
7. CSP blocked Angular's inline stylesheet loader; inline critical CSS is disabled.
8. Fault photographs were anonymous; retrieval now requires authentication and validates the path.
9. Response headers, focus visibility, reduced motion, and mobile menu state were hardened.

No push, deployment, Azure resource, production Stripe call, or paid resource was performed.
