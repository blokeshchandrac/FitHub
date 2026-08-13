import { Component, inject, signal } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule } from '@angular/forms';
import { Exercise } from '../../../core/workouts/workout.models';
import { WorkoutService } from '../../../core/workouts/workout.service';

@Component({ selector: 'app-exercise-library-page', imports: [ReactiveFormsModule], templateUrl: './exercise-library-page.html' })
export class ExerciseLibraryPage {
  private readonly service = inject(WorkoutService);
  protected readonly exercises = signal<Exercise[]>([]);
  protected readonly filters = new FormGroup({ search: new FormControl('', { nonNullable: true }), muscleGroup: new FormControl('', { nonNullable: true }), equipment: new FormControl('', { nonNullable: true }) });
  constructor() { this.search(); }
  protected search(): void { const value = this.filters.getRawValue(); this.service.searchExercises(value.search, value.muscleGroup, value.equipment).subscribe((items) => this.exercises.set(items)); }
}
