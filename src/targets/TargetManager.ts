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
    let radius = 1.4;
    let shieldMesh: THREE.Mesh | null = null;
    let healthBarMesh: THREE.Group | null = null;
    let healthBarFill: THREE.Mesh | null = null;
    const rotorMeshes: THREE.Mesh[] = [];

    if (tier === 'bullseye') {
      // Concentric 3-ring bullseye target
      radius = 1.6;
      maxHp = 100;

      // Stand gantry
      const stand = new THREE.Mesh(
        new THREE.CylinderGeometry(0.12, 0.16, 2.2, 12),
        new THREE.MeshStandardMaterial({ color: 0x334155, metalness: 0.8, roughness: 0.4 })
      );
      stand.position.y = 1.1;
      stand.castShadow = true;
      targetGroup.add(stand);

      // Angled Disc Face
      const discFace = new THREE.Group();
      discFace.position.set(0, 2.2, 0);
      discFace.rotation.x = Math.PI / 12; // tilted slightly up toward launchpad
      targetGroup.add(discFace);

      // Outer Cyan Ring (R = 1.5)
      const outerRing = new THREE.Mesh(
        new THREE.CylinderGeometry(1.5, 1.5, 0.08, 32),
        new THREE.MeshPhysicalMaterial({ color: 0x0284c7, roughness: 0.3, clearcoat: 0.5 })
      );
      outerRing.rotation.x = Math.PI / 2;
      outerRing.castShadow = true;
      discFace.add(outerRing);

      // Mid Gold Ring (R = 0.95)
      const midRing = new THREE.Mesh(
        new THREE.CylinderGeometry(0.95, 0.95, 0.1, 32),
        new THREE.MeshPhysicalMaterial({ color: 0xeab308, roughness: 0.2, clearcoat: 0.8 })
      );
      midRing.rotation.x = Math.PI / 2;
      midRing.position.z = 0.015;
      discFace.add(midRing);

      // Inner Red Bullseye (R = 0.42)
      const bullseyeCore = new THREE.Mesh(
        new THREE.CylinderGeometry(0.42, 0.42, 0.12, 32),
        new THREE.MeshPhysicalMaterial({
          color: 0xef4444,
          roughness: 0.15,
          emissive: 0xdc2626,
          emissiveIntensity: 0.35,
        })
      );
      bullseyeCore.rotation.x = Math.PI / 2;
      bullseyeCore.position.z = 0.03;
      discFace.add(bullseyeCore);
    } else if (tier === 'shielded') {
      // Shielded Military Drone / Bunker
      radius = 1.8;
      maxHp = 100;
      shieldHp = 100; // Requires 2 direct hits

      const bunker = new THREE.Mesh(
        new THREE.CylinderGeometry(1.2, 1.6, 1.5, 8),
        new THREE.MeshStandardMaterial({ color: 0x1e293b, metalness: 0.9, roughness: 0.3 })
      );
      bunker.position.y = 0.75;
      bunker.castShadow = true;
      targetGroup.add(bunker);

      // Glowing Translucent Forcefield Dome
      const domeGeo = new THREE.SphereGeometry(1.9, 24, 16, 0, Math.PI * 2, 0, Math.PI * 0.65);
      const domeMat = new THREE.MeshPhysicalMaterial({
        color: 0x00f0ff,
        transparent: true,
        opacity: 0.45,
        roughness: 0.1,
        metalness: 0.1,
        transmission: 0.6,
        emissive: 0x00a8ff,
        emissiveIntensity: 0.6,
        side: THREE.DoubleSide,
      });
      shieldMesh = new THREE.Mesh(domeGeo, domeMat);
      shieldMesh.position.y = 0.2;
      targetGroup.add(shieldMesh);
    } else if (tier === 'drone') {
      // Aerial Hover Drone
      radius = 1.3;
      maxHp = 80;

      const bodyMesh = new THREE.Mesh(
        new THREE.SphereGeometry(0.65, 16, 16),
        new THREE.MeshPhysicalMaterial({ color: 0x334155, metalness: 0.85, roughness: 0.25 })
      );
      bodyMesh.scale.set(1.4, 0.6, 1.2);
      bodyMesh.castShadow = true;
      targetGroup.add(bodyMesh);

      // 4 Drone Rotors
      for (let i = 0; i < 4; i++) {
        const angle = (i * Math.PI) / 2 + Math.PI / 4;
        const arm = new THREE.Mesh(
          new THREE.CylinderGeometry(0.04, 0.04, 1.1, 8),
          new THREE.MeshStandardMaterial({ color: 0x64748b })
        );
        arm.position.set(Math.cos(angle) * 0.7, 0, Math.sin(angle) * 0.7);
        arm.rotation.y = angle;
        arm.rotation.z = Math.PI / 2;
        targetGroup.add(arm);

        const rotor = new THREE.Mesh(
          new THREE.BoxGeometry(0.8, 0.02, 0.08),
          new THREE.MeshBasicMaterial({ color: 0x00f0ff })
        );
        rotor.position.set(Math.cos(angle) * 1.25, 0.1, Math.sin(angle) * 1.25);
        targetGroup.add(rotor);
        rotorMeshes.push(rotor);
      }
    } else if (tier === 'boss') {
      // Boss Dreadnought Flagship
      radius = 3.4;
      maxHp = 350;

      const hull = new THREE.Mesh(
        new THREE.BoxGeometry(5.2, 1.8, 3.2),
        new THREE.MeshPhysicalMaterial({
          color: 0x1e242d,
          metalness: 0.92,
          roughness: 0.28,
          clearcoat: 0.3,
        })
      );
      hull.position.y = 2.4;
      hull.castShadow = true;
      targetGroup.add(hull);

      // Rotating Radar Array on dorsal deck
      const radar = new THREE.Mesh(
        new THREE.CylinderGeometry(0.9, 0.9, 0.12, 16),
        new THREE.MeshStandardMaterial({ color: 0xf59e0b, metalness: 0.8 })
      );
      radar.position.set(0, 3.4, 0);
      targetGroup.add(radar);
      rotorMeshes.push(radar); // Rotates smoothly

      // Boss Health Bar Billboard floating above
      healthBarMesh = new THREE.Group();
      healthBarMesh.position.set(0, 4.6, 0);
      targetGroup.add(healthBarMesh);

      const barBg = new THREE.Mesh(
        new THREE.PlaneGeometry(3.6, 0.35),
        new THREE.MeshBasicMaterial({ color: 0x000000, side: THREE.DoubleSide })
      );
      healthBarMesh.add(barBg);

      healthBarFill = new THREE.Mesh(
        new THREE.PlaneGeometry(3.5, 0.28),
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
      destroyed: false,
    };

    this.currentTarget = instance;
    return instance;
  }

  public update(delta: number, elapsed: number, cameraPos: THREE.Vector3): void {
    if (!this.currentTarget || this.currentTarget.destroyed) return;

    const t = this.currentTarget;

    // 1. Drone sinusoidal patrol animation
    if (t.tier === 'drone') {
      const offsetX = Math.sin(elapsed * 1.8) * 2.8;
      const offsetY = Math.cos(elapsed * 2.5) * 0.6;
      t.mesh.position.set(
        t.initialPosition.x + offsetX,
        t.initialPosition.y + offsetY,
        t.initialPosition.z
      );

      // Spin rotors
      for (const r of t.rotorMeshes) {
        r.rotation.y += delta * 35.0;
      }
    } else if (t.tier === 'boss') {
      // Gentle floating sway for Boss Dreadnought
      t.mesh.position.y = t.initialPosition.y + Math.sin(elapsed * 1.2) * 0.4;
      for (const r of t.rotorMeshes) {
        r.rotation.y += delta * 1.2;
      }

      // Orient health bar toward camera
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

    // 1. Check shield absorption
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

    // 2. Concentric accuracy calculation for bullseye
    let accuracyBonus = 25;
    let isBullseye = false;
    let label = 'TARGET HIT!';

    const centerPos = t.mesh.position.clone();
    centerPos.y += t.tier === 'bullseye' ? 2.2 : 1.0;
    const distFromCenter = hitPoint.distanceTo(centerPos);

    if (t.tier === 'bullseye') {
      if (distFromCenter < 0.45) {
        accuracyBonus = 500;
        isBullseye = true;
        label = 'DIRECT BULLSEYE! (+500)';
      } else if (distFromCenter < 1.0) {
        accuracyBonus = 250;
        label = 'MID-RING STRIKE! (+250)';
      } else {
        accuracyBonus = 100;
        label = 'OUTER RING HIT! (+100)';
      }
    }

    t.hp -= baseDamage;

    // Update Boss health bar
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
