import * as THREE from 'three';
import { SimulationMember } from '../simulation.models';
import { createTextSprite } from './label.factory';

export function createMemberObject(member: SimulationMember): THREE.Group {
  const group = new THREE.Group();
  group.name = `member:${member.id}`;
  group.userData['interaction'] = 'member';
  group.userData['interactionId'] = member.id;
  group.userData['targetPosition'] = new THREE.Vector3(member.position.x, 0, member.position.z);

  const body = new THREE.Mesh(
    new THREE.CapsuleGeometry(0.3, 0.85, 5, 12),
    new THREE.MeshStandardMaterial({ color: member.shirtColor, roughness: 0.7 }),
  );
  body.position.y = 1.15;
  group.add(body);
  const head = new THREE.Mesh(new THREE.SphereGeometry(0.26, 16, 12), new THREE.MeshStandardMaterial({ color: 0xc88f67, roughness: 0.8 }));
  head.position.y = 2.05;
  group.add(head);
  const legs = [-0.17, 0.17].map(x => {
    const leg = new THREE.Mesh(new THREE.CapsuleGeometry(0.095, 0.58, 4, 8), new THREE.MeshStandardMaterial({ color: 0x29313d }));
    leg.position.set(x, 0.38, 0);
    return leg;
  });
  group.add(...legs);
  const selectionRing = new THREE.Mesh(
    new THREE.RingGeometry(0.44, 0.55, 32),
    new THREE.MeshBasicMaterial({ color: 0xc9ff4a, transparent: true, opacity: 0.85, side: THREE.DoubleSide }),
  );
  selectionRing.name = 'member-selection-ring';
  selectionRing.rotation.x = -Math.PI / 2;
  selectionRing.position.y = 0.025;
  selectionRing.visible = false;
  group.add(selectionRing);

  const label = createTextSprite(member.shortName, 2.2, '#ffffff');
  label.name = 'member-name-label';
  label.position.set(0, 2.7, 0);
  group.add(label);

  group.traverse(child => {
    child.userData['interaction'] = 'member';
    child.userData['interactionId'] = member.id;
  });
  return group;
}
