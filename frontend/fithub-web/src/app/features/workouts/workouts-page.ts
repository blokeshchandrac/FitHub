import { DatePipe, DecimalPipe } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { AuthenticationService } from '../../core/authentication/authentication.service';
import { WorkoutHistory, WorkoutTemplate } from '../../core/workouts/workout.models';
import { WorkoutService } from '../../core/workouts/workout.service';

@Component({ selector: 'app-workouts-page', imports: [DatePipe, DecimalPipe, RouterLink], templateUrl: './workouts-page.html' })
export class WorkoutsPage {
  private readonly service = inject(WorkoutService);
  private readonly router = inject(Router);
  protected readonly authentication = inject(AuthenticationService);
  protected readonly templates = signal<WorkoutTemplate[]>([]);
  protected readonly history = signal<WorkoutHistory[]>([]);
  protected readonly error = signal<string | null>(null);
  constructor() { this.load(); }
  protected start(templateId?: string): void { this.service.startSession(templateId).subscribe({ next: (session) => this.router.navigate(['/workouts/session', session.id]), error: () => this.error.set('Workout could not be started.') }); }
  private load(): void { this.service.getTemplates().subscribe({ next: (value) => this.templates.set(value), error: () => this.error.set('Workout templates could not be loaded.') }); this.service.getHistory().subscribe({ next: (value) => this.history.set(value) }); }
}
