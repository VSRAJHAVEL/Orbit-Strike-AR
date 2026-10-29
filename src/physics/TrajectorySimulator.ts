import * as THREE from 'three';
import { RocketStats } from '../rocket/RocketStats';

export class TrajectorySimulator {
  public lineMesh: THREE.Line;
  public impactMarker: THREE.Group;
  private lineGeometry: THREE.BufferGeometry;
  private maxPoints = 80;
  private positions: Float32Array;
  private beadingPoints: THREE.Points;
  private beadingGeometry: THREE.BufferGeometry;

  constructor(scene: THREE.Scene) {
    this.positions = new Float32Array(this.maxPoints * 3);
    this.lineGeometry = new THREE.BufferGeometry();
    this.lineGeometry.setAttribute('position', new THREE.BufferAttribute(this.positions, 3));

    // High-visibility glowing cyan trajectory line
    const lineMaterial = new THREE.LineBasicMaterial({
      color: 0x00f0ff,
      transparent: true,
      opacity: 0.9,
    });
    this.lineMesh = new THREE.Line(this.lineGeometry, lineMaterial);
    scene.add(this.lineMesh);

    // Dotted trajectory beads along the arc
    this.beadingGeometry = new THREE.BufferGeometry();
    this.beadingGeometry.setAttribute('position', new THREE.BufferAttribute(this.positions, 3));
    const beadingMaterial = new THREE.PointsMaterial({
      color: 0x38bdf8,
      size: 0.35,
      transparent: true,
      opacity: 0.85,
    });
    this.beadingPoints = new THREE.Points(this.beadingGeometry, beadingMaterial);
    scene.add(this.beadingPoints);

    // Glowing impact point ring marker on the ground
    this.impactMarker = new THREE.Group();
    scene.add(this.impactMarker);

    // Outer ring
    const ringGeo = new THREE.RingGeometry(1.2, 1.5, 32);
    ringGeo.rotateX(-Math.PI / 2);
    const ringMat = new THREE.MeshBasicMaterial({
      color: 0xf59e0b,
      side: THREE.DoubleSide,
      transparent: true,
      opacity: 0.9,
    });
    const ringMesh = new THREE.Mesh(ringGeo, ringMat);
    this.impactMarker.add(ringMesh);

    // Inner reticle crosshair
    const cross1 = new THREE.Mesh(
      new THREE.PlaneGeometry(0.12, 3.2),
      new THREE.MeshBasicMaterial({ color: 0x00f0ff, side: THREE.DoubleSide })
    );
    cross1.rotation.x = -Math.PI / 2;
    this.impactMarker.add(cross1);

    const cross2 = new THREE.Mesh(
      new THREE.PlaneGeometry(3.2, 0.12),
      new THREE.MeshBasicMaterial({ color: 0x00f0ff, side: THREE.DoubleSide })
    );
    cross2.rotation.x = -Math.PI / 2;
    this.impactMarker.add(cross2);
  }

  public updateTrajectory(
    origin: THREE.Vector3,
    direction: THREE.Vector3,
    forceMagnitude: number,
    stats: RocketStats,
    gravityY: number,
    windVector: THREE.Vector3
  ): THREE.Vector3 {
    const dt = 0.045;
    const pos = origin.clone();
    const dir = direction.clone().normalize();

    // Initial muzzle push velocity along rail
    const initialSpeed = forceMagnitude * 0.45 * stats.thrustMultiplier;
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

      if (pos.y <= 0.25 && i > 3) {
        hitGround = true;
        impactPos.copy(pos);
        impactPos.y = 0.25;
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

    // Fill remaining points with last position
    for (let i = pointCount; i < this.maxPoints; i++) {
      this.positions[i * 3] = impactPos.x;
      this.positions[i * 3 + 1] = impactPos.y;
      this.positions[i * 3 + 2] = impactPos.z;
    }

    this.lineGeometry.setDrawRange(0, pointCount);
    (this.lineGeometry.attributes.position as THREE.BufferAttribute).needsUpdate = true;

    this.beadingGeometry.setDrawRange(0, pointCount);
    (this.beadingGeometry.attributes.position as THREE.BufferAttribute).needsUpdate = true;

    // Position impact ring marker
    this.impactMarker.position.copy(impactPos);
    this.impactMarker.position.y += 0.05; // avoid z-fighting with ground
    this.impactMarker.visible = hitGround;

    return impactPos;
  }

  public setVisible(visible: boolean): void {
    this.lineMesh.visible = visible;
    this.beadingPoints.visible = visible;
    this.impactMarker.visible = visible;
  }
}
