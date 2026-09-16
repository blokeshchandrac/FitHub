import * as THREE from 'three';
import { ParkingSpaceModel } from './parking-simulation.models';

export const PARKING_CAPTURE_WIDTH = 640;
export const PARKING_CAPTURE_HEIGHT = 360;
export const PARKING_CAPTURE_ASPECT = PARKING_CAPTURE_WIDTH / PARKING_CAPTURE_HEIGHT;

export interface ParkingAreaBounds {
  readonly minX: number;
  readonly maxX: number;
  readonly minZ: number;
  readonly maxZ: number;
}

export interface ParkingCameraLayout {
  readonly areaBounds: ParkingAreaBounds;
  readonly areaCenter: THREE.Vector3;
  readonly polePosition: THREE.Vector3;
  readonly lensPosition: THREE.Vector3;
  readonly bodyPosition: THREE.Vector3;
  readonly groundCorners: readonly THREE.Vector3[];
  readonly aspect: number;
  readonly verticalFovDegrees: number;
  readonly near: number;
  readonly far: number;
}

const COVERAGE_MARGIN_METRES = 1.1;
const FRAME_MARGIN = 0.86;

export function calculateParkingCameraLayout(
  spaces: readonly ParkingSpaceModel[],
  aspect = PARKING_CAPTURE_ASPECT,
): ParkingCameraLayout {
  if (spaces.length === 0) throw new Error('Parking camera coverage requires at least one parking space.');
  const areaBounds = calculateParkingAreaBounds(spaces);
  const minX = areaBounds.minX - COVERAGE_MARGIN_METRES;
  const maxX = areaBounds.maxX + COVERAGE_MARGIN_METRES;
  const minZ = areaBounds.minZ - COVERAGE_MARGIN_METRES;
  const maxZ = areaBounds.maxZ + COVERAGE_MARGIN_METRES;
  const areaCenter = new THREE.Vector3((minX + maxX) / 2, 0.1, (minZ + maxZ) / 2);
  const diagonal = Math.hypot(maxX - minX, maxZ - minZ);
  const cameraHeight = Math.max(17, diagonal * 0.64);
  const polePosition = new THREE.Vector3(maxX + 1.5, 0, maxZ + 1.5);
  const horizontalDirection = new THREE.Vector3(areaCenter.x - polePosition.x, 0, areaCenter.z - polePosition.z).normalize();
  const lensPosition = new THREE.Vector3(
    polePosition.x + horizontalDirection.x * 0.75,
    cameraHeight,
    polePosition.z + horizontalDirection.z * 0.75,
  );
  const forward = areaCenter.clone().sub(lensPosition).normalize();
  const bodyPosition = lensPosition.clone().addScaledVector(forward, -0.75);
  const groundCorners = [
    new THREE.Vector3(minX, 0.08, minZ),
    new THREE.Vector3(maxX, 0.08, minZ),
    new THREE.Vector3(maxX, 0.08, maxZ),
    new THREE.Vector3(minX, 0.08, maxZ),
  ];
  const framingCamera = new THREE.PerspectiveCamera(50, aspect, 0.25, 200);
  framingCamera.position.copy(lensPosition);
  framingCamera.lookAt(areaCenter);
  framingCamera.updateMatrixWorld(true);
  const verticalFovDegrees = calculateRequiredVerticalFov(framingCamera, parkingSpaceCorners(spaces), aspect);
  const farthestCorner = Math.max(...groundCorners.map(corner => lensPosition.distanceTo(corner)));

  return {
    areaBounds,
    areaCenter,
    polePosition,
    lensPosition,
    bodyPosition,
    groundCorners,
    aspect,
    verticalFovDegrees,
    near: 0.25,
    far: farthestCorner + 12,
  };
}

export function configureParkingCaptureCamera(camera: THREE.PerspectiveCamera, layout: ParkingCameraLayout): void {
  camera.aspect = layout.aspect;
  camera.fov = layout.verticalFovDegrees;
  camera.near = layout.near;
  camera.far = layout.far;
  camera.position.copy(layout.lensPosition);
  camera.lookAt(layout.areaCenter);
  camera.updateProjectionMatrix();
  camera.updateMatrixWorld(true);
}

export function areAllParkingSpaceCornersVisible(
  camera: THREE.PerspectiveCamera,
  spaces: readonly ParkingSpaceModel[],
  maximumNdc = 0.9,
): boolean {
  camera.updateMatrixWorld(true);
  return parkingSpaceCorners(spaces).every(corner => {
    const projected = corner.clone().project(camera);
    return Math.abs(projected.x) <= maximumNdc && Math.abs(projected.y) <= maximumNdc && projected.z >= -1 && projected.z <= 1;
  });
}

export function parkingSpaceCorners(spaces: readonly ParkingSpaceModel[]): readonly THREE.Vector3[] {
  return spaces.flatMap(space => {
    const halfWidth = space.width / 2;
    const halfLength = space.length / 2;
    const cosine = Math.cos(space.heading);
    const sine = Math.sin(space.heading);
    return [
      [-halfWidth, -halfLength],
      [halfWidth, -halfLength],
      [halfWidth, halfLength],
      [-halfWidth, halfLength],
    ].map(([localX, localZ]) => new THREE.Vector3(
      space.center.x + localX * cosine + localZ * sine,
      0.08,
      space.center.z - localX * sine + localZ * cosine,
    ));
  });
}

function calculateParkingAreaBounds(spaces: readonly ParkingSpaceModel[]): ParkingAreaBounds {
  const corners = parkingSpaceCorners(spaces);
  return {
    minX: Math.min(...corners.map(corner => corner.x)),
    maxX: Math.max(...corners.map(corner => corner.x)),
    minZ: Math.min(...corners.map(corner => corner.z)),
    maxZ: Math.max(...corners.map(corner => corner.z)),
  };
}

function calculateRequiredVerticalFov(
  camera: THREE.PerspectiveCamera,
  corners: readonly THREE.Vector3[],
  aspect: number,
): number {
  let requiredTangent = 0;
  for (const corner of corners) {
    const viewPosition = corner.clone().applyMatrix4(camera.matrixWorldInverse);
    const depth = Math.max(-viewPosition.z, 0.001);
    requiredTangent = Math.max(
      requiredTangent,
      Math.abs(viewPosition.y) / depth / FRAME_MARGIN,
      Math.abs(viewPosition.x) / depth / aspect / FRAME_MARGIN,
    );
  }
  return THREE.MathUtils.clamp(THREE.MathUtils.radToDeg(2 * Math.atan(requiredTangent)), 38, 78);
}
