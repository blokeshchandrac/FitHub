# Stage 7 rule-based recommendations

FitHub recommendations are deterministic rules, not AI or machine learning. Every stored recommendation includes the exact `RuleCode` and human-readable `Reason` that produced it. Regeneration expires the previous active set instead of erasing its provenance.

## Rules

| Rule code | Inputs | Result |
|---|---|---|
| `TODAY_RECOVERY` | Completed workout today | Suggest recovery, mobility, walking or rest. |
| `TODAY_COMEBACK` | No attendance, or last attendance at least 3 days ago | Suggest a manageable full-body return session. |
| `TODAY_GOAL` | Fitness goal, workouts in the last 14 days and attendance | Suggest strength, hypertrophy, conditioning, weight-management circuit or balanced full body. |
| `TARGET_WEIGHT_PROGRESSIVE_OVERLOAD` | Most recent set with at least 8 repetitions and positive weight | Target = previous weight × 1.025, rounded to the nearest 0.5 kg. Up to three recent exercises are shown. |
| `MISSING_MUSCLE_GROUPS` | Completed exercise muscle groups in the last 7 days | List missing groups from Chest, Back, Legs, Shoulders, Arms and Core. |
| `ATTENDANCE_LAPSE` | No attendance, or no attendance for at least 3 days | Suggest scheduling a realistic visit. |
| `ATTENDANCE_STREAK_AT_RISK` | At least a 2-week attendance streak immediately before this week and no visit this week | Remind the member that the streak is at risk. |
| `NUTRITION_BASIC_ESTIMATE` | Latest body weight, activity level and goal | Produce general calorie and macronutrient guidance. |

## Nutrition formula

1. Baseline calories = weight in kg × 22.
2. Activity multiplier: Sedentary 1.20, Light 1.35, Moderate 1.50, VeryActive 1.70.
3. Goal multiplier: WeightManagement 0.90, MuscleGain 1.05, all others 1.00.
4. Calories are rounded to 10 kcal.
5. Protein = 1.6 g/kg; fat = 0.8 g/kg.
6. Carbohydrate = max(0, (calories − protein × 4 − fat × 9) / 4), rounded to grams.

The UI and nutrition recommendation always display: “General guidance only; this is not medical advice. Consult a qualified professional for individual nutrition needs.” No diagnosis or individualized medical claim is made.
