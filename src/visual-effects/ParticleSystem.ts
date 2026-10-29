import * as THREE from 'three';

interface Particle {
  pos: THREE.Vector3;
  vel: THREE.Vector3;
  life: number;
  maxLife: number;
  size: number;
  color: THREE.Color;
  type: 'flame' | 'smoke' | 'spark' | 'debris';
}

export class ParticleSystem {
  public group: THREE.Group;
  private particles: Particle[] = [];
  private pointsMesh: THREE.Points;
  private geo: THREE.BufferGeometry;
  private maxCount = 1200;
  private positions: Float32Array;
  private colors: Float32Array;
  private sizes: Float32Array;

  // Shockwave meshes pool
  private shockwaves: { mesh: THREE.Mesh; scale: number; maxScale: number; alpha: number }[] = [];

  constructor(scene: THREE.Scene) {
    this.group = new THREE.Group();
    scene.add(this.group);

    this.positions = new Float32Array(this.maxCount * 3);
    this.colors = new Float32Array(this.maxCount * 3);
    this.sizes = new Float32Array(this.maxCount);

    this.geo = new THREE.BufferGeometry();
    this.geo.setAttribute('position', new THREE.BufferAttribute(this.positions, 3));
    this.geo.setAttribute('color', new THREE.BufferAttribute(this.colors, 3));
    this.geo.setAttribute('size', new THREE.BufferAttribute(this.sizes, 1));

    // Custom shader material for fast GPU particle rendering
    const mat = new THREE.ShaderMaterial({
      transparent: true,
      depthWrite: false,
      blending: THREE.AdditiveBlending,
      vertexShader: `
        attribute float size;
        attribute vec3 color;
        varying vec3 vColor;
        void main() {
          vColor = color;
          vec4 mvPosition = viewMatrix * modelMatrix * vec4(position, 1.0);
          gl_PointSize = size * (320.0 / -mvPosition.z);
          gl_Position = projectionMatrix * mvPosition;
        }
      `,
      fragmentShader: `
        varying vec3 vColor;
        void main() {
          vec2 coord = gl_PointCoord - vec2(0.5);
          float dist = length(coord);
          if (dist > 0.5) discard;
          float alpha = smoothstep(0.5, 0.05, dist);
          gl_FragColor = vec4(vColor, alpha);
        }
      `,
    });

    this.pointsMesh = new THREE.Points(this.geo, mat);
    this.group.add(this.pointsMesh);
  }

  public emitExhaust(origin: THREE.Vector3, dir: THREE.Vector3, glowColorHex: number, count = 5): void {
    const oppDir = dir.clone().multiplyScalar(-1);

    for (let i = 0; i < count; i++) {
      if (this.particles.length >= this.maxCount) break;

      const spread = new THREE.Vector3(
        (Math.random() - 0.5) * 0.35,
        (Math.random() - 0.5) * 0.35,
        (Math.random() - 0.5) * 0.35
      );
      const vel = oppDir.clone().add(spread).multiplyScalar(8 + Math.random() * 12);

      const isFlame = Math.random() > 0.35;
      const col = isFlame ? new THREE.Color(glowColorHex) : new THREE.Color(0x71717a);

      this.particles.push({
        pos: origin.clone(),
        vel,
        life: 0,
        maxLife: isFlame ? 0.35 + Math.random() * 0.25 : 1.2 + Math.random() * 0.8,
        size: isFlame ? 0.35 : 0.85,
        color: col,
        type: isFlame ? 'flame' : 'smoke',
      });
    }
  }

  public triggerExplosion(pos: THREE.Vector3, isBoss = false): void {
    const count = isBoss ? 260 : 140;

    // 1. Particle fireball & debris
    for (let i = 0; i < count; i++) {
      if (this.particles.length >= this.maxCount) break;

      const dir = new THREE.Vector3(
        (Math.random() - 0.5) * 2,
        Math.random() * 1.8 + 0.2, // upward blast bias
        (Math.random() - 0.5) * 2
      ).normalize();

      const speed = 6 + Math.random() * (isBoss ? 28 : 16);
      const isDebris = Math.random() > 0.6;

      this.particles.push({
        pos: pos.clone(),
        vel: dir.multiplyScalar(speed),
        life: 0,
        maxLife: isDebris ? 1.6 + Math.random() * 1.2 : 0.65 + Math.random() * 0.45,
        size: isDebris ? 0.45 : 1.4,
        color: isDebris
          ? new THREE.Color(0xffaa22)
          : Math.random() > 0.4
          ? new THREE.Color(0xff4400)
          : new THREE.Color(0xffcc00),
        type: isDebris ? 'debris' : 'flame',
      });
    }

    // 2. Shockwave expanding ring along ground
    const ringGeo = new THREE.RingGeometry(0.1, 0.45, 32);
    ringGeo.rotateX(-Math.PI / 2);
    const ringMat = new THREE.MeshBasicMaterial({
      color: 0x00f0ff,
      side: THREE.DoubleSide,
      transparent: true,
      opacity: 0.95,
    });
    const shockMesh = new THREE.Mesh(ringGeo, ringMat);
    shockMesh.position.set(pos.x, 0.23, pos.z);
    this.group.add(shockMesh);

    this.shockwaves.push({
      mesh: shockMesh,
      scale: 1.0,
      maxScale: isBoss ? 26 : 14,
      alpha: 1.0,
    });
  }

  public update(delta: number, windVec = new THREE.Vector3(0, 0, 0)): void {
    // 1. Update particles
    for (let i = this.particles.length - 1; i >= 0; i--) {
      const p = this.particles[i];
      p.life += delta;

      if (p.life >= p.maxLife) {
        this.particles.splice(i, 1);
        continue;
      }

      // Physics
      p.pos.addScaledVector(p.vel, delta);

      if (p.type === 'debris') {
        p.vel.y -= 14.0 * delta; // Gravity on debris
        if (p.pos.y <= 0.22) {
          p.pos.y = 0.22;
          p.vel.set(0, 0, 0); // Landed
        }
      } else if (p.type === 'smoke') {
        p.vel.addScaledVector(windVec, delta * 1.5);
        p.vel.y += 0.8 * delta; // smoke rises
        p.size += delta * 1.2; // billows out
      }

      // Fade color over life
      const pct = p.life / p.maxLife;
      if (p.type === 'flame') {
        p.color.lerp(new THREE.Color(0x331100), pct * 0.4);
      }
    }

    // 2. Update buffer attributes
    const activeCount = Math.min(this.particles.length, this.maxCount);
    for (let i = 0; i < activeCount; i++) {
      const p = this.particles[i];
      this.positions[i * 3] = p.pos.x;
      this.positions[i * 3 + 1] = p.pos.y;
      this.positions[i * 3 + 2] = p.pos.z;

      this.colors[i * 3] = p.color.r;
      this.colors[i * 3 + 1] = p.color.g;
      this.colors[i * 3 + 2] = p.color.b;

      this.sizes[i] = p.size;
    }

    this.geo.setDrawRange(0, activeCount);
    (this.geo.attributes.position as THREE.BufferAttribute).needsUpdate = true;
    (this.geo.attributes.color as THREE.BufferAttribute).needsUpdate = true;
    (this.geo.attributes.size as THREE.BufferAttribute).needsUpdate = true;

    // 3. Update shockwaves
    for (let i = this.shockwaves.length - 1; i >= 0; i--) {
      const sw = this.shockwaves[i];
      sw.scale += delta * 24.0;
      sw.alpha -= delta * 1.8;

      sw.mesh.scale.set(sw.scale, sw.scale, sw.scale);
      (sw.mesh.material as THREE.MeshBasicMaterial).opacity = Math.max(0, sw.alpha);

      if (sw.alpha <= 0 || sw.scale >= sw.maxScale) {
        this.group.remove(sw.mesh);
        sw.mesh.geometry.dispose();
        this.shockwaves.splice(i, 1);
      }
    }
  }

  public clear(): void {
    this.particles = [];
    this.geo.setDrawRange(0, 0);
    (this.geo.attributes.position as THREE.BufferAttribute).needsUpdate = true;
  }
}

