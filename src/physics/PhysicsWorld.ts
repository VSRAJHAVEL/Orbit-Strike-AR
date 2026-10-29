import * as THREE from 'three';
import RAPIER from '@dimforge/rapier3d-compat';
import { RocketStats } from '../rocket/RocketStats';

export interface RocketPhysicsState {
  position: THREE.Vector3;
  quaternion: THREE.Quaternion;
  velocity: THREE.Vector3;
  speed: number;
  altitude: number;
  isBurning: boolean;
  fuelRemainingPct: number;
  hasCollided: boolean;
  collisionPoint: THREE.Vector3 | null;
  collisionTargetId: string | null;
}

export class PhysicsWorld {
  public world!: RAPIER.World;
  private isInitialized = false;
  private currentGravity = 9.81;
  private groundCollider!: RAPIER.Collider;

  // Active Rocket Rigidbody & Collider
  public rocketBody: RAPIER.RigidBody | null = null;
  public rocketCollider: RAPIER.Collider | null = null;
  private activeStats: RocketStats | null = null;
  private launchForce = 0;
  private launchDirection = new THREE.Vector3(0, 1, 0);
  private flightTimer = 0;
  private isRocketActive = false;
  private windVector = new THREE.Vector3(0, 0, 0);
  private collisionOccurred = false;
  private lastCollisionPoint: THREE.Vector3 | null = null;
  private lastCollisionTargetId: string | null = null;

  // Target colliders map
  private targetColliders = new Map<number, { id: string; mesh: THREE.Object3D }>();

  // Event queue
  private eventQueue!: RAPIER.EventQueue;

  public async init(initialGravity = 9.81): Promise<void> {
    await RAPIER.init();
    this.currentGravity = initialGravity;

    const gravityVec = new RAPIER.Vector3(0, -this.currentGravity, 0);
    this.world = new RAPIER.World(gravityVec);
    this.eventQueue = new RAPIER.EventQueue(true);

    // Static Ground plane collider (at y = 0.2m)
    const groundDesc = RAPIER.ColliderDesc.cuboid(300, 0.2, 300)
      .setTranslation(0, -0.1, 0)
      .setRestitution(0.15)
      .setFriction(0.8);
    this.groundCollider = this.world.createCollider(groundDesc);

    this.isInitialized = true;
  }

  public setGravity(g: number): void {
    this.currentGravity = g;
    if (this.world) {
      this.world.gravity = new RAPIER.Vector3(0, -g, 0);
    }
  }

  public setWind(windVec: THREE.Vector3): void {
    this.windVector.copy(windVec);
  }

  public registerTarget(id: string, position: THREE.Vector3, radius: number, mesh: THREE.Object3D): RAPIER.Collider {
    const colDesc = RAPIER.ColliderDesc.ball(radius)
      .setTranslation(position.x, position.y, position.z)
      .setActiveEvents(RAPIER.ActiveEvents.COLLISION_EVENTS);
    const col = this.world.createCollider(colDesc);
    this.targetColliders.set(col.handle, { id, mesh });
    return col;
  }

  public removeTargetCollider(col: RAPIER.Collider): void {
    this.targetColliders.delete(col.handle);
    this.world.removeCollider(col, false);
  }

  public spawnRocket(
    startPos: THREE.Vector3,
    direction: THREE.Vector3,
    forceMagnitude: number,
    stats: RocketStats
  ): void {
    this.despawnRocket();

    this.activeStats = stats;
    this.launchForce = forceMagnitude;
    this.launchDirection.copy(direction).normalize();
    this.flightTimer = 0;
    this.isRocketActive = true;
    this.collisionOccurred = false;
    this.lastCollisionPoint = null;
    this.lastCollisionTargetId = null;

    // Create dynamic rigid body with CCD (continuous collision detection) to prevent tunneling
    const bodyDesc = RAPIER.RigidBodyDesc.dynamic()
      .setTranslation(startPos.x, startPos.y, startPos.z)
      .setCcdEnabled(true)
      .setCanSleep(false)
      .setLinearDamping(0.02)
      .setAngularDamping(0.1);

    this.rocketBody = this.world.createRigidBody(bodyDesc);

    // Rocket capsule collider
    const colDesc = RAPIER.ColliderDesc.capsule(0.45, 0.16)
      .setMass(stats.mass)
      .setRestitution(0.1)
      .setActiveEvents(RAPIER.ActiveEvents.COLLISION_EVENTS);

    this.rocketCollider = this.world.createCollider(colDesc, this.rocketBody);

    // Initial muzzle push velocity along launch rail
    const initialSpeed = this.launchForce * 0.42 * stats.thrustMultiplier;
    const initialVel = this.launchDirection.clone().multiplyScalar(initialSpeed);
    this.rocketBody.setLinvel(new RAPIER.Vector3(initialVel.x, initialVel.y, initialVel.z), true);

    // Align rotation with launch direction
    const q = new THREE.Quaternion().setFromUnitVectors(new THREE.Vector3(0, 1, 0), this.launchDirection);
    this.rocketBody.setRotation(new RAPIER.Quaternion(q.x, q.y, q.z, q.w), true);
  }

  public despawnRocket(): void {
    if (this.rocketBody) {
      this.world.removeRigidBody(this.rocketBody);
      this.rocketBody = null;
      this.rocketCollider = null;
    }
    this.isRocketActive = false;
  }

  public step(delta: number): void {
    if (!this.isInitialized || !this.world) return;

    if (this.isRocketActive && this.rocketBody && this.activeStats) {
      this.flightTimer += delta;

      const linvel = this.rocketBody.linvel();
      const currentVel = new THREE.Vector3(linvel.x, linvel.y, linvel.z);
      const speed = currentVel.length();

      // 1. Aerodynamic drag force: F_drag = -0.5 * rho * v^2 * Cd * v_hat
      if (speed > 0.01) {
        // Air density drops slightly with altitude, vacuum on moon = 0
        const isVacuum = this.currentGravity < 2.0;
        const airDensity = isVacuum ? 0.0 : 1.2;
        const dragMag = 0.5 * airDensity * (speed * speed) * this.activeStats.dragCoefficient * 0.04;
        const dragForce = currentVel.clone().normalize().multiplyScalar(-dragMag);

        this.rocketBody.applyImpulse(
          new RAPIER.Vector3(dragForce.x * delta, dragForce.y * delta, dragForce.z * delta),
          true
        );
      }

      // 2. Active rocket motor thrust
      if (this.flightTimer < this.activeStats.burnDuration) {
        // Sustained thrust along rocket nose forward heading
        const rot = this.rocketBody.rotation();
        const quat = new THREE.Quaternion(rot.x, rot.y, rot.z, rot.w);
        const forward = new THREE.Vector3(0, 1, 0).applyQuaternion(quat);

        const thrustMag = this.launchForce * 1.85 * this.activeStats.thrustMultiplier;
        const thrustForce = forward.multiplyScalar(thrustMag * delta);

        this.rocketBody.applyImpulse(
          new RAPIER.Vector3(thrustForce.x, thrustForce.y, thrustForce.z),
          true
        );
      }

      // 3. Environmental Wind gust force
      if (this.windVector.lengthSq() > 0.01 && this.currentGravity > 2.0) {
        const windImpulse = this.windVector.clone().multiplyScalar(0.4 * delta);
        this.rocketBody.applyImpulse(
          new RAPIER.Vector3(windImpulse.x, windImpulse.y, windImpulse.z),
          true
        );
      }

      // 4. Aerodynamic stability: torque aligns nose toward velocity vector
      if (speed > 1.5) {
        const velDir = currentVel.clone().normalize();
        const rot = this.rocketBody.rotation();
        const curQuat = new THREE.Quaternion(rot.x, rot.y, rot.z, rot.w);
        const targetQuat = new THREE.Quaternion().setFromUnitVectors(new THREE.Vector3(0, 1, 0), velDir);

        curQuat.slerp(targetQuat, Math.min(1.0, delta * 6.5));
        this.rocketBody.setRotation(new RAPIER.Quaternion(curQuat.x, curQuat.y, curQuat.z, curQuat.w), true);
      }

      // Ground impact check
      const pos = this.rocketBody.translation();
      if (pos.y <= 0.22 && !this.collisionOccurred) {
        this.collisionOccurred = true;
        this.lastCollisionPoint = new THREE.Vector3(pos.x, 0.2, pos.z);
        this.lastCollisionTargetId = 'ground';
      }
    }

    // Step physics world
    this.world.step(this.eventQueue);

    // Process collision events
    this.eventQueue.drainCollisionEvents((h1, h2, started) => {
      if (!started || this.collisionOccurred || !this.rocketCollider) return;

      if (h1 === this.rocketCollider.handle || h2 === this.rocketCollider.handle) {
        const otherHandle = h1 === this.rocketCollider.handle ? h2 : h1;
        const targetInfo = this.targetColliders.get(otherHandle);

        this.collisionOccurred = true;
        if (this.rocketBody) {
          const t = this.rocketBody.translation();
          this.lastCollisionPoint = new THREE.Vector3(t.x, t.y, t.z);
        }

        if (targetInfo) {
          this.lastCollisionTargetId = targetInfo.id;
        } else if (otherHandle === this.groundCollider.handle) {
          this.lastCollisionTargetId = 'ground';
        }
      }
    });
  }

  public getRocketState(): RocketPhysicsState | null {
    if (!this.rocketBody || !this.activeStats) return null;

    const t = this.rocketBody.translation();
    const r = this.rocketBody.rotation();
    const v = this.rocketBody.linvel();

    const vel = new THREE.Vector3(v.x, v.y, v.z);
    const speed = vel.length();
    const isBurning = this.flightTimer < this.activeStats.burnDuration;
    const fuelRemainingPct = Math.max(0, 1.0 - this.flightTimer / this.activeStats.burnDuration);

    return {
      position: new THREE.Vector3(t.x, t.y, t.z),
      quaternion: new THREE.Quaternion(r.x, r.y, r.z, r.w),
      velocity: vel,
      speed,
      altitude: Math.max(0, t.y - 0.2),
      isBurning,
      fuelRemainingPct,
      hasCollided: this.collisionOccurred,
      collisionPoint: this.lastCollisionPoint,
      collisionTargetId: this.lastCollisionTargetId,
    };
  }

  public isReady(): boolean {
    return this.isInitialized;
  }
}
