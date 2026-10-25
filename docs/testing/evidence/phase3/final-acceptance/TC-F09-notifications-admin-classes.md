# TC-F09 — Notifications, administration, Trainer and classes

- **Requirements:** FR-31–FR-36 and implemented associated functionality
- **Class:** Final Strength Fundamentals
- **Trainer/location:** Tariq / Studio A
- **Schedule:** 25 October 2026, 10:00–11:00 local display
- **Capacity:** 2

## Notifications and administration

- An optional preference was disabled and persisted; the next qualifying event created no notification.
- Re-enabling the preference allowed one later qualifying notification.
- Existing notifications remained visible and stable deduplication keys prevented duplicates.
- Administrator search, membership/account status, Trainer assignment, attendance/payment/workout totals and actor/timestamp audit evidence were verified.
- Non-Administrators were denied administration APIs.
- Trainer created and assigned an owned plan only to an assigned Member.

## Class workflow

1. Trainer created the exact future class; it began Pending Approval and was not visible to Members.
2. Administrator approved it; it then appeared to Members.
3. The first booking exposed P3-002 and passed after `f507c9d` moved the PostgreSQL row lock into the retry strategy.
4. A third Member was initially waitlisted, exposing P3-003.
5. After `c78f599`, the class reached 2/2 confirmed with zero waiting. A different synthetic active Member received HTTP 409 “This class is full.” No waitlist row was created.
6. Priya's temporary final-place booking was cancelled after the retest; the final state was 1/2 confirmed, zero waiting, Priya `Cancelled`.
7. Trainer/Administrator participant visibility and Member inability to approve were verified.

**Decision:** Passed after P3-002 and P3-003 corrections.

**Evidence:** `screenshots/TC-F09-notification-preferences.jpg`, `screenshots/TC-F09-class-booking-original-error.jpg`, `screenshots/TC-F09-full-class-waitlist-defect.jpg`, `screenshots/TC-F09-class-capacity-confirmation.jpg`, `screenshots/TC-F09-class-booking-retest.jpg`; hosted API retest returned HTTP 409 for full capacity and HTTP 200 for cleanup cancellation.

