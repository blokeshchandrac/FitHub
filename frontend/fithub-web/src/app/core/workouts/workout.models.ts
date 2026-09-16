export interface Exercise { id: string; name: string; muscleGroup: string; equipment: string | null; instructions: string | null; }
export type WorkoutTemplateKind = 'Personal' | 'TrainerPlan';
export interface WorkoutTemplate { id: string; name: string; description: string | null; isPublic: boolean; ownerUserId: string | null; exercises: Exercise[]; isAssigned: boolean; kind: WorkoutTemplateKind; }
export interface WorkoutSet { id: string; setNumber: number; repetitions: number | null; weightKg: number | null; durationSeconds: number | null; volume: number; isPersonalRecord: boolean; }
export interface PreviousPerformance { completedAt: string; bestWeightKg: number; volume: number; }
export interface WorkoutExercise { exerciseLogId: string; exercise: Exercise; notes: string | null; previousPerformance: PreviousPerformance | null; sets: WorkoutSet[]; }
export interface WorkoutSession { id: string; workoutTemplateId: string | null; templateName: string | null; status: string; startedAt: string; endedAt: string | null; notes: string | null; volume: number; exercises: WorkoutExercise[]; }
export interface WorkoutHistory { id: string; name: string; status: string; startedAt: string; endedAt: string | null; volume: number; }
export interface WorkoutTemplateCommand { name: string; description: string; isPublic: boolean; exerciseIds: string[]; }
export interface WorkoutSetCommand { repetitions: number; weightKg: number; durationSeconds: number | null; }
export interface AssignedMember { id: string; displayName: string; email: string; assignedAt: string; profile: { experienceLevel: string | null; fitnessGoal: string; plannedTrainingDays: readonly string[] }; }
export interface WorkoutAssignment { id: string; templateId: string; templateName: string; memberId: string; memberName: string; assignedAt: string; endedAt: string | null; isActive: boolean; }
