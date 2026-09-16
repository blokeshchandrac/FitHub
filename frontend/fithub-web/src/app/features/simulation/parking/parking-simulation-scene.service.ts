import { Injectable, NgZone, inject } from '@angular/core';
import * as THREE from 'three';
import { OrbitControls } from 'three/addons/controls/OrbitControls.js';
import {
  PARKING_CAPTURE_HEIGHT,
  PARKING_CAPTURE_WIDTH,
  ParkingCameraLayout,
  calculateParkingCameraLayout,
  configureParkingCaptureCamera,
} from './parking-camera-coverage';
import { ParkingCar, ParkingSpaceModel, createParkingSpaces } from './parking-simulation.models';
import { createTextSprite } from '../objects/label.factory';

export interface ParkingSceneHit { readonly kind: 'car' | 'space'; readonly id: string; }
export interface ParkingSceneFrame { readonly nowMs: number; readonly deltaSeconds: number; }

@Injectable()
export class ParkingSimulationSceneService {
  private readonly zone = inject(NgZone);
  private host?: HTMLElement;
  private renderer?: THREE.WebGLRenderer;
  private scene?: THREE.Scene;
  private camera?: THREE.PerspectiveCamera;
  private captureCamera?: THREE.PerspectiveCamera;
  private coverageSurface?: THREE.Mesh;
  private coverageBoundary?: THREE.LineSegments;
  private controls?: OrbitControls;
  private resizeObserver?: ResizeObserver;
  private animationFrame = 0;
  private lastFrameMs = 0;
  private frameHandler?: (frame: ParkingSceneFrame) => void;
  private readonly raycaster = new THREE.Raycaster();
  private readonly pointer = new THREE.Vector2();
  private readonly carObjects = new Map<string, THREE.Group>();
  private readonly spaceObjects = new Map<string, THREE.Group>();
  private readonly temporaryVector = new THREE.Vector3();
  private readonly cameraTarget = new THREE.Vector3();
  private readonly renderSize = new THREE.Vector2();
  private readonly monitoredSpaces = createParkingSpaces();
  private followCarId: string | null = null;
  private scanPulseEndsAt = 0;

  initialize(host: HTMLElement): boolean {
    this.host = host;
    try {
      this.renderer = new THREE.WebGLRenderer({ antialias: true, alpha: false, preserveDrawingBuffer: true });
    } catch {
      return false;
    }
    this.renderer.setPixelRatio(Math.min(window.devicePixelRatio, 1.75));
    this.renderer.shadowMap.enabled = true;
    this.renderer.shadowMap.type = THREE.PCFShadowMap;
    this.renderer.outputColorSpace = THREE.SRGBColorSpace;
    host.appendChild(this.renderer.domElement);
    this.renderer.domElement.tabIndex = 0;
    this.renderer.domElement.setAttribute('aria-label', 'Interactive three-dimensional FitHub parking area');

    this.scene = new THREE.Scene();
    this.scene.background = new THREE.Color(0x0a0e0c);
    this.scene.fog = new THREE.Fog(0x0a0e0c, 35, 72);
    this.camera = new THREE.PerspectiveCamera(46, 1, 0.1, 140);
    this.camera.position.set(25, 27, 31);
    this.captureCamera = new THREE.PerspectiveCamera();
    this.controls = new OrbitControls(this.camera, this.renderer.domElement);
    this.controls.enableDamping = true;
    this.controls.target.set(0, 0, 2);
    this.controls.minDistance = 12;
    this.controls.maxDistance = 62;
    this.controls.maxPolarAngle = Math.PI / 2.05;

    this.buildEnvironment();
    this.resizeObserver = new ResizeObserver(() => this.resize());
    this.resizeObserver.observe(host);
    this.resize();
    this.zone.runOutsideAngular(() => {
      this.lastFrameMs = performance.now();
      this.animationFrame = requestAnimationFrame(this.animate);
    });
    return true;
  }

  setFrameHandler(handler: (frame: ParkingSceneFrame) => void): void { this.frameHandler = handler; }

  sync(spaces: readonly ParkingSpaceModel[], cars: readonly ParkingCar[], selectedCarId: string | null, selectedSpaceId: string | null): void {
    if (!this.scene) return;
    for (const space of spaces) {
      let object = this.spaceObjects.get(space.id);
      if (!object) {
        object = this.createSpace(space);
        this.spaceObjects.set(space.id, object);
        this.scene.add(object);
      }
      this.updateSpace(object, space, selectedSpaceId === space.id);
    }
    const carIds = new Set(cars.map(car => car.id));
    for (const [id, object] of this.carObjects) {
      if (!carIds.has(id)) {
        this.scene.remove(object);
        disposeObject(object);
        this.carObjects.delete(id);
      }
    }
    for (const car of cars) {
      let object = this.carObjects.get(car.id);
      if (!object) {
        object = this.createCar(car);
        this.carObjects.set(car.id, object);
        this.scene.add(object);
      }
      if (!object.userData['driving']) {
        object.position.set(car.position.x, 0, car.position.z);
        object.rotation.y = car.heading;
        object.userData['speed'] = car.speed;
      }
      const ring = object.getObjectByName('selection-ring');
      if (ring) ring.visible = selectedCarId === car.id;
    }
  }

  hitTest(clientX: number, clientY: number): ParkingSceneHit | null {
    if (!this.renderer || !this.camera) return null;
    const bounds = this.renderer.domElement.getBoundingClientRect();
    this.pointer.set(((clientX - bounds.left) / bounds.width) * 2 - 1, -((clientY - bounds.top) / bounds.height) * 2 + 1);
    this.raycaster.setFromCamera(this.pointer, this.camera);
    for (const hit of this.raycaster.intersectObjects([...this.carObjects.values(), ...this.spaceObjects.values()], true)) {
      let object: THREE.Object3D | null = hit.object;
      while (object) {
        const kind = object.userData['parkingInteraction'] as ParkingSceneHit['kind'] | undefined;
        const id = object.userData['parkingId'] as string | undefined;
        if (kind && id) return { kind, id };
        object = object.parent;
      }
    }
    return null;
  }

  driveCar(carId: string, pressedKeys: ReadonlySet<string>, deltaSeconds: number): ParkingCar | null {
    const object = this.carObjects.get(carId);
    if (!object) return null;
    let speed = Number(object.userData['speed'] ?? 0);
    const forward = pressedKeys.has('KeyW') || pressedKeys.has('ArrowUp');
    const reverse = pressedKeys.has('KeyS') || pressedKeys.has('ArrowDown');
    const left = pressedKeys.has('KeyA') || pressedKeys.has('ArrowLeft');
    const right = pressedKeys.has('KeyD') || pressedKeys.has('ArrowRight');
    const braking = pressedKeys.has('Space');
    const maximum = pressedKeys.has('ShiftLeft') || pressedKeys.has('ShiftRight') ? 10 : 7;
    if (forward) speed = Math.min(maximum, speed + 7 * deltaSeconds);
    else if (reverse) speed = Math.max(-4, speed - 6 * deltaSeconds);
    else speed *= Math.exp(-2.5 * deltaSeconds);
    if (braking) speed *= Math.exp(-12 * deltaSeconds);
    if (Math.abs(speed) < 0.015) speed = 0;

    const steering = (left ? 1 : 0) - (right ? 1 : 0);
    if (steering && Math.abs(speed) > 0.1) object.rotation.y += steering * Math.sign(speed) * 1.35 * deltaSeconds;
    const nextX = object.position.x + Math.sin(object.rotation.y) * speed * deltaSeconds;
    const nextZ = object.position.z + Math.cos(object.rotation.y) * speed * deltaSeconds;
    if (this.validCarPosition(carId, nextX, nextZ)) object.position.set(nextX, 0, nextZ);
    else speed = 0;
    object.userData['speed'] = speed;
    object.userData['driving'] = Math.abs(speed) > 0;
    return { id: carId, position: { x: object.position.x, z: object.position.z }, heading: object.rotation.y, speed, color: Number(object.userData['color']) };
  }

  resetCar(car: ParkingCar): void {
    const object = this.carObjects.get(car.id);
    if (!object) return;
    object.position.set(car.position.x, 0, car.position.z);
    object.rotation.y = car.heading;
    object.userData['speed'] = 0;
    object.userData['driving'] = false;
  }

  getCarSnapshots(cars: readonly ParkingCar[]): readonly ParkingCar[] {
    return cars.map(car => {
      const object = this.carObjects.get(car.id);
      return object ? { ...car, position: { x: object.position.x, z: object.position.z }, heading: object.rotation.y, speed: Number(object.userData['speed'] ?? 0) } : car;
    });
  }

  setFollowCar(carId: string | null): void { this.followCarId = carId; }
  focusCanvas(): void { this.renderer?.domElement.focus({ preventScroll: true }); }
  pulseCamera(): void { this.scanPulseEndsAt = performance.now() + 700; }

  captureCameraPreview(): string | null {
    if (!this.renderer || !this.scene || !this.captureCamera || !this.host || !this.camera) return null;
    this.renderer.getSize(this.renderSize);
    this.renderer.setSize(PARKING_CAPTURE_WIDTH, PARKING_CAPTURE_HEIGHT, false);
    this.renderer.render(this.scene, this.captureCamera);
    let result: string | null = null;
    try { result = this.renderer.domElement.toDataURL('image/jpeg', 0.72); }
    catch { result = null; }
    finally {
      this.renderer.setSize(this.renderSize.x, this.renderSize.y, false);
      this.camera.aspect = this.renderSize.x / Math.max(this.renderSize.y, 1);
      this.camera.updateProjectionMatrix();
      this.renderer.render(this.scene, this.camera);
    }
    return result;
  }

  dispose(): void {
    cancelAnimationFrame(this.animationFrame);
    this.resizeObserver?.disconnect();
    this.controls?.dispose();
    if (this.scene) disposeObject(this.scene);
    this.renderer?.dispose();
    this.renderer?.domElement.remove();
    this.carObjects.clear();
    this.spaceObjects.clear();
    this.frameHandler = undefined;
  }

  private readonly animate = (nowMs: number): void => {
    if (!this.renderer || !this.scene || !this.camera) return;
    const deltaSeconds = Math.min((nowMs - this.lastFrameMs) / 1000, 0.05);
    this.lastFrameMs = nowMs;
    this.controls?.update();
    if (this.followCarId && this.controls) {
      const car = this.carObjects.get(this.followCarId);
      if (car) {
        this.cameraTarget.set(car.position.x, 0.7, car.position.z);
        this.controls.target.lerp(this.cameraTarget, 1 - Math.exp(-4 * deltaSeconds));
      }
    }
    const lens = this.scene.getObjectByName('parking-camera-lens') as THREE.Mesh | undefined;
    if (lens) {
      const material = lens.material as THREE.MeshStandardMaterial;
      material.emissiveIntensity = nowMs < this.scanPulseEndsAt ? 2.5 + Math.sin(nowMs * 0.04) : 0.8;
    }
    this.renderer.render(this.scene, this.camera);
    this.frameHandler?.({ nowMs, deltaSeconds });
    this.animationFrame = requestAnimationFrame(this.animate);
  };

  private resize(): void {
    if (!this.host || !this.renderer || !this.camera) return;
    const width = Math.max(this.host.clientWidth, 1);
    const height = Math.max(this.host.clientHeight, 1);
    this.renderer.setSize(width, height, false);
    this.camera.aspect = width / height;
    this.camera.updateProjectionMatrix();
    this.updateCaptureCameraAndCoverage();
  }

  private buildEnvironment(): void {
    if (!this.scene) return;
    this.scene.add(new THREE.HemisphereLight(0xcce5d2, 0x11130f, 2.2));
    const sun = new THREE.DirectionalLight(0xffffff, 2.1);
    sun.position.set(12, 24, 14);
    sun.castShadow = true;
    this.scene.add(sun);
    const floor = new THREE.Mesh(new THREE.BoxGeometry(28, 0.3, 42), new THREE.MeshStandardMaterial({ color: 0x171c19, roughness: 0.94 }));
    floor.position.set(0, -0.18, 4);
    floor.receiveShadow = true;
    this.scene.add(floor);
    const road = new THREE.Mesh(new THREE.PlaneGeometry(12, 10), new THREE.MeshStandardMaterial({ color: 0x252b28, roughness: 1 }));
    road.rotation.x = -Math.PI / 2;
    road.position.set(-6, 0.01, 19);
    this.scene.add(road);
    for (const z of [13.4, 4.5, -4.5, -13.4]) {
      const lane = new THREE.Mesh(new THREE.BoxGeometry(25, 0.08, 0.18), new THREE.MeshBasicMaterial({ color: 0xd3d9d4 }));
      lane.position.set(0, 0.02, z);
      this.scene.add(lane);
    }
    for (const x of [-13.5, 13.5]) {
      const kerb = new THREE.Mesh(new THREE.BoxGeometry(0.35, 0.45, 42), new THREE.MeshStandardMaterial({ color: 0x56605a }));
      kerb.position.set(x, 0.08, 4);
      this.scene.add(kerb);
    }
    this.addSign(-11, 16, 'ENTRANCE');
    this.addSign(8.5, 16, 'EXIT');
    this.addCameraInstallation();
  }

  private addSign(x: number, z: number, text: string): void {
    if (!this.scene) return;
    const post = new THREE.Mesh(new THREE.CylinderGeometry(0.08, 0.08, 2.2, 10), new THREE.MeshStandardMaterial({ color: 0x8b9690 }));
    post.position.set(x, 1.1, z);
    const label = createTextSprite(text, 2, '#c9ff4a');
    label.position.set(x, 2.5, z);
    this.scene.add(post, label);
  }

  private addCameraInstallation(): void {
    if (!this.scene || !this.captureCamera) return;
    const layout = calculateParkingCameraLayout(this.monitoredSpaces);
    const poleHeight = layout.lensPosition.y - 0.7;
    const pole = new THREE.Mesh(new THREE.CylinderGeometry(0.13, 0.2, poleHeight, 12), new THREE.MeshStandardMaterial({ color: 0x758078, metalness: 0.6 }));
    pole.position.set(layout.polePosition.x, poleHeight / 2, layout.polePosition.z);
    const cameraBody = new THREE.Mesh(new THREE.BoxGeometry(1.2, 0.65, 1.5), new THREE.MeshStandardMaterial({ color: 0x303833, metalness: 0.55 }));
    cameraBody.position.copy(layout.bodyPosition);
    cameraBody.lookAt(layout.areaCenter);
    const lens = new THREE.Mesh(new THREE.CylinderGeometry(0.22, 0.22, 0.25, 16), new THREE.MeshStandardMaterial({ color: 0x071014, emissive: 0x35d7f4, emissiveIntensity: 0.8 }));
    lens.name = 'parking-camera-lens';
    lens.position.copy(layout.lensPosition);
    lens.quaternion.setFromUnitVectors(new THREE.Vector3(0, 1, 0), layout.areaCenter.clone().sub(layout.lensPosition).normalize());
    const mountTop = new THREE.Vector3(layout.polePosition.x, poleHeight, layout.polePosition.z);
    const mount = createCylinderBetween(mountTop, layout.bodyPosition, 0.1, 0x758078);
    this.coverageSurface = new THREE.Mesh(createCoverageSurfaceGeometry(layout), new THREE.MeshBasicMaterial({
      color: 0x35d7f4,
      transparent: true,
      opacity: 0.035,
      depthWrite: false,
      side: THREE.DoubleSide,
    }));
    this.coverageSurface.name = 'parking-camera-coverage-surface';
    this.coverageSurface.renderOrder = 1;
    this.coverageSurface.raycast = () => undefined;
    this.coverageBoundary = new THREE.LineSegments(createCoverageBoundaryGeometry(layout), new THREE.LineBasicMaterial({
      color: 0x64e6ff,
      transparent: true,
      opacity: 0.48,
      depthWrite: false,
    }));
    this.coverageBoundary.name = 'parking-camera-coverage-boundary';
    this.coverageBoundary.renderOrder = 2;
    this.coverageBoundary.raycast = () => undefined;
    configureParkingCaptureCamera(this.captureCamera, layout);
    this.scene.add(pole, mount, cameraBody, lens, this.coverageSurface, this.coverageBoundary);
  }

  private updateCaptureCameraAndCoverage(): void {
    if (!this.captureCamera || !this.coverageSurface || !this.coverageBoundary) return;
    const layout = calculateParkingCameraLayout(this.monitoredSpaces, PARKING_CAPTURE_WIDTH / PARKING_CAPTURE_HEIGHT);
    configureParkingCaptureCamera(this.captureCamera, layout);
    this.coverageSurface.geometry.dispose();
    this.coverageSurface.geometry = createCoverageSurfaceGeometry(layout);
    this.coverageBoundary.geometry.dispose();
    this.coverageBoundary.geometry = createCoverageBoundaryGeometry(layout);
  }

  private createSpace(space: ParkingSpaceModel): THREE.Group {
    const group = new THREE.Group();
    group.position.set(space.center.x, 0.03, space.center.z);
    group.rotation.y = space.heading;
    group.userData['parkingInteraction'] = 'space';
    group.userData['parkingId'] = space.id;
    const surface = new THREE.Mesh(new THREE.PlaneGeometry(space.width, space.length), new THREE.MeshBasicMaterial({ color: 0x163020, transparent: true, opacity: 0.34, side: THREE.DoubleSide }));
    surface.rotation.x = -Math.PI / 2;
    surface.name = 'space-surface';
    const border = new THREE.LineLoop(new THREE.BufferGeometry().setFromPoints([
      new THREE.Vector3(-space.width / 2, 0.02, -space.length / 2), new THREE.Vector3(space.width / 2, 0.02, -space.length / 2),
      new THREE.Vector3(space.width / 2, 0.02, space.length / 2), new THREE.Vector3(-space.width / 2, 0.02, space.length / 2),
    ]), new THREE.LineDashedMaterial({ color: 0x4ade80, dashSize: 10, gapSize: 0 }));
    border.computeLineDistances();
    border.name = 'space-border';
    const label = createTextSprite(space.id, 1.8, '#d8e0da');
    label.position.set(0, 0.08, -space.length * 0.34);
    label.material.depthTest = true;
    label.rotation.x = -Math.PI / 2;
    group.add(surface, border, label);
    return group;
  }

  private updateSpace(object: THREE.Group, space: ParkingSpaceModel, selected: boolean): void {
    const colors = { AVAILABLE: 0x4ade80, OCCUPIED: 0xfb7185, MISUSED_OR_BLOCKED: 0xfb923c, UNKNOWN: 0x9ca3af } as const;
    const color = colors[space.state];
    const surface = object.getObjectByName('space-surface') as THREE.Mesh;
    const border = object.getObjectByName('space-border') as THREE.LineLoop;
    (surface.material as THREE.MeshBasicMaterial).color.setHex(color);
    (surface.material as THREE.MeshBasicMaterial).opacity = selected ? 0.52 : 0.25;
    const material = border.material as THREE.LineDashedMaterial;
    material.color.setHex(selected ? 0xc9ff4a : color);
    material.dashSize = space.state === 'MISUSED_OR_BLOCKED' || space.state === 'UNKNOWN' ? 0.55 : 10;
    material.gapSize = space.state === 'MISUSED_OR_BLOCKED' || space.state === 'UNKNOWN' ? 0.28 : 0;
    material.needsUpdate = true;
  }

  private createCar(car: ParkingCar): THREE.Group {
    const group = new THREE.Group();
    group.position.set(car.position.x, 0, car.position.z);
    group.rotation.y = car.heading;
    group.userData['parkingInteraction'] = 'car';
    group.userData['parkingId'] = car.id;
    group.userData['speed'] = 0;
    group.userData['color'] = car.color;
    const material = new THREE.MeshStandardMaterial({ color: car.color, roughness: 0.4, metalness: 0.4 });
    const body = new THREE.Mesh(new THREE.BoxGeometry(2, 0.65, 4.2), material);
    body.position.y = 0.65;
    body.castShadow = true;
    const cabin = new THREE.Mesh(new THREE.BoxGeometry(1.65, 0.72, 2), new THREE.MeshStandardMaterial({ color: 0x18242b, roughness: 0.25, metalness: 0.35 }));
    cabin.position.set(0, 1.25, -0.15);
    cabin.castShadow = true;
    group.add(body, cabin);
    for (const x of [-1.02, 1.02]) for (const z of [-1.35, 1.35]) {
      const wheel = new THREE.Mesh(new THREE.CylinderGeometry(0.36, 0.36, 0.26, 14), new THREE.MeshStandardMaterial({ color: 0x090a09 }));
      wheel.rotation.z = Math.PI / 2;
      wheel.position.set(x, 0.42, z);
      group.add(wheel);
    }
    const ring = new THREE.Mesh(new THREE.RingGeometry(1.65, 1.85, 40), new THREE.MeshBasicMaterial({ color: 0xc9ff4a, side: THREE.DoubleSide, transparent: true, opacity: 0.88 }));
    ring.name = 'selection-ring';
    ring.rotation.x = -Math.PI / 2;
    ring.position.y = 0.04;
    ring.visible = false;
    const label = createTextSprite(car.id, 2.1);
    label.position.set(0, 2.2, 0);
    group.add(ring, label);
    return group;
  }

  private validCarPosition(carId: string, x: number, z: number): boolean {
    if (x < -12.1 || x > 12.1 || z < -13 || z > 21.2) return false;
    if ((x - 11.5) ** 2 + (z - 15) ** 2 < 3.2) return false;
    for (const [id, other] of this.carObjects) {
      if (id !== carId && (x - other.position.x) ** 2 + (z - other.position.z) ** 2 < 7) return false;
    }
    return true;
  }
}

function createCoverageSurfaceGeometry(layout: ParkingCameraLayout): THREE.BufferGeometry {
  const [frontLeft, frontRight, backRight, backLeft] = layout.groundCorners;
  const lens = layout.lensPosition;
  const points = [
    lens, frontLeft, frontRight,
    lens, frontRight, backRight,
    lens, backRight, backLeft,
    lens, backLeft, frontLeft,
    frontLeft, backLeft, backRight,
    frontLeft, backRight, frontRight,
  ];
  return new THREE.BufferGeometry().setFromPoints(points);
}

function createCoverageBoundaryGeometry(layout: ParkingCameraLayout): THREE.BufferGeometry {
  const points: THREE.Vector3[] = [];
  for (const corner of layout.groundCorners) points.push(layout.lensPosition, corner);
  for (let index = 0; index < layout.groundCorners.length; index++) {
    points.push(layout.groundCorners[index], layout.groundCorners[(index + 1) % layout.groundCorners.length]);
  }
  return new THREE.BufferGeometry().setFromPoints(points);
}

function createCylinderBetween(start: THREE.Vector3, end: THREE.Vector3, radius: number, color: number): THREE.Mesh {
  const direction = end.clone().sub(start);
  const mesh = new THREE.Mesh(
    new THREE.CylinderGeometry(radius, radius, direction.length(), 10),
    new THREE.MeshStandardMaterial({ color, metalness: 0.6 }),
  );
  mesh.position.copy(start).add(end).multiplyScalar(0.5);
  mesh.quaternion.setFromUnitVectors(new THREE.Vector3(0, 1, 0), direction.normalize());
  return mesh;
}

function disposeObject(root: THREE.Object3D): void {
  root.traverse(object => {
    const mesh = object as THREE.Mesh;
    mesh.geometry?.dispose();
    const materials = Array.isArray(mesh.material) ? mesh.material : mesh.material ? [mesh.material] : [];
    for (const material of materials) {
      const candidate = material as THREE.Material & { map?: THREE.Texture };
      candidate.map?.dispose();
      material.dispose();
    }
  });
}
