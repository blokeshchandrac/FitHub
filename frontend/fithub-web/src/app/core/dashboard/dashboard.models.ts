export interface PersonalRecord { exerciseId: string; exerciseName: string; weightKg: number; achievedAt: string; }
export interface WeightTrendPoint { recordedAt: string; weightKg: number; }
export interface MemberDashboard { attendanceFrequencyPerWeek: number; workoutFrequencyPerWeek: number; weeklyVolume: number; personalRecords: PersonalRecord[]; currentWorkoutStreakWeeks: number; consistencyScore: number; weightTrend: { latestWeightKg: number | null; changeKg: number | null; points: WeightTrendPoint[] }; membershipVisits: number; membershipCostPerVisit: number | null; }
