import { Component, inject, signal } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { forkJoin } from 'rxjs';
import { AssignedMember, Exercise, WorkoutAssignment, WorkoutTemplate } from '../../../core/workouts/workout.models';
import { WorkoutService } from '../../../core/workouts/workout.service';

@Component({
  selector: 'app-workout-assignments-page',
  imports: [ReactiveFormsModule],
  templateUrl: './workout-assignments-page.html',
})
export class WorkoutAssignmentsPage {
  private readonly service = inject(WorkoutService);
  protected readonly plans = signal<readonly WorkoutTemplate[]>([]);
  protected readonly members = signal<readonly AssignedMember[]>([]);
  protected readonly exercises = signal<readonly Exercise[]>([]);
  protected readonly assignments = signal<readonly WorkoutAssignment[]>([]);
  protected readonly editingPlanId = signal<string | null>(null);
  protected readonly message = signal<string | null>(null);
  protected readonly error = signal<string | null>(null);
  protected readonly planForm = new FormGroup({
    name: new FormControl('', { nonNullable: true, validators: [Validators.required, Validators.maxLength(150)] }),
    description: new FormControl('', { nonNullable: true, validators: [Validators.maxLength(2000)] }),
    exerciseIds: new FormControl<string[]>([], { nonNullable: true, validators: [Validators.required, Validators.minLength(1)] }),
  });
  protected readonly assignmentForm = new FormGroup({
    templateId: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
    memberUserId: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
  });

  constructor() { this.load(); }

  protected savePlan(): void {
    if (this.planForm.invalid) { this.planForm.markAllAsTouched(); return; }
    this.clearFeedback();
    const value = this.planForm.getRawValue();
    const command = { ...value, isPublic: false };
    const operation = this.editingPlanId()
      ? this.service.updateTrainerPlan(this.editingPlanId()!, command)
      : this.service.createTrainerPlan(command);
    operation.subscribe({
      next: () => { this.message.set(this.editingPlanId() ? 'Trainer plan updated.' : 'Trainer plan created.'); this.cancelEdit(); this.load(); },
      error: error => this.error.set(this.validationMessage(error.error) ?? 'Trainer plan could not be saved.'),
    });
  }

  protected editPlan(plan: WorkoutTemplate): void {
    this.editingPlanId.set(plan.id);
    this.planForm.setValue({ name: plan.name, description: plan.description ?? '', exerciseIds: plan.exercises.map(item => item.id) });
  }

  protected cancelEdit(): void {
    this.editingPlanId.set(null);
    this.planForm.reset({ name: '', description: '', exerciseIds: [] });
  }

  protected assign(): void {
    if (this.assignmentForm.invalid) { this.assignmentForm.markAllAsTouched(); return; }
    this.clearFeedback();
    const value = this.assignmentForm.getRawValue();
    this.service.assignTemplate(value.templateId, value.memberUserId).subscribe({
      next: () => { this.message.set('Workout plan assigned.'); this.assignmentForm.reset({ templateId: '', memberUserId: '' }); this.load(); },
      error: error => this.error.set(this.validationMessage(error.error) ?? 'Plan could not be assigned.'),
    });
  }

  protected removeAssignment(assignment: WorkoutAssignment): void {
    this.clearFeedback();
    this.service.removeTrainerAssignment(assignment.id).subscribe({
      next: () => { this.message.set('Future plan assignment removed. Completed workouts were unchanged.'); this.load(); },
      error: () => this.error.set('Plan assignment could not be removed.'),
    });
  }

  private load(): void {
    forkJoin({
      plans: this.service.getTrainerPlans(),
      members: this.service.getTrainerMembers(),
      exercises: this.service.getTrainerExercises(),
      assignments: this.service.getTrainerAssignments(),
    }).subscribe({
      next: result => {
        this.plans.set(result.plans);
        this.members.set(result.members);
        this.exercises.set(result.exercises);
        this.assignments.set(result.assignments);
      },
      error: () => this.error.set('Trainer workout-plan data could not be loaded.'),
    });
  }

  private clearFeedback(): void { this.message.set(null); this.error.set(null); }

  private validationMessage(response: unknown): string | null {
    if (!response || typeof response !== 'object' || !('errors' in response)) return null;
    const errors = (response as { errors?: Record<string, string[]> }).errors;
    return errors ? Object.values(errors).flat().join(' ') : null;
  }
}
