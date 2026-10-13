# FitHub Phase 2 Manual System and Acceptance Test Evidence

**Execution dates:** 16–17 September 2026  
**Environment:** Local Development, Docker Compose, Stripe Test Mode  
**Repository:** `blokeshchandrac/FitHub`  
**Testing approach:** Manual system and acceptance testing, matching the submitted Phase 1 report

## 1. Evidence policy and scope

This document records browser-based manual workflows. PostgreSQL queries, application logs and source inspection are used only to corroborate what was observed in the user interface or to identify the cause of a failure. Build output is a startup/readiness check, not a substitute for manual testing.

All people and accounts in this document are synthetic development data. Passwords, raw NFC UIDs, QR tokens, access/refresh tokens, Stripe secret values, Checkout/PaymentIntent identifiers and device credentials are deliberately omitted. The standard Stripe Test Mode Visa ending `4242` is test data supplied by Stripe, not a real payment card.

This pass preserves the Phase 1 structure:

| Test case | Requirements | Scenario |
| --- | --- | --- |
| TC-01 | FR-01–FR-05 | Account, profile and role-based access |
| TC-02 | FR-06, FR-07 and FR-09 | Membership plan, payment and payment record |
| TC-03 | FR-10–FR-11 | QR attendance and attendance history |
| TC-04 | FR-12–FR-16 | Workout planning, logging and progress tracking |
| TC-05 | FR-17–FR-18 | Dashboard and rule-based guidance |
| TC-06 | FR-21–FR-24 | Smart-equipment status and clearance protection |
| TC-07 | FR-25, FR-27–FR-28 | Equipment fault reporting and safety control |
| TC-08 | FR-29–FR-30 | Parking availability and uncertain detection |
| TC-09 | FR-33, FR-35–FR-36 | Notifications and administration |

Could Have requirements are described only where the associated implementation exists. They were not substituted for the nine submitted manual cases.

## 2. Application startup information

### Working startup command

```powershell
docker compose --env-file .env up -d --build
```

### Working local endpoints

| Service | URL/port | Observed result |
| --- | --- | --- |
| Angular frontend | `http://localhost:4040/` | Running |
| ASP.NET Core API | `http://localhost:8080/` | Running |
| API health | `http://localhost:8080/health` | Successful response |
| PostgreSQL | `localhost:5432` | Container healthy |
| Equipment simulator | `http://localhost:4040/simulation/equipment` | Integrated Angular/API route |
| Parking simulator | `http://localhost:4040/simulation/parking` | Integrated Angular/API route |

The simulators use the frontend, API and PostgreSQL containers; no separate simulator container is required. The frontend image uses Node `22.22.3`, matching `.nvmrc`. A production Angular build completed with existing simulator stylesheet-budget warnings. No budget was increased.

Evidence: [`evidence/startup-verification.log`](evidence/startup-verification.log)

## 3. Manual test-data register

### 3.1 Accounts

| Email | Synthetic name | Role | Effective membership on 2026-09-17 | Onboarding | Branch | Assigned trainer | Password source |
| --- | --- | --- | --- | --- | --- | --- | --- |
| `phase2.registration.20260916@fithub.test` | Elena Martinez | Member | Active, Monthly, expires 2026-10-16 | Complete | DEMO | Tariq Manualtrainer | Tester-held; not stored in repository |
| `phase2.active-member@fithub.test` | Priya Testmember | Member | Active, Monthly, expires 2026-10-16 | Complete | DEMO | None | Ignored `.env` variable `DEMO_SEED_PASSWORD` |
| `active-member@fithub.demo` | Active Member | Member | Active, Monthly, expires 2026-10-16 | Complete | DEMO | None | Ignored `.env` variable `DEMO_SEED_PASSWORD` |
| `no-membership@fithub.demo` | No Membership | Member | Expired on 2026-09-13; one historical Pending record also exists | Complete | DEMO | None | Ignored `.env` variable `DEMO_SEED_PASSWORD` |
| `member@fithub.local` | Morgan Member | Member | None | Complete | DEMO | None | Ignored `.env` variable `DEMO_SEED_PASSWORD` |
| `phase2.manual-trainer@fithub.test` | Tariq Manualtrainer | Trainer | Not applicable | Not Member-gated | DEMO | Elena is assigned to this Trainer | Ignored `.env` variable `DEMO_SEED_PASSWORD` |
| `phase2.trainer@fithub.test` | Theo Testtrainer | Trainer | Not applicable | Not Member-gated | DEMO | None | Ignored `.env` variable `DEMO_SEED_PASSWORD` |
| `trainer@fithub.local` | Terry Trainer | Trainer | Not applicable | Not Member-gated | DEMO | None | Ignored `.env` variable `DEMO_SEED_PASSWORD` |
| `phase2.manual-admin@fithub.test` | Amina Manualadmin | Administrator | Not applicable | Not Member-gated | DEMO | Assignment creator | Ignored `.env` variable `DEMO_SEED_PASSWORD` |
| `phase2.admin@fithub.test` | Amara Testadmin | Administrator | Not applicable | Not Member-gated | DEMO | None | Ignored `.env` variable `DEMO_SEED_PASSWORD` |
| `admin@fithub.local` | Ada Admin | Administrator | Not applicable | Not Member-gated | DEMO | None | Ignored `.env` variable `DEMO_SEED_PASSWORD` |

The complete password value is intentionally absent from this document. A tester runs the application with a locally configured, ignored `.env` and retrieves `DEMO_SEED_PASSWORD` from that local environment. It must not be committed.

### 3.2 Profile entered for the acceptance Member

| Field | Exact value |
| --- | --- |
| Legal/display name | Elena Martinez |
| Preferred name | Elena |
| Date of birth | 1998-04-15 |
| Height | 168 cm |
| Weight | 64 kg |
| Primary goal | Improve strength / stored Strength goal |
| Activity level | Moderately active / stored Moderate level |
| Experience level | Beginner |
| Planned training days | Monday, Wednesday, Friday |
| Preferred branch | `DEMO` — FitHub Demonstration Branch |

### 3.3 Plans, branch and operational records

| Data type | Exact development values |
| --- | --- |
| Membership plans | Monthly — EUR 29.90 — 1 month — “Flexible monthly membership”; Annual — EUR 499.00 — 12 months — “Twelve-month membership” |
| Branch | `DEMO` — FitHub Demonstration Branch — Active |
| Equipment | `BIKE-001` Exercise bike; `CABLE-001` Cable machine; `RACK-001` Power rack; `TREAD-001` Treadmill |
| Equipment starting state for TC-06/07 | Available |
| Persisted Member access card | ID `c4f1aa9a-67a4-4c9c-8f12-75fbaa7d85a1`, assigned to `member@fithub.local`, Active; raw UID is only in ignored configuration and is not evidence |
| Simulator Member cards | Session-only `SIM-CARD-<member-id>` identifiers; not separate customer records |
| Exercises | Back Squat — Legs — Barbell; Bench Press — Chest — Barbell and bench; Lat Pulldown — Back — Cable machine |
| Parking spaces | Existing `A-01`, `A-02`, `B-01`; simulation `P-A1`–`P-A4`, `P-B1`–`P-B4`, `P-C1`–`P-C4` |
| Parking camera | `SIM-PARK-CAMERA-01` |
| Fault categories | Broken part; unusual sound; safety concern; display/electrical problem; cleanliness; other |
| Fault statuses | Open; UnderReview; Scheduled; Resolved; Rejected |
| Notification preferences | No preference entity/table or preference interface exists |

### 3.4 Prepared Member records created during execution

| Record | Exact value/result |
| --- | --- |
| Payment | Monthly Purchase; Succeeded; EUR 29.90; paid `2026-09-16T21:10:30.140392Z`; non-demo |
| Membership | Active from `2026-09-16T21:10:30.140392Z` to `2026-10-16T21:10:30.140392Z` |
| Attendance | Checked in `2026-09-17T05:36:25.102869Z`; not checked out |
| Personal workout template | Phase 2 Strength A |
| Completed workout | Back Squat and Bench Press; total volume 2,450 kg |
| Body measurement | 64 kg, 168 cm, 22.5% body fat, chest 90 cm, waist 72 cm, hip 96 cm |
| Trainer plan | Elena Strength Progression, owned by Tariq, active assignment to Elena |
| Trainer assignment | Tariq→Elena, Active, assigned `2026-09-17T05:35:26.687502Z` by Amina |
| Fault report | TREAD-001, Safety concern, UnderReview |

## 4. Implementation status by requirement

The baseline contains the expected 37 requirements: 16 Must Have, 13 Should Have and 8 Could Have. No different FR numbering was found in the runtime results. Earlier repository test documents use feature-specific IDs in addition to, rather than instead of, the Phase 1 FR identifiers.

| ID | Priority | Status determined in this pass | Concrete basis |
| --- | --- | --- | --- |
| FR-01 | Must | **Partially implemented** | Registration works with unique email/password/name, but no required consent field or persisted consent evidence exists. |
| FR-02 | Must | **Implemented** | Member, Trainer and Administrator logins produced role-specific, authenticated navigation and timed JWT/refresh sessions. |
| FR-03 | Should | **Not implemented** | UI/API accept a forgot-password request, but the generated token is discarded and no single-use email link/reset completion exists. |
| FR-04 | Must | **Implemented** | Backend role policies, ownership filters and persisted Trainer–Member authorization were exercised; Member was denied `/administration`. |
| FR-05 | Must | **Implemented** | Full profile including goal, experience, weekdays, height, weight and active branch persisted through onboarding. |
| FR-06 | Must | **Implemented** | Server-backed plans were visible; Administrator plan endpoints and active flags exist. |
| FR-07 | Must | **Implemented** | Stripe Test Mode checkout used the server price; a confirmed payment activated membership; redirect alone was not the recorded source of activation. |
| FR-08 | Could | **Partially implemented** | Renewal/upgrade request types and membership history exist, but an eligible renewal/upgrade was not manually executed in this pass. |
| FR-09 | Should | **Implemented** | Member payment history/receipt control and Administrator payment records were reachable; the test payment appeared once as Succeeded. |
| FR-10 | Must | **Implemented after DEF-001 fix** | Short-lived QR generated, accepted once, reuse rejected, attendance persisted. |
| FR-11 | Should | **Implemented** | Member visit history/frequency and Administrator attendance totals/trend were visible. |
| FR-12 | Should | **Implemented** | Member personal template and Trainer-owned reusable plan/assignment were created and persisted separately. |
| FR-13 | Must | **Partially implemented** | Exercises, sets, repetitions, weight, duration and notes persist; no dedicated rest-time value exists. |
| FR-14 | Must | **Implemented** | Completed session history and prior exercise set performance were visible. |
| FR-15 | Should | **Implemented** | Personal records and conservative progression targets were displayed from stored sets. |
| FR-16 | Should | **Implemented** | Body measurements persisted and appeared in progress/BMI history. |
| FR-17 | Must | **Implemented** | Dashboard calculated attendance/workout frequency, volume, records, streak, consistency and cost/visit from stored records. |
| FR-18 | Must | **Implemented** | Explainable recommendations used goal, experience, planned days, attendance and workouts; empty history returned “Not enough data.” |
| FR-19 | Could | **Not implemented** | No Gemini question workflow was found or advertised. |
| FR-20 | Could | **Implemented** | General calories/macronutrient estimates were produced from the completed profile with a non-medical disclaimer. |
| FR-21 | Must | **Implemented** | Current equipment state and timestamps were visible to authorized Administrator/Member views. |
| FR-22 | Must | **Partially implemented** | Connected claim, sharing and second-member join persisted; third member was rejected. A normal end-to-end release was not completed in this pass. |
| FR-23 | Must | **Implemented but defective** | Countdown logic exists, but connected execution produced contradictory enter/exit/start/cancel events and did not complete a stable 30-second release. |
| FR-24 | Must | **Implemented but defective** | Device/simulator events authenticate, persist and publish through SignalR, but the clearance transition emitted a spurious backend-unavailable event and inconsistent state. |
| FR-25 | Should | **Implemented** | Member created a selected-machine/category/comment fault report. |
| FR-26 | Could | **Partially implemented** | Photo input, content validation and local photo storage code exist; no photo was uploaded manually in this pass. |
| FR-27 | Should | **Implemented** | Administrator moved the fault to UnderReview with maintenance notes and notification. |
| FR-28 | Must | **Implemented** | Administrator placed TREAD-001 into Maintenance and later cleared it, with persisted history. |
| FR-29 | Should | **Implemented** | Authorized Member and Administrator parking views showed Available/Unknown spaces and totals. |
| FR-30 | Should | **Implemented** | Authenticated simulator submitted confidence-aware observations, persisted them and published ParkingHub updates. |
| FR-31 | Could | **Partially implemented** | Member list/book/cancel and capacity/duplicate logic exist across UI/API/database, but the workflow was not manually executed in this pass. |
| FR-32 | Could | **Partially implemented** | Trainer create/edit and Administrator approve/reject flows exist; the full workflow was not manually executed in this pass. |
| FR-33 | Should | **Partially implemented** | Deduplicated personal-record and fault notifications were persisted and displayed; Member notification preferences do not exist. |
| FR-34 | Could | **Not implemented** | No assigned Trainer–Member messaging workflow was found; Administrator announcements are not a complete substitute. |
| FR-35 | Should | **Implemented** | Administrator searched users, managed status and performed an audited Trainer–Member assignment. |
| FR-36 | Should | **Implemented** | Administrator dashboard used stored payments, attendance, equipment, parking, workout and fault data. |
| FR-37 | Could | **Partially implemented** | CSV operational exports and activity records exist, but a full filtered-export/security-event review workflow was not manually demonstrated. |

## 5. Manual test cases

### TC-01 — Account, profile and role-based access

**Requirements:** FR-01–FR-05  
**Objective:** Confirm registration, Member onboarding, role separation and Trainer assignment authorization.  
**Preconditions:** Docker services running; `DEMO` branch active; Elena email unused at the start; Phase 2 Admin/Trainer seeds present.  
**Accounts:** Elena Member, Amina Administrator, Tariq Trainer (see account register).  
**Concrete input:** Elena’s complete profile in section 3.2; trainer assignment Tariq→Elena.

| Step | Manual action | Exact expected result | Actual observed result |
| ---: | --- | --- | --- |
| 1 | Open `/register`; enter Elena Martinez, `phase2.registration.20260916@fithub.test`, tester-held password and confirmation; submit. | Unique account created; duplicate email rules remain server-enforced; new Member enters onboarding. | Account created and browser opened `/welcome`. The form had no consent input. |
| 2 | Enter preferred name Elena, DOB 1998-04-15, 168 cm, 64 kg, Improve strength, Moderately active, Beginner, Monday/Wednesday/Friday and DEMO branch; submit. | Profile persists; onboarding completes; no normal dashboard until membership is active. | Accepted and redirected to `/membership/choose`; database profile contained all entered fields. |
| 3 | While signed in as the Member, enter `/administration`. | Backend/frontend deny Administrator features without exposing their contents. | Browser displayed `/unauthorized`. |
| 4 | Sign in as Amina and open Trainer administration; select Tariq and search Elena. | Administrator can find active Trainer and eligible active Member. | Both records appeared. |
| 5 | Assign Elena to Tariq. | One Active assignment is persisted with timestamp and assigning Administrator. | First attempt did nothing because of DEF-002. After the submit-handler fix, UI displayed “Member assigned to trainer”; database stored `2026-09-17T05:35:26.687502Z` and Amina’s user ID. |
| 6 | Sign in as Tariq and open assigned Members/workout assignments. | Trainer sees Elena, not a general list of all active Members. | After reload, Elena was the only assigned Member shown. |
| 7 | Open Forgot password and submit a known email. | A single-use, time-limited email link should be delivered and allow password reset. | Request returned a generic response, but code inspection confirmed the generated token is discarded; no email or reset-completion flow exists. |

**Actual decision:** **FAIL**. Registration, complete profile, roles and Trainer assignment worked after DEF-002, but FR-01 consent is missing and FR-03 is not implemented.  
**Evidence:** [`evidence/TC-01-account-profile-role.log`](evidence/TC-01-account-profile-role.log)  
**Defects/gaps:** DEF-002 fixed; GAP-001; GAP-002.

### TC-02 — Membership plan, Stripe Test Mode payment and payment record

**Requirements:** FR-06, FR-07, FR-09  
**Objective:** Confirm that the selected server-priced plan reaches Stripe Test Mode, backend confirmation activates membership, and the same amount appears in history.  
**Preconditions:** Elena registered and onboarded; no active membership; Stripe Test Mode keys/webhook configured locally.  
**Account:** `phase2.registration.20260916@fithub.test`.  
**Concrete input:** Monthly plan; displayed EUR 29.90; standard successful Stripe Test Mode Visa ending 4242, future expiry, synthetic CVC.

| Step | Manual action | Exact expected result | Actual observed result |
| ---: | --- | --- | --- |
| 1 | Open membership selection. | Active server-controlled plans show price/duration. | Monthly EUR 29.90/1 month and Annual EUR 499.00/12 months appeared. |
| 2 | Select Monthly and start checkout. | FitHub sends only the plan/action; Stripe-hosted Test Mode checkout displays the server amount. | Stripe Sandbox opened and displayed Monthly EUR 29.90. |
| 3 | Enter the successful Test Mode card and submit. | Stripe accepts test payment and redirects; FitHub waits for backend-confirmed state. | Stripe succeeded; FitHub returned to the app and membership became Active. |
| 4 | Open Membership. | Monthly is Active with one-month server-derived dates. | Start 2026-09-16, expiry 2026-10-16. |
| 5 | Open Payments. | Exactly one confirmed EUR 29.90 Purchase appears; pending/failed rows do not masquerade as revenue. | One Succeeded EUR 29.90 record appeared with Print action. |
| 6 | Corroborate PostgreSQL. | Payment and membership use the same user/plan/amount; no card data is stored. | Succeeded EUR 29.90 at `2026-09-16T21:10:30.140392Z`; matching Active membership through `2026-10-16T21:10:30.140392Z`; no card number/CVC columns. |

**Actual decision:** **PASS**.  
**Evidence:** [`evidence/TC-02-membership-payment.log`](evidence/TC-02-membership-payment.log)  
**Defect reference:** None.

### TC-03 — QR attendance and attendance history

**Requirements:** FR-10–FR-11  
**Objective:** Confirm that an active Member receives a short-lived QR credential, staff can consume it exactly once, and history/frequency update.  
**Preconditions:** Elena’s membership Active; Amina authenticated as Administrator; no Elena attendance before test.  
**Accounts:** Elena Member and Amina Administrator.  
**Concrete input:** Fresh QR token generated by FitHub; raw value omitted because it is an access credential.

| Step | Manual action | Exact expected result | Actual observed result |
| ---: | --- | --- | --- |
| 1 | As Elena, open Attendance and generate a check-in QR. | Short-lived credential is displayed without creating attendance yet. | Credential generated. |
| 2 | As Amina, paste it into `/attendance/scanner` and select “Validate and check in QR”. | Token is validated, one Attendance is created and an accepted time is shown. | **Original failure:** no request/row because form submit did not invoke `scan()` (DEF-001). |
| 3 | Apply the focused form-event fix, rebuild, generate a new QR and repeat step 2. | Same workflow now creates one visit. | “Check-in accepted at 9/17/2026, 7:36:25 AM”; PostgreSQL time `2026-09-17T05:36:25.102869Z`. |
| 4 | Submit the same token again. | Reuse is rejected; no second Attendance is created. | “Check-in token has already been used.” One database row remained. |
| 5 | Return to Elena’s Attendance page. | History and visit frequency include the accepted visit. | 1 this week, 1 this month, 1 current-membership visit; EUR 29.90 cost/visit; check-in time shown. |

**Original decision:** **FAIL** (DEF-001).  
**Retest decision:** **PASS** after fix.  
**Evidence:** [`evidence/TC-03-attendance.log`](evidence/TC-03-attendance.log)  
**Defects:** DEF-001 fixed and manually retested; DEF-003 visual field-state fix production-built, final visual retest pending.

### TC-04 — Workout planning, logging and progress tracking

**Requirements:** FR-12–FR-16  
**Objective:** Confirm personal templates, Trainer plans for assigned Members, session/set history, personal records and body measurements.  
**Preconditions:** Elena has active membership; Tariq is actively assigned to Elena; three exercise records exist.  
**Accounts:** Elena Member and Tariq Trainer.  
**Concrete input:** “Phase 2 Strength A”, “Elena Strength Progression”, six exact set rows and the measurement listed below.

| Step | Manual action | Exact expected result | Actual observed result |
| ---: | --- | --- | --- |
| 1 | As Elena, create “Phase 2 Strength A” with Back Squat and Bench Press. | Personal reusable template persists under Elena and is not changed into a Trainer plan. | Saved as `Personal`, owner Elena. |
| 2 | Start the template and log Back Squat: 8×60/45 s, 8×62.5/45 s, 6×65/50 s. Add note “Controlled depth; 90-second rest between sets.” | Sets persist with reps, weight, duration and notes. A dedicated rest-time value should also be available. | Set values persisted; rest could only be stored in notes. |
| 3 | Log Bench Press: 10×40/40 s, 8×42.5/45 s, 8×42.5/45 s. Add note that 90-second rest was recorded only in notes. Complete with the exact session note in section 3.4. | Completed immutable history appears with calculated performance. | Completed; stored volume 2,450 kg; history and set details visible. |
| 4 | Record 64 kg, 168 cm, 22.5% body fat, 90/72/96 cm and the baseline note. | Measurement persists and trend/BMI update. | Values persisted; BMI displayed as 22.7. |
| 5 | Review personal records. | Best recorded weights are derived from set history. | Back Squat 65 kg; Bench Press 42.5 kg. |
| 6 | As Tariq, create “Elena Strength Progression” with squat/bench and assign it to Elena. | Trainer owns the reusable plan; assignment allowed only because Elena is assigned to Tariq. | Plan saved as `TrainerPlan`; active assignment to Elena persisted. |

**Actual decision:** **FAIL** because FR-13 requires rest time and the data model/form have no dedicated field. Other mapped workflows passed.  
**Evidence:** [`evidence/TC-04-workouts-progress.log`](evidence/TC-04-workouts-progress.log)  
**Defect:** DEF-005 open.

### TC-05 — Dashboard and rule-based guidance

**Requirements:** FR-17–FR-18  
**Objective:** Confirm stored activity drives explainable dashboard metrics and recommendations, including honest insufficient-data handling.  
**Preconditions:** Elena profile complete; first observe before TC-03/04 activity, then observe after their stored records.  
**Account:** Elena Member.  
**Concrete data:** Monday/Wednesday/Friday plan, Beginner, Strength, one attendance, one 2,450 kg workout, one measurement.

| Step | Manual action | Exact expected result | Actual observed result |
| ---: | --- | --- | --- |
| 1 | Before attendance/workout data, open Recommendations and refresh. | No fabricated personal claim; response says “Not enough data” and identifies missing inputs. | “Not enough data” appeared with “No attendance history.” |
| 2 | After TC-03/04, open Dashboard. | Metrics are calculated from stored activity/payment/measurement data. | 0.2 visits/week, 0.2 workouts/week, 2,450 kg weekly volume, 1-week streak, 12.5% consistency, 1 visit, EUR 29.90/visit, 64 kg and both PRs. |
| 3 | Refresh Recommendations. | Guidance uses goal, experience, planned days, attendance and completed workouts; reasons are readable. | Recovery guidance, 0/12 planned-day consistency, missed Wednesday, squat 64 kg and bench 43.5 kg targets, and muscle groups to revisit appeared. |
| 4 | Inspect nutrition guidance. | General estimate is derived from valid profile information and contains a non-medical disclaimer. | 2,110 kcal, 102 g protein, 51 g fat, 311 g carbohydrate plus disclaimer. |

**Actual decision:** **PASS**.  
**Evidence:** [`evidence/TC-05-dashboard-recommendations.log`](evidence/TC-05-dashboard-recommendations.log)  
**Defect reference:** None.

### TC-06 — Smart-equipment status and clearance protection

**Requirements:** FR-21–FR-24  
**Objective:** Confirm real connected equipment state, Member/card decisions, claim, sharing limits, sensor clearance and SignalR/persistence.  
**Preconditions:** Amina authenticated; Simulation enabled in Development; TREAD-001 Available; Elena and Priya active; equipment mappings present.  
**Accounts:** Amina Administrator, Elena Member, Priya Member and a third active development Member.  
**Concrete input:** `/simulation/equipment`; EQ-TREADMILL-01 mapped to `TREAD-001`; 30-second configured clearance duration.

| Step | Manual action | Exact expected result | Actual observed result |
| ---: | --- | --- | --- |
| 1 | Open equipment Virtual Lab, Start Lab and select Connected Mode. | API and normal EquipmentHub connect; four virtual machines map to real equipment; Members load once. | API connected, SignalR connected/reconnecting state shown, four mappings and eight development Members loaded. |
| 2 | Scan Elena at entrance, take Elena’s session NFC card and drop it on the Treadmill reader. | Active membership/card authorizes; TREAD-001 becomes Occupied, Elena becomes owner and normal equipment view/history update. | Claim accepted and persisted; Occupied/owner history visible. |
| 3 | Enable Sharing/Flexible mode. | State becomes SharingAllowed and yellow sharing indication is used. | SharingEnabled persisted; SharingAllowed shown. |
| 4 | Enter Priya, take Priya’s card and scan the same machine. | One additional active Member joins as sharing partner. | Priya joined; Elena and Priya both displayed/persisted. |
| 5 | Try a third active Member. | Rejected without changing owner/partner. | `SHARING_FULL` denial; state remained shared. |
| 6 | Move Elena outside while Priya remains. | No clearance countdown because one assigned user remains. | No initial countdown was started. |
| 7 | Move Priya outside as well. | Enter ClearanceWarning once, show orange warning and count down 30 real seconds. | SENSOR_ZONE_EXITED and countdown-start events appeared, then BACKEND_UNAVAILABLE, SENSOR_ZONE_ENTERED and cancellation appeared in the same transition window. |
| 8 | Leave both outside until expiry. | Machine releases to Available, clears both assignments and broadcasts committed state. | Could not be completed because the contradictory transition cancelled the countdown and left the machine SharingAllowed. |
| 9 | Use connected Reset to return the test environment to a safe state. | Persisted mapped equipment returns to baseline Available with history. | Reset accepted; TREAD-001 returned to Available in PostgreSQL and Administration history. |

**Actual decision:** **FAIL**. Claiming/sharing/persistence work, but the defining FR-23 clearance path is defective and FR-24 emits inconsistent connected events.  
**Evidence:** [`evidence/TC-06-smart-equipment.log`](evidence/TC-06-smart-equipment.log)  
**Defect:** DEF-004 open.

### TC-07 — Equipment fault reporting and safety control

**Requirements:** FR-25, FR-27–FR-28  
**Objective:** Confirm a Member can report an exact machine fault and an Administrator can process it and safety-lock the machine.  
**Preconditions:** Elena active; TREAD-001 Available; Amina Administrator.  
**Accounts:** Elena Member and Amina Administrator.  
**Concrete input:** TREAD-001; Safety concern; “Emergency stop button remained depressed after release during Phase 2 manual test.”; no photo.

| Step | Manual action | Exact expected result | Actual observed result |
| ---: | --- | --- | --- |
| 1 | As Elena, create the fault using the exact equipment, category and comment. | One Open report persists under Elena; no image is required. | Submission succeeded and Open report appeared. |
| 2 | As Amina, open fault administration and set UnderReview with “Inspected by Amina; equipment isolated pending emergency-stop test.” | Status/note persist and Member receives a status notification. | Report became UnderReview; note persisted; notification was created. |
| 3 | In equipment administration, override TREAD-001 to Maintenance with “Safety lock for DEF/TC-07 emergency-stop investigation.” | State becomes safety-locked and no longer appears Available. | List displayed Maintenance; status history persisted. |
| 4 | After inspection test, clear to Available with “Phase 2 safety-lock test complete; returned to service.” | Maintenance clears explicitly and history remains. | Equipment returned to Available; fault remained UnderReview with its note. |

**Actual decision:** **PASS**.  
**Evidence:** [`evidence/TC-07-fault-safety.log`](evidence/TC-07-fault-safety.log)  
**Limitation:** A separate Member claim attempt while Maintenance was active was not executed in this pass. The lock transition and persistence were executed.

### TC-08 — Parking availability and uncertain detection

**Requirements:** FR-29–FR-30  
**Objective:** Confirm authenticated, confidence-aware camera observations update real parking state, and uncertainty never becomes false availability.  
**Preconditions:** Amina Administrator; Development Simulation enabled; 12 mapped P-* spaces; parking camera online.  
**Account:** Amina Administrator for simulation; Elena Member for read-only verification.  
**Concrete input:** `SIM-PARK-CAMERA-01`; P-A1–P-C4; normal confidence 0.98; Offline and Low-confidence controls.

| Step | Manual action | Exact expected result | Actual observed result |
| ---: | --- | --- | --- |
| 1 | Open `/simulation/parking`, select Connected Mode. | API verifies Administrator, ParkingHub connects and controls remain usable. | UI logged API VERIFIED, SIGNALR CONNECTED and Connected. |
| 2 | With empty simulation spaces, select Scan Now. | One batch analyses all 12; each persisted Available at high confidence; car count 0. | 12 observations persisted, 0 alerts; all P-* spaces Available at 0.98. |
| 3 | Open the normal Member Parking page. | Read-only page receives committed parking state; it cannot submit scans. | P-* spaces appeared as Available from `SIM-PARK-CAMERA-01`; totals updated. |
| 4 | Set Camera Offline and scan. | Monitored spaces become Unknown, not Available; offline event logged. | All 12 P-* spaces persisted Unknown; CAMERA_OFFLINE logged. |
| 5 | Return Online, enable low-confidence/obstructed view and scan. | Below-threshold observations remain Unknown rather than guessed. | All 12 remained Unknown; log stated confidence was below threshold. |
| 6 | Clear low-confidence and scan. | Correct high-confidence state is restored and broadcast. | All 12 restored to Available at 0.98 and the Member view reflected persisted values. |

**Actual decision:** **PASS** for FR-29 and FR-30.  
**Evidence:** [`evidence/TC-08-parking.log`](evidence/TC-08-parking.log)  
**Limitation:** The two-space obstruction/deduplicated-alert path was not repeated in this particular Phase 2 evidence pass.

### TC-09 — Notifications and administration

**Requirements:** FR-33, FR-35–FR-36  
**Objective:** Confirm Administrator management/analytics and Member notifications, including deduplication and preferences.  
**Preconditions:** TC-01 assignment, TC-02 payment, TC-03 attendance, TC-04 workout and TC-07 fault records exist.  
**Accounts:** Amina Administrator, Elena Member, Tariq Trainer.  
**Concrete data:** Tariq→Elena assignment; two personal records; Treadmill fault UnderReview.

| Step | Manual action | Exact expected result | Actual observed result |
| ---: | --- | --- | --- |
| 1 | As Amina, open Administrator Dashboard. | Cards/analytics derive from stored operational data, not Member personal-fitness cards. | Active Members/memberships, confirmed net revenue, payments, trainers, faults, attendance trend, parking and equipment utilization displayed. |
| 2 | Open Users/Trainers, search Tariq/Elena and inspect assignment. | Administrator can search and manage accounts; assignment states who/when. | Search worked; Active Tariq→Elena assignment showed Amina and `2026-09-17T05:35:26.687502Z`. |
| 3 | As Tariq, view assigned Members. | Only assigned Members appear. | Elena appeared after reload. |
| 4 | As Elena, open Notifications after the workout and fault review. | Relevant, nonduplicated events appear. | Three records appeared: Back Squat PR 65 kg, Bench Press PR 42.5 kg, and Treadmill fault UnderReview. Stable deduplication keys existed. |
| 5 | Open notification preferences and change an event preference. | FR-33 requires Member preferences to control personalised notifications. | No preference page, API, entity or table exists; this step could not be performed. |

**Actual decision:** **FAIL**. FR-35/FR-36 and deduplicated notifications worked; FR-33 remains partial because preferences are absent.  
**Evidence:** [`evidence/TC-09-notifications-administration.log`](evidence/TC-09-notifications-administration.log)  
**Defect:** DEF-006 open.

## 6. Manual-case totals

| Result | Count | Cases |
| --- | ---: | --- |
| Passed | 5 | TC-02, TC-03 (retest), TC-05, TC-07, TC-08 |
| Failed | 4 | TC-01, TC-04, TC-06, TC-09 |
| Blocked | 0 | — |
| Not Implemented at case level | 0 | — |
| **Total** | **9** | TC-01–TC-09 |

“Not Implemented” still applies to individual requirements FR-03, FR-19 and FR-34; each submitted test case spans several requirements, so its case-level decision is based on the complete scenario.

## 7. Defect and retest register

| ID | Original failed result | Likely/confirmed cause and affected files | Correction | Retest result |
| --- | --- | --- | --- | --- |
| DEF-001 | Scanner submit caused no request or Attendance. | Confirmed: plain form used `ngSubmit` without a form directive in `frontend/fithub-web/src/app/features/attendance/scanner/scanner-page.html`. | Native `(submit)` invokes `scan()` and prevents default. | **PASS**: accepted once, reuse rejected, row/history present. |
| DEF-002 | Trainer assignment submit caused no request/assignment. | Confirmed: same form-event issue in `frontend/fithub-web/src/app/features/administration/trainers/trainer-assignment-page.html`. | Native `(submit)` invokes `assign()` and prevents default. | **PASS**: assignment persisted and Trainer saw Elena. |
| DEF-003 | After successful check-in, the empty control also showed “Enter a valid token”. | Confirmed: `setValue('')` retained touched state in `scanner-page.ts`. | Use `token.reset('')` after success. | Production build passed; final browser-only visual retest outstanding. |
| DEF-004 | Shared equipment clearance started, emitted backend-unavailable/enter/cancel events and failed to release. | Not yet isolated; affected connected sensor transition/reconciliation between simulation engine/API and equipment state. | No simulator fix made during this testing/evidence task. | **OPEN**. Reproduce with both shared Members leaving. |
| DEF-005 | No dedicated rest-time value when logging a set/exercise. | Set contract/entity/form contain duration but no rest-time field. | Not changed in this task. | **OPEN**. |
| DEF-006 | Member cannot configure notification preferences. | No preference model/table/API/UI exists. | Not changed in this task. | **OPEN**. |
| GAP-001 | Registration cannot record required consent. | Registration request/form/entity contain no consent data. | Not changed in this task. | **OPEN**. |
| GAP-002 | Forgot-password does not deliver or consume a reset link. | Authentication service generates then discards the token; no delivery/reset completion. | Not changed in this task. | **OPEN**. |

Detailed transcript: [`evidence/defect-and-retest-register.log`](evidence/defect-and-retest-register.log)

## 8. Could Have requirement register

| Requirement | Current result | Manual-test decision for this pass |
| --- | --- | --- |
| FR-08 renewal/upgrade | Contracts and workflow are present, but no eligible transaction was executed. | Not executed; partial implementation evidence only. |
| FR-19 Gemini questions | No implementation found. | Not Implemented. |
| FR-20 nutrition estimates | 2,110 kcal / 102 g protein / 51 g fat / 311 g carbohydrate displayed with disclaimer. | Implemented and observed as part of TC-05. |
| FR-26 fault photographs | Input, validation and storage code exist; TC-07 intentionally used no photo. | Not executed; partial implementation evidence only. |
| FR-31 Member class booking/cancellation | UI/API/database logic exists. | Not executed in this pass; do not claim Pass. |
| FR-32 Trainer classes/Admin review | UI/API/database logic exists. | Not executed in this pass; do not claim Pass. |
| FR-34 Trainer–Member messaging/Admin announcements | No complete assigned-user messaging workflow found. | Not Implemented. |
| FR-37 filtered exports/security audit review | CSV exports and activity data exist, but a complete filtered workflow was not executed. | Not executed; partial implementation evidence only. |

## 9. Supporting evidence files

- [`evidence/startup-verification.log`](evidence/startup-verification.log)
- [`evidence/TC-01-account-profile-role.log`](evidence/TC-01-account-profile-role.log)
- [`evidence/TC-02-membership-payment.log`](evidence/TC-02-membership-payment.log)
- [`evidence/TC-03-attendance.log`](evidence/TC-03-attendance.log)
- [`evidence/TC-04-workouts-progress.log`](evidence/TC-04-workouts-progress.log)
- [`evidence/TC-05-dashboard-recommendations.log`](evidence/TC-05-dashboard-recommendations.log)
- [`evidence/TC-06-smart-equipment.log`](evidence/TC-06-smart-equipment.log)
- [`evidence/TC-07-fault-safety.log`](evidence/TC-07-fault-safety.log)
- [`evidence/TC-08-parking.log`](evidence/TC-08-parking.log)
- [`evidence/TC-09-notifications-administration.log`](evidence/TC-09-notifications-administration.log)
- [`evidence/defect-and-retest-register.log`](evidence/defect-and-retest-register.log)

These are contemporaneous text transcripts of the manual session plus safe database corroboration. No screenshot filename is claimed where no screenshot was saved.

## 10. Information still requiring manual confirmation

1. Re-run one successful scanner check-in after DEF-003 and confirm that no required-field message appears beside the success message.
2. After DEF-004 is corrected, repeat the exact TC-06 shared-user departure/return/30-second expiry sequence and capture the final automatic release.
3. While TREAD-001 is in Maintenance, attempt a Member claim and record the exact denial message before clearing the lock.
4. Repeat the parking two-space obstruction, unchanged rescan and correction to capture alert creation, deduplication and resolution in this Phase 2 evidence set.
5. Execute FR-08 renewal/upgrade, FR-26 photo upload, FR-31 class booking/cancellation, FR-32 Trainer/Admin class approval and FR-37 filtered export only if those Could Have cases are added to the lecturer-approved manual scope.
6. FR-03, FR-19 and FR-34 require implementation before they can be manually tested as complete workflows.

## 11. Source areas inspected

The implementation assessment traced the Angular routes/components/services into the corresponding controllers, application/infrastructure services, entities, migrations and persistence. Principal areas included:

- Authentication/profile: `frontend/fithub-web/src/app/features/auth`, `features/onboarding`, `core/authentication`, `backend/src/FitHub.Api/Controllers/AuthenticationController.cs`, `ProfileController.cs`, `FitHub.Infrastructure/Authentication`, `Profiles`.
- Membership/payment/attendance: Angular membership/payments/attendance features; `MembershipController`, `StripeWebhookController`, `AttendanceController`; `MembershipService`, `PaymentWebhookService`, `AttendanceService`.
- Workouts/progress/recommendations: Angular workouts/progress/recommendations features; workout/progress/recommendation controllers and services; workout/progress entities.
- Equipment/faults: Angular equipment/fault/simulation features; `EquipmentController`, `SimulationController`, fault controllers; equipment/fault services/entities; `EquipmentHub`.
- Parking: Angular parking and parking simulator; parking/simulation/detection controllers and services/entities; `ParkingHub`.
- Administration/classes/notifications: administration, trainer, class and notification Angular features and their corresponding controllers/services/entities.
- Persistence/startup: `FitHubDbContext`, EF Core configurations/migrations, `DevelopmentDataSeeder`, `Program.cs`, Dockerfiles and Compose configuration.

The Phase 1 Word/PDF report was not edited.
