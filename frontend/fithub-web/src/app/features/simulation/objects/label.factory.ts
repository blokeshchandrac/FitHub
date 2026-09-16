import * as THREE from 'three';

export function createTextSprite(text: string, width = 3.2, accent = '#c9ff4a'): THREE.Sprite {
  const sprite = new THREE.Sprite(new THREE.SpriteMaterial({ map: createLabelTexture(text, accent), transparent: true, depthTest: false }));
  sprite.scale.set(width, width * 0.28, 1);
  sprite.renderOrder = 20;
  sprite.userData['labelText'] = text;
  sprite.userData['ignoreRaycast'] = true;
  sprite.raycast = () => undefined;
  return sprite;
}

export function updateTextSprite(sprite: THREE.Sprite, text: string, accent = '#c9ff4a'): void {
  if (sprite.userData['labelText'] === text) return;
  const material = sprite.material as THREE.SpriteMaterial;
  material.map?.dispose();
  material.map = createLabelTexture(text, accent);
  material.needsUpdate = true;
  sprite.userData['labelText'] = text;
}

function createLabelTexture(text: string, accent: string): THREE.CanvasTexture {
  const canvas = document.createElement('canvas');
  canvas.width = 768;
  canvas.height = 168;
  const context = canvas.getContext('2d');
  if (!context) return new THREE.CanvasTexture(canvas);

  context.fillStyle = 'rgba(7, 11, 9, 0.88)';
  roundedRect(context, 4, 4, 760, 160, 24);
  context.fill();
  context.strokeStyle = accent;
  context.lineWidth = 4;
  context.stroke();
  context.fillStyle = '#f5f7f2';
  context.font = '700 38px system-ui, sans-serif';
  context.textAlign = 'center';
  context.textBaseline = 'middle';
  context.fillText(text, 384, 84, 700);
  const texture = new THREE.CanvasTexture(canvas);
  texture.colorSpace = THREE.SRGBColorSpace;
  texture.needsUpdate = true;
  return texture;
}

function roundedRect(context: CanvasRenderingContext2D, x: number, y: number, width: number, height: number, radius: number): void {
  context.beginPath();
  context.roundRect(x, y, width, height, radius);
}
