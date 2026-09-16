import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { ProfileService } from './profile.service';

describe('ProfileService', () => {
  beforeEach(() => TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting()] }));
  afterEach(() => TestBed.inject(HttpTestingController).verify());

  it('sends the controlled experience, planned weekdays and branch fields', () => {
    const service = TestBed.inject(ProfileService);
    service.update({
      preferredName: 'Alex', dateOfBirth: '1995-05-20', heightCm: 175, weightKg: 75,
      fitnessGoal: 'Strength', activityLevel: 'Moderate', experienceLevel: 'Intermediate',
      plannedTrainingDays: ['Monday', 'Thursday'], preferredGymBranchId: 'branch-1',
    }).subscribe();
    const request = TestBed.inject(HttpTestingController).expectOne('/api/profile');
    expect(request.request.method).toBe('PUT');
    expect(request.request.body.experienceLevel).toBe('Intermediate');
    expect(request.request.body.plannedTrainingDays).toEqual(['Monday', 'Thursday']);
    expect(request.request.body.preferredGymBranchId).toBe('branch-1');
    request.flush({});
  });

  it('loads only active profile branches from the profile catalogue endpoint', () => {
    TestBed.inject(ProfileService).branches().subscribe();
    const request = TestBed.inject(HttpTestingController).expectOne('/api/profile/branches');
    expect(request.request.method).toBe('GET');
    request.flush([]);
  });
});
