import * as THREE from 'three';
import {
  areAllParkingSpaceCornersVisible,
  calculateParkingCameraLayout,
  configureParkingCaptureCamera,
} from './parking-camera-coverage';
import { createParkingSpaces } from './parking-simulation.models';

describe('parking camera coverage', () => {
  it('raises the camera and frames every corner of all twelve parking spaces', () => {
    const spaces = createParkingSpaces();
    const layout = calculateParkingCameraLayout(spaces);
    const camera = new THREE.PerspectiveCamera();
    configureParkingCaptureCamera(camera, layout);

    expect(layout.lensPosition.y).toBeGreaterThan(17);
    expect(layout.groundCorners).toHaveLength(4);
    expect(areAllParkingSpaceCornersVisible(camera, spaces)).toBe(true);
  });

  it('places the narrow origin at the lens and the wide boundary beyond every parking space', () => {
    const spaces = createParkingSpaces();
    const layout = calculateParkingCameraLayout(spaces);

    expect(layout.lensPosition.x).toBeGreaterThan(layout.areaBounds.maxX);
    expect(layout.lensPosition.z).toBeGreaterThan(layout.areaBounds.maxZ);
    expect(Math.min(...layout.groundCorners.map(corner => corner.x))).toBeLessThan(layout.areaBounds.minX);
    expect(Math.max(...layout.groundCorners.map(corner => corner.x))).toBeGreaterThan(layout.areaBounds.maxX);
    expect(Math.min(...layout.groundCorners.map(corner => corner.z))).toBeLessThan(layout.areaBounds.minZ);
    expect(Math.max(...layout.groundCorners.map(corner => corner.z))).toBeGreaterThan(layout.areaBounds.maxZ);
  });
});
