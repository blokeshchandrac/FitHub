# TC-F06 — Connected smart equipment

- **Requirements:** FR-21–FR-24
- **Machine:** TREAD-001
- **Users:** two synthetic active Members plus one rejected third Member
- **Final deployed correction:** `240c61ff70b0c18e955339f445d4ebd498360256`

## Connected workflow result

The hosted simulator completed:

`Available → Occupied → SharingAllowed → ClearanceWarning → SharingAllowed → ClearanceWarning → Available`

- First Member claimed the persisted machine.
- Sharing was enabled and one partner joined.
- A third Member was rejected without changing owner or partner.
- One assigned Member leaving did not start clearance.
- Both leaving started one 30-second warning.
- A return cancelled the warning and restored sharing.
- Both leaving again and remaining outside for the real 30 seconds released the machine.
- PostgreSQL ended with state `Available`; owner, partner and grace timestamp were null.
- The Administrator operational page updated through the normal `EquipmentHub` path.
- No false `BACKEND_UNAVAILABLE` or false sensor-entry event appeared.

## Active Member real-time retest

P3-004 originally denied the hub to a non-checked-in Active Member. After `240c61f`, Priya saw all four persisted machine states. An Administrator override changed TREAD-001 `Available → Maintenance → Available`; the already-open Member page updated both times without refresh. The final API result had null owner, partner and grace fields. No new SignalR error appeared.

**Decision:** Passed after correction P3-004.

**Evidence:** `screenshots/TC-F06-clearance-warning.jpg`, `screenshots/TC-F06-equipment-auto-release.jpg`; CI run 40141094047; hosted API and UI observations documented above.

