# TC-F03 — QR attendance

- **Requirements:** FR-10–FR-11
- **Member:** Sofia Finaltest
- **Plan amount:** EUR 29.90

## Result

1. A fresh short-lived attendance credential was generated for Sofia.
2. The Administrator scanner accepted it exactly once and created one attendance visit.
3. Reuse of the same credential was rejected.
4. An altered/invalid value was rejected.
5. The scanner displayed the success result without simultaneously displaying the required-field error.
6. Member attendance history showed one visit; the dashboard calculated EUR 29.90 per visit from the confirmed membership cost and visit count.

**Decision:** Passed.

**Evidence:** `screenshots/TC-F03-attendance-success.jpg`, `screenshots/TC-F03-sofia-attendance.jpg`. Raw QR credentials are intentionally excluded.

