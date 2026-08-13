export interface EquipmentSummary { id: string; code: string; name: string; location: string | null; state: string | null; stateColor: string; liveDetailsAvailable: boolean; }
export interface EquipmentHistory { id: string; previousState: string; state: string; eventType: string; occurredAt: string; changedByUserId: string | null; note: string | null; }
export interface EquipmentDetail extends EquipmentSummary { description: string | null; ownerUserId: string | null; partnerUserId: string | null; presenceDetected: boolean | null; clearanceGraceStartedAt: string | null; version: number; history: readonly EquipmentHistory[]; }
export interface EquipmentChanged { equipmentId: string; state: string; version: number; changedAt: string; }
