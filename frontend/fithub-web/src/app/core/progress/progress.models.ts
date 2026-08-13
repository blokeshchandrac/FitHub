export interface BodyMeasurementCommand { weightKg: number | null; heightCm: number | null; bodyFatPercentage: number | null; chestCm: number | null; waistCm: number | null; hipCm: number | null; notes: string; }
export interface ProgressPoint { recordedAt: string; weightKg: number | null; bodyFatPercentage: number | null; waistCm: number | null; }
export interface BodyMeasurement extends BodyMeasurementCommand { id: string; recordedAt: string; }
export interface BodyProgress { latestWeightKg: number | null; latestHeightCm: number | null; bmi: number | null; graph: ProgressPoint[]; history: BodyMeasurement[]; }
