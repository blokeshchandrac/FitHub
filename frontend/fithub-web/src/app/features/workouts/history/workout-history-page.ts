import { DatePipe, DecimalPipe } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { WorkoutHistory } from '../../../core/workouts/workout.models';
import { WorkoutService } from '../../../core/workouts/workout.service';
@Component({ selector: 'app-workout-history-page', imports: [DatePipe, DecimalPipe, RouterLink], templateUrl: './workout-history-page.html' })
export class WorkoutHistoryPage { private readonly service = inject(WorkoutService); protected readonly history = signal<WorkoutHistory[]>([]); constructor() { this.service.getHistory().subscribe((value) => this.history.set(value)); } protected durationMinutes(item: WorkoutHistory): number | null { return item.endedAt ? Math.round((new Date(item.endedAt).getTime() - new Date(item.startedAt).getTime()) / 60000) : null; } }
