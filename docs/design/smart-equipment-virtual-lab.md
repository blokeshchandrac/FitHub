# FitHub Smart Equipment Virtual Lab

## Purpose and safety boundary

`/simulation/equipment` is an educational Three.js demonstration of FitHub equipment rules. `/simulation` is the shared virtual-lab selection page. The equipment simulation is not firmware emulation, an electrical simulator, or a user role. Primitive models sit behind a GLB loader boundary so licensed models can replace them later.

Simulation is available only when `Simulation:Enabled` is true and the API environment is `Development` or `Testing`. Production returns `404` and does not map `/hubs/simulation`. The simulation never reads payments, passwords, email addresses, or attendance data and never changes Member, membership, card, payment, or production equipment records.

## Member catalogue

`GET /api/simulation/members` reads users in the existing Member role and returns only the Member ID, display name, effective membership status, card identifier/status, account status, and card source. Active membership uses the same UTC status/start/end rule as FitHub. An existing `MemberAccessCard` is represented by its record ID; its one-way UID hash is never returned. A development Member without an issued card receives a deterministic `SIM-CARD-{memberId}` presentation token so the registered account can be demonstrated without creating a database record.

The **Refresh Members** action reloads this projection after registration or a confirmed Stripe test webhook. If no development Members exist, the lab shows an explicit empty state. Member status scenarios (`Actual`, active, expired, blocked card, or suspended account) live only in the current browser/request session and never update PostgreSQL.

## Structure

- `SimulationPageComponent` coordinates lifecycle, pointer/keyboard controls, transitions, and meaningful connected events.
- `SimulationSceneService` owns the single animation loop, Three.js resources, ray casting, camera-relative movement, world-space sensor observations, label layout, resize handling, and disposal.
- `SimulationEngineService` owns independent machine state, Member/card assignments, overrides, event history, and monotonic clearance deadlines.
- `SimulationApiService` owns the development Member catalogue, event API, and SignalR connection.
- Focused card-tray, workbench, and HUD components keep controls outside the canvas.
- `SimulationMemberCatalogService` performs the minimum database projection; `SimulationSessionService` keeps connected machine sessions in bounded server memory.

## Clearance state machine

The lab displays `AVAILABLE`, `OCCUPIED`, `SHARING_ALLOWED`, `CLEARANCE_WARNING`, `TEMPORARILY_UNAVAILABLE`, and `OFFLINE_UNKNOWN` as text in the inspector. A permanent floor border communicates the same state: green, red, yellow, pulsing orange, dark patterned grey, or interrupted grey. Floating state tags are not used.

Each machine has an independent 30-second deadline (`Simulation:ClearanceSeconds`) and a 2.2 m display threshold (`Simulation:ClearanceThresholdMetres`). On every render frame, the scene tests the interpolated world position of every assigned Member against the machine's directional cone. Hysteresis prevents boundary flicker.

- Any assigned Member inside produces `PERSON_DETECTED`.
- All assigned Members outside produces `AREA_CLEAR` and starts a monotonic `performance.now()` deadline.
- A return restores the prior `OCCUPIED` or `SHARING_ALLOWED` state and records `CLEARANCE_COUNTDOWN_CANCELLED`.
- Thirty elapsed seconds clears assignments and records `AUTOMATIC_RELEASE_COMPLETED`.
- A power or HC-SR04 fault cancels the deadline and enters `OFFLINE_UNKNOWN`; sensor uncertainty never releases a machine.
- On a shared machine, the timer starts only after both owner and partner are outside.

Only zone transitions, countdown start/cancel/complete, claims, card actions, hardware faults, and other meaningful actions are logged or sent to the connected API. Pointer movement is never sent per frame.

## Movement and labels

Click an avatar to select it. WASD and arrow keys move relative to the camera's floor direction with frame-delta speed; Shift adds a small boost. Pointer dragging uses pointer capture, canvas-relative ground-plane ray casting, exponential interpolation, gym bounds, and simple machine collision rectangles. Form controls and menus retain arrow-key behavior.

Member labels are non-raycastable sprites anchored above the head. Their scale is distance-aware, extremely distant labels are hidden, collision checks offset lower-priority labels, and the selected Member wins collisions. Context menus are clamped to the viewport. `TAP HERE` appears only while a card is held.

## Connected event contract

`POST /api/simulation/events` accepts only explicitly simulated events with a `SIM-` session ID, known equipment/device pair, UTC observation time, optional catalogue card ID, and an enum-validated session override. The backend re-reads current Member status for card events, applies the override to an in-memory snapshot, updates the isolated session, and publishes `simulationEquipmentChanged` through `/hubs/simulation`.

Representative events include `ENTRY_SCAN_APPROVED`, `MACHINE_CLAIMED`, `SHARING_ENABLED`, `SECOND_MEMBER_JOINED`, `SENSOR_ZONE_ENTERED`, `SENSOR_ZONE_EXITED`, `CLEARANCE_COUNTDOWN_STARTED`, `CLEARANCE_COUNTDOWN_CANCELLED`, `AUTOMATIC_RELEASE_COMPLETED`, `SENSOR_FAULT`, and `DEVICE_OFFLINE`.

## Manual demonstration

1. Open `/simulation/equipment`, close Help, and choose **Refresh Members**.
2. Select an existing active development Member and scan their card at the entrance.
3. Click the avatar and move with WASD/arrows. Double-click, take the NFC card, and scan the selected machine.
4. Move outside the blue cone. Verify the popup begins at `00:30`, the orange border pulses, and the machine remains unavailable.
5. Return before zero and verify the original active state returns. Leave again and wait for automatic release.
6. Enable sharing and add a second active Member. Verify one departure does not start a timer, both departures do, and either return cancels it.
7. Set the first Member to expired, retry entry, restore `Actual`, and verify the database status remains unchanged.
8. Disconnect PN532, sensor, power, and LEDs; verify safe state and hardware mismatch behavior.
9. Enable Connected Mode and confirm API and SignalR status plus isolated events in the server log.

Three.js remains route-lazy. The scene caps pixel ratio, uses one capped-delta animation loop, reuses scratch vectors, throttles label layout, and disposes renderer, controls, observers, materials, textures, and listeners on exit.
