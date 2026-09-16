import { Injectable, NgZone, inject } from '@angular/core';
import * as THREE from 'three';
import { OrbitControls } from 'three/addons/controls/OrbitControls.js';
import { SceneHit, SimulationMachine, SimulationMember, SimulationPosition, SimulationSensorResult } from './simulation.models';
import { createEntranceScanner, createMachineObject } from './objects/machine.factory';
import { createMemberObject } from './objects/member.factory';
import { updateTextSprite } from './objects/label.factory';

export interface SimulationFrame {
  readonly nowMs: number;
  readonly deltaSeconds: number;
}

export interface SensorObservation {
  readonly result: SimulationSensorResult;
  readonly distanceMetres: number;
}

@Injectable()
export class SimulationSceneService {
  private readonly zone = inject(NgZone);
  private host?: HTMLElement;
  private scene?: THREE.Scene;
  private camera?: THREE.PerspectiveCamera;
  private renderer?: THREE.WebGLRenderer;
  private controls?: OrbitControls;
  private resizeObserver?: ResizeObserver;
  private animationFrame = 0;
  private frameHandler?: (frame: SimulationFrame) => void;
  private readonly raycaster = new THREE.Raycaster();
  private readonly pointer = new THREE.Vector2();
  private readonly groundPlane = new THREE.Plane(new THREE.Vector3(0, 1, 0), 0);
  private readonly machineObjects = new Map<string, THREE.Group>();
  private readonly memberObjects = new Map<string, THREE.Group>();
  private readonly scratchWorld = new THREE.Vector3();
  private readonly scratchProjected = new THREE.Vector3();
  private readonly scratchForward = new THREE.Vector3();
  private readonly scratchRight = new THREE.Vector3();
  private readonly scratchTarget = new THREE.Vector3();
  private focusTarget?: THREE.Vector3;
  private selectedMemberId: string | null = null;
  private elapsed = 0;
  private lastFrameAt = 0;
  private labelFrame = 0;

  initialize(host: HTMLElement): boolean {
    this.dispose();
    this.host = host;
    try {
      this.renderer = new THREE.WebGLRenderer({ antialias: true, alpha: false, powerPreference: 'high-performance' });
    } catch {
      return false;
    }
    this.renderer.setPixelRatio(Math.min(window.devicePixelRatio, 1.75));
    this.renderer.shadowMap.enabled = true;
    this.renderer.shadowMap.type = THREE.PCFSoftShadowMap;
    this.renderer.outputColorSpace = THREE.SRGBColorSpace;
    this.renderer.domElement.setAttribute('aria-label', 'Interactive three-dimensional FitHub virtual gym');
    this.renderer.domElement.setAttribute('role', 'application');
    this.renderer.domElement.tabIndex = 0;
    host.replaceChildren(this.renderer.domElement);

    this.scene = new THREE.Scene();
    this.scene.background = new THREE.Color(0x080b09);
    this.scene.fog = new THREE.Fog(0x080b09, 16, 35);
    this.camera = new THREE.PerspectiveCamera(48, 1, 0.1, 100);
    this.camera.position.set(11, 11, 15);
    this.controls = new OrbitControls(this.camera, this.renderer.domElement);
    this.controls.enableDamping = true;
    this.controls.dampingFactor = 0.08;
    this.controls.minDistance = 7;
    this.controls.maxDistance = 28;
    this.controls.maxPolarAngle = Math.PI * 0.48;
    this.controls.target.set(-0.5, 0.7, 0);

    this.addEnvironment();
    this.scene.add(createEntranceScanner());
    this.resizeObserver = new ResizeObserver(() => this.resize());
    this.resizeObserver.observe(host);
    this.resize();
    this.lastFrameAt = performance.now();
    this.zone.runOutsideAngular(() => this.animate(this.lastFrameAt));
    return true;
  }

  setFrameHandler(handler: (frame: SimulationFrame) => void): void { this.frameHandler = handler; }

  sync(
    machines: readonly SimulationMachine[],
    members: readonly SimulationMember[],
    selectedMachineId: string,
    selectedMemberId: string | null,
    showTapTargets: boolean,
  ): void {
    if (!this.scene) return;
    this.selectedMemberId = selectedMemberId;
    for (const machine of machines) {
      let object = this.machineObjects.get(machine.id);
      if (!object) {
        object = createMachineObject(machine);
        this.machineObjects.set(machine.id, object);
        this.scene.add(object);
      }
      this.updateMachine(object, machine, machine.id === selectedMachineId, showTapTargets);
    }

    for (const member of members) {
      let object = this.memberObjects.get(member.id);
      if (member.entered && !object) {
        object = createMemberObject(member);
        object.position.set(member.position.x, 0, member.position.z);
        this.memberObjects.set(member.id, object);
        this.scene.add(object);
      }
      if (member.entered && object) {
        let target = object.userData['targetPosition'] as THREE.Vector3 | undefined;
        if (!target) {
          target = new THREE.Vector3();
          object.userData['targetPosition'] = target;
        }
        target.set(member.position.x, 0, member.position.z);
        const ring = object.getObjectByName('member-selection-ring');
        if (ring) ring.visible = member.id === selectedMemberId;
      }
      if (!member.entered && object) {
        this.scene.remove(object);
        disposeObject(object);
        this.memberObjects.delete(member.id);
      }
    }
  }

  hitTest(clientX: number, clientY: number): SceneHit | null {
    const intersections = this.intersections(clientX, clientY);
    for (const intersection of intersections) {
      let object: THREE.Object3D | null = intersection.object;
      while (object) {
        if (object.userData['ignoreRaycast']) break;
        const kind = object.userData['interaction'] as SceneHit['kind'] | undefined;
        const id = object.userData['interactionId'] as string | undefined;
        if (kind && id) return { kind, id };
        object = object.parent;
      }
    }
    return null;
  }

  groundPosition(clientX: number, clientY: number): SimulationPosition | null {
    if (!this.camera || !this.renderer) return null;
    this.setPointer(clientX, clientY);
    this.raycaster.setFromCamera(this.pointer, this.camera);
    return this.raycaster.ray.intersectPlane(this.groundPlane, this.scratchWorld)
      ? { x: this.scratchWorld.x, z: this.scratchWorld.z }
      : null;
  }

  cameraRelativeMovement(forward: number, right: number): SimulationPosition {
    if (!this.camera || (!forward && !right)) return { x: 0, z: 0 };
    this.camera.getWorldDirection(this.scratchForward);
    this.scratchForward.y = 0;
    this.scratchForward.normalize();
    this.scratchRight.crossVectors(this.scratchForward, this.camera.up).normalize();
    this.scratchTarget.copy(this.scratchForward).multiplyScalar(forward).addScaledVector(this.scratchRight, right);
    if (this.scratchTarget.lengthSq() > 1) this.scratchTarget.normalize();
    return { x: this.scratchTarget.x, z: this.scratchTarget.z };
  }

  observeSensor(machine: SimulationMachine, previousResult: SimulationSensorResult): SensorObservation {
    if (machine.disconnectedWires.includes('power') || machine.disconnectedWires.includes('ultrasonic-trigger') || machine.disconnectedWires.includes('ultrasonic-echo'))
      return { result: 'SENSOR_FAULT', distanceMetres: machine.distanceMetres };
    if (!machine.memberIds.length) return { result: 'AREA_CLEAR', distanceMetres: 9.9 };

    let minimumDistance = 20;
    let detected = false;
    const tolerance = previousResult === 'PERSON_DETECTED' ? 0.16 : -0.05;
    for (const memberId of machine.memberIds) {
      const object = this.memberObjects.get(memberId);
      if (!object) continue;
      const dx = object.position.x - machine.position.x;
      const dz = object.position.z - machine.position.z;
      minimumDistance = Math.min(minimumDistance, Math.hypot(dx, dz));
      const start = 0.25 - tolerance;
      const end = 4.15 + tolerance;
      const progress = THREE.MathUtils.clamp((dz - 0.25) / 3.9, 0, 1);
      const halfWidth = 0.38 + progress * 1.42 + tolerance;
      if (dz >= start && dz <= end && Math.abs(dx) <= halfWidth) detected = true;
    }
    return { result: detected ? 'PERSON_DETECTED' : 'AREA_CLEAR', distanceMetres: minimumDistance === 20 ? 9.9 : minimumDistance };
  }

  focusMachine(machine: SimulationMachine): void {
    this.focusTarget = new THREE.Vector3(machine.position.x, 0.7, machine.position.z);
  }

  setOrbitEnabled(enabled: boolean): void { if (this.controls) this.controls.enabled = enabled; }
  focusCanvas(): void { this.renderer?.domElement.focus({ preventScroll: true }); }

  setDropHighlight(hit: SceneHit | null): void {
    for (const object of this.machineObjects.values()) {
      const reader = object.getObjectByName('machine-reader-target') as THREE.Mesh | undefined;
      if (reader) setHighlighted(reader, hit?.kind === 'machine-reader' && hit.id === object.name.replace('machine:', ''));
    }
    const entrance = this.scene?.getObjectByName('entrance-reader-target') as THREE.Mesh | undefined;
    if (entrance) setHighlighted(entrance, hit?.kind === 'entrance-scanner');
  }

  flashNfc(targetId: string): void {
    const target = targetId === 'ENTRANCE'
      ? this.scene?.getObjectByName('entrance-reader-target')
      : this.machineObjects.get(targetId)?.getObjectByName('machine-reader-target');
    if (!target) return;
    target.scale.setScalar(1.16);
    window.setTimeout(() => target.scale.setScalar(1), 280);
  }

  dispose(): void {
    cancelAnimationFrame(this.animationFrame);
    this.frameHandler = undefined;
    this.resizeObserver?.disconnect();
    this.controls?.dispose();
    if (this.scene) disposeObject(this.scene);
    this.renderer?.dispose();
    this.renderer?.domElement.remove();
    this.machineObjects.clear();
    this.memberObjects.clear();
    this.host = undefined;
    this.scene = undefined;
    this.camera = undefined;
    this.renderer = undefined;
    this.controls = undefined;
  }

  private addEnvironment(): void {
    if (!this.scene) return;
    const floor = new THREE.Mesh(new THREE.PlaneGeometry(22, 15), new THREE.MeshStandardMaterial({ color: 0x111714, roughness: 0.92 }));
    floor.rotation.x = -Math.PI / 2;
    floor.receiveShadow = true;
    this.scene.add(floor, new THREE.GridHelper(22, 22, 0x33413a, 0x202a25));
    this.scene.add(new THREE.HemisphereLight(0xe5ffe5, 0x101913, 1.7));
    const key = new THREE.DirectionalLight(0xffffff, 2.3);
    key.position.set(4, 10, 6);
    key.castShadow = true;
    key.shadow.mapSize.set(1024, 1024);
    this.scene.add(key);
    const accent = new THREE.PointLight(0xc9ff4a, 18, 16, 2);
    accent.position.set(-5, 4, -1);
    this.scene.add(accent);
    const wall = new THREE.Mesh(new THREE.PlaneGeometry(22, 6), new THREE.MeshStandardMaterial({ color: 0x0d1310, roughness: 1 }));
    wall.position.set(0, 3, -7.5);
    this.scene.add(wall);
  }

  private updateMachine(object: THREE.Group, machine: SimulationMachine, selected: boolean, showTapTargets: boolean): void {
    const outline = object.getObjectByName('selection-outline');
    if (outline) outline.visible = selected;
    const zone = object.getObjectByName('sensor-zone') as THREE.Mesh | undefined;
    if (zone) zone.visible = selected;
    const tap = object.getObjectByName('tap-label');
    if (tap) tap.visible = showTapTargets;
    const timer = object.getObjectByName('clearance-timer-label') as THREE.Sprite | undefined;
    if (timer) {
      timer.visible = machine.state === 'CLEARANCE_WARNING' && machine.clearanceRemainingSeconds !== null;
      if (timer.visible) updateTextSprite(timer, `Area clear · Releasing in ${formatCountdown(machine.clearanceRemainingSeconds!)}`, '#fb923c');
    }
    const border = object.getObjectByName('state-border') as THREE.LineLoop | undefined;
    const borderMaterial = border?.material as THREE.LineDashedMaterial | undefined;
    if (borderMaterial) {
      borderMaterial.color.setHex(stateBorderColor(machine.state));
      borderMaterial.dashSize = machine.state === 'OFFLINE_UNKNOWN' ? 0.24 : machine.state === 'TEMPORARILY_UNAVAILABLE' ? 0.12 : 100;
      borderMaterial.gapSize = machine.state === 'OFFLINE_UNKNOWN' ? 0.16 : machine.state === 'TEMPORARILY_UNAVAILABLE' ? 0.09 : 0;
      borderMaterial.needsUpdate = true;
      border?.computeLineDistances();
    }
    const platform = object.getObjectByName('machine-platform') as THREE.Mesh | undefined;
    const platformMaterial = platform?.material as THREE.MeshStandardMaterial | undefined;
    if (platformMaterial) platformMaterial.color.setHex(machine.state === 'TEMPORARILY_UNAVAILABLE' ? 0x272b29 : 0x111714);

    const green = machine.state === 'AVAILABLE';
    const red = machine.state === 'OCCUPIED';
    const yellow = machine.state === 'SHARING_ALLOWED';
    const orange = machine.state === 'CLEARANCE_WARNING';
    setLight(object, 'green', green && !machine.disconnectedWires.includes('green-led') && !machine.disconnectedWires.includes('power'));
    setLight(object, 'red', red && !machine.disconnectedWires.includes('red-led') && !machine.disconnectedWires.includes('power'));
    setLight(object, 'yellow', yellow && !machine.disconnectedWires.includes('yellow-led') && !machine.disconnectedWires.includes('power'));
    setLight(object, 'orange', orange && !machine.disconnectedWires.includes('power'));
  }

  private animate = (nowMs: number): void => {
    this.animationFrame = requestAnimationFrame(this.animate);
    if (!this.renderer || !this.scene || !this.camera) return;
    const deltaSeconds = Math.min(0.05, Math.max(0, (nowMs - this.lastFrameAt) / 1000));
    this.lastFrameAt = nowMs;
    this.elapsed += deltaSeconds;
    const smoothing = 1 - Math.exp(-11 * deltaSeconds);
    for (const object of this.memberObjects.values()) {
      const target = object.userData['targetPosition'] as THREE.Vector3 | undefined;
      if (target) object.position.lerp(target, smoothing);
    }
    for (const object of this.machineObjects.values()) {
      const warning = object.getObjectByName('status-light-orange') as THREE.Mesh | undefined;
      const warningMaterial = warning?.material as THREE.MeshStandardMaterial | undefined;
      if (warningMaterial?.emissiveIntensity) warningMaterial.emissiveIntensity = 1.5 + Math.sin(this.elapsed * 5) * 0.7;
      const border = object.getObjectByName('state-border') as THREE.LineLoop | undefined;
      const material = border?.material as THREE.LineBasicMaterial | undefined;
      if (material && material.color.getHex() === 0xfb923c) material.opacity = 0.58 + Math.sin(this.elapsed * 5) * 0.28;
      else if (material) material.opacity = 1;
    }
    if (this.focusTarget && this.controls) {
      this.controls.target.lerp(this.focusTarget, smoothing);
      if (this.controls.target.distanceTo(this.focusTarget) < 0.03) this.focusTarget = undefined;
    }
    if (++this.labelFrame % 4 === 0) this.adjustMemberLabels();
    this.controls?.update();
    this.frameHandler?.({ nowMs, deltaSeconds });
    this.renderer.render(this.scene, this.camera);
  };

  private adjustMemberLabels(): void {
    if (!this.camera || !this.renderer) return;
    const width = this.renderer.domElement.clientWidth;
    const height = this.renderer.domElement.clientHeight;
    const occupied: Array<{ x: number; y: number }> = [];
    const ordered = [...this.memberObjects.entries()].sort(([left], [right]) => left === this.selectedMemberId ? -1 : right === this.selectedMemberId ? 1 : 0);
    for (const [memberId, object] of ordered) {
      const label = object.getObjectByName('member-name-label') as THREE.Sprite | undefined;
      if (!label) continue;
      object.getWorldPosition(this.scratchProjected);
      this.scratchProjected.y += 2.7;
      const distance = this.camera.position.distanceTo(this.scratchProjected);
      this.scratchProjected.project(this.camera);
      const x = (this.scratchProjected.x * 0.5 + 0.5) * width;
      let y = (-this.scratchProjected.y * 0.5 + 0.5) * height;
      label.visible = distance < 27 && this.scratchProjected.z > -1 && this.scratchProjected.z < 1;
      if (!label.visible) continue;
      let offset = 0;
      if (memberId !== this.selectedMemberId) {
        while (occupied.some(item => Math.abs(item.x - x) < 105 && Math.abs(item.y - (y - offset)) < 34) && offset < 1.2) offset += 0.32;
      }
      label.position.y = 2.7 + offset;
      const scale = THREE.MathUtils.clamp(distance / 14, 0.82, 1.22);
      label.scale.set(2.2 * scale, 0.616 * scale, 1);
      y -= offset * (height / Math.max(1, distance));
      occupied.push({ x, y });
    }
  }

  private intersections(clientX: number, clientY: number): THREE.Intersection[] {
    if (!this.camera || !this.scene || !this.renderer) return [];
    this.setPointer(clientX, clientY);
    this.raycaster.setFromCamera(this.pointer, this.camera);
    return this.raycaster.intersectObjects(this.scene.children, true);
  }

  private setPointer(clientX: number, clientY: number): void {
    const rect = this.renderer!.domElement.getBoundingClientRect();
    this.pointer.x = ((clientX - rect.left) / rect.width) * 2 - 1;
    this.pointer.y = -((clientY - rect.top) / rect.height) * 2 + 1;
  }

  private resize(): void {
    if (!this.host || !this.renderer || !this.camera) return;
    const width = Math.max(1, this.host.clientWidth);
    const height = Math.max(1, this.host.clientHeight);
    this.renderer.setSize(width, height, false);
    this.camera.aspect = width / height;
    this.camera.updateProjectionMatrix();
  }
}

function setLight(group: THREE.Group, name: string, active: boolean): void {
  const mesh = group.getObjectByName(`status-light-${name}`) as THREE.Mesh | undefined;
  const material = mesh?.material as THREE.MeshStandardMaterial | undefined;
  if (!material) return;
  const activeColor = mesh!.userData['activeColor'] as number;
  material.color.setHex(active ? activeColor : 0x303633);
  material.emissive.setHex(active ? activeColor : 0x000000);
  material.emissiveIntensity = active ? 1.7 : 0;
}

function setHighlighted(mesh: THREE.Mesh, highlighted: boolean): void {
  const material = mesh.material as THREE.MeshStandardMaterial;
  mesh.scale.setScalar(highlighted ? 1.12 : 1);
  material.emissiveIntensity = highlighted ? 1.2 : 0.15;
}

function stateBorderColor(state: SimulationMachine['state']): number {
  if (state === 'AVAILABLE') return 0x4ade80;
  if (state === 'OCCUPIED') return 0xfb7185;
  if (state === 'SHARING_ALLOWED') return 0xfbbf24;
  if (state === 'CLEARANCE_WARNING') return 0xfb923c;
  if (state === 'TEMPORARILY_UNAVAILABLE') return 0x4b5563;
  return 0x9ca3af;
}

function formatCountdown(seconds: number): string { return `00:${seconds.toString().padStart(2, '0')}`; }

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
