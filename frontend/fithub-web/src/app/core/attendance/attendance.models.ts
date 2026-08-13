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
