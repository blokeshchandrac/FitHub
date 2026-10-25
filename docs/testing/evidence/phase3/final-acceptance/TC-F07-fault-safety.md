# TC-F07 — Equipment fault report and safety lock

- **Requirements:** FR-25, FR-27–FR-28
- **Equipment:** TREAD-001
- **Category:** Safety concern
- **Comment:** Emergency stop button did not return normally during the final acceptance test.

## Result

1. The original empty-comment attempt exposed P3-001; it was recorded before correction.
2. After `f6b9dfe`, the empty comment was rejected and the exact report above persisted without a photograph.
3. Administrator moved the report to Under Review, recorded a maintenance note and placed TREAD-001 in Maintenance.
4. A Member claim was rejected while the safety lock was active.
5. The enabled fault-update notification was delivered once.
6. Administrator resolved the report with a reason and explicitly returned the machine to Available.
7. The report, history, actor and timestamps remained after return to service.

**Decision:** Passed after correction P3-001.

**Evidence:** `screenshots/TC-F07-original-empty-comment-defect.jpg`, `screenshots/TC-F07-comment-required-retest.jpg`, `screenshots/TC-F07-maintenance-blocks-claim.jpg`, `screenshots/TC-F07-fault-resolution-notification.jpg`, `screenshots/TC-F07-resolved-returned-service.jpg`.

Optional photos use the hosted service's ephemeral filesystem and are not claimed as persistent. The core report workflow works without a photo.

