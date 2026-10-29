import * as THREE from 'three';
import { RocketStats } from '../rocket/RocketStats';

export class TrajectorySimulator {
  public lineMesh: THREE.Line;
  public impactMarker: THREE.Mesh;
  private lineGeometry: THREE.BufferGeometry;
  private maxPoints = 65;
  private positions: Float32Array;

  constructor(scene: THREE.Scene) {
    this.positions = new Float32Array(this.maxPoints * 3);
    this.lineGeometry = new THREE.BufferGeometry();
    this.lineGeometry.setAttribute('position', new THREE.BufferAttribute(this.positions, 3));

    const lineMaterial = new THREE.LineBasicMaterial({
      color: 0x00f0ff,
      linewidth: 3,
      transparent: true,
      opacity: 0.85,
    });
    this.lineMesh = new THREE.Line(this.lineGeometry, lineMaterial);
    scene.add(this.lineMesh);

    // Glowing impact point ring marker on the ground
    const ringGeo = new THREE.RingGeometry(0.3, 0.45, 24);
    ringGeo.rotateX(-Math.PI / 2);
    const ringMat = new THREE.MeshBasicMaterial({
      color: 0xffaa00,
      side: THREE.DoubleSide,
      transparent: true,
      opacity: 0.85,
    });
    this.impactMarker = new THREE.Mesh(ringGeo, ringMat);
    scene.add(this.impactMarker);
  }

  public updateTrajectory(
    origin: THREE.Vector3,
    direction: THREE.Vector3,
    forceMagnitude: number,
    stats: RocketStats,
    gravityY: number,
    windVector: THREE.Vector3
  ): THREE.Vector3 {
    const dt = 0.045; // Step time for trajectory forward integration
    const pos = origin.clone();
    const dir = direction.clone().normalize();

    // Initial muzzle push velocity along rail
    const initialSpeed = forceMagnitude * 0.42 * stats.thrustMultiplier;
    const vel = dir.clone().multiplyScalar(initialSpeed);

    const isVacuum = gravityY < 2.0;
    const airDensity = isVacuum ? 0.0 : 1.2;
    const gravAccel = new THREE.Vector3(0, -gravityY, 0);

    let pointCount = 0;
    let hitGround = false;
    let impactPos = pos.clone();

    for (let i = 0; i < this.maxPoints; i++) {
      this.positions[i * 3] = pos.x;
      this.positions[i * 3 + 1] = pos.y;
      this.positions[i * 3 + 2] = pos.z;
      pointCount++;

      if (pos.y <= 0.22 && i > 2) {
        hitGround = true;
        impactPos.copy(pos);
        impactPos.y = 0.22;
        break;
      }

      // Physics integration step matching Rapier
      const timeElapsed = i * dt;
      let accel = gravAccel.clone();

      // Sustained burn acceleration
      if (timeElapsed < stats.burnDuration) {
        const sustainedThrust = (forceMagnitude * 1.85 * stats.thrustMultiplier) / stats.mass;
        accel.add(dir.clone().multiplyScalar(sustainedThrust));
      }

      // Aerodynamic drag
      const speed = vel.length();
      if (speed > 0.1) {
        const dragMag = (0.5 * airDensity * (speed * speed) * stats.dragCoefficient * 0.04) / stats.mass;
        const dragAccel = vel.clone().normalize().multiplyScalar(-dragMag);
        accel.add(dragAccel);
      }

      // Wind force
      if (!isVacuum && windVector.lengthSq() > 0.01) {
        accel.add(windVector.clone().multiplyScalar(0.4 / stats.mass));
      }

      vel.add(accel.clone().multiplyScalar(dt));
      pos.add(vel.clone().multiplyScalar(dt));
    }

    // Fill remaining points with last position to maintain valid line
    for (let i = pointCount; i < this.maxPoints; i++) {
      this.positions[i * 3] = impactPos.x;
      this.positions[i * 3 + 1] = impactPos.y;
      this.positions[i * 3 + 2] = impactPos.z;
    }

    this.lineGeometry.setDrawRange(0, pointCount);
    (this.lineGeometry.attributes.position as THREE.BufferAttribute).needsUpdate = true;

    // Position impact ring marker
    this.impactMarker.position.copy(impactPos);
    this.impactMarker.visible = hitGround;

    return impactPos;
  }

  public setVisible(visible: boolean): void {
    this.lineMesh.visible = visible;
    this.impactMarker.visible = visible;
  }
}
