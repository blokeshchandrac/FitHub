import { Component, computed, inject, signal } from '@angular/core';
import { Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { AuthenticationService } from '../authentication/authentication.service';

type ApplicationRole = 'Member' | 'Trainer' | 'Administrator';

interface NavigationItem {
  readonly label: string;
  readonly path: string;
  readonly icon: string;
  readonly roles: readonly ApplicationRole[];
}

@Component({
  selector: 'app-application-layout',
  imports: [RouterLink, RouterLinkActive, RouterOutlet],
  templateUrl: './application-layout.html',
  styleUrl: './application-layout.scss',
})
export class ApplicationLayout {
  protected readonly authentication = inject(AuthenticationService);
  private readonly router = inject(Router);

  protected readonly navigationOpen = signal(false);
  protected readonly isAdministrator = computed(() => this.authentication.hasRole('Administrator'));
  protected readonly homePath = computed(() =>
    this.authentication.hasRole('Administrator')
      ? '/administration'
      : this.authentication.hasRole('Trainer')
        ? '/trainer/classes'
        : '/dashboard');

  protected readonly navigationItems: readonly NavigationItem[] = [
    { label: 'Dashboard', path: '/dashboard', icon: 'D', roles: ['Member'] },
    { label: 'Trainer dashboard', path: '/trainer/classes', icon: 'T', roles: ['Trainer'] },
    { label: 'Admin dashboard', path: '/administration', icon: 'A', roles: ['Administrator'] },
    { label: 'Membership', path: '/membership', icon: 'M', roles: ['Member'] },
    { label: 'Payments', path: '/payments', icon: '€', roles: ['Member'] },
    { label: 'Attendance', path: '/attendance', icon: 'A', roles: ['Member'] },
    { label: 'Workouts', path: '/workouts', icon: 'W', roles: ['Member'] },
    { label: 'Progress', path: '/progress', icon: 'P', roles: ['Member'] },
    { label: 'Recommendations', path: '/recommendations', icon: 'R', roles: ['Member'] },
    { label: 'Class booking', path: '/booking', icon: 'B', roles: ['Member'] },
    { label: 'Workout plans', path: '/workouts/assign', icon: 'W', roles: ['Trainer'] },
    { label: 'Users', path: '/administration/users', icon: 'U', roles: ['Administrator'] },
    { label: 'Trainers', path: '/administration/trainers', icon: 'T', roles: ['Administrator'] },
    { label: 'Class management', path: '/administration/classes', icon: 'C', roles: ['Administrator'] },
    { label: 'Membership plans', path: '/administration/membership-plans', icon: 'M', roles: ['Administrator'] },
    { label: 'Payments / Revenue', path: '/administration/payments', icon: '€', roles: ['Administrator'] },
    { label: 'Equipment', path: '/equipment', icon: 'E', roles: ['Member', 'Administrator'] },
    { label: 'Fault reports', path: '/fault-reports', icon: 'F', roles: ['Member'] },
    { label: 'Fault administration', path: '/administration/fault-reports', icon: '!', roles: ['Administrator'] },
    { label: 'Parking', path: '/parking', icon: 'P', roles: ['Member', 'Administrator'] },
    { label: 'Scanner / validator', path: '/attendance/scanner', icon: 'S', roles: ['Trainer', 'Administrator'] },
    { label: 'Notifications', path: '/notifications', icon: 'N', roles: ['Member', 'Trainer', 'Administrator'] },
    { label: 'Notification preferences', path: '/notification-preferences', icon: 'P', roles: ['Member'] },
    { label: 'System activity', path: '/administration/activity', icon: 'L', roles: ['Administrator'] },
    { label: 'Account', path: '/account', icon: 'U', roles: ['Member', 'Trainer', 'Administrator'] },
  ];

  protected hasAnyRole(roles: readonly ApplicationRole[]): boolean {
    return roles.some(role => this.authentication.hasRole(role));
  }

  protected logout(): void {
    this.authentication.logout().subscribe({
      next: () => this.router.navigateByUrl('/login'),
      error: () => this.router.navigateByUrl('/login'),
    });
  }
}
