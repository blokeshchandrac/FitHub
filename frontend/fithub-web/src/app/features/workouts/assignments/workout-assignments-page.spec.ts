import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { WorkoutAssignmentsPage } from './workout-assignments-page';

describe('WorkoutAssignmentsPage', () => {
  beforeEach(() => TestBed.configureTestingModule({
    imports: [WorkoutAssignmentsPage],
    providers: [provideHttpClient(), provideHttpClientTesting()],
  }));
  afterEach(() => TestBed.inject(HttpTestingController).verify());

  it('loads trainer-owned plans and assigned Members without calling the Member template API', () => {
    TestBed.createComponent(WorkoutAssignmentsPage);
    const http = TestBed.inject(HttpTestingController);
    const plans = http.expectOne('/api/trainer/workout-plans');
    expect(plans.request.method).toBe('GET');
    plans.flush([]);
    http.expectOne('/api/trainer/workout-assignments/members').flush([]);
    http.expectOne('/api/trainer/workout-plans/exercises').flush([]);
    http.expectOne('/api/trainer/workout-assignments').flush([]);
    http.expectNone('/api/workout-templates');
  });
});
