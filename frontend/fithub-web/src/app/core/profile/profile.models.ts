export type FitnessGoal = 'GeneralFitness' | 'WeightManagement' | 'MuscleGain' | 'Strength' | 'Endurance';
export type ActivityLevel = 'Sedentary' | 'Light' | 'Moderate' | 'VeryActive';
export type ExperienceLevel = 'Beginner' | 'Intermediate' | 'Advanced';
export type TrainingDay = 'Monday' | 'Tuesday' | 'Wednesday' | 'Thursday' | 'Friday' | 'Saturday' | 'Sunday';

export interface GymBranch {
  readonly id: string;
  readonly code: string;
  readonly name: string;
}

export interface MemberProfile {
  readonly userId: string;
  readonly email: string;
  readonly firstName: string;
  readonly lastName: string;
  readonly preferredName: string | null;
  readonly dateOfBirth: string | null;
  readonly heightCm: number | null;
  readonly weightKg: number | null;
  readonly fitnessGoal: FitnessGoal;
  readonly activityLevel: ActivityLevel;
  readonly experienceLevel: ExperienceLevel | null;
  readonly plannedTrainingDays: readonly TrainingDay[];
  readonly preferredGymBranchId: string | null;
  readonly preferredGymBranchName: string | null;
  readonly onboardingCompleted: boolean;
}

export interface UpdateMemberProfile {
  readonly preferredName: string;
  readonly dateOfBirth: string;
  readonly heightCm: number;
  readonly weightKg: number;
  readonly fitnessGoal: FitnessGoal;
  readonly activityLevel: ActivityLevel;
  readonly experienceLevel: ExperienceLevel;
  readonly plannedTrainingDays: readonly TrainingDay[];
  readonly preferredGymBranchId: string;
}
