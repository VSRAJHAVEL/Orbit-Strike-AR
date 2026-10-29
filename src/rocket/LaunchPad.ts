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
    const baseGeo = new THREE.CylinderGeometry(2.2, 2.6, 0.4, 6);
    const darkMetal = new THREE.MeshPhysicalMaterial({
      color: 0x1e242d,
      metalness: 0.85,
      roughness: 0.35,
      clearcoat: 0.2,
    });
    const baseMesh = new THREE.Mesh(baseGeo, darkMetal);
    baseMesh.position.y = 0.2;
    baseMesh.castShadow = true;
    baseMesh.receiveShadow = true;
    this.group.add(baseMesh);

    // 4 Heavy Hydraulic Stabilizer Outriggers
    for (let i = 0; i < 4; i++) {
      const angle = (i * Math.PI) / 2 + Math.PI / 4;
      const legArm = new THREE.Mesh(
        new THREE.BoxGeometry(0.35, 0.22, 1.6),
        darkMetal
      );
      legArm.position.set(Math.cos(angle) * 2.2, 0.15, Math.sin(angle) * 2.2);
      legArm.rotation.y = -angle;
      legArm.castShadow = true;
      this.group.add(legArm);

      const footPad = new THREE.Mesh(
        new THREE.CylinderGeometry(0.38, 0.45, 0.12, 8),
        new THREE.MeshStandardMaterial({ color: 0xeab308, roughness: 0.6 })
      );
      footPad.position.set(Math.cos(angle) * 3.0, 0.06, Math.sin(angle) * 3.0);
      footPad.castShadow = true;
      this.group.add(footPad);
    }

    // 2. Azimuth Turret Yaw Bearing
    this.turretYaw = new THREE.Group();
    this.turretYaw.position.set(0, 0.4, 0);
    this.group.add(this.turretYaw);

    const bearingMesh = new THREE.Mesh(
      new THREE.CylinderGeometry(1.4, 1.5, 0.25, 24),
      new THREE.MeshPhysicalMaterial({ color: 0x334155, metalness: 0.9, roughness: 0.25 })
    );
    bearingMesh.position.y = 0.12;
    bearingMesh.castShadow = true;
    this.turretYaw.add(bearingMesh);

    // Blast Deflector Wedge
    const deflector = new THREE.Mesh(
      new THREE.BoxGeometry(1.6, 0.35, 1.2),
      new THREE.MeshStandardMaterial({ color: 0x475569, metalness: 0.7, roughness: 0.5 })
    );
    deflector.position.set(0, 0.3, -0.6);
    deflector.rotation.x = Math.PI / 6;
    deflector.castShadow = true;
    this.turretYaw.add(deflector);

    // 3. Elevation Pitch Gantry Pivot
    this.gantryPitch = new THREE.Group();
    this.gantryPitch.position.set(0, 0.35, 0);
    this.turretYaw.add(this.gantryPitch);

    // Launch Guide Rails (Twin titanium rails)
    const railMat = new THREE.MeshPhysicalMaterial({
      color: 0x0ea5e9,
      metalness: 0.95,
      roughness: 0.15,
      emissive: 0x0284c7,
      emissiveIntensity: 0.25,
    });

    const railL = new THREE.Mesh(new THREE.BoxGeometry(0.08, 3.2, 0.12), railMat);
    railL.position.set(-0.25, 1.6, 0);
    railL.castShadow = true;
    this.gantryPitch.add(railL);

    const railR = new THREE.Mesh(new THREE.BoxGeometry(0.08, 3.2, 0.12), railMat);
    railR.position.set(0.25, 1.6, 0);
    railR.castShadow = true;
    this.gantryPitch.add(railR);

    // Gantry Structural Truss Cross-Members
    for (let y = 0.5; y <= 2.8; y += 0.6) {
      const cross = new THREE.Mesh(
        new THREE.BoxGeometry(0.58, 0.05, 0.08),
        darkMetal
      );
      cross.position.set(0, y, -0.05);
      this.gantryPitch.add(cross);
    }

    // Hydraulic Elevation Piston
    this.hydraulicPiston = new THREE.Mesh(
      new THREE.CylinderGeometry(0.07, 0.09, 1.6, 12),
      new THREE.MeshPhysicalMaterial({ color: 0xf1f5f9, metalness: 0.98, roughness: 0.08 })
    );
    this.hydraulicPiston.position.set(0, 0.7, -0.45);
    this.hydraulicPiston.rotation.x = -Math.PI / 8;
    this.turretYaw.add(this.hydraulicPiston);

    // 4. Rocket Mount Attachment Point
    this.rocketMountPoint = new THREE.Group();
    this.rocketMountPoint.position.set(0, 0.85, 0.22);
    this.gantryPitch.add(this.rocketMountPoint);

    // Set initial default aiming
    this.setAim(55, 0);
  }

  public setPosition(pos: THREE.Vector3): void {
    this.group.position.copy(pos);
  }

  public setAim(pitchDeg: number, yawDeg: number): void {
    // Yaw rotates turret base around Y axis (inverted to match intuitive compass heading)
    this.turretYaw.rotation.y = -THREE.MathUtils.degToRad(yawDeg);

    // Pitch tilts gantry back from vertical (90 deg = straight up, 15 deg = low trajectory)
    // 90 deg pitch corresponds to 0 rotation back; lower pitch tilts forward/down
    const tiltAngle = THREE.MathUtils.degToRad(90 - pitchDeg);
    this.gantryPitch.rotation.x = tiltAngle;

    // Adjust hydraulic cylinder scale/rotation to match mechanical linkage
    const pistonExtension = 1.0 + (90 - pitchDeg) / 120;
    this.hydraulicPiston.scale.set(1, pistonExtension, 1);
  }

  public getLaunchTransform(): { position: THREE.Vector3; direction: THREE.Vector3 } {
    const pos = new THREE.Vector3();
    this.rocketMountPoint.getWorldPosition(pos);

    // Direction points along the launch rail axis
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
