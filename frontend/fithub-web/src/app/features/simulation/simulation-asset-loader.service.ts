import { Injectable } from '@angular/core';
import { Group } from 'three';
import { GLTFLoader } from 'three/addons/loaders/GLTFLoader.js';

/** Future-safe GLB boundary. Version one uses primitives and does not download external assets. */
@Injectable()
export class SimulationAssetLoaderService {
  private readonly loader = new GLTFLoader();

  loadGlb(url: string): Promise<Group> {
    return this.loader.loadAsync(url).then(result => result.scene);
  }
}
