export interface CheckInToken { token: string; expiresAt: string; }
export interface AttendanceRecord { id: string; checkedInAt: string; checkedOutAt: string | null; }
export interface AttendanceSummary {
  visitsThisWeek: number;
  visitsThisMonth: number;
  visitsInCurrentMembership: number;
  costPerVisit: number | null;
  history: AttendanceRecord[];
}
export interface CheckInResult { attendanceId: string; userId: string; checkedInAt: string; }
export type AccessValidationCode='VALID'|'BLOCKED'|'EXPIRED'|'MEMBERSHIP_INACTIVE'|'USER_DISABLED'|'INVALID_TOKEN';
export interface AccessValidation{readonly code:AccessValidationCode;readonly message:string;readonly credentialType:string|null;readonly userId:string|null;readonly memberName:string|null;readonly membershipExpiresAt:string|null;}
