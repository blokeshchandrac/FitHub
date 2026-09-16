import { Component, DestroyRef, inject, OnInit, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { auditTime } from 'rxjs';
import { AuthenticationService } from '../../core/authentication/authentication.service';
import { ParkingAlert, ParkingOverview } from '../../core/parking/parking.models';
import { ParkingService } from '../../core/parking/parking.service';

@Component({ selector: 'app-parking-page', imports: [FormsModule], templateUrl: './parking-page.html' })
export class ParkingPage implements OnInit {
  private readonly service = inject(ParkingService);
  private readonly destroyRef = inject(DestroyRef);
  protected readonly auth = inject(AuthenticationService);
  protected readonly overview = signal<ParkingOverview | null>(null);
  protected readonly alerts = signal<readonly ParkingAlert[]>([]);
  protected readonly error = signal('');
  protected newCode = '';
  protected newZone = '';
  protected accessible = false;

  ngOnInit(): void {
    this.load();
    this.service.changed.pipe(auditTime(75), takeUntilDestroyed(this.destroyRef)).subscribe(() => this.load());
    void this.service.connect();
  }

  protected correct(id: string, state: string, reason: string): void {
    if (!reason.trim()) { this.error.set('A correction reason is required.'); return; }
    this.service.correct(id, state, reason).subscribe({
      next: () => this.load(),
      error: error => this.error.set(error.error?.detail ?? 'Correction failed.'),
    });
  }

  protected create(): void {
    this.service.create(this.newCode, this.newZone, this.accessible).subscribe({
      next: () => { this.newCode = ''; this.newZone = ''; this.load(); },
      error: error => this.error.set(error.error?.detail ?? 'Space could not be added.'),
    });
  }

  private load(): void {
    this.service.overview().subscribe({
      next: overview => this.overview.set(overview),
      error: () => this.error.set('Parking information could not be loaded.'),
    });
    if (this.auth.hasRole('Administrator')) {
      this.service.alerts(true).subscribe({
        next: alerts => this.alerts.set(alerts),
        error: () => this.error.set('Parking alerts could not be loaded.'),
      });
    }
  }
}
