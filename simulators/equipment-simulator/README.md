# FitHub equipment simulator

The simulator is an interactive Node.js console controller for the same `/api/device/equipment/{equipmentId}` HTTP API intended for a future ESP32. It never connects to PostgreSQL.

Configure the variables shown in `.env.example`, then run `npm start`. The controls are claim, sharing, occupied, release, detected, clear, heartbeat, disconnect and maintenance. `npm run demo` sends a complete valid sequence through the real API, `npm run show-controls` prints the mappings without sending them, and `npm test` verifies all control mappings and credential headers.

Device credentials are sent in `X-FitHub-Device-Id` and `X-FitHub-Device-Key`. Only a PBKDF2 password hash is stored by the API. The simulated card UID is HMAC-hashed before lookup.
