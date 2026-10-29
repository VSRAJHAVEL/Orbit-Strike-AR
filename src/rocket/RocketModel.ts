import * as THREE from 'three';
import { RocketStats, ROCKET_ARCHETYPES } from './RocketStats';

export class RocketModel {
  public mesh: THREE.Group;
  private currentStats: RocketStats;
  private engineGlowMesh!: THREE.Mesh;
  private engineLight!: THREE.PointLight;
  private bodyMaterial!: THREE.MeshPhysicalMaterial;
  private accentMaterial!: THREE.MeshPhysicalMaterial;

  constructor(initialStats: RocketStats = ROCKET_ARCHETYPES.viper_basic) {
    this.currentStats = initialStats;
    this.mesh = new THREE.Group();
    this.buildGeometry();
  }

  public setArchetype(stats: RocketStats): void {
    this.currentStats = stats;
    while (this.mesh.children.length > 0) {
      const obj = this.mesh.children[0];
      this.mesh.remove(obj);
    }
    this.buildGeometry();
  }

  private buildGeometry(): void {
    const stats = this.currentStats;

    // Heroic dimensions for crisp cinematic visibility
    const isHeavy = stats.archetype === 'heavy';
    const isSpeedster = stats.archetype === 'speedster';
    const isPrecision = stats.archetype === 'precision';

    const radius = isHeavy ? 0.45 : isSpeedster ? 0.26 : 0.34;
    const bodyHeight = isSpeedster ? 3.2 : isHeavy ? 2.7 : 2.5;
    const noseHeight = isSpeedster ? 1.5 : isPrecision ? 1.3 : 0.95;

    // PBR Materials
    this.bodyMaterial = new THREE.MeshPhysicalMaterial({
      color: stats.primaryColor,
      metalness: 0.88,
      roughness: 0.2,
      clearcoat: 0.5,
      clearcoatRoughness: 0.1,
    });

    this.accentMaterial = new THREE.MeshPhysicalMaterial({
      color: stats.accentColor,
      metalness: 0.8,
      roughness: 0.28,
    });

    const glowMaterial = new THREE.MeshBasicMaterial({
      color: stats.glowColor,
    });

    // 1. Main Cylindrical Fuselage
    const bodyGeo = new THREE.CylinderGeometry(radius, radius, bodyHeight, 28);
    const bodyMesh = new THREE.Mesh(bodyGeo, this.bodyMaterial);
    bodyMesh.position.y = bodyHeight / 2;
    bodyMesh.castShadow = true;
    bodyMesh.receiveShadow = true;
    this.mesh.add(bodyMesh);

    // Accent Stripe rings with panel seams
    const stripeGeo = new THREE.CylinderGeometry(radius * 1.015, radius * 1.015, 0.14, 28);
    const stripe1 = new THREE.Mesh(stripeGeo, this.accentMaterial);
    stripe1.position.y = bodyHeight * 0.78;
    this.mesh.add(stripe1);

    const stripe2 = new THREE.Mesh(stripeGeo, glowMaterial);
    stripe2.position.y = bodyHeight * 0.32;
    this.mesh.add(stripe2);

    // 2. Aerodynamic Nosecone
    const noseGeo = new THREE.ConeGeometry(radius, noseHeight, 28);
    const noseMesh = new THREE.Mesh(noseGeo, this.bodyMaterial);
    noseMesh.position.y = bodyHeight + noseHeight / 2;
    noseMesh.castShadow = true;
    this.mesh.add(noseMesh);

    // Thermal Needle Tip
    const tipGeo = new THREE.SphereGeometry(radius * 0.25, 16, 16);
    const tipMesh = new THREE.Mesh(tipGeo, this.accentMaterial);
    tipMesh.position.y = bodyHeight + noseHeight;
    this.mesh.add(tipMesh);

    // 3. 4 Swept Delta Titanium Fins
    const finShape = new THREE.Shape();
    finShape.moveTo(0, 0);
    finShape.lineTo(0.65, -0.35);
    finShape.lineTo(0.45, 0.85);
    finShape.lineTo(0, 0.75);
    finShape.closePath();

    const extrudeSettings = { depth: 0.05, bevelEnabled: true, bevelThickness: 0.02, bevelSize: 0.02 };
    const finGeo = new THREE.ExtrudeGeometry(finShape, extrudeSettings);

    for (let i = 0; i < 4; i++) {
      const angle = (i * Math.PI) / 2;
      const finMesh = new THREE.Mesh(finGeo, this.accentMaterial);
      finMesh.position.set(0, 0.2, 0);
      finMesh.rotation.y = angle;
      finMesh.translateOnAxis(new THREE.Vector3(1, 0, 0), radius * 0.88);
      finMesh.castShadow = true;
      this.mesh.add(finMesh);
    }

    // 4. Machined Titanium Engine Bell Nozzle
    const nozzleRadius = radius * (isHeavy ? 1.15 : 0.95);
    const nozzleGeo = new THREE.CylinderGeometry(radius * 0.55, nozzleRadius, 0.65, 24, 1, true);
    const nozzleMat = new THREE.MeshPhysicalMaterial({
      color: 0x1e293b,
      metalness: 0.95,
      roughness: 0.18,
      side: THREE.DoubleSide,
    });
    const nozzleMesh = new THREE.Mesh(nozzleGeo, nozzleMat);
    nozzleMesh.position.y = -0.25;
    nozzleMesh.castShadow = true;
    this.mesh.add(nozzleMesh);

    // 5. Plasma Chamber & Dynamic Exhaust PointLight
    const glowGeo = new THREE.SphereGeometry(radius * 0.5, 16, 16);
    this.engineGlowMesh = new THREE.Mesh(glowGeo, glowMaterial);
    this.engineGlowMesh.position.y = -0.22;
    this.mesh.add(this.engineGlowMesh);

    this.engineLight = new THREE.PointLight(stats.glowColor, 0, 24);
    this.engineLight.position.y = -0.55;
    this.mesh.add(this.engineLight);
  }

  public setEngineBurning(isBurning: boolean, intensity = 1.0): void {
    if (isBurning) {
      this.engineGlowMesh.visible = true;
      this.engineLight.intensity = (4.0 + Math.random() * 2.5) * intensity;
      this.engineGlowMesh.scale.setScalar(1.0 + Math.random() * 0.35);
    } else {
      this.engineGlowMesh.visible = false;
      this.engineLight.intensity = 0;
    }
  }

  public getStats(): RocketStats {
    return this.currentStats;
  }
}
