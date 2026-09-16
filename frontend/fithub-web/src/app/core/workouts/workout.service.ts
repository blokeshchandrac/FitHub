import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { AssignedMember, Exercise, WorkoutAssignment, WorkoutHistory, WorkoutSession, WorkoutSetCommand, WorkoutTemplate, WorkoutTemplateCommand } from './workout.models';

@Injectable({ providedIn: 'root' })
export class WorkoutService {
  private readonly http = inject(HttpClient);
  searchExercises(search = '', muscleGroup = '', equipment = ''): Observable<Exercise[]> {
    let params = new HttpParams();
    if (search) params = params.set('search', search);
    if (muscleGroup) params = params.set('muscleGroup', muscleGroup);
    if (equipment) params = params.set('equipment', equipment);
    return this.http.get<Exercise[]>('/api/exercises', { params });
  }
  getTemplates(): Observable<WorkoutTemplate[]> { return this.http.get<WorkoutTemplate[]>('/api/workout-templates'); }
  createTemplate(command: WorkoutTemplateCommand): Observable<WorkoutTemplate> { return this.http.post<WorkoutTemplate>('/api/workout-templates', command); }
  updateTemplate(id: string, command: WorkoutTemplateCommand): Observable<WorkoutTemplate> { return this.http.put<WorkoutTemplate>(`/api/workout-templates/${id}`, command); }
  startSession(templateId?: string): Observable<WorkoutSession> { return this.http.post<WorkoutSession>(`/api/workout-sessions${templateId ? `?templateId=${templateId}` : ''}`, {}); }
  getSession(id: string): Observable<WorkoutSession> { return this.http.get<WorkoutSession>(`/api/workout-sessions/${id}`); }
  getHistory(): Observable<WorkoutHistory[]> { return this.http.get<WorkoutHistory[]>('/api/workout-sessions'); }
  logExercise(sessionId: string, exerciseId: string, notes: string, sets: WorkoutSetCommand[]): Observable<WorkoutSession> { return this.http.put<WorkoutSession>(`/api/workout-sessions/${sessionId}/exercise`, { exerciseId, notes, sets }); }
  completeSession(sessionId: string, notes: string): Observable<WorkoutSession> { return this.http.post<WorkoutSession>(`/api/workout-sessions/${sessionId}/complete`, { notes }); }
  getTrainerMembers(): Observable<AssignedMember[]> { return this.http.get<AssignedMember[]>('/api/trainer/workout-assignments/members'); }
  getTrainerPlans(): Observable<WorkoutTemplate[]> { return this.http.get<WorkoutTemplate[]>('/api/trainer/workout-plans'); }
  getTrainerExercises(): Observable<Exercise[]> { return this.http.get<Exercise[]>('/api/trainer/workout-plans/exercises'); }
  createTrainerPlan(command: WorkoutTemplateCommand): Observable<WorkoutTemplate> { return this.http.post<WorkoutTemplate>('/api/trainer/workout-plans', command); }
  updateTrainerPlan(id: string, command: WorkoutTemplateCommand): Observable<WorkoutTemplate> { return this.http.put<WorkoutTemplate>(`/api/trainer/workout-plans/${id}`, command); }
  getTrainerAssignments(): Observable<WorkoutAssignment[]> { return this.http.get<WorkoutAssignment[]>('/api/trainer/workout-assignments'); }
  assignTemplate(templateId: string, memberUserId: string): Observable<WorkoutAssignment> { return this.http.post<WorkoutAssignment>('/api/trainer/workout-assignments', { templateId, memberUserId }); }
  removeTrainerAssignment(id: string): Observable<void> { return this.http.delete<void>(`/api/trainer/workout-assignments/${id}`); }
}
