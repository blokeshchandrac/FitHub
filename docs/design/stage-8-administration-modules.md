# Stage 8 administration and small modules

## Administrator dashboard

`GET /api/administration/dashboard` and every CSV endpoint require the Administrator policy. The dashboard returns:

- users grouped by ASP.NET Core Identity role;
- each user's latest membership grouped by status, including `NoMembership`;
- net revenue from non-demo payments whose status is `Succeeded` or `Refunded`, less the cumulative confirmed refund amount;
- payment counts for today and the current month, pending class approvals, trainer count, active accounts/memberships, and recent registrations;
- daily attendance counts for the last 14 UTC dates;
- completed workouts and distinct engaged members over 30 days;
- open fault reports, excluding Resolved and Rejected;
- current parking counts by state;
- seven-day equipment utilization and downtime.

Equipment history is reconstructed as time intervals. Utilization is `(Occupied hours + SharingAllowed hours) / monitored hours × 100`. Downtime is the sum of Maintenance and Offline hours. The state at the reporting-window boundary comes from the latest earlier event, the first later event's previous state, or the current state when no history exists.

CSV exports are available for `payments`, `attendance`, `equipment`, `faults`, and `parking`. Payment exports contain confirmed gross, refunded, and net amounts and exclude development seed payments. Fields are invariant-culture formatted, RFC-style escaped when needed, and formula-leading characters are neutralized.

Administrators manage users at `/api/administration/users`, including database-enforced blocking/unblocking, membership pausing/reactivation, and membership-card blocking/unblocking. Blocking an account revokes outstanding refresh tokens and causes existing JWTs to fail their database-backed validation. Only an Administrator can create a Trainer account through `/api/administration/trainers`; public registration always creates a Member.

## Class booking

- Trainers create and edit their own sessions through `/api/trainer/classes`; every new or edited session is `PendingApproval`.
- Administrators inspect and approve/reject sessions through `/api/administration/classes`. Rejection requires a reason, and a Trainer cannot approve their own class.
- Members list, book, and cancel through `/api/classes`; the list contains only `Approved` future sessions and requires an active membership.
- Trainers and Administrators can inspect participants and record class attendance through their own role-specific endpoints. Administrators do not book classes.
- Booking locks the class row in PostgreSQL before counting confirmed places. A free place yields Confirmed; otherwise the booking is Waitlisted.
- The unique `(ClassScheduleId, UserId)` index prevents duplicate rows. A cancelled booking is reused when a member books again.
- Cancelling a confirmed booking promotes the earliest waiting-list booking.
- Classes cannot be created in the past, booked after starting, configured with invalid times/capacity, or reduced below their registered-member count.
- Class attendance can be recorded only after the session starts. Attended and no-show members remain part of the joined count.

Session statuses are `PendingApproval`, `Approved`, `Rejected`, `Cancelled`, and `Completed`.

## Scanner and access validation

Trainer and Administrator staff can call `POST /api/attendance/validate-access`. The service validates the scanned QR token or hashed member card against the member account, card block state, expiry, and current database membership. Results are explicit: `VALID`, `BLOCKED`, `EXPIRED`, `MEMBERSHIP_INACTIVE`, `USER_DISABLED`, or `INVALID_TOKEN`. Staff permission allows the validation operation; it never bypasses the scanned credential's rules.

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
