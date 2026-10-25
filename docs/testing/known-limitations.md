# Known limitations and remaining risks

These are the factual limitations of the final Phase 3 release. They do not invalidate the nine verified core acceptance workflows.

1. **Mobile acceptance breadth.** Responsive layouts were checked during development, but the final hosted acceptance campaign did not repeat every workflow at every mobile viewport. This is not a claim of complete device or WCAG conformance.
2. **NFR-16 load evidence.** No approved 20-concurrent-user run with p95 read/write latency and error-rate measurements exists. Extensibility of the modular architecture is not evidence of scalability under increased load.
3. **Optional fault photographs.** Fault reports persist in Neon PostgreSQL, but optional photographs currently use the Render Free service's ephemeral filesystem and can disappear after a restart or redeploy. Losing a photograph does not delete or corrupt the report.
4. **Stripe dashboard cleanup.** Two Stripe Test Mode webhook registrations remain. Backend event idempotency prevented duplicate payments and membership activation during testing, but one redundant dashboard registration should be removed after submission without changing the source release.
5. **Free-tier cold starts.** Render Free and sleeping Neon compute can delay the first request. The frontend and SignalR clients retry/reconnect, and evaluators should allow approximately one minute before retrying an initial login.

Additional boundaries: the equipment and parking laboratories are connected software simulations, not validation of physical ESP32/NFC/ultrasonic hardware or real camera accuracy. Accessibility, penetration, disaster-recovery and long-duration load testing remain future work.
