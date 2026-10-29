import * as THREE from 'three';

export class LaunchPad {
  public group: THREE.Group;
  private turretYaw: THREE.Group;
  private gantryPitch: THREE.Group;
  private rocketMountPoint: THREE.Group;
  private hydraulicPiston: THREE.Mesh;

  constructor(scene: THREE.Scene) {
    this.group = new THREE.Group();
    scene.add(this.group);

    // 1. Mobile Hexagonal Launch Base
    const baseGeo = new THREE.CylinderGeometry(3.2, 3.8, 0.5, 6);
    const darkMetal = new THREE.MeshPhysicalMaterial({
      color: 0x1e242d,
      metalness: 0.88,
      roughness: 0.3,
      clearcoat: 0.25,
    });
    const baseMesh = new THREE.Mesh(baseGeo, darkMetal);
    baseMesh.position.y = 0.25;
    baseMesh.castShadow = true;
    baseMesh.receiveShadow = true;
    this.group.add(baseMesh);

    // 4 Heavy Hydraulic Outriggers
    for (let i = 0; i < 4; i++) {
      const angle = (i * Math.PI) / 2 + Math.PI / 4;
      const legArm = new THREE.Mesh(
        new THREE.BoxGeometry(0.45, 0.28, 2.2),
        darkMetal
      );
      legArm.position.set(Math.cos(angle) * 3.2, 0.2, Math.sin(angle) * 3.2);
      legArm.rotation.y = -angle;
      legArm.castShadow = true;
      this.group.add(legArm);

      const footPad = new THREE.Mesh(
        new THREE.CylinderGeometry(0.55, 0.65, 0.16, 8),
        new THREE.MeshStandardMaterial({ color: 0xf59e0b, roughness: 0.5 })
      );
      footPad.position.set(Math.cos(angle) * 4.4, 0.08, Math.sin(angle) * 4.4);
      footPad.castShadow = true;
      this.group.add(footPad);
    }

    // 2. Azimuth Turret Yaw Bearing
    this.turretYaw = new THREE.Group();
    this.turretYaw.position.set(0, 0.5, 0);
    this.group.add(this.turretYaw);

    const bearingMesh = new THREE.Mesh(
      new THREE.CylinderGeometry(2.0, 2.2, 0.35, 32),
      new THREE.MeshPhysicalMaterial({ color: 0x334155, metalness: 0.9, roughness: 0.25 })
    );
    bearingMesh.position.y = 0.18;
    bearingMesh.castShadow = true;
    this.turretYaw.add(bearingMesh);

    // Blast Deflector Wedge (curved back toward +Z so blast vents away from downfield)
    const deflector = new THREE.Mesh(
      new THREE.BoxGeometry(2.4, 0.5, 1.8),
      new THREE.MeshStandardMaterial({ color: 0x475569, metalness: 0.75, roughness: 0.4 })
    );
    deflector.position.set(0, 0.4, 0.9);
    deflector.rotation.x = -Math.PI / 6;
    deflector.castShadow = true;
    this.turretYaw.add(deflector);

    // 3. Elevation Pitch Gantry Pivot
    this.gantryPitch = new THREE.Group();
    this.gantryPitch.position.set(0, 0.5, 0);
    this.turretYaw.add(this.gantryPitch);

    // Twin Heavy Titanium Launch Rails
    const railMat = new THREE.MeshPhysicalMaterial({
      color: 0x0ea5e9,
      metalness: 0.95,
      roughness: 0.15,
      emissive: 0x0284c7,
      emissiveIntensity: 0.35,
    });

    const railL = new THREE.Mesh(new THREE.BoxGeometry(0.12, 4.8, 0.18), railMat);
    railL.position.set(-0.4, 2.4, 0);
    railL.castShadow = true;
    this.gantryPitch.add(railL);

    const railR = new THREE.Mesh(new THREE.BoxGeometry(0.12, 4.8, 0.18), railMat);
    railR.position.set(0.4, 2.4, 0);
    railR.castShadow = true;
    this.gantryPitch.add(railR);

    // Gantry Structural Truss Cross-Members
    for (let y = 0.6; y <= 4.2; y += 0.8) {
      const cross = new THREE.Mesh(
        new THREE.BoxGeometry(0.92, 0.08, 0.12),
        darkMetal
      );
      cross.position.set(0, y, 0.08);
      this.gantryPitch.add(cross);
    }

    // Hydraulic Elevation Piston
    this.hydraulicPiston = new THREE.Mesh(
      new THREE.CylinderGeometry(0.1, 0.14, 2.4, 16),
      new THREE.MeshPhysicalMaterial({ color: 0xf8fafc, metalness: 0.98, roughness: 0.08 })
    );
    this.hydraulicPiston.position.set(0, 1.1, 0.65);
    this.hydraulicPiston.rotation.x = Math.PI / 8;
    this.turretYaw.add(this.hydraulicPiston);

    // 4. Rocket Mount Attachment Point on front of rail (facing -Z downfield)
    this.rocketMountPoint = new THREE.Group();
    this.rocketMountPoint.position.set(0, 1.2, -0.35);
    this.gantryPitch.add(this.rocketMountPoint);

    // Default aim: 55 degrees pitch, 0 yaw
    this.setAim(55, 0);
  }

  public setPosition(pos: THREE.Vector3): void {
    this.group.position.copy(pos);
  }

  public setAim(pitchDeg: number, yawDeg: number): void {
    // Yaw rotates turret base around Y axis (matches compass azimuth)
    this.turretYaw.rotation.y = THREE.MathUtils.degToRad(yawDeg);

    // Pitch tilts forward toward -Z (90 deg = vertical, 15 deg = low trajectory)
    const tiltAngle = THREE.MathUtils.degToRad(90 - pitchDeg);
    this.gantryPitch.rotation.x = -tiltAngle; // NEGATIVE rotates forward toward -Z downfield!

    // Mechanical hydraulic linkage stretch
    const pistonExt = 1.0 + (90 - pitchDeg) / 100;
    this.hydraulicPiston.scale.set(1, pistonExt, 1);
  }

  public getLaunchTransform(): { position: THREE.Vector3; direction: THREE.Vector3 } {
    const pos = new THREE.Vector3();
    this.rocketMountPoint.getWorldPosition(pos);

    // Direction along the launch rail axis
    const dir = new THREE.Vector3(0, 1, 0);
    const quat = new THREE.Quaternion();
    this.gantryPitch.getWorldQuaternion(quat);
    dir.applyQuaternion(quat).normalize();

    return { position: pos, direction: dir };
  }

  public getMountPoint(): THREE.Group {
    return this.rocketMountPoint;
  }
}
