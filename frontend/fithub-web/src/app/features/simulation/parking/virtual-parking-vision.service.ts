import { Injectable } from '@angular/core';
import {
  PARKING_VISION_DEFAULTS,
  ParkingCar,
  ParkingObservation,
  ParkingSpaceModel,
  ParkingVisionConfiguration,
  ParkingVisionResult,
  ParkingViolation,
  Point2,
} from './parking-simulation.models';

@Injectable()
export class VirtualParkingVisionService {
  readonly cameraId = 'SIM-PARK-CAMERA-01';

  analyse(
    spaces: readonly ParkingSpaceModel[],
    cars: readonly ParkingCar[],
    observedAt: string,
    cameraOnline: boolean,
    lowConfidence: boolean,
    configuration: ParkingVisionConfiguration = PARKING_VISION_DEFAULTS,
  ): ParkingVisionResult {
    if (!cameraOnline || lowConfidence) {
      const confidence = cameraOnline ? 0.35 : 0;
      return {
        observations: spaces.map(space => this.observation(space.id, 'UNKNOWN', confidence, observedAt)),
        violations: [],
        detectedCars: 0,
      };
    }

    const states = new Map<string, { status: ParkingObservation['status']; confidence: number }>(
      spaces.map(space => [space.id, { status: 'AVAILABLE', confidence: 0.98 }]),
    );
    const violations: ParkingViolation[] = [];
    let detectedCars = 0;

    for (const car of cars) {
      const carPolygon = rectangleCorners(car.position, configuration.carWidth, configuration.carLength, car.heading);
      const carArea = configuration.carWidth * configuration.carLength;
      const overlaps = spaces
        .map(space => ({ space, ratio: intersectionArea(carPolygon, space) / carArea }))
        .filter(item => item.ratio >= configuration.minimumAffectedRatio)
        .sort((left, right) => right.ratio - left.ratio);
      if (overlaps.length === 0) continue;
      detectedCars++;

      const best = overlaps[0];
      const adjacentRatio = overlaps[1]?.ratio ?? 0;
      const headingDifference = headingDifferenceDegrees(car.heading, best.space.heading);
      if (best.ratio >= configuration.minimumInsideRatio &&
          adjacentRatio <= configuration.maximumAdjacentOverlap &&
          headingDifference <= configuration.maximumHeadingDifferenceDegrees) {
        states.set(best.space.id, { status: 'OCCUPIED', confidence: roundConfidence(Math.min(best.ratio, 0.99)) });
        continue;
      }

      const affected = overlaps.filter(item => item.ratio >= configuration.minimumAffectedRatio);
      if (affected.length >= 2 && affected[0].ratio + affected[1].ratio >= 0.7) {
        const affectedIds = affected.slice(0, 2).map(item => item.space.id).sort();
        const confidence = roundConfidence(Math.min(0.98, affected[0].ratio + affected[1].ratio));
        for (const id of affectedIds) states.set(id, { status: 'MISUSED_OR_BLOCKED', confidence });
        violations.push({
          vehicleSimulationId: car.id,
          affectedSpaceIds: affectedIds,
          violationType: 'TWO_SPACE_OBSTRUCTION',
          detectedAt: observedAt,
          sourceDeviceId: this.cameraId,
          reason: `Parking alert: ${car.id} is obstructing spaces ${affectedIds[0]} and ${affectedIds[1]}.`,
        });
        continue;
      }

      const uncertain = affected.slice(0, 2);
      for (const item of uncertain) states.set(item.space.id, { status: 'UNKNOWN', confidence: 0.45 });
    }

    return {
      observations: spaces.map(space => {
        const state = states.get(space.id)!;
        return this.observation(space.id, state.status, state.confidence, observedAt);
      }),
      violations,
      detectedCars,
    };
  }

  private observation(spaceId: string, status: ParkingObservation['status'], confidence: number, observedAt: string): ParkingObservation {
    return { spaceId, status, confidence, observedAt, sourceDeviceId: this.cameraId };
  }
}

function rectangleCorners(center: Point2, width: number, length: number, heading: number): Point2[] {
  const halfWidth = width / 2;
  const halfLength = length / 2;
  const cosine = Math.cos(heading);
  const sine = Math.sin(heading);
  return [
    { x: -halfWidth, z: -halfLength }, { x: halfWidth, z: -halfLength },
    { x: halfWidth, z: halfLength }, { x: -halfWidth, z: halfLength },
  ].map(point => ({
    x: center.x + point.x * cosine + point.z * sine,
    z: center.z - point.x * sine + point.z * cosine,
  }));
}

function intersectionArea(carPolygon: readonly Point2[], space: ParkingSpaceModel): number {
  const cosine = Math.cos(-space.heading);
  const sine = Math.sin(-space.heading);
  let polygon = carPolygon.map(point => {
    const x = point.x - space.center.x;
    const z = point.z - space.center.z;
    return { x: x * cosine + z * sine, z: -x * sine + z * cosine };
  });
  const halfWidth = space.width / 2;
  const halfLength = space.length / 2;
  polygon = clip(polygon, point => point.x >= -halfWidth, (a, b) => intersectVertical(a, b, -halfWidth));
  polygon = clip(polygon, point => point.x <= halfWidth, (a, b) => intersectVertical(a, b, halfWidth));
  polygon = clip(polygon, point => point.z >= -halfLength, (a, b) => intersectHorizontal(a, b, -halfLength));
  polygon = clip(polygon, point => point.z <= halfLength, (a, b) => intersectHorizontal(a, b, halfLength));
  if (polygon.length < 3) return 0;
  return Math.abs(polygon.reduce((sum, point, index) => {
    const next = polygon[(index + 1) % polygon.length];
    return sum + point.x * next.z - next.x * point.z;
  }, 0)) / 2;
}

function clip(points: readonly Point2[], inside: (point: Point2) => boolean, intersection: (a: Point2, b: Point2) => Point2): Point2[] {
  if (points.length === 0) return [];
  const result: Point2[] = [];
  for (let index = 0; index < points.length; index++) {
    const current = points[index];
    const previous = points[(index + points.length - 1) % points.length];
    const currentInside = inside(current);
    const previousInside = inside(previous);
    if (currentInside !== previousInside) result.push(intersection(previous, current));
    if (currentInside) result.push(current);
  }
  return result;
}

function intersectVertical(a: Point2, b: Point2, x: number): Point2 {
  const t = (x - a.x) / (b.x - a.x || Number.EPSILON);
  return { x, z: a.z + (b.z - a.z) * t };
}

function intersectHorizontal(a: Point2, b: Point2, z: number): Point2 {
  const t = (z - a.z) / (b.z - a.z || Number.EPSILON);
  return { x: a.x + (b.x - a.x) * t, z };
}

function headingDifferenceDegrees(first: number, second: number): number {
  let difference = Math.abs(first - second) % Math.PI;
  if (difference > Math.PI / 2) difference = Math.PI - difference;
  return difference * 180 / Math.PI;
}

function roundConfidence(value: number): number { return Math.round(value * 100) / 100; }
