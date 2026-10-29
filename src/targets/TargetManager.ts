import * as THREE from 'three';
import { PhysicsWorld } from '../physics/PhysicsWorld';

export type TargetTier = 'bullseye' | 'shielded' | 'drone' | 'boss';

export interface TargetInstance {
  id: string;
  tier: TargetTier;
  mesh: THREE.Group;
  collider: any;
  position: THREE.Vector3;
  initialPosition: THREE.Vector3;
  hp: number;
  maxHp: number;
  shieldHp: number;
  shieldMesh: THREE.Mesh | null;
  healthBarMesh: THREE.Group | null;
  healthBarFill: THREE.Mesh | null;
  rotorMeshes: THREE.Mesh[];
  beaconLight: THREE.PointLight;
  destroyed: boolean;
}

export class TargetManager {
  public group: THREE.Group;
  private physics: PhysicsWorld;
  private currentTarget: TargetInstance | null = null;
  private scene: THREE.Scene;

  constructor(scene: THREE.Scene, physics: PhysicsWorld) {
    this.scene = scene;
    this.physics = physics;
    this.group = new THREE.Group();
    scene.add(this.group);
  }

  public spawnTarget(tier: TargetTier, position: THREE.Vector3): TargetInstance {
    this.clearCurrentTarget();

    const targetGroup = new THREE.Group();
    targetGroup.position.copy(position);
    this.group.add(targetGroup);

    let maxHp = 100;
    let shieldHp = 0;
    let radius = 2.4;
    let shieldMesh: THREE.Mesh | null = null;
    let healthBarMesh: THREE.Group | null = null;
    let healthBarFill: THREE.Mesh | null = null;
    const rotorMeshes: THREE.Mesh[] = [];

    // Illuminated Beacon PointLight
    const beaconLight = new THREE.PointLight(0x00f0ff, 4.0, 30);
    beaconLight.position.set(0, 3.5, 0);
    targetGroup.add(beaconLight);

    if (tier === 'bullseye') {
      // High-visibility Concentric 3-ring Bullseye Target
      radius = 2.8;
      maxHp = 100;

      // Heavy Industrial Stand Gantry
      const stand = new THREE.Mesh(
        new THREE.CylinderGeometry(0.22, 0.35, 3.5, 16),
        new THREE.MeshStandardMaterial({ color: 0x1e293b, metalness: 0.85, roughness: 0.3 })
      );
      stand.position.y = 1.75;
      stand.castShadow = true;
      targetGroup.add(stand);

      // Rotating Target Face (Angled toward the launchpad)
      const discFace = new THREE.Group();
      discFace.position.set(0, 3.5, 0);
      discFace.lookAt(new THREE.Vector3(0, 3.5, 0)); // Points toward launch pad
      targetGroup.add(discFace);

      // Outer Cyan Ring (Radius 2.6m)
      const outerRing = new THREE.Mesh(
        new THREE.CylinderGeometry(2.6, 2.6, 0.15, 36),
        new THREE.MeshPhysicalMaterial({
          color: 0x0284c7,
          roughness: 0.2,
          metalness: 0.7,
          emissive: 0x0284c7,
          emissiveIntensity: 0.2,
        })
      );
      outerRing.rotation.x = Math.PI / 2;
      outerRing.castShadow = true;
      discFace.add(outerRing);

      // Mid Gold Ring (Radius 1.6m)
      const midRing = new THREE.Mesh(
        new THREE.CylinderGeometry(1.6, 1.6, 0.18, 36),
        new THREE.MeshPhysicalMaterial({
          color: 0xf59e0b,
          roughness: 0.15,
          metalness: 0.85,
          emissive: 0xd97706,
          emissiveIntensity: 0.3,
        })
      );
      midRing.rotation.x = Math.PI / 2;
      midRing.position.z = 0.02;
      discFace.add(midRing);

      // Center Bullseye Core (Radius 0.75m)
      const bullseyeCore = new THREE.Mesh(
        new THREE.CylinderGeometry(0.75, 0.75, 0.22, 36),
        new THREE.MeshPhysicalMaterial({
          color: 0xef4444,
          roughness: 0.1,
          metalness: 0.5,
          emissive: 0xdc2626,
          emissiveIntensity: 0.8,
        })
      );
      bullseyeCore.rotation.x = Math.PI / 2;
      bullseyeCore.position.z = 0.05;
      discFace.add(bullseyeCore);

      // Skyward Holographic Pillar Beacon
      const beamGeo = new THREE.CylinderGeometry(0.08, 0.25, 45, 12, 1, true);
      const beamMat = new THREE.MeshBasicMaterial({
        color: 0x00f0ff,
        transparent: true,
        opacity: 0.35,
        side: THREE.DoubleSide,
      });
      const beamMesh = new THREE.Mesh(beamGeo, beamMat);
      beamMesh.position.y = 22.5;
      targetGroup.add(beamMesh);
    } else if (tier === 'shielded') {
      // Armored Bunker with Forcefield Dome
      radius = 3.2;
      maxHp = 100;
      shieldHp = 100;

      const bunker = new THREE.Mesh(
        new THREE.CylinderGeometry(2.0, 2.6, 2.4, 8),
        new THREE.MeshStandardMaterial({ color: 0x1e293b, metalness: 0.9, roughness: 0.3 })
      );
      bunker.position.y = 1.2;
      bunker.castShadow = true;
      targetGroup.add(bunker);

      // Glowing Translucent Forcefield Dome
      const domeGeo = new THREE.SphereGeometry(3.2, 28, 20, 0, Math.PI * 2, 0, Math.PI * 0.65);
      const domeMat = new THREE.MeshPhysicalMaterial({
        color: 0x00f0ff,
        transparent: true,
        opacity: 0.45,
        roughness: 0.1,
        metalness: 0.1,
        transmission: 0.7,
        emissive: 0x00a8ff,
        emissiveIntensity: 0.8,
        side: THREE.DoubleSide,
      });
      shieldMesh = new THREE.Mesh(domeGeo, domeMat);
      shieldMesh.position.y = 0.3;
      targetGroup.add(shieldMesh);
    } else if (tier === 'drone') {
      // Aerial Reconnaissance Drone
      radius = 2.2;
      maxHp = 90;

      const bodyMesh = new THREE.Mesh(
        new THREE.SphereGeometry(1.1, 16, 16),
        new THREE.MeshPhysicalMaterial({ color: 0x334155, metalness: 0.88, roughness: 0.22 })
      );
      bodyMesh.scale.set(1.5, 0.65, 1.3);
      bodyMesh.castShadow = true;
      targetGroup.add(bodyMesh);

      // 4 Drone Quad-Rotors
      for (let i = 0; i < 4; i++) {
        const angle = (i * Math.PI) / 2 + Math.PI / 4;
        const arm = new THREE.Mesh(
          new THREE.CylinderGeometry(0.08, 0.08, 1.8, 8),
          new THREE.MeshStandardMaterial({ color: 0x64748b })
        );
        arm.position.set(Math.cos(angle) * 1.2, 0, Math.sin(angle) * 1.2);
        arm.rotation.y = angle;
        arm.rotation.z = Math.PI / 2;
        targetGroup.add(arm);

        const rotor = new THREE.Mesh(
          new THREE.BoxGeometry(1.6, 0.04, 0.14),
          new THREE.MeshBasicMaterial({ color: 0x00f0ff })
        );
        rotor.position.set(Math.cos(angle) * 2.1, 0.15, Math.sin(angle) * 2.1);
        targetGroup.add(rotor);
        rotorMeshes.push(rotor);
      }
    } else if (tier === 'boss') {
      // Boss Dreadnought Flagship
      radius = 5.5;
      maxHp = 350;

      const hull = new THREE.Mesh(
        new THREE.BoxGeometry(8.5, 2.8, 5.2),
        new THREE.MeshPhysicalMaterial({
          color: 0x1e242d,
          metalness: 0.94,
          roughness: 0.25,
          clearcoat: 0.4,
        })
      );
      hull.position.y = 3.5;
      hull.castShadow = true;
      targetGroup.add(hull);

      // Dorsal Rotating Radar Array
      const radar = new THREE.Mesh(
        new THREE.CylinderGeometry(1.4, 1.4, 0.2, 20),
        new THREE.MeshStandardMaterial({ color: 0xf59e0b, metalness: 0.85 })
      );
      radar.position.set(0, 5.1, 0);
      targetGroup.add(radar);
      rotorMeshes.push(radar);

      // Health Bar Billboard floating above
      healthBarMesh = new THREE.Group();
      healthBarMesh.position.set(0, 6.8, 0);
      targetGroup.add(healthBarMesh);

      const barBg = new THREE.Mesh(
        new THREE.PlaneGeometry(5.2, 0.45),
        new THREE.MeshBasicMaterial({ color: 0x000000, side: THREE.DoubleSide })
      );
      healthBarMesh.add(barBg);

      healthBarFill = new THREE.Mesh(
        new THREE.PlaneGeometry(5.0, 0.38),
        new THREE.MeshBasicMaterial({ color: 0xef4444, side: THREE.DoubleSide })
      );
      healthBarFill.position.z = 0.02;
      healthBarMesh.add(healthBarFill);
    }

    // Register with Rapier physics engine
    const targetId = `target_${Date.now()}`;
    const collider = this.physics.registerTarget(targetId, position, radius, targetGroup);

    const instance: TargetInstance = {
      id: targetId,
      tier,
      mesh: targetGroup,
      collider,
      position: position.clone(),
      initialPosition: position.clone(),
      hp: maxHp,
      maxHp,
      shieldHp,
      shieldMesh,
      healthBarMesh,
      healthBarFill,
      rotorMeshes,
      beaconLight,
      destroyed: false,
    };

    this.currentTarget = instance;
    return instance;
  }

  public update(delta: number, elapsed: number, cameraPos: THREE.Vector3): void {
    if (!this.currentTarget || this.currentTarget.destroyed) return;

    const t = this.currentTarget;

    // Beacon pulse
    t.beaconLight.intensity = 3.5 + Math.sin(elapsed * 4.0) * 1.5;

    // Drone flight kinematics
    if (t.tier === 'drone') {
      const offsetX = Math.sin(elapsed * 1.8) * 3.8;
      const offsetY = Math.cos(elapsed * 2.2) * 1.2;
      t.mesh.position.set(
        t.initialPosition.x + offsetX,
        t.initialPosition.y + offsetY,
        t.initialPosition.z
      );

      for (const r of t.rotorMeshes) {
        r.rotation.y += delta * 40.0;
      }
    } else if (t.tier === 'boss') {
      t.mesh.position.y = t.initialPosition.y + Math.sin(elapsed * 1.0) * 0.6;
      for (const r of t.rotorMeshes) {
        r.rotation.y += delta * 1.2;
      }

      if (t.healthBarMesh) {
        t.healthBarMesh.lookAt(cameraPos);
      }
    }
  }

  public registerDamage(hitPoint: THREE.Vector3, baseDamage: number): {
    destroyed: boolean;
    points: number;
    hitLabel: string;
    isBullseye: boolean;
  } {
    if (!this.currentTarget || this.currentTarget.destroyed) {
      return { destroyed: false, points: 0, hitLabel: 'MISS', isBullseye: false };
    }

    const t = this.currentTarget;

    // 1. Forcefield absorption
    if (t.shieldHp > 0) {
      t.shieldHp = 0;
      if (t.shieldMesh) {
        t.mesh.remove(t.shieldMesh);
        t.shieldMesh = null;
      }
      return {
        destroyed: false,
        points: 75,
        hitLabel: 'SHIELD SHATTERED! (+75)',
        isBullseye: false,
      };
    }

    // 2. Concentric accuracy calculation
    let accuracyBonus = 50;
    let isBullseye = false;
    let label = 'TARGET HIT!';

    const centerPos = t.mesh.position.clone();
    centerPos.y += t.tier === 'bullseye' ? 3.5 : 1.2;
    const distFromCenter = hitPoint.distanceTo(centerPos);

    if (t.tier === 'bullseye') {
      if (distFromCenter < 0.85) {
        accuracyBonus = 500;
        isBullseye = true;
        label = 'DIRECT BULLSEYE! (+500)';
      } else if (distFromCenter < 1.8) {
        accuracyBonus = 250;
        label = 'MID-RING STRIKE! (+250)';
      } else {
        accuracyBonus = 100;
        label = 'OUTER RING HIT! (+100)';
      }
    }

    t.hp -= baseDamage;

    if (t.healthBarFill) {
      const pct = Math.max(0, t.hp / t.maxHp);
      t.healthBarFill.scale.set(pct, 1, 1);
    }

    if (t.hp <= 0) {
      t.destroyed = true;
      this.physics.removeTargetCollider(t.collider);
      this.group.remove(t.mesh);

      const destructionScore = t.tier === 'boss' ? 800 : accuracyBonus + 150;
      return {
        destroyed: true,
        points: destructionScore,
        hitLabel: isBullseye ? label : `TARGET OBLITERATED! (+${destructionScore})`,
        isBullseye,
      };
    }

    return {
      destroyed: false,
      points: accuracyBonus,
      hitLabel: `ARMOR PENETRATED -${baseDamage} HP (+${accuracyBonus})`,
      isBullseye,
    };
  }

  public clearCurrentTarget(): void {
    if (this.currentTarget) {
      this.physics.removeTargetCollider(this.currentTarget.collider);
      this.group.remove(this.currentTarget.mesh);
      this.currentTarget = null;
    }
  }

  public getCurrentTarget(): TargetInstance | null {
    return this.currentTarget;
  }
}
