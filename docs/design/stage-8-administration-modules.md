# Stage 8 administration and small modules

## Administrator dashboard

`GET /api/administration/dashboard` and every CSV endpoint require the Administrator policy. The dashboard returns:

- users grouped by ASP.NET Core Identity role;
- each user's latest membership grouped by status, including `NoMembership`;
- revenue summed only from payments whose status is `Succeeded`;
- daily attendance counts for the last 14 UTC dates;
- completed workouts and distinct engaged members over 30 days;
- open fault reports, excluding Resolved and Rejected;
- current parking counts by state;
- seven-day equipment utilization and downtime.

Equipment history is reconstructed as time intervals. Utilization is `(Occupied hours + SharingAllowed hours) / monitored hours × 100`. Downtime is the sum of Maintenance and Offline hours. The state at the reporting-window boundary comes from the latest earlier event, the first later event's previous state, or the current state when no history exists.

CSV exports are available for `payments`, `attendance`, `equipment`, `faults`, and `parking`. Payment exports also include only `Succeeded` payments. Fields are invariant-culture formatted, RFC-style escaped when needed, and formula-leading characters are neutralized.

## Class booking

- `POST /api/classes` requires Trainer or Administrator. Trainers are assigned as the instructor for classes they create; administrators may select another trainer or administrator.
- Member list, book and cancel actions require an active membership.
- Booking locks the class row in PostgreSQL before counting confirmed places. A free place yields Confirmed; otherwise the booking is Waitlisted.
- The unique `(ClassScheduleId, UserId)` index prevents duplicate rows. A cancelled booking is reused when a member books again.
- Cancelling a confirmed booking promotes the earliest waiting-list booking.
- Classes cannot be created in the past, booked after starting, or configured with invalid times/capacity.

## Notifications

`GET /api/notifications` synchronizes and returns up to 100 notifications. `POST /api/notifications/{id}/read` only changes a notification owned by the authenticated user. Unique per-user deduplication keys make generation idempotent.

| Type | Trigger |
|---|---|
| MembershipExpiry | Active membership expires within 7 days. |
| ClassReminder | Confirmed class starts within 24 hours. |
| WorkoutConsistency | No completed workout in the last 7 days. |
| StreakWarning | At least two prior consecutive attendance weeks and no visit this week. |
| PersonalRecord | A recorded all-time exercise weight maximum occurred in the last 7 days. |
| FaultReportStatusUpdate | Administrator changes the member's fault-report status. |

The optional trainer-member inbox was deliberately not expanded into social networking. Stage 8 remains limited to operational notifications and bookings.
