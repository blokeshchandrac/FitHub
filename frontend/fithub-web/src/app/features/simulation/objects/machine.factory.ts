import * as THREE from 'three';
import { SimulationMachine, SimulationMachineType } from '../simulation.models';
import { createTextSprite } from './label.factory';

const FRAME = 0x69736d;
const DARK = 0x111714;
const SCREEN = 0x071b13;

export function createMachineObject(machine: SimulationMachine): THREE.Group {
  const group = new THREE.Group();
  group.name = `machine:${machine.id}`;
  group.position.set(machine.position.x, 0, machine.position.z);
  group.userData['interaction'] = 'machine';
  group.userData['interactionId'] = machine.id;

  const platform = mesh(new THREE.BoxGeometry(3.2, 0.18, 2.4), DARK);
  platform.position.y = 0.1;
  platform.name = 'machine-platform';
  group.add(platform);
  addMachineGeometry(group, machine.type);
  addReader(group, machine.id);
  addLights(group);
  addSensorZone(group);

  const label = createTextSprite(`${machine.name} · ${machine.id}`, 3.6);
  label.name = 'machine-name-label';
  label.position.set(0, 3.7, 0);
  group.add(label);

  const timer = createTextSprite('Area clear · Releasing in 00:30', 3.8, '#fb923c');
  timer.name = 'clearance-timer-label';
  timer.position.set(0, 3.1, 0);
  timer.visible = false;
  group.add(timer);

  const stateBorder = createFloorBorder(0x4ade80, 1);
  stateBorder.name = 'state-border';
  group.add(stateBorder);

  const outline = createFloorBorder(0xc9ff4a, 1.08);
  outline.name = 'selection-outline';
  outline.visible = false;
  group.add(outline);

  group.traverse(child => {
    if (child === group || child.userData['interaction']) return;
    child.userData['interaction'] = 'machine';
    child.userData['interactionId'] = machine.id;
  });
  return group;
}

export function createEntranceScanner(): THREE.Group {
  const group = new THREE.Group();
  group.name = 'entrance-scanner';
  group.position.set(-8, 0, 0);
  const post = mesh(new THREE.BoxGeometry(0.8, 2.2, 0.8), FRAME);
  post.position.y = 1.1;
  group.add(post);
  const reader = mesh(new THREE.BoxGeometry(1.3, 0.18, 1.1), 0x183d2a, 0xc9ff4a);
  reader.position.y = 2.25;
  reader.rotation.x = -0.18;
  reader.name = 'entrance-reader-target';
  reader.userData['interaction'] = 'entrance-scanner';
  reader.userData['interactionId'] = 'ENTRANCE';
  group.add(reader);
  const label = createTextSprite('GYM ENTRY · TAP CARD', 2.6);
  label.position.set(0, 3, 0);
  group.add(label);
  return group;
}

function addMachineGeometry(group: THREE.Group, type: SimulationMachineType): void {
  if (type === 'treadmill') {
    const belt = mesh(new THREE.BoxGeometry(2.5, 0.2, 1), 0x222927);
    belt.position.set(0, 0.35, 0.2);
    group.add(belt);
    addPost(group, -0.9, 0.9, -0.5, 1.8);
    addPost(group, 0.9, 0.9, -0.5, 1.8);
    const console = mesh(new THREE.BoxGeometry(2.1, 0.75, 0.3), SCREEN, 0x35d7f4);
    console.position.set(0, 1.75, -0.5);
    group.add(console);
    return;
  }
  if (type === 'bench') {
    const bench = mesh(new THREE.BoxGeometry(0.8, 0.25, 2), 0x26302b);
    bench.position.set(0, 0.75, 0.25);
    group.add(bench);
    addPost(group, -1.15, 1.3, -0.55, 2.6);
    addPost(group, 1.15, 1.3, -0.55, 2.6);
    const bar = mesh(new THREE.CylinderGeometry(0.08, 0.08, 3, 12), 0xaab3ae);
    bar.rotation.z = Math.PI / 2;
    bar.position.set(0, 2.25, -0.55);
    group.add(bar);
    return;
  }
  if (type === 'bike') {
    const wheel = mesh(new THREE.TorusGeometry(0.68, 0.08, 10, 28), 0x3d4943);
    wheel.rotation.y = Math.PI / 2;
    wheel.position.set(0, 0.85, 0.15);
    group.add(wheel);
    addPost(group, 0, 1.2, 0.15, 1.9);
    const seat = mesh(new THREE.BoxGeometry(0.65, 0.18, 0.45), 0x222927);
    seat.position.set(0, 1.75, 0.15);
    group.add(seat);
    const handle = mesh(new THREE.BoxGeometry(1.2, 0.1, 0.1), 0xaab3ae);
    handle.position.set(0, 2, -0.7);
    group.add(handle);
    return;
  }

  addPost(group, -1.1, 1.5, 0, 3);
  addPost(group, 1.1, 1.5, 0, 3);
  const top = mesh(new THREE.BoxGeometry(2.5, 0.2, 0.35), FRAME);
  top.position.y = 2.9;
  group.add(top);
  const cableMaterial = new THREE.LineBasicMaterial({ color: 0xd6ded8 });
  const points = [new THREE.Vector3(-0.8, 2.7, 0), new THREE.Vector3(0, 1.1, 0.8), new THREE.Vector3(0.8, 2.7, 0)];
  group.add(new THREE.Line(new THREE.BufferGeometry().setFromPoints(points), cableMaterial));
}

function addPost(group: THREE.Group, x: number, y: number, z: number, height: number): void {
  const post = mesh(new THREE.BoxGeometry(0.16, height, 0.16), FRAME);
  post.position.set(x, y, z);
  group.add(post);
}

function addReader(group: THREE.Group, machineId: string): void {
  const reader = mesh(new THREE.BoxGeometry(1.2, 0.16, 0.75), 0x183d2a, 0xc9ff4a);
  reader.position.set(1.05, 1, 0.78);
  reader.name = 'machine-reader-target';
  reader.userData['interaction'] = 'machine-reader';
  reader.userData['interactionId'] = machineId;
  group.add(reader);
  const tap = createTextSprite('TAP HERE', 1.5);
  tap.name = 'tap-label';
  tap.position.set(1.05, 1.45, 0.78);
  tap.userData['interaction'] = 'machine-reader';
  tap.userData['interactionId'] = machineId;
  tap.visible = false;
  group.add(tap);
}

function addLights(group: THREE.Group): void {
  const colors = [['green', 0x22c55e], ['red', 0xef4444], ['yellow', 0xfacc15], ['orange', 0xf97316]] as const;
  colors.forEach(([name, color], index) => {
    const light = mesh(new THREE.SphereGeometry(0.13, 16, 12), 0x252b28);
    light.position.set(-0.525 + index * 0.35, 2.72, 0.82);
    light.name = `status-light-${name}`;
    light.userData['activeColor'] = color;
    group.add(light);
  });
}

function addSensorZone(group: THREE.Group): void {
  const material = new THREE.MeshBasicMaterial({ color: 0x35d7f4, transparent: true, opacity: 0.1, side: THREE.DoubleSide, depthWrite: false });
  const cone = new THREE.Mesh(new THREE.ConeGeometry(1.8, 3.8, 32, 1, true), material);
  cone.name = 'sensor-zone';
  cone.rotation.x = Math.PI / 2;
  cone.position.set(0, 0.25, 2.25);
  group.add(cone);
}

function mesh(geometry: THREE.BufferGeometry, color: number, emissive = 0x000000): THREE.Mesh {
  return new THREE.Mesh(geometry, new THREE.MeshStandardMaterial({ color, emissive, emissiveIntensity: emissive ? 0.15 : 0, roughness: 0.68, metalness: 0.22 }));
}

function createFloorBorder(color: number, scale: number): THREE.LineLoop {
  const points = [
    new THREE.Vector3(-1.7 * scale, 0.22, -1.3 * scale),
    new THREE.Vector3(1.7 * scale, 0.22, -1.3 * scale),
    new THREE.Vector3(1.7 * scale, 0.22, 1.3 * scale),
    new THREE.Vector3(-1.7 * scale, 0.22, 1.3 * scale),
  ];
  const line = new THREE.LineLoop(new THREE.BufferGeometry().setFromPoints(points), new THREE.LineDashedMaterial({ color, dashSize: 100, gapSize: 0, transparent: true, opacity: 1 }));
  line.computeLineDistances();
  return line;
}
