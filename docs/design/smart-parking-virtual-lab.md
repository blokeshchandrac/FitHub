# FitHub Smart Parking Virtual Lab

## Purpose and boundaries

`/simulation/parking` is a development and presentation feature. It is a Three.js driving environment backed by deterministic geometry calculations; it does not claim to run OpenCV or identify real vehicles. Cars use fictional identifiers only. The page never sends driver, Member, payment, email or registration data.

The route is lazy-loaded independently from `/simulation/equipment`. Opening either lab does not create the other Three.js scene. `/simulation` is a small selection page linking to both labs and the normal FitHub application.

## Parking environment

The scene contains twelve spaces in three rows: `P-A1`–`P-A4`, `P-B1`–`P-B4`, and `P-C1`–`P-C4`. Every space is 4.4 × 5.8 virtual metres. Cars have a 2 × 4.2 metre footprint and start on the entrance road. A high-corner camera and translucent coverage volume show the monitored area.

Cars use delta-time acceleration, braking, damping and steering. WASD and arrow keys drive, Space brakes, Shift raises the temporary speed limit, and R returns the selected car to the entrance. Movement is constrained to the parking floor and uses proximity boundaries for other cars and the camera pole.

## Virtual camera detection

`VirtualParkingVisionService` clips the oriented four-corner car polygon against each parking-space polygon. The intersection area is divided by the car footprint area. Default thresholds are centralized in `PARKING_VISION_DEFAULTS`:

- at least 80% of the footprint in one space;
- no more than 10% overlap with a neighbouring space;
- alignment within 15 degrees, treating forward and reverse parking as equivalent;
- at least 10% overlap before a space is considered affected;
- observations below 60% confidence remain `UNKNOWN`.

A correct result marks one space `OCCUPIED`. Substantial overlap across two spaces marks both `MISUSED_OR_BLOCKED`, but detected-car totals still count the car once. An ambiguous single-space overlap becomes `UNKNOWN`. Cars on the road or driving lanes do not affect a space. Camera Offline and Low Confidence deliberately return `UNKNOWN` rather than inferring availability.

## Scan lifecycle

Normal mode schedules a scan every 60 elapsed seconds. Demo Speed schedules every five seconds. The render loop compares `performance.now()` with the next deadline, so the interval is independent of frame rate. **Scan Now** invokes the identical capture and analysis pipeline. The latest preview is rendered once from the fixed virtual camera and is not a live video stream.

## Alert lifecycle

The browser deduplication key combines vehicle identifier and sorted affected spaces. Repeated unchanged scans update no additional local alert. A later scan without that violation resolves the alert.

Connected Mode submits a batch containing all twelve observations plus active geometry violations. The backend key additionally includes `simulationSessionId`. `ParkingSimulationService` creates or observes one `ParkingViolationAlert`, resolves missing active alerts for the session, commits the states and alert transaction, then broadcasts `parkingChanged` and `parkingAlertChanged` through the existing parking SignalR hub. The Administrator parking page displays active and resolved alerts.

## Security and runtime modes

Local Mode performs all driving, capture, detection, alert and history work in the browser and sends nothing to FitHub.

Connected Mode requires:

1. `Simulation:Enabled=true`;
2. ASP.NET Core `Development` or automated `Testing` environment;
3. an authenticated user with the `Administrator` policy.

Normal Docker and Development use the real application services. Production does not accept parking-simulation scans. The endpoint accepts only `SIM-PARK-CAMERA-01`, the twelve known simulation spaces, valid state/confidence values and the supported two-space violation type. No secret is embedded in Angular.

## Manual demonstration

1. Open `/simulation/parking`, dismiss Help, add a car and click it.
2. Drive into `P-A1`, align with the painted rectangle and choose **Scan Now**. Confirm one occupied space and one detected car.
3. Reset the car, drive across the `P-A2`/`P-A3` line and scan. Confirm two blocked spaces and one active alert.
4. Scan again without moving. Confirm the active-alert count remains one.
5. Move completely into `P-A2` and scan. Confirm `P-A2` occupied, `P-A3` available and the alert resolved in history.
6. Move onto the road and scan. Confirm the space is available and the car is not counted as parked.
7. Set Camera Offline and scan. Confirm all twelve spaces become unknown. Restore Online and scan again.
8. Sign in as an Administrator, return to the simulator, enable Connected Mode and repeat. Open the normal Administrator parking page to verify persisted space history and active/resolved alert state.
