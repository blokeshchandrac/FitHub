# TC-F08 — Connected parking simulation

- **Requirements:** FR-29–FR-30
- **Layout:** three rows of four spaces (12 total)
- **Mode:** hosted Administrator Connected Mode

## Result

1. Connected Mode established the API and normal `ParkingHub` connection.
2. Camera coverage included all 12 spaces.
3. Empty scan reported the expected available totals.
4. Correct parking in P-A1 changed only that space.
5. A two-space obstruction across P-A1/P-A2 marked both affected spaces and created exactly one active incorrect-parking alert.
6. Repeating the same scan did not duplicate the alert.
7. Correcting the parking resolved the alert.
8. Camera Offline changed the affected observations to Unknown rather than Available.
9. A 35% confidence scan remained Unknown; normal confidence restored the correct states.
10. The normal operational parking page showed the persisted values without refresh. No number plate, driver identity or face data was collected.

**Decision:** Passed.

**Evidence:** `screenshots/TC-F08-correct-parking.jpg`, `screenshots/TC-F08-two-space-obstruction.jpg`, `screenshots/TC-F08-offline-low-confidence.jpg`, `screenshots/TC-F08-operational-parking.jpg`.

