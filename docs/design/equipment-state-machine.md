# Stage 6 equipment state machine

## States and presentation

| State | Colour | Meaning |
|---|---|---|
| Available | Green | May be claimed by one member. |
| Occupied | Red | Has one owner and cannot be claimed by another member. |
| SharingAllowed | Yellow | The owner permits exactly one partner to join. |
| Maintenance | Grey | Withdrawn from use because of maintenance or a safety report. |
| Offline | Dark grey | The device disconnected or missed its heartbeat deadline. |

## Transitions

- `Available -> Occupied`: a checked-in member or recognized member card claims the machine.
- `Occupied -> SharingAllowed`: only the owner enables sharing.
- `SharingAllowed -> Occupied`: the owner disables sharing; any partner assignment is cleared.
- While `SharingAllowed`, one non-owner may claim the partner position. A second partner is rejected.
- A partner release removes only the partner. If the owner releases while a partner is present, the partner becomes the owner and the state becomes `Occupied`.
- Owner release without a partner changes the state to `Available`, unless the sensor still detects an object. A detected object requires explicit clearance confirmation.
- `Maintenance` is entered by an administrator, the authenticated device maintenance control, or a serious safety report.
- `Offline` is entered on explicit disconnect or heartbeat timeout. A later heartbeat restores the state that existed before the outage; it does not infer availability.
- An administrator may override a state only with a recorded non-empty reason.

## Ultrasonic clearance rule

`sensor clear` records absence and starts a 30-second grace period for occupied or shared equipment. It never changes the current state to `Available`. Availability requires one of:

1. explicit owner/card release;
2. `confirm-clearance` after the sensor has remained clear for the full grace period; or
3. an administrator override with a reason.

Every state and sensor event is appended to `EquipmentStatusEvents` with its previous state, resulting state, event type, time, optional user and note. PostgreSQL `xmin` optimistic concurrency prevents simultaneous claims from both succeeding.

## Location policy

Member live details, commands and the SignalR hub require a currently open attendance record created within the last 18 hours. This prevents an abandoned open attendance record from granting indefinite access. Trainers and administrators may access the operational view. Authenticated users outside the gym receive only code, name and location; state, colour, owner, sensor data and history are withheld.
