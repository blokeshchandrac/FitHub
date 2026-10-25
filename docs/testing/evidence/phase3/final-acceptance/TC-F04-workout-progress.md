# TC-F04 — Workout, rest time and progress

- **Requirements:** FR-12–FR-16
- **Member:** Sofia Finaltest
- **Template:** Final Strength Session
- **Description:** Final Phase 3 acceptance workout.

## Exact workout

- Back Squat: 8 × 60 kg, 45 s duration, 90 s rest; 8 × 62.5 kg, 45 s duration, 90 s rest; 6 × 65 kg, 50 s duration, 120 s rest.
- Bench Press: 10 × 40 kg, 40 s duration, 90 s rest; 8 × 42.5 kg, 45 s duration, 90 s rest; 8 × 42.5 kg, 45 s duration, 120 s rest.
- Body measurement: 62 kg, 165 cm, 21.5% body fat, chest 88 cm, waist 70 cm, hip 94 cm.

## Result

1. Duration and rest were distinct inputs and remained distinct in completed history after refresh.
2. All six sets persisted exactly.
3. Total volume was 2,450 kg; rest and duration did not affect it.
4. Personal records were Back Squat 65 kg and Bench Press 42.5 kg.
5. Measurement persisted and BMI displayed as 22.8.
6. The assigned Trainer created/assigned a separate Trainer plan and could not access unrelated Members.
7. Completed Member workout history was not altered by Trainer-plan editing.
8. Negative values and rest above 3,600 seconds were rejected by validation/covered regression tests.

**Decision:** Passed.

**Evidence:** `screenshots/TC-F04-sofia-workout-rest-time.jpg`, `screenshots/TC-F04-sofia-body-progress.jpg`, plus the completed-session and dashboard records.

