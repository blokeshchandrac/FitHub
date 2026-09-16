import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import {
  AdminPayment,
  AdminUserDetails,
  AdminUserSummary,
  CreateTrainerRequest,
  EligibleMember,
  SystemActivity,
  TrainerMemberAssignment,
} from './administration-management.models';

@Injectable({ providedIn: 'root' })
export class AdministrationManagementService {
  private readonly http = inject(HttpClient);

  users(search = '', role = '', active?: boolean) {
    let params = new HttpParams();
    if (search.trim()) params = params.set('search', search.trim());
    if (role) params = params.set('role', role);
    if (active !== undefined) params = params.set('active', active);
    return this.http.get<readonly AdminUserSummary[]>('/api/administration/users', { params });
  }

  user(id: string) { return this.http.get<AdminUserDetails>(`/api/administration/users/${id}`); }
  setUserActive(id: string, active: boolean) { return this.http.put<void>(`/api/administration/users/${id}/active`, { active }); }
  setMembershipEnabled(id: string, enabled: boolean) { return this.http.put<void>(`/api/administration/memberships/${id}/enabled`, { enabled }); }
  setCardActive(id: string, active: boolean) { return this.http.put<void>(`/api/administration/cards/${id}/active`, { active }); }
  trainers() { return this.http.get<readonly AdminUserSummary[]>('/api/administration/trainers'); }
  createTrainer(request: CreateTrainerRequest) { return this.http.post<AdminUserSummary>('/api/administration/trainers', request); }
  activity() { return this.http.get<readonly SystemActivity[]>('/api/administration/activity'); }
  payments() { return this.http.get<readonly AdminPayment[]>('/api/administration/payments'); }

  trainerAssignments(trainerId: string, includeEnded = true) {
    return this.http.get<readonly TrainerMemberAssignment[]>(`/api/administration/trainers/${trainerId}/assignments`, {
      params: { includeEnded },
    });
  }

  eligibleMembers(trainerId: string, search = '') {
    let params = new HttpParams();
    if (search.trim()) params = params.set('search', search.trim());
    return this.http.get<readonly EligibleMember[]>(`/api/administration/trainers/${trainerId}/assignments/eligible-members`, { params });
  }

  assignMember(trainerId: string, memberId: string) {
    return this.http.post<TrainerMemberAssignment>(`/api/administration/trainers/${trainerId}/assignments`, { memberId });
  }

  unassignMember(trainerId: string, assignmentId: string) {
    return this.http.delete<void>(`/api/administration/trainers/${trainerId}/assignments/${assignmentId}`);
  }
}
