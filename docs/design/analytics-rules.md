# FitHub analytics rules

Stage 5 analytics are calculated by backend application/infrastructure services and returned as DTOs. Angular displays the results and graphs but is not the authoritative calculation layer. All calendar boundaries use UTC.

## Workout volume

For every logged set:

```text
set volume = repetitions × weight in kilograms
workout volume = sum of all set volumes
```

Missing repetitions or weight contribute zero. Duration-only exercise sets therefore contribute no weighted volume. Negative repetitions and weights are rejected.

## Previous performance and personal records

Previous performance is the most recent completed workout before the current session containing the same exercise and at least one set. It reports that exercise log's best set weight and total weighted volume.

A weighted set is a personal record when:

```text
current set weight > maximum set weight for the same member and exercise
                     across completed workouts before the current session
```

The first positive weighted performance for an exercise establishes the initial record. Equal weight does not create a new record. Dashboard records show the maximum weight for each exercise, with the earliest achievement when the maximum is tied.

## Workout and attendance frequency

Both frequencies use the trailing 28 days:

```text
attendance frequency per week = check-ins during trailing 28 days ÷ 4
workout frequency per week = completed workouts during trailing 28 days ÷ 4
```

Values are rounded to one decimal place.

## Weekly workout volume

Weekly volume is the sum of completed workout volume from Monday 00:00 UTC through the current time.

## Current workout streak

A week is active when it contains at least one completed workout. Current streak is the number of consecutive active UTC calendar weeks, beginning with the current Monday-to-Sunday week and moving backwards. If the current week has no completed workout, the current streak is zero.

## Consistency score

Consistency uses the current UTC week plus the preceding seven UTC calendar weeks:

```text
consistency score = distinct active workout weeks ÷ 8 × 100
```

Multiple workouts in one week still count as one active week. The result is bounded from 0 to 100 and rounded to one decimal place.

## BMI

BMI uses the latest recorded non-null height and latest recorded non-null weight, even if those values were logged in different entries:

```text
height in metres = height in centimetres ÷ 100
BMI = weight in kilograms ÷ height in metres²
```

BMI is rounded to one decimal place. FitHub displays the number only and does not classify it or present a medical diagnosis. Entry validation permits weight 20–400 kg and height 100–250 cm to reduce accidental inputs.

## Weight trend

The graph contains up to the latest 180 non-null weight entries in chronological order. Dashboard weight change is:

```text
latest displayed weight − earliest displayed weight
```

The difference is rounded to two decimals. It is not interpreted medically.

## Membership utilization and cost per visit

Membership visits are check-ins whose timestamps fall within the latest membership's start-inclusive, end-exclusive period.

```text
membership cost = sum of successful payments linked to that membership
cost per visit = membership cost ÷ visits during membership period
```

Cost per visit is rounded to two decimals. When there are no visits or no successful membership payment, the value is absent rather than zero.
