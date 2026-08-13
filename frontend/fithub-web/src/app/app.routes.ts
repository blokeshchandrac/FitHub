import { Routes } from '@angular/router';
import { anyRoleGuard, authenticationGuard, roleGuard } from './core/authentication/authentication.guards';
import { activeMembershipGuard, membershipEntryGuard, membershipOnboardingGuard } from './core/membership/active-membership.guard';
import { ApplicationLayout } from './core/layouts/application-layout';
import { MembershipOnboardingLayout } from './core/layouts/membership-onboarding-layout';
import { PublicLayout } from './core/layouts/public-layout';

export const routes: Routes = [
  {
    path: '', component: PublicLayout, children: [
      { path: 'auth', pathMatch: 'full', redirectTo: 'login' },
      { path: 'login', loadComponent: () => import('./features/auth/login/login-page').then(m => m.LoginPage) },
      { path: 'register', loadComponent: () => import('./features/auth/registration/registration-page').then(m => m.RegistrationPage) },
      { path: 'forgot-password', loadComponent: () => import('./features/auth/forgot-password/forgot-password-page').then(m => m.ForgotPasswordPage) },
      { path: 'unauthorized', loadComponent: () => import('./features/auth/unauthorized/unauthorized-page').then(m => m.UnauthorizedPage) },
    ],
  },
  {
    path: '', component: MembershipOnboardingLayout, canActivate: [authenticationGuard], children: [
      { path: 'membership/choose', canActivate: [membershipOnboardingGuard], loadComponent: () => import('./features/membership/membership-page').then(m => m.MembershipPage) },
      { path: 'membership/renew', canActivate: [membershipOnboardingGuard], loadComponent: () => import('./features/membership/membership-page').then(m => m.MembershipPage) },
      { path: 'membership/confirming', canActivate: [membershipOnboardingGuard], loadComponent: () => import('./features/membership/membership-confirmation-page').then(m => m.MembershipConfirmationPage) },
      { path: 'membership/checkout/success', canActivate: [membershipOnboardingGuard], loadComponent: () => import('./features/membership/membership-confirmation-page').then(m => m.MembershipConfirmationPage) },
      { path: 'membership/checkout/cancelled', canActivate: [membershipOnboardingGuard], loadComponent: () => import('./features/membership/membership-page').then(m => m.MembershipPage) },
      { path: 'membership/status-error', loadComponent: () => import('./features/membership/membership-status-error-page').then(m => m.MembershipStatusErrorPage) },
      { path: 'payments', loadComponent: () => import('./features/payments/payments-page').then(m => m.PaymentsPage) },
      { path: 'account', loadComponent: () => import('./features/account/account-page').then(m => m.AccountPage) },
    ],
  },
  {
    path: '', component: ApplicationLayout, canActivate: [authenticationGuard], children: [
      { path: 'dashboard', canActivate: [activeMembershipGuard], loadComponent: () => import('./features/dashboard/dashboard-page').then(m => m.DashboardPage) },
      { path: 'membership', canActivate: [membershipEntryGuard], loadComponent: () => import('./features/membership/membership-page').then(m => m.MembershipPage) },
      { path: 'attendance', canActivate: [activeMembershipGuard], loadComponent: () => import('./features/attendance/attendance-page').then(m => m.AttendancePage) },
      { path: 'attendance/scanner', canActivate: [anyRoleGuard('Trainer', 'Administrator')], loadComponent: () => import('./features/attendance/scanner/scanner-page').then(m => m.ScannerPage) },
      { path: 'workouts', canActivate: [activeMembershipGuard], loadComponent: () => import('./features/workouts/workouts-page').then(m => m.WorkoutsPage) },
      { path: 'workouts/exercises', canActivate: [activeMembershipGuard], loadComponent: () => import('./features/workouts/exercise-library/exercise-library-page').then(m => m.ExerciseLibraryPage) },
      { path: 'workouts/templates/new', canActivate: [activeMembershipGuard], loadComponent: () => import('./features/workouts/template-editor/template-editor-page').then(m => m.TemplateEditorPage) },
      { path: 'workouts/templates/:id/edit', canActivate: [activeMembershipGuard], loadComponent: () => import('./features/workouts/template-editor/template-editor-page').then(m => m.TemplateEditorPage) },
      { path: 'workouts/session/:id', canActivate: [activeMembershipGuard], loadComponent: () => import('./features/workouts/session/workout-session-page').then(m => m.WorkoutSessionPage) },
      { path: 'workouts/history', canActivate: [activeMembershipGuard], loadComponent: () => import('./features/workouts/history/workout-history-page').then(m => m.WorkoutHistoryPage) },
      { path: 'workouts/assign', canActivate: [anyRoleGuard('Trainer', 'Administrator')], loadComponent: () => import('./features/workouts/assignments/workout-assignments-page').then(m => m.WorkoutAssignmentsPage) },
      { path: 'progress', canActivate: [activeMembershipGuard], loadComponent: () => import('./features/progress/progress-page').then(m => m.ProgressPage) },
      { path: 'equipment', canActivate: [activeMembershipGuard], loadComponent: () => import('./features/equipment/equipment-page').then(m => m.EquipmentPage) },
      { path: 'equipment/:id', canActivate: [activeMembershipGuard], loadComponent: () => import('./features/equipment/detail/equipment-detail-page').then(m => m.EquipmentDetailPage) },
      { path: 'fault-reports', canActivate: [activeMembershipGuard], loadComponent: () => import('./features/fault-reports/fault-reports-page').then(m => m.FaultReportsPage) },
      { path: 'parking', canActivate: [activeMembershipGuard], loadComponent: () => import('./features/parking/parking-page').then(m => m.ParkingPage) },
      { path: 'recommendations', canActivate: [activeMembershipGuard], loadComponent: () => import('./features/recommendations/recommendations-page').then(m => m.RecommendationsPage) },
      { path: 'booking', canActivate: [activeMembershipGuard], loadComponent: () => import('./features/booking/booking-page').then(m => m.BookingPage) },
      { path: 'notifications', canActivate: [activeMembershipGuard], loadComponent: () => import('./features/notifications/notifications-page').then(m => m.NotificationsPage) },
      { path: 'administration', canActivate: [roleGuard('Administrator')], loadComponent: () => import('./features/administration/administration-page').then(m => m.AdministrationPage) },
      { path: 'administration/membership-plans', canActivate: [roleGuard('Administrator')], loadComponent: () => import('./features/administration/membership-plans/membership-plans-page').then(m => m.MembershipPlansPage) },
      { path: 'administration/fault-reports', canActivate: [roleGuard('Administrator')], loadComponent: () => import('./features/administration/fault-reports/fault-report-admin-page').then(m => m.FaultReportAdminPage) },
    ],
  },
  { path: '', pathMatch: 'full', redirectTo: 'dashboard' },
  { path: '**', redirectTo: 'dashboard' },
];
