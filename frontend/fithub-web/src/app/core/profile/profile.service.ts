import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { GymBranch, MemberProfile, UpdateMemberProfile } from './profile.models';

@Injectable({ providedIn: 'root' })
export class ProfileService {
  private readonly http = inject(HttpClient);
  get(): Observable<MemberProfile> { return this.http.get<MemberProfile>('/api/profile'); }
  branches(): Observable<GymBranch[]> { return this.http.get<GymBranch[]>('/api/profile/branches'); }
  update(request: UpdateMemberProfile): Observable<MemberProfile> { return this.http.put<MemberProfile>('/api/profile', request); }
}
