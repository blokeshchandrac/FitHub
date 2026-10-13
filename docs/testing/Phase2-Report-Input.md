# FitHub Phase 2 Report Input

**Source material:** `Phase2-Manual-Test-Evidence.md`, `evidence/defect-and-retest-register.log`, `DevelopmentDataSeeder.cs`, and the Development demonstration-account configuration.  
**Environment:** Local Development using synthetic test data.  
**Security note:** Password values, API keys, Stripe secrets, database passwords, connection strings, raw QR credentials and raw NFC UIDs are intentionally excluded.

## 1. Test-account information

The shared password for seeded demonstration accounts is supplied through the environment variable **`DEMO_SEED_PASSWORD`**, which is mapped to `SeedData:DemoPassword`. The password value must remain in the ignored local environment and is not reproduced here.

| Complete synthetic name | Email address | Role | Membership status during testing | Onboarding status | Assigned trainer | Branch | NFC card/token identifier and status |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Elena Martinez | `phase2.registration.20260916@fithub.test` | Member | Active Monthly membership; active from `2026-09-16T21:10:30.140392Z` to `2026-10-16T21:10:30.140392Z` | Complete | Tariq Manualtrainer | `DEMO` — FitHub Demonstration Branch | Session-only simulator card `SIM-CARD-6ee09722207a4e8ca035be161f84378e`, Active. Attendance QR raw value was deliberately not retained; accepted attendance reference is `2026-09-17T05:36:25.102869Z`. |
| Priya Testmember | `phase2.active-member@fithub.test` | Member | Active Monthly membership; evidence records expiry `2026-10-16` | Complete through development seeding | None | `DEMO` — FitHub Demonstration Branch | Session-only simulator card `SIM-CARD-2db7743650494bf69c581709154ac553`, Active. |
| Tariq Manualtrainer | `phase2.manual-trainer@fithub.test` | Trainer | Not applicable | Seed-complete; Trainer is not subject to Member fitness onboarding | Not applicable | `DEMO` — FitHub Demonstration Branch | None applicable. |
| Amina Manualadmin | `phase2.manual-admin@fithub.test` | Administrator | Not applicable | Seed-complete; Administrator is not subject to Member fitness onboarding | Not applicable | `DEMO` — FitHub Demonstration Branch | None applicable. |
| loki loki | Email was not recorded in the retained manual-test evidence | Member | Active during TC-06 | Not recorded in the retained evidence | None recorded | Not recorded in the retained evidence | Session-only simulator card `SIM-CARD-bf34ce101b394de29ac78d2a9d41a0cf`, Active. Used only as the third-Member sharing-limit check. |

The development seeder also defines additional demonstration accounts, but they are not listed above because the completed TC-01–TC-09 evidence does not identify them as actors in those manual test steps.

## 2. Concrete test data

### TC-01 — Account, profile and role-based access

- Registered Member: Elena Martinez, `phase2.registration.20260916@fithub.test`, role Member.
- Registration password: tester-held value, not retained in the report or repository.
- Preferred name: Elena.
- Date of birth: `1998-04-15`.
- Height: `168 cm`.
- Weight: `64 kg`.
- Primary goal: Improve strength; stored goal value Strength.
- Activity level: Moderately active; stored value Moderate.
- Experience level: Beginner.
- Planned training days: Monday, Wednesday and Friday.
- Preferred branch: `DEMO` — FitHub Demonstration Branch.
- Administrator: Amina Manualadmin, `phase2.manual-admin@fithub.test`.
- Trainer: Tariq Manualtrainer, `phase2.manual-trainer@fithub.test`.
- Assignment: Tariq→Elena, Active, assigned at `2026-09-17T05:35:26.687502Z` by Amina.
- Authorization check: Elena manually opened `/administration` and was sent to `/unauthorized`.
- Failed requirements observed: registration had no consent input; forgot-password did not deliver a usable single-use reset link.

### TC-02 — Membership plan, Stripe Test Mode payment and payment record

- Member: Elena Martinez, Member.
- Selected plan: Monthly.
- Displayed and charged amount: `EUR 29.90`.
- Duration: one month.
- Plan description: “Flexible monthly membership”.
- Alternative plan shown: Annual, `EUR 499.00`, 12 months.
- Stripe input: Stripe’s standard successful Test Mode Visa ending `4242`, a future expiry and a synthetic CVC.
- Stripe result: successful Sandbox payment.
- Payment record: Purchase, Succeeded, `EUR 29.90`, non-demo, paid at `2026-09-16T21:10:30.140392Z`.
- Membership result: Active from `2026-09-16T21:10:30.140392Z` through `2026-10-16T21:10:30.140392Z`.
- FitHub did not retain the card number or CVC.

### TC-03 — QR attendance and attendance history

- Member: Elena Martinez, active Member.
- Staff validator: Amina Manualadmin, Administrator.
- QR/check-in value: a fresh short-lived FitHub QR credential; the raw value was deliberately omitted because it was an access credential.
- Safe identifying reference: accepted check-in at `2026-09-17T05:36:25.102869Z`, displayed locally as `9/17/2026, 7:36:25 AM`.
- Original result: selecting “Validate and check in QR” produced no request and no attendance row.
- Result after correction: “Check-in accepted at 9/17/2026, 7:36:25 AM.”
- Reuse result: “Check-in token has already been used.”
- Resulting attendance values: 1 visit this week, 1 this month, 1 during the current membership and `EUR 29.90` membership cost per visit.

### TC-04 — Workout planning, logging and progress tracking

- Member: Elena Martinez.
- Trainer: Tariq Manualtrainer.
- Member template: `Phase 2 Strength A`.
- Template description: `Manual acceptance workout: squat and bench progression.`
- Exercises: Back Squat and Bench Press.
- Back Squat sets:
  - Set 1: 8 repetitions × `60 kg`, duration `45 s`.
  - Set 2: 8 repetitions × `62.5 kg`, duration `45 s`.
  - Set 3: 6 repetitions × `65 kg`, duration `50 s`.
  - Note: `Controlled depth; 90-second rest between sets.`
- Bench Press sets:
  - Set 1: 10 repetitions × `40 kg`, duration `40 s`.
  - Set 2: 8 repetitions × `42.5 kg`, duration `45 s`.
  - Set 3: 8 repetitions × `42.5 kg`, duration `45 s`.
  - Note: `Paused repetitions; 90-second rest used but recorded only in notes.`
- Session note: `Phase 2 manual workout completed without pain.`
- Session result: Completed; calculated training volume `2,450 kg`.
- Body measurement: weight `64 kg`, height `168 cm`, body fat `22.5%`, chest `90 cm`, waist `72 cm`, hip `96 cm`.
- Measurement note: `Phase 2 baseline measurement.`
- Calculated BMI displayed: `22.7`.
- Personal records: Back Squat `65 kg`; Bench Press `42.5 kg`.
- Trainer plan: `Elena Strength Progression`.
- Trainer-plan description: `Three-week conservative squat and bench plan for the assigned Member.`
- Assignment result: Active assignment to Elena; plan owner Tariq.
- Failure observed: no dedicated rest-time field; the 90-second rest could only be recorded in notes.

### TC-05 — Dashboard and rule-based guidance

- Member: Elena Martinez.
- Profile source values: Strength goal, Beginner experience, planned Monday/Wednesday/Friday, height `168 cm`, weight `64 kg`, Moderate activity.
- Stored activity source values: one attendance visit and one completed `2,450 kg` workout.
- Stored progress values: latest weight `64 kg`; Back Squat PR `65 kg`; Bench Press PR `42.5 kg`.
- Initial insufficient-data result: `Not enough data`, with reason `No attendance history`.
- Dashboard calculations after activity was recorded:
  - Attendance frequency: `0.2 visits/week`.
  - Workout frequency: `0.2 workouts/week`.
  - Weekly volume: `2,450 kg`.
  - Current streak: `1 week`.
  - Consistency: `12.5%`.
  - Membership visits: `1`.
  - Membership cost per visit: `EUR 29.90`.
- Recommendation results:
  - `Recovery is today's plan` because a workout had already been completed that day.
  - Activity on `0 of 12` planned days during the preceding 28 days.
  - Missed planned day: Wednesday.
  - Conservative Back Squat target: `64 kg`.
  - Conservative Bench Press target: `43.5 kg`.
  - Muscle groups to revisit: Back, Shoulders, Arms and Core.
  - General estimate: `2,110 kcal`, `102 g` protein, `51 g` fat and `311 g` carbohydrate, with a non-medical disclaimer.

### TC-06 — Smart-equipment status and clearance protection

- Administrator: Amina Manualadmin.
- Route: `/simulation/equipment`.
- Virtual machine: `EQ-TREADMILL-01`.
- Persisted machine: `TREAD-001` — Treadmill, Cardio zone.
- Initial state: Available.
- Owner: Elena Martinez; simulator NFC card `SIM-CARD-6ee09722207a4e8ca035be161f84378e`, Active.
- Sharing partner: Priya Testmember; simulator NFC card `SIM-CARD-2db7743650494bf69c581709154ac553`, Active.
- Third-Member limit check: loki loki; simulator NFC card `SIM-CARD-bf34ce101b394de29ac78d2a9d41a0cf`, Active.
- Connected Mode result: API and EquipmentHub connected; four virtual machines mapped; eight development Members loaded.
- Claim transition: Available → Occupied; Elena stored as owner.
- Sharing transition: Occupied → SharingAllowed; Priya stored as the one sharing partner.
- Third-Member result: rejected with `SHARING_FULL`; owner and sharing partner remained unchanged.
- Clearance-warning duration: `30 seconds`.
- One-user departure result: no countdown while the other assigned Member remained in the sensor area.
- Both-users departure result: SENSOR_ZONE_EXITED and CLEARANCE_COUNTDOWN_STARTED were followed by BACKEND_UNAVAILABLE, SENSOR_ZONE_ENTERED and CLEARANCE_COUNTDOWN_CANCELLED.
- Failed final state: the automatic release did not complete; the machine remained SharingAllowed.
- Cleanup result: connected Reset returned persisted TREAD-001 to Available.

### TC-07 — Equipment fault reporting and safety control

- Member reporter: Elena Martinez.
- Administrator: Amina Manualadmin.
- Equipment: `TREAD-001` — Treadmill.
- Initial equipment state: Available.
- Fault category: Safety concern.
- Required comment: `Emergency stop button remained depressed after release during Phase 2 manual test.`
- Photograph: none.
- Initial report state: Open.
- Administrator review state: UnderReview.
- Maintenance note: `Inspected by Amina; equipment isolated pending emergency-stop test.`
- Administrator safety action: Available → Maintenance.
- Safety-action reason: `Safety lock for DEF/TC-07 emergency-stop investigation.`
- Cleanup action: Maintenance → Available.
- Cleanup reason: `Phase 2 safety-lock test complete; returned to service.`
- Notification result: `Your report for Treadmill is now UnderReview.`

### TC-08 — Parking availability and uncertain detection

- Administrator controlling the simulation: Amina Manualadmin.
- Member read-only verification: Elena Martinez.
- Route: `/simulation/parking`.
- Source camera: `SIM-PARK-CAMERA-01`.
- Simulation spaces: `P-A1`, `P-A2`, `P-A3`, `P-A4`, `P-B1`, `P-B2`, `P-B3`, `P-B4`, `P-C1`, `P-C2`, `P-C3`, `P-C4`.
- Baseline scan: all 12 simulation spaces Available at confidence `0.98`; 0 active alerts; 0 detected parked cars in the empty scene.
- Camera Offline scan: all 12 monitored spaces changed to Unknown; CAMERA_OFFLINE recorded.
- Camera Online with low-confidence/obstructed view: all 12 remained Unknown rather than being guessed Available or Occupied.
- Restored normal-confidence scan: all 12 simulation spaces returned to Available at confidence `0.98` with source `SIM-PARK-CAMERA-01`.
- Member parking total: 15 database spaces because the 12 simulation spaces coexist with seeded `A-01`, `A-02` and `B-01`.
- The two-space obstruction/alert scenario was not repeated in this evidence pass.

### TC-09 — Notifications and administration

- Administrator: Amina Manualadmin.
- Trainer: Tariq Manualtrainer.
- Member: Elena Martinez.
- Administrator assignment action: assigned Elena to Tariq; Active at `2026-09-17T05:35:26.687502Z`; assigned by Amina.
- Administration data observed: active Members, active memberships, confirmed net revenue, payments today/month, trainer count, open faults, attendance trend, parking totals and equipment utilization.
- Notification category `PersonalRecord`: `New recorded best for Back Squat: 65 kg.`
- Notification category `PersonalRecord`: `New recorded best for Bench Press: 42.5 kg.`
- Notification category `FaultReportStatusUpdate`: `Your report for Treadmill is now UnderReview.`
- Deduplication result: each stored notification had a stable deduplication key.
- Failed preference result: no notification-preference page, API, entity or table existed, so preference-controlled delivery could not be performed.

## 3. Manual test-case table

| ID | Related requirements | Concrete preconditions and test data | Manual test steps | Exact expected result | Actual result | Decision | Evidence/defect reference |
| --- | --- | --- | --- | --- | --- | --- | --- |
| TC-01 | FR-01–FR-05 | `DEMO` branch Active; new account `phase2.registration.20260916@fithub.test`; Elena profile values: 1998-04-15, 168 cm, 64 kg, Strength, Moderate, Beginner, Mon/Wed/Fri; Amina Administrator; Tariq Trainer. | 1. Register Elena.<br>2. Complete `/welcome` with the exact profile.<br>3. Enter `/administration` as Elena.<br>4. Sign in as Amina, find Tariq and Elena, and assign Elena.<br>5. Sign in as Tariq and view assigned Members.<br>6. Submit Forgot password for a known account. | Registration records required data and opens onboarding; profile persists and routes to membership selection; Member is denied Administration; assignment persists with creator/time; Tariq sees only Elena; forgot-password delivers a single-use reset link. | Registration/profile succeeded and Elena was denied Administration. Assignment originally did nothing, then persisted after DEF-002 correction. Tariq saw Elena. Registration contained no consent input. Forgot-password did not deliver a usable reset link. | **Failed** | `evidence/TC-01-account-profile-role.log`; DEF-002; GAP-001; GAP-002. |
| TC-02 | FR-06, FR-07 and FR-09 | Elena onboarded with no active membership; Monthly `EUR 29.90`, one month; Stripe successful Test Mode Visa ending 4242. | 1. Open plan selection.<br>2. Select Monthly.<br>3. Complete Stripe Sandbox payment.<br>4. Open Membership and Payments.<br>5. Corroborate payment/membership records. | Stripe displays the server-controlled EUR 29.90; confirmed payment activates a one-month membership; exactly one Succeeded EUR 29.90 Purchase appears; FitHub stores no card details. | Stripe succeeded; Payment Succeeded at `2026-09-16T21:10:30.140392Z`; Monthly membership Active through `2026-10-16T21:10:30.140392Z`; history displayed once. | **Passed** | `evidence/TC-02-membership-payment.log`. |
| TC-03 | FR-10–FR-11 | Elena Active; Amina Administrator; no prior Elena attendance; fresh short-lived QR; safe reference `2026-09-17T05:36:25.102869Z`. | 1. Generate QR as Elena.<br>2. Submit at `/attendance/scanner` as Amina.<br>3. After DEF-001 correction, generate a new QR and repeat.<br>4. Reuse the accepted token.<br>5. View Elena’s attendance history. | First valid submission creates exactly one visit; reuse is rejected; history/frequency and cost per visit update. | **Original:** no request and no attendance row.<br>**After correction:** check-in accepted at 7:36:25 AM; reuse rejected as already used; one visit persisted; history showed 1 visit and EUR 29.90/visit. | **Failed originally; manual retest passed after correction** | `evidence/TC-03-attendance.log`; DEF-001. DEF-003 correction has no completed final visual retest. |
| TC-04 | FR-12–FR-16 | Elena Active; Tariq assigned; exercises Back Squat and Bench Press; personal template `Phase 2 Strength A`; Trainer plan `Elena Strength Progression`; six exact set rows listed in section 2. | 1. Create the personal template.<br>2. Log all squat and bench sets.<br>3. Record 90-second rests.<br>4. Complete the session.<br>5. Record the exact body measurement.<br>6. Review history/PRs.<br>7. Create and assign the Trainer plan. | Personal and Trainer plans remain separate; sets include reps, weight, duration, rest and notes; completed history and PRs appear; measurement/BMI persist; assigned Trainer plan is active. | Plans, sets, notes, 2,450 kg volume, history, BMI 22.7, PRs and Trainer assignment persisted. Rest had no dedicated field and was stored only in notes. | **Failed** | `evidence/TC-04-workouts-progress.log`; DEF-005. |
| TC-05 | FR-17–FR-18 | Elena profile complete; planned Mon/Wed/Fri; first run without history, then one attendance, one 2,450 kg workout, 64 kg measurement and two PRs. | 1. Refresh Recommendations before activity.<br>2. Complete TC-03/TC-04 activity.<br>3. Open Dashboard.<br>4. Refresh Recommendations and inspect reasons/nutrition disclaimer. | Empty data produces “Not enough data”; stored activity produces exact calculated metrics and explainable, experience-aware/planned-day guidance. | Initial `Not enough data — No attendance history`. Later: 0.2 visits/week, 0.2 workouts/week, 2,450 kg, 1-week streak, 12.5%, EUR 29.90/visit; missed Wednesday; 64 kg squat and 43.5 kg bench targets; stated nutrition estimate/disclaimer. | **Passed** | `evidence/TC-05-dashboard-recommendations.log`. |
| TC-06 | FR-21–FR-24 | Amina; Connected Mode; EQ-TREADMILL-01→TREAD-001; initial Available; Elena/Priya Active; exact session cards in section 2; third active Member; 30-second clearance. | 1. Connect simulator.<br>2. Enter Elena and claim Treadmill.<br>3. Enable sharing.<br>4. Join Priya.<br>5. Try a third Member.<br>6. Move one shared Member out.<br>7. Move both out and wait for clearance.<br>8. Reset cleanup. | Available→Occupied→SharingAllowed; one partner joins; third rejected; one departure does not warn; both departures start one 30-second warning; expiry releases to Available and clears assignments. | Claim/sharing persisted and third Member was rejected. Both departures produced contradictory exit/start/backend-unavailable/enter/cancel events; countdown did not complete; machine remained SharingAllowed until Reset restored Available. | **Failed** | `evidence/TC-06-smart-equipment.log`; DEF-004. |
| TC-07 | FR-25, FR-27–FR-28 | Elena; Amina; TREAD-001 Available; Safety concern; exact emergency-stop comment; no photo. | 1. Elena submits the exact report.<br>2. Amina moves it to UnderReview with the exact note.<br>3. Amina changes TREAD-001 to Maintenance with the stated reason.<br>4. Amina explicitly returns it to Available after the test. | Open report persists; Administrator review/note and notification persist; Maintenance removes normal availability; explicit clear returns Available while history remains. | Open→UnderReview; note and Member notification persisted; TREAD-001 Available→Maintenance→Available with the exact reasons. A separate Member claim attempt during Maintenance was not executed. | **Passed** | `evidence/TC-07-fault-safety.log`. |
| TC-08 | FR-29–FR-30 | Amina controlling; Elena read-only; SIM-PARK-CAMERA-01; P-A1–P-C4; normal confidence 0.98. | 1. Connect parking simulator.<br>2. Scan empty area.<br>3. Verify normal Member parking view.<br>4. Set camera Offline and scan.<br>5. Return Online with low confidence and scan.<br>6. Restore normal confidence and scan. | Authenticated observations persist and broadcast; 12 empty spaces become Available at 0.98; Offline/low confidence produce Unknown; successful later scan restores the correct state. | 12 Available at 0.98; all 12 became Unknown Offline; remained Unknown at low confidence; restored Available at 0.98. Member page showed persisted values. Two-space alert path was not repeated. | **Passed** | `evidence/TC-08-parking.log`. |
| TC-09 | FR-33, FR-35–FR-36 | Amina Administrator; Tariq Trainer; Elena Member; completed assignment, payment, attendance, workout and fault records. | 1. Open Administrator dashboard.<br>2. Search users/Trainer and inspect assignment.<br>3. Verify Tariq’s assigned-Member view.<br>4. Open Elena’s notifications.<br>5. Attempt to configure notification preferences. | Stored operations drive analytics; Administrator action is attributable; Trainer sees assigned Member; relevant notifications appear once according to Member preferences. | Analytics and assignment worked. Elena saw two PersonalRecord and one FaultReportStatusUpdate notification with deduplication keys. No preference model/page/API/table existed. | **Failed** | `evidence/TC-09-notifications-administration.log`; DEF-006. |

## 4. Defect summary

| Reference | Description | Affected test case | Original result | Current status | Manual retest completed? | Evidence reference |
| --- | --- | --- | --- | --- | --- | --- |
| DEF-001 | Attendance scanner submit did not invoke the check-in action. | TC-03 | Clicking “Validate and check in QR” produced no request and no Attendance row. | Corrected: native submit invokes the scanner action and prevents default submission. | **Yes.** A new QR was accepted once, reuse was rejected, and attendance/history persisted. | `evidence/TC-03-attendance.log`; `evidence/defect-and-retest-register.log`. |
| DEF-002 | Trainer assignment form submit did not invoke assignment. | TC-01 | Administrator selected Tariq and Elena, but no request or assignment was created. | Corrected: native submit invokes assignment and prevents default submission. | **Yes.** Tariq→Elena persisted and Elena appeared for Tariq. | `evidence/TC-01-account-profile-role.log`; `evidence/defect-and-retest-register.log`. |
| DEF-003 | Successful scanner check-in also displayed a required-field error after the field was cleared. | TC-03 | Success message and “Enter a valid token or card UID” appeared together. | Correction present: the control is reset after successful check-in. | **No.** The final browser-only visual retest is outstanding. | `evidence/TC-03-attendance.log`; `evidence/defect-and-retest-register.log`. |
| DEF-004 | Connected equipment clearance emitted contradictory events and did not complete automatic release. | TC-06 | Both shared Members leaving caused countdown start, backend-unavailable, immediate enter and cancellation events; machine remained SharingAllowed. | **Open.** No simulator correction was made during the manual-evidence task. | **No.** | `evidence/TC-06-smart-equipment.log`; `evidence/defect-and-retest-register.log`. |
| DEF-005 | Workout sets have no dedicated rest-time field. | TC-04 | The 90-second rest was stored only in free-text notes. | **Open.** | **No.** | `evidence/TC-04-workouts-progress.log`; `evidence/defect-and-retest-register.log`. |
| DEF-006 | Notification preferences are absent. | TC-09 | Notifications/deduplication existed, but the Member could not configure preference-controlled delivery. | **Open.** No preference persistence or interface exists. | **No.** | `evidence/TC-09-notifications-administration.log`; `evidence/defect-and-retest-register.log`. |
| GAP-001 | Registration consent is absent. | TC-01 | Public registration contained no consent input or persisted consent evidence. | **Open requirement gap.** | **No.** | `evidence/TC-01-account-profile-role.log`; `evidence/defect-and-retest-register.log`. |
| GAP-002 | Forgot-password does not deliver a single-use reset link. | TC-01 | The request returned a generic response, but no usable email link/reset-completion workflow existed. | **Open requirement gap.** | **No.** | `evidence/TC-01-account-profile-role.log`; `evidence/defect-and-retest-register.log`. |
