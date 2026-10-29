import * as THREE from 'three';
import { ChapterConfig } from './ChapterConfig';

export class TerrainGenerator {
  public group: THREE.Group;
  private terrainMesh: THREE.Mesh;
  private terrainMaterial: THREE.MeshPhysicalMaterial;
  private padMesh: THREE.Mesh;
  private rocksGroup: THREE.Group;
  private dustParticles: THREE.Points;
  private dustGeo: THREE.BufferGeometry;
  private dustVelocities: Float32Array;

  constructor(scene: THREE.Scene, initialConfig: ChapterConfig) {
    this.group = new THREE.Group();
    scene.add(this.group);

    // 1. Procedural PBR Landscape with Dunes / Topography
    const size = 320;
    const segments = 90;
    const terrainGeo = new THREE.PlaneGeometry(size, size, segments, segments);
    terrainGeo.rotateX(-Math.PI / 2);

    const posAttr = terrainGeo.attributes.position;
    for (let i = 0; i < posAttr.count; i++) {
      const x = posAttr.getX(i);
      const z = posAttr.getZ(i);
      const distFromCenter = Math.sqrt(x * x + z * z);

      // Keep center smooth for launchpad, roll into dunes further out
      const centerFactor = Math.min(1.0, Math.max(0.0, (distFromCenter - 14) / 35));
      const duneWave = (Math.sin(x * 0.04 + z * 0.03) * 2.2 + Math.cos(x * 0.08 - z * 0.06) * 1.4) * centerFactor;
      posAttr.setY(i, duneWave);
    }
    terrainGeo.computeVertexNormals();

    this.terrainMaterial = new THREE.MeshPhysicalMaterial({
      color: initialConfig.terrainColor,
      roughness: initialConfig.terrainRoughness,
      metalness: initialConfig.terrainMetalness,
      clearcoat: 0.05,
      clearcoatRoughness: 0.8,
      flatShading: false,
    });

    this.terrainMesh = new THREE.Mesh(terrainGeo, this.terrainMaterial);
    this.terrainMesh.receiveShadow = true;
    this.group.add(this.terrainMesh);

    // 2. Spaceport Concrete Apron & Blast Deflector Trench
    const padGeo = new THREE.CylinderGeometry(8.5, 9.2, 0.45, 32);
    const padMat = new THREE.MeshPhysicalMaterial({
      color: 0x22262d,
      roughness: 0.65,
      metalness: 0.35,
      clearcoat: 0.1,
    });
    this.padMesh = new THREE.Mesh(padGeo, padMat);
    this.padMesh.position.set(0, 0.2, 0);
    this.padMesh.receiveShadow = true;
    this.padMesh.castShadow = true;
    this.group.add(this.padMesh);

    // Hazard Stripes Ring around pad perimeter
    const ringGeo = new THREE.RingGeometry(8.3, 8.8, 32);
    ringGeo.rotateX(-Math.PI / 2);
    const ringMat = new THREE.MeshBasicMaterial({
      color: 0xffaa00,
      side: THREE.DoubleSide,
    });
    const ringMesh = new THREE.Mesh(ringGeo, ringMat);
    ringMesh.position.set(0, 0.43, 0);
    this.group.add(ringMesh);

    // Embedded Runway Blue/Cyan Edge Beacon Lights
    const beaconCount = 12;
    const beaconGeo = new THREE.CylinderGeometry(0.1, 0.1, 0.25, 8);
    const beaconMat = new THREE.MeshBasicMaterial({ color: 0x00f0ff });
    for (let i = 0; i < beaconCount; i++) {
      const angle = (i / beaconCount) * Math.PI * 2;
      const r = 8.1;
      const bMesh = new THREE.Mesh(beaconGeo, beaconMat);
      bMesh.position.set(Math.cos(angle) * r, 0.45, Math.sin(angle) * r);
      this.group.add(bMesh);
    }

    // 3. Natural Faceted Rock Formations
    this.rocksGroup = new THREE.Group();
    const rockGeo = new THREE.DodecahedronGeometry(1.0, 1);
    const rockMat = new THREE.MeshPhysicalMaterial({
      color: 0x735a47,
      roughness: 0.95,
      metalness: 0.05,
      flatShading: true,
    });

    for (let i = 0; i < 28; i++) {
      const angle = (i / 28) * Math.PI * 2 + (Math.random() - 0.5) * 0.4;
      const dist = 22 + Math.random() * 45;
      const scaleX = 1.2 + Math.random() * 2.8;
      const scaleY = 0.8 + Math.random() * 2.4;
      const scaleZ = 1.2 + Math.random() * 2.8;

      const rock = new THREE.Mesh(rockGeo, rockMat);
      rock.position.set(Math.cos(angle) * dist, scaleY * 0.45, Math.sin(angle) * dist);
      rock.scale.set(scaleX, scaleY, scaleZ);
      rock.rotation.set(Math.random() * Math.PI, Math.random() * Math.PI, Math.random() * Math.PI);
      rock.castShadow = true;
      rock.receiveShadow = true;
      this.rocksGroup.add(rock);
    }
    this.group.add(this.rocksGroup);

    // 4. Volumetric Ground Dust / Sand Particles
    const dustCount = 450;
    this.dustGeo = new THREE.BufferGeometry();
    const dustPositions = new Float32Array(dustCount * 3);
    this.dustVelocities = new Float32Array(dustCount * 3);

    for (let i = 0; i < dustCount; i++) {
      dustPositions[i * 3] = (Math.random() - 0.5) * 70;
      dustPositions[i * 3 + 1] = Math.random() * 4.5 + 0.1;
      dustPositions[i * 3 + 2] = (Math.random() - 0.5) * 70;

      this.dustVelocities[i * 3] = Math.random() * 1.5 + 1.0; // drift +X
      this.dustVelocities[i * 3 + 1] = (Math.random() - 0.5) * 0.2;
      this.dustVelocities[i * 3 + 2] = (Math.random() - 0.5) * 0.5;
    }
    this.dustGeo.setAttribute('position', new THREE.BufferAttribute(dustPositions, 3));

    const dustMat = new THREE.PointsMaterial({
      color: 0xffd29d,
      size: 0.28,
      transparent: true,
      opacity: 0.35,
      blending: THREE.AdditiveBlending,
      sizeAttenuation: true,
    });
    this.dustParticles = new THREE.Points(this.dustGeo, dustMat);
    this.group.add(this.dustParticles);
  }

  public getTerrainHeight(x: number, z: number): number {
    const distFromCenter = Math.sqrt(x * x + z * z);
    const centerFactor = Math.min(1.0, Math.max(0.0, (distFromCenter - 14) / 35));
    return (Math.sin(x * 0.04 + z * 0.03) * 2.2 + Math.cos(x * 0.08 - z * 0.06) * 1.4) * centerFactor;
  }

  public update(delta: number): void {
    // Animate drifting dust particles across terrain
    const pos = this.dustGeo.attributes.position as THREE.BufferAttribute;
    const array = pos.array as Float32Array;

    for (let i = 0; i < array.length / 3; i++) {
      array[i * 3] += this.dustVelocities[i * 3] * delta;
      array[i * 3 + 1] += this.dustVelocities[i * 3 + 1] * delta;
      array[i * 3 + 2] += this.dustVelocities[i * 3 + 2] * delta;

      // Wrap around boundary
      if (array[i * 3] > 35) array[i * 3] = -35;
      if (array[i * 3 + 1] > 5) array[i * 3 + 1] = 0.2;
      if (array[i * 3 + 1] < 0.1) array[i * 3 + 1] = 3.5;
    }
    pos.needsUpdate = true;
  }

  public updateForChapter(config: ChapterConfig): void {
    this.terrainMaterial.color.setHex(config.terrainColor);
    this.terrainMaterial.roughness = config.terrainRoughness;
    this.terrainMaterial.metalness = config.terrainMetalness;

    // Adjust dust color and visibility based on environment
    const dustMat = this.dustParticles.material as THREE.PointsMaterial;
    if (config.id === 'moon') {
      dustMat.opacity = 0.0; // No atmospheric dust in lunar vacuum
    } else if (config.id === 'arctic') {
      dustMat.color.setHex(0xe8f2ff); // Snow particles
      dustMat.opacity = 0.55;
    } else if (config.id === 'city') {
      dustMat.color.setHex(0x55bbff); // Mist / rain sheen
      dustMat.opacity = 0.25;
    } else {
      dustMat.color.setHex(0xffd29d); // Desert dust
      dustMat.opacity = 0.4;
    }
  }
}

