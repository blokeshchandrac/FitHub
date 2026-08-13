# FitHub UI/UX redesign plan

## Document status

- Planning date: 13 August 2026
- Scope: post-Stage-9 UI/UX redesign planning only
- Implementation status: not started
- Production UI files changed during this audit: none
- Reference images: reviewed for mood, density, typography, selective imagery, and restrained neon accents only. Their branding, layouts, photography, and other assets must not be copied.

This plan preserves the working authentication, database-backed membership gate, Stripe Test Mode Checkout, webhook authority, role authorization, API contracts, and Stage 1-9 functionality. Registration and membership purchase will feel like one guided journey, but FitHub will never render or collect card-number or CVC fields. Stripe Checkout remains the payment surface.

## Audit summary and baseline evidence

### Repository safety

`git status --short --branch` reports `No commits yet on master` and every project path is untracked. The ignored local `.env` exists, but there is currently no Git commit to use as a rollback point. No file should be reset, discarded, or overwritten as part of the redesign.

Before Checkpoint 1, establish a recoverable baseline with the user's approval: preferably a local-only initial commit that excludes `.env`, build output, test output, and dependencies. If a local commit is not approved, record checksums and preserve a separate local snapshot before broad style refactoring. Never push that checkpoint without separate approval.

### Frontend technology and conventions

| Area | Current state |
|---|---|
| Angular | Angular 22.1 standalone application |
| Node | Repository declares 22.22.3 in `.nvmrc`; host default 22.17.0 is incompatible with Angular CLI 22.1 |
| Package manager | npm 10.9.8 |
| Routing | Standalone route table with route-level `loadComponent` lazy loading |
| State | Angular signals plus RxJS services; no NgRx or other global state library |
| Styling | SCSS configured, but almost all feature styles live in one 158-line `styles.scss`; the three layouts use inline templates/styles |
| Icons | No icon library; navigation uses letters as icon substitutes |
| Charts | No chart library; two small custom SVG polyline components and a CSS administrator bar chart |
| Images/fonts | No exercise, trainer, athlete, or bundled font assets found |
| Testing | Angular unit-test builder with Vitest 4 and jsdom; 7 spec files |
| Image optimization | `NgOptimizedImage` is not currently used |
| Change detection | Signals are used, but components do not currently opt into `OnPush` |

### Build and test baseline

The host's default Node 22.17.0 fails before Angular compilation because Angular CLI 22.1 requires Node 22.22.3 or newer. Using the repository's existing local Node 22.22.3 runtime:

- Angular tests: 20 passed, 0 failed, across 7 test files.
- Angular production build: passed.
- Initial bundle: 142.06 kB raw / 35.98 kB estimated transfer.
- Live frontend: HTTP 200 at `http://localhost:4200`.
- Live API health: `{"status":"Healthy"}` at `http://localhost:8080/health`.

All redesign verification must run with the `.nvmrc` version or the Docker build runtime, not the incompatible host default.

### Responsive baseline observations

The running application was inspected with viewport overrides at 390 × 844, 768 × 1024, and 1440 × 900:

| Viewport | Pages inspected | Findings |
|---|---|---|
| 390 × 844 | Login and inactive-membership plan selection | No horizontal document overflow. Content stacks correctly, but the onboarding header wraps into two rows, the plan journey has no step context, and the visual treatment is a generic light form/card system. |
| 768 × 1024 | Active-member dashboard | No horizontal document overflow. The desktop sidebar is replaced by a hidden slide-out drawer; there is no tablet rail. Metric cards use space inefficiently and the top bar has few usable actions. |
| 1440 × 900 | Active-member and administrator dashboards | No horizontal overflow. The full sidebar is visible, but it is long and role-mixed. Dashboard cards are visually repetitive, charts are weak, and the light page does not reflect the requested FitHub identity. |

The current `no-membership@fithub.demo` account became active during the successful Stripe test and now reaches `/dashboard`. The current inactive-member visual baseline uses `member@fithub.local`. This is local data state, not a seed-code change, and must not be silently cleaned up.

## Current route and page inventory

### Public and authentication routes

| Route | Page/layout | Current behavior |
|---|---|---|
| `/` | No child page under `PublicLayout` | Currently renders an effectively blank public content area; there is no landing page. |
| `/login` | Login | Email/password login and development demo-account shortcuts. |
| `/register` | Registration | One account form; creates a Member and routes according to membership context. |
| `/forgot-password` | Password recovery | Safe account-recovery request. |
| `/unauthorized` | Unauthorized | Generic denial page. |
| `/auth` | Redirect | Redirects to `/login`. |

### Authenticated, membership-safe routes

These routes use `MembershipOnboardingLayout`, have no member sidebar, and remain reachable without active membership:

| Route | Page |
|---|---|
| `/membership/choose` | Plan selection for no-membership users |
| `/membership/renew` | Renewal/reactivation plan selection |
| `/membership/confirming` | Pending webhook confirmation |
| `/membership/checkout/success` | Stripe return; polls backend and never activates directly |
| `/membership/checkout/cancelled` | Neutral cancellation state and plan selection |
| `/membership/status-error` | Retryable membership-context error |
| `/payments` | Current user's payment history and printable receipt action |
| `/account` | Read-only name, email, and role summary |

### Active-member and staff application routes

| Area | Routes | Current protection |
|---|---|---|
| Dashboard | `/dashboard` | Active-membership guard; staff bypass |
| Membership | `/membership` | Membership-entry guard |
| Attendance | `/attendance` | Active-membership guard |
| Scanner | `/attendance/scanner` | Trainer or Administrator |
| Workouts | `/workouts`, `/workouts/exercises`, `/workouts/templates/new`, `/workouts/templates/:id/edit`, `/workouts/session/:id`, `/workouts/history` | Active-membership guard; staff bypass |
| Assignments | `/workouts/assign` | Trainer or Administrator |
| Progress | `/progress` | Active-membership guard; staff bypass |
| Equipment | `/equipment`, `/equipment/:id` | Active-membership guard; API adds checked-in location policy for detailed live data |
| Fault reports | `/fault-reports` | Active-membership guard |
| Parking | `/parking` | Active-membership guard |
| Recommendations/nutrition | `/recommendations` | Active-membership guard |
| Classes | `/booking` | Active-membership guard; staff can create classes |
| Notifications | `/notifications` | Active-membership guard |
| Administration | `/administration`, `/administration/membership-plans`, `/administration/fault-reports` | Administrator |

Unknown routes currently redirect to `/dashboard`, after which existing guards determine the real destination.

### Capability gaps discovered during inventory

The following requested experiences do not exist and therefore cannot be solved honestly with CSS alone:

- There is no public landing route or anonymous active-plan catalogue.
- There is no `/join` flow or persisted onboarding-progress model.
- Registration currently captures only name, email, and password. Fitness goal exists on `ApplicationUser`, but its current update endpoint is part of active-member recommendations.
- `ApplicationUser` has no trainer biography, image, specialities, qualifications, languages, experience, or training-style data.
- There are no trainer discovery/profile APIs, trainer-selection persistence, trainer availability, or trainer-session booking routes.
- There is no trainer dashboard; trainers currently land on workout assignment.
- The administrator dashboard exists but does not include pending/failed payment counts or trainer/class overview.
- Exercise entities expose only name, description, muscle group, equipment, and instructions. They have no image, thumbnail, alt text, source, licence, or attribution metadata.
- Exercise favourites, active-workout autosave/resume metadata, rest timers, and focus mode do not exist.
- Profile is read-only and has no theme/settings controls.
- Messaging/community was deliberately not implemented in Stage 8 and must not be fabricated as part of a visual redesign.

Any backend work for these gaps must be the smallest bounded extension, follow Domain → Application → Infrastructure → API dependency direction, use migrations and validation where persistence changes, and receive tests. It must not become an unrelated product rewrite.

## Current UI problems

1. The product has no guest landing page; `/` is visually blank.
2. The current light blue/white palette and generic card language do not convey the requested dark, modern FitHub identity.
3. At least 30 colour literals are repeated across global and inline component styles. There is no semantic token layer or theme system.
4. Public, onboarding, and application layouts share duplicated brand/header CSS but not reusable header/navigation components.
5. The authenticated shell exposes one long role-mixed navigation list. Trainers and administrators see many member-oriented links rather than a role-composed workspace.
6. Mobile receives a translated desktop drawer, not a purpose-built five-item bottom navigation.
7. Tablet receives the same drawer behavior rather than a compact navigation rail.
8. Navigation uses letters such as `D`, `M`, and `P` instead of a consistent accessible icon set.
9. There is no user menu, theme switcher, notification action, command/search action, fullscreen control, or skip link in the application shell.
10. Dashboard metric cards have identical weight and weak prioritization. The member first screen lacks a prominent workout action and useful operational summaries.
11. Trainer users have no dashboard composition. Administrator visuals do not clearly prioritize exceptions such as failed/pending payments, faults, and maintenance.
12. Charts have minimal axes/labels. The administrator bar chart depends on hover titles, which are not sufficient for keyboard or screen-reader users.
13. Exercise discovery is text-only. There are no images, categories, favourites, detail view, or image fallbacks.
14. Active workout logging uses a table plus small general-purpose inputs; it is not optimized for one-handed gym use and has no rest timer/focus mode.
15. Most API-driven pages render no skeleton while loading. Empty, retry, denied, and offline/unknown states are inconsistent.
16. Wide tables rely primarily on horizontal overflow rather than purpose-built mobile cards where a card representation would be clearer.
17. Many components and templates are compressed into one-line files, which makes UI review and focused tests harder even though files remain feature-separated.
18. The current system font stack references `Inter`, but no Inter files are bundled; appearance depends on local availability.
19. Images have no common component, dimensions policy, fallback, source metadata, or loading strategy.
20. The baseline tests cover core authentication and membership behavior but very few feature components, navigation modes, or accessibility states.

## Proposed information architecture

### Public experience

| Route | Purpose |
|---|---|
| `/` | Landing page: hero, benefits, smart-equipment explanation, workout/progress preview, genuine trainer preview, active plans, application preview, CTA, footer |
| `/trainers` | Public trainer discovery using genuine profile fields only |
| `/trainers/:id` | Public-safe trainer profile |
| `/login` | Returning-user sign-in |
| `/forgot-password` | Password recovery |
| `/join` | Join shell that resolves and resumes the correct step |
| `/join/account` | Separate account-creation form |
| `/join/profile` | Personal details and fitness-goal form |
| `/join/plan` | Active plan selection |
| `/join/trainer` | Optional trainer selection or explicit skip |
| `/join/checkout` | Review and explicit handoff to Stripe; no card fields |
| `/join/confirmation` | Backend-confirmed status, delayed-webhook polling, and completion |
| `/join/cancelled` | Neutral cancellation state with resume action |

Existing membership URLs should remain as compatible redirects or aliases until all links and Stripe callback configuration are migrated safely. The currently working Stripe success and cancellation endpoints must not be broken merely to obtain prettier URLs.

### Authenticated member experience

- Primary desktop: Home, Workouts, Equipment, Progress, Classes.
- Secondary desktop: Attendance, Parking, Recommendations, Membership, Payments, Fault reports, Notifications, Profile/Settings.
- Mobile bottom navigation, maximum five: Home, Workout, Equipment, Progress, More.
- `More` provides secondary actions in an accessible sheet/drawer with a visible close action and correct focus management.

### Trainer experience

- Home/dashboard
- Today/schedule
- Members
- Workout plans and assignments
- Classes
- Scanner
- Notifications
- Profile/settings

Trainer routes must not be sent through membership purchase. Member pages should not be used as the trainer dashboard merely because the active-membership policy allows staff operational access.

### Administrator experience

- Operations dashboard
- Members and roles (read/operational view only where current APIs support it)
- Membership plans and payments
- Attendance
- Trainers/classes
- Equipment and maintenance/faults
- Parking
- Reports/CSV
- Notifications/profile/settings

Administration navigation should foreground exceptions and operational work rather than expose every member feature as a flat list.

## Role-based navigation matrix

`View` means visible navigation and permitted route. `Context` means available through a small account/onboarding header rather than the full application navigation. API authorization remains authoritative in all cases.

| Destination | Guest | Inactive Member | Active Member | Trainer | Administrator |
|---|---:|---:|---:|---:|---:|
| Landing/pricing/trainers | View | View | Optional public view | Optional public view | Optional public view |
| Login/register/join account | View | Redirect to resume | Redirect to app | Redirect to trainer | Redirect to admin |
| Join/profile/plan/trainer/checkout/confirmation | — | View | Redirect to Membership or Dashboard | — | — |
| Account/profile/security/theme | — | Context | View | View | View |
| Membership/payment history/receipt | — | Context | View | Only when operationally relevant | Operational/admin view only |
| Member dashboard | — | — | View | — | — |
| Workouts/exercises/progress | — | — | View | Trainer-specific plan/member tools | Operational access only where needed |
| Attendance/check-in QR | — | — | View | Scanner/schedule view | Scanner/operational view |
| Equipment/faults/parking | Public summary only if explicitly designed | — | View subject to location-detail policy | Operational view | Full operational/admin view |
| Recommendations/nutrition | — | — | View | — | — |
| Classes | Public preview optional | — | Book/cancel | Create/manage | Create/manage/overview |
| Notifications | — | Account/payment notices only if backend supports them | View | View | View |
| Trainer discovery/profile | View | Join selection | View/selection or booking when supported | Own profile | Manage/overview when supported |
| Administration/reports | — | — | — | — | View |

The shell must derive its navigation from authenticated role plus backend-confirmed membership context. Hiding links is presentation, not authorization.

## Design system

### Token architecture

Use CSS custom properties in dedicated files rather than component colour literals:

```text
styles/
  _reset.scss
  _tokens-core.scss
  _themes.scss
  _typography.scss
  _utilities.scss
  _motion.scss
```

Core token groups:

- Background/surface: `--color-page`, `--color-surface`, `--color-surface-raised`, `--color-surface-interactive`, `--color-overlay`.
- Text/border: `--color-text`, `--color-text-secondary`, `--color-text-muted`, `--color-border`, `--color-border-strong`.
- Accent: `--color-accent`, `--color-accent-hover`, `--color-on-accent`, `--color-accent-soft`.
- Feedback: `--color-success`, `--color-warning`, `--color-error`, `--color-info`, plus corresponding subtle surfaces and on-colours.
- Focus: `--focus-color`, `--focus-width`, `--focus-offset`.
- Typography: display/body font families, weights, fluid size steps, line heights, and tabular-number feature.
- Spacing: a 4 px base scale from `--space-1` through large section spacing.
- Shape: control, card, panel, and pill radii; avoid making every container a pill.
- Elevation: two restrained shadow levels and one focus/elevated overlay shadow.
- Motion: 120, 180, and 220 ms durations plus easing tokens.
- Shell: topbar height, expanded/collapsed sidebar widths, rail width, mobile-navigation height, and content maximum width.
- Layering: base, sticky, navigation, popover, dialog, and toast z-index tokens.

Candidate theme values must be measured for WCAG contrast before acceptance, not copied blindly:

| Theme | Page / surface direction | Primary accent | Secondary/highlight | On-accent |
|---|---|---|---|---|
| Volt (default) | Near-black `#090b0a`; charcoal `#111411` / `#181c18` | Electric lime around `#c7ff3d` | Restrained green-lime soft surfaces | Near-black text |
| Pulse | Dark blackberry `#100b13`; plum-charcoal surfaces | Rose/magenta around `#ff4f9a` | Soft violet; coral only for non-semantic highlights | Near-black/deep blackberry text where contrast passes |
| Ocean | Midnight navy `#07111e`; blue-charcoal surfaces | Cyan around `#22d3ee` | Clear blue accents | Deep navy text |
| High Contrast | Black surfaces, white text, strong white/bright borders, no translucent panels | Bright yellow or cyan selected by measured contrast | Minimal secondary decoration | Black text |

Semantic equipment state tokens are theme-invariant:

- Available: green plus check/available text.
- Occupied: red plus occupied icon/text.
- Sharing allowed: amber/yellow plus sharing icon/text.
- Maintenance: neutral blue/grey plus maintenance icon/text.
- Offline/unknown: grey/dark grey plus offline/unknown icon/text.

No status may rely on colour alone.

### Typography

- Start with a performant system UI stack and explicit heavy weights for display headings; do not depend on an unbundled `Inter` name.
- Use a bold, compact display scale only for landing hero and major section headings.
- Use smaller application headings with strong hierarchy and ordinary sentence case.
- Enable `font-variant-numeric: tabular-nums` for metrics, prices, timers, sets, and tables.
- Uppercase is limited to short eyebrows/status metadata with sufficient letter spacing.
- A locally hosted display font may be evaluated later only with documented licensing and measured bundle cost.

### Icon strategy

The current app has no icon library. Checkpoint 1 should either add a small, tree-shakable, licence-verified icon dependency such as Lucide Angular after explicit review, or maintain a narrowly scoped local SVG icon set with source/licence documentation. Do not use emoji, single letters, icon fonts, or inaccessible background-only glyphs. Icon-only controls require accessible names and tooltips where helpful.

## Theme architecture

Create a focused `ThemeService` and initialization boundary:

1. Store only a non-sensitive theme identifier; never store access or refresh tokens.
2. Namespace preferences per opaque user ID, with a guest preference and last-used scope so different users on one browser do not overwrite one another.
3. Apply `data-theme` to `document.documentElement`.
4. Restore the last safe theme before Angular renders through a tiny, non-secret boot initializer in `index.html` or an Angular initializer that runs before shell paint.
5. Validate stored values against `volt | pulse | ocean | high-contrast`; unknown values fall back to Volt.
6. On authentication change, resolve the user's scoped preference; on logout, return to the guest preference.
7. React to `forced-colors`, `prefers-contrast`, `prefers-reduced-motion`, and system colour preference where useful without overriding an explicit user selection.
8. Expose a labelled radiogroup in Profile/Settings and a compact topbar switcher. Selection must show text and a selected indicator, not colour alone.
9. Set `color-scheme` and browser-control colours appropriately per theme.

## Shared-component architecture

Adapt the current folders rather than creating a parallel application:

```text
app/
  core/
    layout/
      public-shell/
      onboarding-shell/
      app-shell/
      trainer-shell/           # composition can reuse app-shell primitives
      administrator-shell/     # composition can reuse app-shell primitives
    navigation/
      navigation.models.ts
      navigation.service.ts
      desktop-sidebar/
      tablet-navigation-rail/
      mobile-bottom-navigation/
      more-navigation/
    theme/
      theme.models.ts
      theme.service.ts
      theme-initializer.ts
    fullscreen/
      fullscreen.service.ts
    onboarding/
      onboarding-context.service.ts
  shared/
    ui/
      app-icon/
      button/
      section-card/
      metric-card/
      status-badge/
      page-header/
      empty-state/
      error-state/
      permission-state/
      skeleton-loader/
      image-fallback/
      responsive-data-view/
      toast/
  public/
    landing/
    trainers/
    pricing/
  onboarding/
    join-shell/
    account-step/
    profile-step/
    plan-step/
    trainer-step/
    checkout-step/
    confirmation-step/
  features/
    # retain existing feature boundaries
```

`AppShell` coordinates layout only. Header, sidebar, rail, bottom navigation, user menu, notification button, command/search button, theme switcher, and fullscreen button remain separate focused components. Navigation configuration is typed and centralized by role/capability; individual pages do not recalculate membership entitlement.

Shared components must implement loading, empty, error, permission, and disabled semantics once while allowing feature-specific copy. Avoid a universal mega-card component with dozens of conditional inputs.

## Responsive behavior

Use content-driven breakpoints, initially validated around these ranges:

- 0-599 px: mobile shell with bottom navigation, single-column content, full-width primary controls, safe-area padding, and no desktop sidebar DOM.
- 600-1023 px: tablet shell with compact navigation rail or accessible drawer, two-column cards when content permits, and no hidden desktop-width tables.
- 1024 px and above: collapsible desktop sidebar plus compact topbar and a controlled content maximum.

Mobile requirements:

- Support 320 CSS pixels with no page-level horizontal scroll.
- Keep bottom navigation to Home, Workout, Equipment, Progress, More.
- Reserve bottom padding equal to navigation height plus `env(safe-area-inset-bottom)`.
- Maintain practical 44 × 44 px targets where possible.
- Convert data tables into labelled cards when row scanning remains clear; use a labelled controlled scroll region only when tabular comparison is essential.
- Keep active-workout number controls and complete-set action within thumb reach.
- Do not use hover-only content or tiny chart labels.

Desktop and tablet requirements:

- Sidebar collapse state must be keyboard accessible and remembered as a non-sensitive preference.
- Tablet rail labels remain available through visible text when expanded and accessible names/tooltips when compact.
- Content grids must use intrinsic sizing and `minmax(0, 1fr)` to prevent overflow.
- Large imagery stays on public/trainer/exercise discovery pages, not behind operational tables, forms, or charts.

## Fullscreen and active-workout focus mode

Create a `FullscreenService` that wraps feature detection, `requestFullscreen()`, `document.exitFullscreen()`, `fullscreenchange`, errors, and a readonly signal. The button appears only when supported, runs only from user interaction, shows correct enter/exit icons and labels, and never runs automatically.

Focus mode is separate application state owned by the active-workout feature. It hides non-essential shell navigation but retains:

- Exit workout and abandon confirmation
- Current exercise information
- Set logging and undo/edit affordance
- Rest timer
- Safety/instruction access
- Connectivity/autosave status
- Fullscreen toggle when supported

Exiting fullscreen must not end focus mode, and exiting focus mode must not silently exit or enter browser fullscreen.

## Public landing-page plan

The new `/` route will use the public shell and contain:

1. Compact header with FitHub brand, Explore, Trainers, Membership, Sign in, and Start membership.
2. Hero with an original/licensed inclusive fitness image or illustration, a restrained theme-aware ring, readable gradient mask, clear FitHub-specific promise, and two calls to action.
3. Benefits grounded only in implemented functionality: unified membership/workouts, transparent recommendations, equipment status, progress/attendance analytics, trainer-assigned plans, and parking status.
4. Smart-equipment explanation showing member/NFC input → ESP32/controller → clearance aid → semantic light state → API/SignalR update. It must explicitly avoid implying that sensor absence alone makes equipment available.
5. Workout/progress preview using representative UI, not invented user statistics.
6. Trainer preview only after genuine profile data exists; otherwise use a transparent “Meet the training team” empty state rather than fabricated claims.
7. Active membership plans loaded from a read-only backend catalogue. Prices must not be duplicated in static HTML.
8. Application preview composed from product UI rather than copied dashboard imagery.
9. Final CTA and accessible footer with legal, privacy, accessibility, attribution, and sign-in links.

The current plan catalogue is authenticated. Checkpoint 2 therefore needs either a narrowly scoped anonymous active-plan endpoint or `[AllowAnonymous]` on the read-only plans action, with authorization tests. Checkout remains authenticated and server-priced.

## Unified registration and purchase journey

The `/join` experience is one shell with separate steps and separate forms:

1. **Create account** — posts the existing registration contract and establishes the secure session.
2. **Personal details and fitness goal** — persists only validated profile data. It never requests medical diagnosis data.
3. **Choose membership** — reads active plans and records selection locally/server-side without trusting price from the browser.
4. **Choose trainer (optional)** — persists a genuine trainer preference or explicit skip. It does not claim a booked appointment.
5. **Secure Stripe checkout** — presents an order review and then calls the existing authenticated checkout API. FitHub renders no card fields.
6. **Confirmation** — polls backend membership state and displays “Payment confirmed” only after the verified webhook activates membership.

Resumption rules:

- Before account creation, store only non-sensitive draft fields in memory/session storage if needed; never store passwords.
- After account creation, the backend is the source of truth for onboarding state, selected plan/trainer, payment state, and membership state.
- Refresh, sign-out/sign-in, cancellation, decline, delayed webhook, and duplicate webhook delivery resolve from backend state.
- Existing authenticated accounts entering `/join` resume the first incomplete permitted step.
- Active Members opening `/join` go to Membership or Dashboard rather than restarting purchase.
- Cancelled Checkout keeps the account inactive and returns to plan/review with “No charge was made.”
- A success URL alone never advances to complete; `PendingPayment` shows confirmation polling and retry guidance.
- Existing payment history, idempotency, printable receipt, and server-owned EUR pricing remain intact.

A small backend onboarding context may be required because it does not exist today. It should expose one authenticated DTO rather than forcing unrelated step components to infer progress independently. The existing `AccountContextService` remains the single membership/auth context and can compose the onboarding state without weakening it.

## Trainer discovery and selection

No trainer profile model currently exists. Before building trainer cards, Checkpoint 2 must define the smallest professional extension:

- A one-to-one `TrainerProfile` linked to an Identity user in the Trainer role.
- Bounded biography, training style, years of experience, image URL/path, image alt, and demonstration-data marker.
- Structured bounded collections for specialties, qualifications, and languages; do not parse marketing strings in Angular.
- Public-safe trainer list/detail DTOs that never expose account/security fields.
- Optional member trainer preference/selection with a unique current relationship and audit timestamps.
- Development-only idempotent trainer profile seed labelled as demonstration content.

Do not show ratings, reviews, qualifications, languages, availability, or appointment times unless persisted data supports them. “Next available” is omitted until a real availability model exists. Existing class schedule times may be shown as upcoming classes, but they must not be relabelled as private trainer availability.

Cards use optimized image, full name, genuine specialties/qualifications/languages/style, View profile, and a selection button. Selected state requires border, check icon, visible text, and `aria-pressed` or native radio semantics.

## Role-specific dashboards

### Member

The first viewport prioritizes one start/continue-workout action, today's rule-based recommendation, weekly consistency, current streak, and one important notice. Secondary sections contain attendance, personal records, weight trend, membership utilization/cost per visit, upcoming class/trainer preference where genuine, equipment summary, parking summary, and membership status.

Sensitive analytics remain server-calculated. The existing dashboard DTO will be extended or composed through a dedicated facade rather than recomputed from raw browser data.

### Trainer

Create a trainer-specific route and backend summary only for data that exists: upcoming classes, assigned members, assignment activity, plans requiring attention, and operational notifications. Availability controls, messages, and progress alerts must be omitted until their persistence/business rules are explicitly added.

### Administrator

Retain completed-payment revenue as authoritative. Recompose around active members, attendance trend, revenue, pending/failed payment counts, workouts, equipment utilization/downtime, open faults/maintenance, parking, classes/trainers, and CSV reports. Add accessible chart summaries/tables and do not rely on hover or colour.

## Exercise and workout plan

### Current image gap

The three seeded exercises have no image fields and the repository contains no licensed exercise-image assets. External images must not be added until source and per-image licence are verified.

### Selected strategy

1. Extend the exercise model with nullable `imageUrl`, `thumbnailUrl`, `imageAlt`, `imageSource`, `imageLicence`, and `attributionUrl` fields, bounded and configured in EF Core.
2. Start with original FitHub-owned/local exercise illustrations for the small demonstration library. Document authorship/licence and ensure the visual is safe and identifiable; otherwise show a category fallback and rely on written instructions rather than presenting unsafe form guidance.
3. Store optimized thumbnails and detail images locally; never inline production images as base64.
4. Use `NgOptimizedImage`, explicit dimensions/aspect ratio, card lazy loading, detail-page responsive sources, and only hero/LCP priority.
5. Add an image-fallback component that preserves dimensions and meaningful text.
6. Add an attribution page and render source/licence links when content requires them.

wger is not selected by this plan. It may be evaluated in Checkpoint 4 only after reviewing its official API, dataset licence, individual image provenance, attribution requirements, caching/hotlink policy, and removal/update behavior. No assumption that all wger images share one licence is permitted.

### Exercise discovery

- Image-led cards with fixed 4:3 media, name immediately below, muscle group, equipment, and supported favourite action.
- Search plus typed filters for muscle group, equipment, and category.
- Grid/list choice where useful, with skeleton, empty, retry, and broken-image states.
- Detail page/drawer/bottom sheet with image, technique, safety notes, previous performance, alternatives only when data supports them, and Add to workout.

### Active workout

- Large current exercise, previous performance, current/target set, and fast +/- controls.
- Ordinary buttons for every action; gestures are enhancements only.
- One clear Complete set action, rest timer, personal-record feedback, connection/autosave status, and edit/undo.
- Persist unfinished session data through existing session APIs; add bounded draft/autosave behavior only if the API needs it.
- Confirm before abandoning; do not require repeated modal traversal to log a set.
- Focus mode and optional fullscreen as separate states.

## Existing feature-page redesign map

| Feature | Planned treatment |
|---|---|
| Authentication | Shared split/compact auth shell, clearer validation and progress to join; retain safe errors and password rules. |
| Profile/settings | Editable permitted fields, theme controls, security links, account identity; no tokens or secrets displayed. |
| Membership | Dark plan cards, current-plan summary, renewal/upgrade state, trusted price language, preserved checkout behavior. |
| Payments/receipts | Desktop table and mobile cards; explicit Pending/Succeeded/Failed/Cancelled semantics; printable receipt retained. |
| Attendance | QR/action prominence, expiry/replay guidance, visit metrics, mobile history cards. |
| Workouts/templates/history | Visual hierarchy, exercise thumbnails where licensed, clear primary actions, responsive history. |
| Progress/body | Accessible labelled charts with textual summary, compact measurement form/history, no diagnosis. |
| Recommendations/nutrition | Rule/reason disclosure, clear general-guidance disclaimer, goal controls, no fake-AI language. |
| Equipment | Compact cards with machine, location, icon/image, state text/icon/colour, last update, sharing and fault action. |
| Fault reports | Guided machine/category/comment/photo flow, preview/progress/confirmation, report timeline/status. |
| Parking | Summary metrics, clear space grid, last event/confidence where useful, accessible list alternative. |
| Classes | Schedule cards, capacity/waitlist clarity, member book/cancel, staff creation separated by role. |
| Notifications | Grouped unread/read states, typed icons, timestamps, empty/error/loading states. |
| Administration | Exception-first dashboard, responsive operational tables, accessible chart summaries, CSV actions. |
| Messaging/community | No current implementation; excluded from redesign unless separately scoped and approved. |

## Accessibility requirements

Checkpoint acceptance uses WCAG 2.2 AA as the minimum target:

- Validate normal-text contrast at 4.5:1 and large text at 3:1 in every theme and state.
- Preserve visible keyboard focus with at least a 2 CSS-pixel effective indicator and sufficient adjacent contrast.
- Add a skip-to-content link and stable `main` target.
- Use correct banner/nav/main/footer landmarks and one logical page `h1`.
- Give every input a programmatic label, error association, and useful validation message.
- Give icon-only buttons accessible names; do not duplicate decorative SVGs into the accessibility tree.
- Combine colour with text/icons for status and selection.
- Maintain logical DOM/tab order when visual layout changes.
- Provide focus trapping/restoration for modal drawers/dialogs and escape/close behavior without keyboard traps.
- Use polite live regions for loading, checkout confirmation, autosave, SignalR updates, and non-destructive toasts.
- Maintain 44 × 44 px targets where practical and support 200% zoom plus 320 px reflow.
- Respect reduced motion and forced colours; never remove focus outlines without a stronger replacement.
- Charts require visible labels, a concise accessible summary, and a data/table alternative where appropriate.
- Informative images need specific alt text; decorative hero shapes use empty alt or are hidden.

## Loading, empty, error, permission, and offline states

Every API-backed feature must define these states in its component contract:

- Loading: shape-matched skeleton with no layout shift and an accessible status.
- Empty: feature-specific explanation and relevant next action.
- Recoverable error: safe user message, correlation ID only when useful, and Retry.
- Permission denied: explains the permitted next action without exposing authorization internals.
- Offline/unknown: equipment/parking uses explicit unknown semantics; membership context failure never becomes inactive membership.
- Success: concise non-blocking feedback; payment success remains backend-confirmed only.

Raw ASP.NET, EF Core, Stripe, stack, or JSON conversion details never render in Angular.

## Performance plan

- Preserve route-level lazy loading and split the new public/onboarding areas by route.
- Defer below-the-fold landing sections and trainer/exercise grids.
- Use explicit media sizes and responsive thumbnails; do not preload the exercise library.
- Prioritize only the hero/LCP image.
- Use `@for (...; track stableId)` and keep filtered collections computed/memoized.
- Adopt `OnPush` where compatible with signal-driven components and verify SignalR updates.
- Keep account/membership and onboarding context services shared to avoid duplicate requests.
- Cache only non-sensitive theme/layout preferences and licensed static media metadata.
- Measure initial/lazy bundles at each checkpoint and investigate material regression against the 142.06 kB baseline.
- Keep animation to 120-220 ms and disable non-essential motion for reduced-motion users.

## Testing strategy

### Automated frontend

- Theme selection, validation, per-user persistence, early restoration, logout scope, and no wrong-theme flash logic.
- Public and role-specific navigation composition.
- Inactive-member gating and join-flow resume after refresh/login/cancellation/delayed confirmation.
- Stripe cancellation/confirmation UI, no success before backend state, and unchanged checkout request.
- Mobile bottom navigation, tablet rail, More drawer focus behavior, and accessible labels.
- Fullscreen supported/unsupported paths plus `fullscreenchange`; focus mode remains independent.
- Trainer card/profile selection semantics using genuine DTO values.
- Exercise filtering, image fallback/attribution, and lazy image attributes.
- Active-workout set logging, timer controls, autosave state, and abandon confirmation.
- Shared empty/error/skeleton/status components and keyboard behavior.
- Existing authentication, interceptor, account context, membership guard, and checkout tests remain unchanged or stronger.

### Backend tests when backend changes

- Public catalogue exposes active plans only and never checkout/admin behavior.
- Onboarding state is authenticated, resumable, and cannot activate membership.
- Trainer public DTO excludes Identity/security fields; role and data validation are enforced.
- Trainer selection is idempotent and limited to Trainer-role users.
- Exercise image metadata lengths/URLs/licence fields and migration schema.
- Extended dashboard DTOs use authorized server-side aggregation.
- Existing Stripe binding, server pricing, initialization failure, webhook signature, idempotency, duration, cancellation/failure, receipt, and membership-gating tests remain green.

### Build, security, and container checks

At every checkpoint:

```powershell
# Use Node 22.22.3 from .nvmrc
npm.cmd test -- --watch=false
npm.cmd run build -- --configuration production

# When backend files change
dotnet test backend/FitHub.sln -c Release
dotnet build backend/FitHub.sln -c Release

docker compose --env-file .env config --quiet
docker compose --env-file .env build
```

Also scan tracked/source files for credentials, verify `.env` remains ignored/untracked, and never print complete secret values.

### Manual visual matrix

At every checkpoint, inspect relevant guest, inactive-member, active-member, trainer, and administrator pages at:

- 320 × 568
- 390 × 844
- 768 × 1024
- 1024 × 768
- 1440 × 900
- 1920 × 1080
- 200% browser zoom

Record screenshots for the affected checkpoint and check page-level overflow, clipped/overlapping content, mobile-navigation clearance, image failures, chart labels, focus order, theme contrast, reduced motion, delayed loading, empty/error states, and Stripe/membership behavior. Compilation alone is not visual acceptance.

## Implementation checkpoints

### Checkpoint 1 — design foundation

- Core/reset/theme/motion tokens
- Volt, Pulse, Ocean, and High Contrast themes
- Early `ThemeService` restoration and accessible Profile/Settings selection
- Shared icon/button/card/metric/status/state/page-header primitives
- Public, onboarding, active-member, trainer, and administrator shell compositions from shared primitives
- Desktop sidebar, tablet rail, mobile bottom navigation, More menu
- User/notification/search controls as supported by current routes
- Fullscreen service and shell control; no active-workout focus feature yet
- Skip link, focus foundation, safe areas, reduced-motion/forced-colour support

No Stripe, membership, API, domain, database, or business-flow rewrite belongs in Checkpoint 1.

### Checkpoint 2 — public and onboarding

- Landing page, public benefits, smart-equipment explainer, dynamic pricing, trainer preview, footer
- `/join` shell and resumable separate steps
- Existing registration integrated as the account step
- Personal/fitness-goal step through a membership-safe profile endpoint
- Optional trainer profile/selection backend extension and development demo data
- Stripe review/handoff, cancellation, delayed confirmation, receipt/history continuity
- Compatibility redirects for existing membership callback routes only after verification

### Checkpoint 3 — role dashboards

- Member dashboard prioritization and safe DTO composition
- New bounded trainer dashboard based only on real data
- Administrator exception-first dashboard and accessible chart summaries
- Role-specific navigation validation

### Checkpoint 4 — exercises and workout focus

- Exercise image/licence metadata migration and local licensed asset strategy
- Attribution page and image fallback
- Visual exercise library and detail
- Workout templates/history visual redesign
- Fast active-workout logging, rest timer, autosave behavior, focus mode, abandon confirmation
- Fullscreen integration remains progressive enhancement

### Checkpoint 5 — remaining existing features

- Membership, payments/receipts, attendance, progress/body, recommendations/nutrition
- Equipment, fault reports/uploads, parking, classes, notifications, profile/settings
- Administration plans/faults/reports
- No messaging/community feature unless separately approved

### Checkpoint 6 — stabilization

- WCAG-focused review, keyboard and screen-reader spot checks
- Complete responsive/zoom/theme matrix
- Image and bundle optimization
- Full frontend/backend regression, Docker build/config/startup, credential scan
- Final cross-role visual consistency and documented limitations

At the end of every checkpoint: run relevant tests/builds, launch the app, capture desktop/mobile evidence, inspect the complete working-tree diff, list modified files and unfinished items, confirm Stripe/membership gating, and stop for review.

## Risks and mitigations

| Risk | Mitigation |
|---|---|
| No Git baseline and all files untracked | Obtain approval for a local baseline commit or create a non-secret local snapshot before implementation; never reset/discard. |
| Broad global CSS changes regress every page | Introduce tokens/primitives first, migrate shell before features, retain compatibility aliases temporarily, visual-test each role. |
| Theme flash or cross-user preference leakage | Apply validated theme before paint; namespace non-sensitive preference by opaque user; test logout/login transitions. |
| Dark themes reduce readability | Measure every token pair, include High Contrast, test forced colours and 200% zoom. |
| Navigation redesign weakens access control | Keep current guards and API policies; navigation is derived presentation only; retain authorization tests. |
| Join flow accidentally conflates account and payment | Separate routes/forms; create account first; Stripe-hosted card UI only; webhook remains sole activation authority. |
| Existing Stripe callbacks break | Preserve old callback routes as aliases until environment configuration and end-to-end Test Mode checks pass. |
| Public pricing drifts from checkout | Load active plan catalogue from backend; send only plan ID and purchase kind; server remains price authority. |
| Trainer UI invents claims | Add structured genuine fields or omit them; clearly label development seed; no ratings/reviews/availability without data. |
| Exercise image copyright/licensing | Local owned assets first; store per-image provenance; attribution page; no Google/reference-image copying or blind hotlinks. |
| Images damage performance | Thumbnails, explicit dimensions, lazy loading, limited hero priority, route deferral, bundle/network measurement. |
| Chart package inflates bundle | Improve accessible native SVG/CSS first; add a dependency only after bundle/accessibility review and approval. |
| Mobile controls are hidden by bottom navigation | Safe-area and shell spacing tokens; verify all target viewports and active workout one-handed flows. |
| Live SignalR/state updates regress under OnPush | Add component tests and manual equipment/parking update checks before broad OnPush adoption. |
| Local demo account label no longer matches database | Treat seed and mutable runtime data separately; use a fresh database or explicitly approved targeted reset for repeatable demos, never silently delete data. |

## Rollback strategy

1. Before Checkpoint 1, create an approved recoverable baseline excluding local secrets and generated output.
2. Keep each checkpoint internally cohesive and avoid mixing later checkpoint work.
3. Preserve existing routes/components behind compatibility boundaries until their replacement passes role, membership, Stripe, and responsive verification.
4. Record changed files and verification evidence at each checkpoint.
5. If a checkpoint fails, revert only that checkpoint from the approved local baseline; do not reset unrelated work or delete database data.
6. Database extensions require reversible EF migrations and a backup before applying to valuable local data. Never run destructive rollback against production.
7. Stripe configuration, webhook records, payments, and memberships are never modified as a UI rollback mechanism.

## Approval gate

No production UI implementation begins until this plan and route/role implications are approved. The first implementation response must be limited to Checkpoint 1 and must preserve the current working Stripe Checkout, verified-webhook activation, membership layouts/guards, and role policies.
