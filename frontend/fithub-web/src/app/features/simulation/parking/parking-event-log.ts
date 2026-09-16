import { ParkingSimulationEvent } from './parking-simulation.models';

export const PARKING_EVENT_STORAGE_KEY = 'fithub.parking.simulation.events';

export function buildParkingEventCsv(events: readonly ParkingSimulationEvent[]): string {
  const rows = events.map(event => [
    formatExportTime(event.occurredAt),
    event.action.replaceAll('_', ' '),
    event.source,
    event.detail,
    event.result,
  ]);
  return [['Time', 'Event', 'Source', 'Details', 'Result'], ...rows]
    .map(row => row.map(escapeCsv).join(','))
    .join('\r\n');
}

export function parkingEventLogFilename(now = new Date()): string {
  const date = [now.getFullYear(), now.getMonth() + 1, now.getDate()].map(value => value.toString().padStart(2, '0')).join('-');
  const time = [now.getHours(), now.getMinutes(), now.getSeconds()].map(value => value.toString().padStart(2, '0')).join('-');
  return `FitHub_Parking_Simulation_Log_${date}_${time}.csv`;
}

export function restoreParkingEvents(storage: Storage | null): readonly ParkingSimulationEvent[] {
  if (!storage) return [];
  try {
    const parsed: unknown = JSON.parse(storage.getItem(PARKING_EVENT_STORAGE_KEY) ?? '[]');
    return Array.isArray(parsed) ? parsed.filter(isParkingSimulationEvent).slice(0, 100) : [];
  } catch {
    return [];
  }
}

export function persistParkingEvents(storage: Storage | null, events: readonly ParkingSimulationEvent[]): void {
  if (!storage) return;
  try { storage.setItem(PARKING_EVENT_STORAGE_KEY, JSON.stringify(events.slice(0, 100))); }
  catch { /* A full or unavailable browser store must not stop the simulation. */ }
}

function formatExportTime(value: string): string {
  const date = new Date(value);
  if (Number.isNaN(date.getTime())) return value;
  return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())} ${pad(date.getHours())}:${pad(date.getMinutes())}:${pad(date.getSeconds())}`;
}

function pad(value: number): string { return value.toString().padStart(2, '0'); }

function escapeCsv(value: string): string {
  return /[",\r\n]/.test(value) ? `"${value.replaceAll('"', '""')}"` : value;
}

function isParkingSimulationEvent(value: unknown): value is ParkingSimulationEvent {
  if (!value || typeof value !== 'object') return false;
  const candidate = value as Record<string, unknown>;
  return typeof candidate['id'] === 'number' && typeof candidate['occurredAt'] === 'string' &&
    typeof candidate['action'] === 'string' && typeof candidate['source'] === 'string' &&
    typeof candidate['result'] === 'string' && typeof candidate['detail'] === 'string' &&
    ['info', 'success', 'warning', 'error'].includes(String(candidate['tone'])) &&
    ['local', 'connected'].includes(String(candidate['mode']));
}
