# Stage 6 equipment and fault API

All member endpoints use JWT authentication. Live equipment operations require an active membership and current check-in; administrator endpoints require the `Administrator` role.

## Member and administrator equipment API

| Method | Route | Purpose |
|---|---|---|
| GET | `/api/equipment` | Returns live state only when the caller has location access; otherwise returns a summary. |
| GET | `/api/equipment/{id}` | Checked-in live detail and complete history. |
| POST | `/api/equipment/{id}/claim` | Claim an available machine or the sole partner position. |
| POST | `/api/equipment/{id}/sharing-allowed` | Owner enables one-partner sharing. |
| POST | `/api/equipment/{id}/occupied` | Owner returns the shared machine to occupied. |
| POST | `/api/equipment/{id}/release` | Explicit member release. Body: `{ "clearanceConfirmed": false }`. |
| POST | `/api/equipment/{id}/override` | Administrator override. Body contains `state` and mandatory `reason`. |

A stale update returns HTTP 409. A successful database commit emits `equipmentChanged` from `/hubs/equipment`; no broadcast occurs before persistence succeeds. The hub uses the checked-in location policy.

## Shared device API

The simulator and a future ESP32 use exactly these routes. Requests include `X-FitHub-Device-Id` and `X-FitHub-Device-Key`. The raw key is never persisted; ASP.NET Core's PBKDF2 password hasher verifies its stored hash.

| Method | Route suffix | JSON body |
|---|---|---|
| POST | `/claim` | `{ "cardUid": "..." }` |
| POST | `/action` | `{ "action": "sharing-allowed|occupied|release|confirm-clearance|maintenance", "cardUid": "..." }` |
| POST | `/sensor` | `{ "detected": true|false }` |
| POST | `/heartbeat` | none |
| POST | `/disconnect` | none |

Card UIDs are HMAC-SHA256 hashed with a configured pepper before lookup. Device routes use application services and EF Core like every other client; they have no database shortcut.
Card claim and member actions are accepted only when the mapped user has an active membership and a recent open check-in.

## Fault API and photos

- `GET/POST /api/fault-reports` lists and creates the member's reports. Creation uses multipart form data with `equipmentId`, `category`, optional `comment`, `seriousSafetyConcern` and optional `photo`.
- Categories: `BrokenPart`, `UnusualSound`, `SafetyConcern`, `DisplayElectricalProblem`, `Cleanliness`, `Other`. `Other` requires a comment. Only a `SafetyConcern` may be serious.
- Photos are restricted to JPEG, PNG or WebP and 5 MB. PostgreSQL stores only the returned URL. `IPhotoStorage` isolates local disk storage so an Azure Blob implementation can replace it.
- `GET/PUT /api/administration/fault-reports` supports `Open`, `UnderReview`, `Scheduled`, `Resolved`, and `Rejected`. Updates require maintenance notes; resolved/rejected reports receive a resolution date.

## Development configuration

Set `EquipmentDevices__CardUidPepper`, `SeedData__EquipmentDeviceCredential`, and `SeedData__MemberCardUid` using environment variables or user-secrets. Only placeholders are committed. The seeded device IDs use `SIM-{equipment code}`.
