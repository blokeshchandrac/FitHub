import { Component, inject, OnInit, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { FormControl, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { forkJoin } from 'rxjs';
import {
  AdminUserDetails,
  EligibleMember,
  TrainerMemberAssignment,
} from '../../../core/administration/administration-management.models';
import { AdministrationManagementService } from '../../../core/administration/administration-management.service';

@Component({
  selector: 'app-trainer-assignment-page',
  imports: [DatePipe, ReactiveFormsModule, RouterLink],
  templateUrl: './trainer-assignment-page.html',
})
export class TrainerAssignmentPage implements OnInit {
  private readonly service = inject(AdministrationManagementService);
  private readonly route = inject(ActivatedRoute);
  protected readonly trainer = signal<AdminUserDetails | null>(null);
  protected readonly assignments = signal<readonly TrainerMemberAssignment[]>([]);
  protected readonly eligibleMembers = signal<readonly EligibleMember[]>([]);
  protected readonly loading = signal(true);
  protected readonly saving = signal(false);
  protected readonly error = signal('');
  protected readonly message = signal('');
  protected readonly memberId = new FormControl('', { nonNullable: true, validators: [Validators.required] });
  protected readonly memberSearch = new FormControl('', { nonNullable: true });
  private trainerId = '';

  ngOnInit(): void {
    this.trainerId = this.route.snapshot.paramMap.get('id') ?? '';
    this.load();
  }

  protected searchMembers(): void {
    this.service.eligibleMembers(this.trainerId, this.memberSearch.value).subscribe({
      next: members => this.eligibleMembers.set(members),
      error: () => this.error.set('Eligible members could not be loaded.'),
    });
  }

  protected assign(): void {
    if (this.memberId.invalid) { this.memberId.markAsTouched(); return; }
    this.saving.set(true); this.error.set(''); this.message.set('');
    this.service.assignMember(this.trainerId, this.memberId.value).subscribe({
      next: () => {
        this.message.set('Member assigned to trainer.');
        this.memberId.setValue('');
        this.saving.set(false);
        this.loadDetails();
      },
      error: error => {
        this.saving.set(false);
        this.error.set(this.validationMessage(error.error) ?? 'The member could not be assigned.');
      },
    });
  }

  protected unassign(assignment: TrainerMemberAssignment): void {
    this.error.set(''); this.message.set('');
    this.service.unassignMember(this.trainerId, assignment.id).subscribe({
      next: () => { this.message.set('Trainer assignment ended. Its history was preserved.'); this.loadDetails(); },
      error: () => this.error.set('The trainer assignment could not be ended.'),
    });
  }

  private load(): void {
    if (!this.trainerId) { this.error.set('Trainer identifier is missing.'); this.loading.set(false); return; }
    forkJoin({
      trainer: this.service.user(this.trainerId),
      assignments: this.service.trainerAssignments(this.trainerId),
      eligible: this.service.eligibleMembers(this.trainerId),
    }).subscribe({
      next: result => {
        this.trainer.set(result.trainer);
        this.assignments.set(result.assignments);
        this.eligibleMembers.set(result.eligible);
        this.loading.set(false);
      },
      error: () => { this.error.set('Trainer assignment details could not be loaded.'); this.loading.set(false); },
    });
  }

  private loadDetails(): void {
    forkJoin({
      assignments: this.service.trainerAssignments(this.trainerId),
      eligible: this.service.eligibleMembers(this.trainerId, this.memberSearch.value),
    }).subscribe(({ assignments, eligible }) => {
      this.assignments.set(assignments);
      this.eligibleMembers.set(eligible);
    });
  }

  private validationMessage(response: unknown): string | null {
    if (!response || typeof response !== 'object' || !('errors' in response)) return null;
    const errors = (response as { errors?: Record<string, string[]> }).errors;
    return errors ? Object.values(errors).flat().join(' ') : null;
  }
}
