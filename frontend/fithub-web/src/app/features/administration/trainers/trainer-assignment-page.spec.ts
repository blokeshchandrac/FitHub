import { TestBed } from '@angular/core/testing';
import { ActivatedRoute, provideRouter } from '@angular/router';
import { of, throwError } from 'rxjs';
import { AdministrationManagementService } from '../../../core/administration/administration-management.service';
import {
  AdminUserDetails,
  EligibleMember,
  TrainerMemberAssignment,
} from '../../../core/administration/administration-management.models';
import { TrainerAssignmentPage } from './trainer-assignment-page';

describe('TrainerAssignmentPage submission', () => {
  const trainerId = 'trainer-1';
  const member: EligibleMember = { id: 'member-1', displayName: 'Elena Testmember', email: 'elena@fithub.test' };
  const assignment: TrainerMemberAssignment = {
    id: 'assignment-1',
    trainerId,
    trainerName: 'Tariq Testtrainer',
    memberId: member.id,
    memberName: member.displayName,
    memberEmail: member.email,
    status: 'Active',
    assignedAt: '2026-09-17T05:35:26.687502Z',
    assignedByAdministratorId: 'admin-1',
    assignedByAdministratorName: 'Amina Testadmin',
    unassignedAt: null,
    unassignedByAdministratorId: null,
    unassignedByAdministratorName: null,
  };
  const trainer: AdminUserDetails = {
    user: {
      id: trainerId,
      email: 'tariq@fithub.test',
      displayName: 'Tariq Testtrainer',
      roles: ['Trainer'],
      isActive: true,
      membershipStatus: 'None',
      membershipId: null,
      activeCardCount: 0,
      createdAt: '2026-09-01T00:00:00Z',
    },
    preferredName: null,
    dateOfBirth: null,
    heightCm: null,
    weightKg: null,
    fitnessGoal: '',
    activityLevel: '',
    experienceLevel: null,
    plannedTrainingDays: [],
    preferredGymBranchId: null,
    preferredGymBranchName: null,
    onboardingCompleted: true,
    cards: [],
  };
  const service = {
    user: vi.fn(),
    trainerAssignments: vi.fn(),
    eligibleMembers: vi.fn(),
    assignMember: vi.fn(),
    unassignMember: vi.fn(),
  };

  beforeEach(async () => {
    Object.values(service).forEach(mock => mock.mockReset());
    service.user.mockReturnValue(of(trainer));
    service.trainerAssignments.mockReturnValue(of([]));
    service.eligibleMembers.mockReturnValue(of([member]));
    service.assignMember.mockReturnValue(of(assignment));
    service.unassignMember.mockReturnValue(of(undefined));
    await TestBed.configureTestingModule({
      imports: [TrainerAssignmentPage],
      providers: [
        provideRouter([]),
        { provide: AdministrationManagementService, useValue: service },
        { provide: ActivatedRoute, useValue: { snapshot: { paramMap: { get: () => trainerId } } } },
      ],
    }).compileComponents();
  });

  it('assigns once, prevents navigation, confirms success, and reloads assignment data', () => {
    const fixture = TestBed.createComponent(TrainerAssignmentPage);
    fixture.detectChanges();
    const root = fixture.nativeElement as HTMLElement;
    const select = root.querySelector<HTMLSelectElement>('#eligible-member')!;
    select.value = member.id;
    select.dispatchEvent(new Event('change', { bubbles: true }));

    const submit = new Event('submit', { bubbles: true, cancelable: true });
    root.querySelector<HTMLFormElement>('form')!.dispatchEvent(submit);
    fixture.detectChanges();

    expect(submit.defaultPrevented).toBe(true);
    expect(service.assignMember).toHaveBeenCalledOnce();
    expect(service.assignMember).toHaveBeenCalledWith(trainerId, member.id);
    expect(service.trainerAssignments).toHaveBeenCalledTimes(2);
    expect(service.eligibleMembers).toHaveBeenCalledTimes(2);
    expect(root.querySelector('[role="status"]')?.textContent).toContain('Member assigned to trainer.');
  });

  it('does not call the API when no Member is selected', () => {
    const fixture = TestBed.createComponent(TrainerAssignmentPage);
    fixture.detectChanges();
    const root = fixture.nativeElement as HTMLElement;
    const submit = new Event('submit', { bubbles: true, cancelable: true });

    root.querySelector<HTMLFormElement>('form')!.dispatchEvent(submit);
    fixture.detectChanges();

    expect(submit.defaultPrevented).toBe(true);
    expect(service.assignMember).not.toHaveBeenCalled();
    expect(root.querySelector('.field-error')?.textContent).toContain('Choose an eligible Member.');
  });

  it('shows a backend validation error when assignment fails', () => {
    service.assignMember.mockReturnValueOnce(throwError(() => ({
      error: { errors: { memberId: ['Member is disabled.'] } },
    })));
    const fixture = TestBed.createComponent(TrainerAssignmentPage);
    fixture.detectChanges();
    const root = fixture.nativeElement as HTMLElement;
    const select = root.querySelector<HTMLSelectElement>('#eligible-member')!;
    select.value = member.id;
    select.dispatchEvent(new Event('change', { bubbles: true }));

    root.querySelector<HTMLFormElement>('form')!.dispatchEvent(new Event('submit', { bubbles: true, cancelable: true }));
    fixture.detectChanges();

    expect(service.assignMember).toHaveBeenCalledOnce();
    expect(root.querySelector('[role="alert"]')?.textContent).toContain('Member is disabled.');
  });
});
