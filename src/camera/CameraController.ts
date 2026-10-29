import * as THREE from 'three';
import { OrbitControls } from 'three-stdlib';

export class CameraController {
  public controls: OrbitControls;
  private camera: THREE.PerspectiveCamera;
  private domElement: HTMLElement;
  private keysPressed: { [key: string]: boolean } = {};
  private moveSpeed = 12.0;

  constructor(camera: THREE.PerspectiveCamera, domElement: HTMLElement) {
    this.camera = camera;
    this.domElement = domElement;

    this.controls = new OrbitControls(this.camera, this.domElement);
    this.controls.enableDamping = true;
    this.controls.dampingFactor = 0.05;
    this.controls.maxPolarAngle = Math.PI / 2 - 0.02; // Prevent going underground
    this.controls.minDistance = 3.0;
    this.controls.maxDistance = 140.0;
    this.controls.target.set(0, 2.2, -4.0);

    window.addEventListener('keydown', this.onKeyDown);
    window.addEventListener('keyup', this.onKeyUp);
  }

  private onKeyDown = (e: KeyboardEvent) => {
    this.keysPressed[e.code] = true;
  };

  private onKeyUp = (e: KeyboardEvent) => {
    this.keysPressed[e.code] = false;
  };

  public update(delta: number): void {
    // 1. Update OrbitControls damping
    this.controls.update();

    // 2. WASD Keyboard Fly/Pan
    const moveVector = new THREE.Vector3();
    const forward = new THREE.Vector3();
    this.camera.getWorldDirection(forward);
    forward.y = 0;
    forward.normalize();

    const right = new THREE.Vector3();
    right.crossVectors(forward, new THREE.Vector3(0, 1, 0)).normalize();

    if (this.keysPressed['KeyW'] || this.keysPressed['ArrowUp']) moveVector.add(forward);
    if (this.keysPressed['KeyS'] || this.keysPressed['ArrowDown']) moveVector.sub(forward);
    if (this.keysPressed['KeyD'] || this.keysPressed['ArrowRight']) moveVector.add(right);
    if (this.keysPressed['KeyA'] || this.keysPressed['ArrowLeft']) moveVector.sub(right);
    if (this.keysPressed['KeyE']) moveVector.y += 1;
    if (this.keysPressed['KeyQ']) moveVector.y -= 1;

    if (moveVector.lengthSq() > 0) {
      moveVector.normalize().multiplyScalar(this.moveSpeed * delta);
      this.camera.position.add(moveVector);
      this.controls.target.add(moveVector);

      // Clamp camera minimum height
      if (this.camera.position.y < 0.6) this.camera.position.y = 0.6;
    }
  }

  public destroy(): void {
    window.removeEventListener('keydown', this.onKeyDown);
    window.removeEventListener('keyup', this.onKeyUp);
    this.controls.dispose();
  }
}

