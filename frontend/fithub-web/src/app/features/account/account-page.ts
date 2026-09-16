import { Component, inject, OnInit, signal } from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { finalize, forkJoin, switchMap } from 'rxjs';
import { AuthenticationService } from '../../core/authentication/authentication.service';
import { ActivityLevel, ExperienceLevel, FitnessGoal, GymBranch, TrainingDay } from '../../core/profile/profile.models';
import { ProfileService } from '../../core/profile/profile.service';
import { ThemeSelector } from '../../shared/ui/theme-selector/theme-selector';

@Component({
  selector: 'app-account-page',
  imports: [ReactiveFormsModule, ThemeSelector],
  templateUrl: './account-page.html',
})
export class AccountPage implements OnInit {
  protected readonly authentication = inject(AuthenticationService);
  private readonly profiles = inject(ProfileService);
  protected readonly loading = signal(false);
  protected readonly saving = signal(false);
  protected readonly message = signal<string | null>(null);
  protected readonly error = signal<string | null>(null);
  protected readonly branches = signal<readonly GymBranch[]>([]);
  protected readonly experienceLevels: readonly ExperienceLevel[] = ['Beginner', 'Intermediate', 'Advanced'];
  protected readonly trainingDays: readonly TrainingDay[] = ['Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday', 'Sunday'];
  protected readonly form = new FormGroup({
    preferredName: new FormControl('', { nonNullable: true, validators: [Validators.required, Validators.maxLength(50)] }),
    dateOfBirth: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
    heightCm: new FormControl<number | null>(null, { validators: [Validators.required, Validators.min(80), Validators.max(250)] }),
    weightKg: new FormControl<number | null>(null, { validators: [Validators.required, Validators.min(25), Validators.max(350)] }),
    fitnessGoal: new FormControl<FitnessGoal>('GeneralFitness', { nonNullable: true }),
    activityLevel: new FormControl<ActivityLevel>('Moderate', { nonNullable: true }),
    experienceLevel: new FormControl<ExperienceLevel>('Beginner', { nonNullable: true, validators: [Validators.required] }),
    plannedTrainingDays: new FormControl<TrainingDay[]>([], { nonNullable: true, validators: [Validators.required, Validators.minLength(1)] }),
    preferredGymBranchId: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
  });

  ngOnInit(): void {
    if (!this.authentication.hasRole('Member')) return;
    this.loading.set(true);
    forkJoin({ profile: this.profiles.get(), branches: this.profiles.branches() }).pipe(finalize(() => this.loading.set(false))).subscribe({
      next: ({ profile, branches }) => {
        this.branches.set(branches);
        this.form.setValue({
        preferredName: profile.preferredName ?? profile.firstName,
        dateOfBirth: profile.dateOfBirth ?? '', heightCm: profile.heightCm, weightKg: profile.weightKg,
        fitnessGoal: profile.fitnessGoal, activityLevel: profile.activityLevel,
        experienceLevel: profile.experienceLevel ?? 'Beginner', plannedTrainingDays: [...profile.plannedTrainingDays],
        preferredGymBranchId: profile.preferredGymBranchId ?? '',
      }); },
      error: () => this.error.set('Profile details could not be loaded.'),
    });
  }

  protected isDaySelected(day: TrainingDay): boolean { return this.form.controls.plannedTrainingDays.value.includes(day); }
  protected toggleDay(day: TrainingDay, event: Event): void {
    const selected = (event.target as HTMLInputElement).checked;
    const current = this.form.controls.plannedTrainingDays.value;
    this.form.controls.plannedTrainingDays.setValue(selected
      ? [...current.filter(value => value !== day), day]
      : current.filter(value => value !== day));
    this.form.controls.plannedTrainingDays.markAsTouched();
  }

  protected save(): void {
    if (this.form.invalid) { this.form.markAllAsTouched(); return; }
    const value = this.form.getRawValue();
    this.saving.set(true); this.error.set(null); this.message.set(null);
    this.profiles.update({ ...value, heightCm: value.heightCm!, weightKg: value.weightKg! }).pipe(
      switchMap(() => this.authentication.refreshCurrentUser()),
      finalize(() => this.saving.set(false)),
    ).subscribe({
      next: () => this.message.set('Profile updated.'),
      error: (response: HttpErrorResponse) => this.error.set(this.validationMessage(response.error) ?? 'Profile details could not be saved.'),
    });
  }

  private validationMessage(response: unknown): string | null {
    if (!response || typeof response !== 'object' || !('errors' in response)) return null;
    const errors = (response as { errors?: Record<string, string[]> }).errors;
    return errors ? Object.values(errors).flat().join(' ') : null;
  }
}
