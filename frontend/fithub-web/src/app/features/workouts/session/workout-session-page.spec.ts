import { TestBed } from '@angular/core/testing';
import { ActivatedRoute } from '@angular/router';
import { of } from 'rxjs';
import { WorkoutService } from '../../../core/workouts/workout.service';
import { WorkoutSession } from '../../../core/workouts/workout.models';
import { WorkoutSessionPage } from './workout-session-page';

describe('WorkoutSessionPage rest time', () => {
  const service = {
    getSession: vi.fn(),
    searchExercises: vi.fn(),
    logExercise: vi.fn(),
    completeSession: vi.fn(),
  };

  beforeEach(async () => {
    Object.values(service).forEach((mock) => mock.mockReset());
    service.getSession.mockReturnValue(of(session([])));
    service.searchExercises.mockReturnValue(of([exercise]));
    service.completeSession.mockReturnValue(of(session([])));
    await TestBed.configureTestingModule({
      imports: [WorkoutSessionPage],
      providers: [
        { provide: WorkoutService, useValue: service },
        { provide: ActivatedRoute, useValue: { snapshot: { paramMap: { get: () => 'session-1' } } } },
      ],
    }).compileComponents();
  });

  it('sends and displays rest time separately from exercise duration', () => {
    const savedSet = {
      id: 'set-1', setNumber: 1, repetitions: 8, weightKg: 60,
      durationSeconds: 45, restSeconds: 90, volume: 480, isPersonalRecord: false,
    };
    service.logExercise.mockReturnValue(of(session([savedSet])));
    const fixture = TestBed.createComponent(WorkoutSessionPage);
    fixture.detectChanges();
    const root = fixture.nativeElement as HTMLElement;

    const select = root.querySelector<HTMLSelectElement>('#session-exercise')!;
    select.value = exercise.id;
    select.dispatchEvent(new Event('change', { bubbles: true }));
    setNumber(root, '[aria-label="Repetitions"]', 8);
    setNumber(root, '[aria-label="Weight in kilograms"]', 60);
    setNumber(root, '[aria-label="Exercise duration in seconds"]', 45);
    setNumber(root, '[aria-label="Rest time in seconds"]', 90);
    clickButton(root, 'Add set');
    fixture.detectChanges();

    expect(root.querySelector('.set-list')?.textContent).toContain('Duration 45 s');
    expect(root.querySelector('.set-list')?.textContent).toContain('Rest 90 s');
    clickButton(root, 'Save exercise');
    fixture.detectChanges();

    expect(service.logExercise).toHaveBeenCalledWith('session-1', exercise.id, '', [{
      repetitions: 8,
      weightKg: 60,
      durationSeconds: 45,
      restSeconds: 90,
    }]);
    const row = root.querySelector('.exercise-log-card tbody tr');
    expect(row?.textContent).toContain('45 s');
    expect(row?.textContent).toContain('90 s');
  });

});

function setNumber(root: HTMLElement, selector: string, value: number): void {
  const input = root.querySelector<HTMLInputElement>(selector)!;
  input.value = value.toString();
  input.dispatchEvent(new Event('input', { bubbles: true }));
}

function clickButton(root: HTMLElement, label: string): void {
  const button = Array.from(root.querySelectorAll<HTMLButtonElement>('button')).find((item) => item.textContent?.trim() === label)!;
  button.click();
}

function session(sets: WorkoutSession['exercises'][number]['sets']): WorkoutSession {
  return {
    id: 'session-1', workoutTemplateId: 'template-1', templateName: 'Strength', status: 'InProgress',
    startedAt: '2026-10-21T08:00:00Z', endedAt: null, notes: null,
    volume: sets.reduce((total, set) => total + set.volume, 0),
    exercises: [{ exerciseLogId: 'log-1', exercise, notes: null, previousPerformance: null, sets }],
  };
}

const exercise = { id: 'exercise-1', name: 'Bench Press', muscleGroup: 'Chest', equipment: 'Barbell', instructions: null };
