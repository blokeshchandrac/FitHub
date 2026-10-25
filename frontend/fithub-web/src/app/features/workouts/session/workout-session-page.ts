import { DatePipe, DecimalPipe } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';
import { Exercise, WorkoutSession, WorkoutSetCommand } from '../../../core/workouts/workout.models';
import { WorkoutService } from '../../../core/workouts/workout.service';

@Component({ selector: 'app-workout-session-page', imports: [DatePipe, DecimalPipe, ReactiveFormsModule], templateUrl: './workout-session-page.html' })
export class WorkoutSessionPage {
  private readonly service = inject(WorkoutService); private readonly sessionId = inject(ActivatedRoute).snapshot.paramMap.get('id')!;
  protected readonly session = signal<WorkoutSession | null>(null); protected readonly exercises = signal<Exercise[]>([]); protected readonly pendingSets = signal<WorkoutSetCommand[]>([]); protected readonly error = signal<string | null>(null); protected readonly message = signal<string | null>(null);
  protected readonly exerciseId = new FormControl('', { nonNullable: true, validators: [Validators.required] });
  protected readonly setForm = new FormGroup({ repetitions: new FormControl(0, { nonNullable: true, validators: [Validators.min(0), Validators.max(1000)] }), weightKg: new FormControl(0, { nonNullable: true, validators: [Validators.min(0), Validators.max(1000)] }), durationSeconds: new FormControl<number | null>(null, [Validators.min(0), Validators.max(86400)]), restSeconds: new FormControl<number | null>(null, [Validators.min(0), Validators.max(3600)]) });
  protected readonly notes = new FormControl('', { nonNullable: true, validators: [Validators.maxLength(1000)] });
  protected readonly completionNotes = new FormControl('', { nonNullable: true, validators: [Validators.maxLength(2000)] });
  constructor() { this.load(); this.service.searchExercises().subscribe((items) => this.exercises.set(items)); }
  protected selectExercise(id: string): void { this.exerciseId.setValue(id); const log = this.session()?.exercises.find((item) => item.exercise.id === id); this.pendingSets.set(log?.sets.map((set) => ({ repetitions: set.repetitions ?? 0, weightKg: set.weightKg ?? 0, durationSeconds: set.durationSeconds, restSeconds: set.restSeconds })) ?? []); this.notes.setValue(log?.notes ?? ''); }
  protected addSet(): void { if (this.setForm.invalid) return; const set = this.setForm.getRawValue(); if (!set.repetitions && !set.weightKg && !set.durationSeconds) { this.error.set('Enter repetitions, weight, or duration.'); return; } this.pendingSets.update((items) => [...items, set]); this.setForm.reset({ repetitions: 0, weightKg: 0, durationSeconds: null, restSeconds: null }); }
  protected removeSet(index: number): void { this.pendingSets.update((items) => items.filter((_, itemIndex) => itemIndex !== index)); }
  protected saveExercise(): void { if (this.exerciseId.invalid || this.pendingSets().length === 0) { this.error.set('Choose an exercise and add at least one set.'); return; } this.service.logExercise(this.sessionId, this.exerciseId.value, this.notes.value, this.pendingSets()).subscribe({ next: (value) => { this.session.set(value); this.message.set('Exercise performance saved.'); }, error: (error) => this.error.set(Object.values(error.error?.errors ?? {}).flat().join(' ') || 'Performance could not be saved.') }); }
  protected complete(): void { this.service.completeSession(this.sessionId, this.completionNotes.value).subscribe({ next: (value) => { this.session.set(value); this.message.set('Workout completed.'); }, error: (error) => this.error.set(Object.values(error.error?.errors ?? {}).flat().join(' ') || 'Workout could not be completed.') }); }
  private load(): void { this.service.getSession(this.sessionId).subscribe({ next: (value) => this.session.set(value), error: () => this.error.set('Workout session could not be loaded.') }); }
}
