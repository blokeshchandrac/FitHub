# Stage 7 parking simulation

## Scope and honesty

The browser grid in `simulators/parking-simulator` is a synthetic event generator. A click is treated as if an external detector reported a result. It does not open images, run a camera, use OpenCV, or claim computer-vision accuracy.

## State model

| State | Meaning | Synthetic detection allowed? |
|---|---|---|
| Available | No vehicle reported | Yes |
| Occupied | Vehicle reported | Yes |
| Unknown | Detection is uncertain | Yes |
| Disabled | Space excluded administratively | No |

Operational detection sources can move among Available, Occupied and Unknown, including recording a newer result with the same state. They cannot enter Disabled and cannot update a disabled space. Administrators may correct any state to any state, but a non-empty reason is mandatory.

Each current result stores state, confidence from 0 through 1, source and event time. The append-only event stores previous state, resulting state, confidence, source, event type, time and correction reason. Events older than the current result and events more than five minutes in the future are rejected.

## API boundary and authentication

The simulator and a future OpenCV service share an independent detection interface:

- `GET /api/parking/detection/spaces`
- `POST /api/parking/detection/events`

Both use `X-FitHub-Camera-Id` and `X-FitHub-Camera-Key`. ASP.NET Core's PBKDF2 password hasher verifies the per-camera credential hash; raw keys are configuration only. The detector supplies `spaceCode`, `state`, `confidence`, `source` and `eventTime` and never accesses PostgreSQL directly.

Member endpoints use `GET /api/parking`. Administrators use `POST /api/parking` to add spaces and `POST /api/parking/{id}/correction` to correct results. After a database commit, `parkingChanged` is broadcast from `/hubs/parking`.
