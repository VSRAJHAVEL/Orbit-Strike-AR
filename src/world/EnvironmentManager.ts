import * as THREE from 'three';
import { CHAPTERS, ChapterConfig } from './ChapterConfig';

export class EnvironmentManager {
  private scene: THREE.Scene;
  private sunLight: THREE.DirectionalLight;
  private ambientLight: THREE.AmbientLight;
  private hemiLight: THREE.HemisphereLight;
  private skyMesh: THREE.Mesh;
  private skyMaterial: THREE.ShaderMaterial;
  private starsMesh: THREE.Points;
  private currentConfig: ChapterConfig;

  constructor(scene: THREE.Scene) {
    this.scene = scene;
    this.currentConfig = CHAPTERS[0];

    // 1. Primary Directional Sunlight with soft cascaded shadow volume
    this.sunLight = new THREE.DirectionalLight(0xfffaed, 2.8);
    this.sunLight.castShadow = true;
    this.sunLight.shadow.mapSize.width = 2048;
    this.sunLight.shadow.mapSize.height = 2048;
    this.sunLight.shadow.camera.near = 0.5;
    this.sunLight.shadow.camera.far = 250;
    this.sunLight.shadow.camera.left = -30;
    this.sunLight.shadow.camera.right = 30;
    this.sunLight.shadow.camera.top = 30;
    this.sunLight.shadow.camera.bottom = -30;
    this.sunLight.shadow.bias = -0.0004;
    this.sunLight.shadow.normalBias = 0.03;
    this.scene.add(this.sunLight);

    // 2. Ambient & Sky Fill
    this.ambientLight = new THREE.AmbientLight(0x3d3028, 0.7);
    this.scene.add(this.ambientLight);

    this.hemiLight = new THREE.HemisphereLight(0xffeedd, 0x1a2e4a, 0.6);
    this.scene.add(this.hemiLight);

    // 3. Cinematic Atmospheric Sky Dome (Procedural Rayleigh/Mie approximation)
    const skyGeo = new THREE.SphereGeometry(600, 32, 24);
    this.skyMaterial = new THREE.ShaderMaterial({
      side: THREE.BackSide,
      depthWrite: false,
      uniforms: {
        topColor: { value: new THREE.Color(this.currentConfig.skyColors.top) },
        bottomColor: { value: new THREE.Color(this.currentConfig.skyColors.bottom) },
        horizonColor: { value: new THREE.Color(this.currentConfig.skyColors.horizon) },
        sunPosition: { value: new THREE.Vector3(...this.currentConfig.sunDirection).normalize() },
        sunColor: { value: new THREE.Color(this.currentConfig.skyColors.sun) },
      },
      vertexShader: `
        varying vec3 vWorldPosition;
        void main() {
          vec4 worldPos = modelMatrix * vec4(position, 1.0);
          vWorldPosition = worldPos.xyz;
          gl_Position = projectionMatrix * viewMatrix * worldPos;
        }
      `,
      fragmentShader: `
        uniform vec3 topColor;
        uniform vec3 bottomColor;
        uniform vec3 horizonColor;
        uniform vec3 sunPosition;
        uniform vec3 sunColor;
        varying vec3 vWorldPosition;

        void main() {
          vec3 dir = normalize(vWorldPosition);
          float y = dir.y;
          
          // Horizon blending curve
          vec3 skyGrad = mix(horizonColor, topColor, clamp(y * 1.8, 0.0, 1.0));
          if (y < 0.0) {
            skyGrad = mix(horizonColor, bottomColor, clamp(-y * 3.5, 0.0, 1.0));
          }

          // Sun disc & atmospheric Mie glow
          float sunDot = max(dot(dir, sunPosition), 0.0);
          float sunGlow = pow(sunDot, 18.0) * 0.9;
          float sunDisc = smoothstep(0.998, 0.9995, sunDot) * 4.5;

          vec3 finalColor = skyGrad + (sunColor * (sunGlow + sunDisc));
          gl_FragColor = vec4(finalColor, 1.0);
        }
      `,
    });
    this.skyMesh = new THREE.Mesh(skyGeo, this.skyMaterial);
    this.scene.add(this.skyMesh);

    // 4. Procedural Deep-Space Starfield (for Lunar & Night chapters)
    const starsGeo = new THREE.BufferGeometry();
    const starCount = 1800;
    const starPositions = new Float32Array(starCount * 3);
    for (let i = 0; i < starCount; i++) {
      const u = Math.random();
      const v = Math.random();
      const theta = u * 2.0 * Math.PI;
      const phi = Math.acos(2.0 * v - 1.0);
      const r = 550;
      starPositions[i * 3] = r * Math.sin(phi) * Math.cos(theta);
      starPositions[i * 3 + 1] = Math.abs(r * Math.cos(phi)) + 15; // Upper dome
      starPositions[i * 3 + 2] = r * Math.sin(phi) * Math.sin(theta);
    }
    starsGeo.setAttribute('position', new THREE.BufferAttribute(starPositions, 3));
    const starsMat = new THREE.PointsMaterial({
      color: 0xffffff,
      size: 1.8,
      transparent: true,
      opacity: 0.0,
      sizeAttenuation: false,
    });
    this.starsMesh = new THREE.Points(starsGeo, starsMat);
    this.scene.add(this.starsMesh);

    // Apply default chapter
    this.applyChapter(0);
  }

  public applyChapter(index: number): void {
    const config = CHAPTERS[Math.max(0, Math.min(index, CHAPTERS.length - 1))];
    this.currentConfig = config;

    // Sunlight
    this.sunLight.color.setHex(config.skyColors.sun);
    this.sunLight.intensity = config.sunIntensity;
    this.sunLight.position.set(...config.sunDirection);

    // Fill lights
    this.ambientLight.color.setHex(config.ambientColor);
    this.ambientLight.intensity = config.ambientIntensity;
    this.hemiLight.color.setHex(config.skyColors.sun);
    this.hemiLight.groundColor.setHex(config.skyColors.top);

    // Fog
    this.scene.fog = new THREE.FogExp2(config.fogColor, config.fogDensity);

    // Sky shader uniforms
    this.skyMaterial.uniforms.topColor.value.setHex(config.skyColors.top);
    this.skyMaterial.uniforms.bottomColor.value.setHex(config.skyColors.bottom);
    this.skyMaterial.uniforms.horizonColor.value.setHex(config.skyColors.horizon);
    this.skyMaterial.uniforms.sunPosition.value.set(...config.sunDirection).normalize();
    this.skyMaterial.uniforms.sunColor.value.setHex(config.skyColors.sun);

    // Stars visibility (only visible in Night City & Lunar Vacuum)
    const isNightOrSpace = config.id === 'city' || config.id === 'moon';
    (this.starsMesh.material as THREE.PointsMaterial).opacity = isNightOrSpace ? (config.id === 'moon' ? 0.95 : 0.45) : 0.0;
  }

  public getCurrentConfig(): ChapterConfig {
    return this.currentConfig;
  }
}

