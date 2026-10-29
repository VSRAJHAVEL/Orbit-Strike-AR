import * as THREE from 'three';

export interface ARPlacementResult {
  position: THREE.Vector3;
  rotation: THREE.Quaternion;
}

export class ARManager {
  private renderer: THREE.WebGLRenderer;
  private scene: THREE.Scene;
  private reticle: THREE.Mesh;
  private isARSupported = false;
  private xrSession: any = null;
  private xrRefSpace: any = null;
  private xrHitTestSource: any = null;
  private webcamVideo: HTMLVideoElement | null = null;
  private webcamTexture: THREE.VideoTexture | null = null;
  private isWebcamActive = false;
  private onPlaceCallback: ((pose: ARPlacementResult) => void) | null = null;

  constructor(renderer: THREE.WebGLRenderer, scene: THREE.Scene) {
    this.renderer = renderer;
    this.scene = scene;

    // Holographic Cyan Ground Placement Reticle
    const reticleGeo = new THREE.RingGeometry(0.35, 0.45, 32);
    reticleGeo.rotateX(-Math.PI / 2);
    const reticleMat = new THREE.MeshBasicMaterial({
      color: 0x00f0ff,
      side: THREE.DoubleSide,
      transparent: true,
      opacity: 0.85,
    });
    this.reticle = new THREE.Mesh(reticleGeo, reticleMat);
    this.reticle.visible = false;
    this.scene.add(this.reticle);

    this.checkWebXRSupport();
  }

  private async checkWebXRSupport(): Promise<void> {
    if ('xr' in navigator && (navigator as any).xr?.isSessionSupported) {
      try {
        this.isARSupported = await (navigator as any).xr.isSessionSupported('immersive-ar');
      } catch {
        this.isARSupported = false;
      }
    }
  }

  public async startAR(onPlace: (pose: ARPlacementResult) => void): Promise<boolean> {
    this.onPlaceCallback = onPlace;

    // 1. If WebXR immersive-ar supported, launch phone AR
    if (this.isARSupported && (navigator as any).xr) {
      try {
        const session = await (navigator as any).xr.requestSession('immersive-ar', {
          requiredFeatures: ['hit-test'],
          optionalFeatures: ['dom-overlay', 'light-estimation'],
          domOverlay: { root: document.getElementById('hud-root') || document.body },
        });

        this.xrSession = session;
        await this.renderer.xr.setSession(session);

        const refSpace = await session.requestReferenceSpace('viewer');
        this.xrHitTestSource = await session.requestHitTestSource({ space: refSpace });
        this.xrRefSpace = await session.requestReferenceSpace('local');

        session.addEventListener('select', this.onXRSelect);
        session.addEventListener('end', this.onXREnd);

        this.reticle.visible = true;
        return true;
      } catch (err) {
        console.warn('[ARManager] WebXR session request failed, falling back to webcam AR:', err);
      }
    }

    // 2. Fallback: Laptop / Desktop Webcam Passthrough AR
    return await this.startWebcamFallback();
  }

  private async startWebcamFallback(): Promise<boolean> {
    try {
      const stream = await navigator.mediaDevices.getUserMedia({
        video: { facingMode: 'environment', width: { ideal: 1280 }, height: { ideal: 720 } },
        audio: false,
      });

      this.webcamVideo = document.createElement('video');
      this.webcamVideo.autoplay = true;
      this.webcamVideo.playsInline = true;
      this.webcamVideo.srcObject = stream;
      await this.webcamVideo.play();

      this.webcamTexture = new THREE.VideoTexture(this.webcamVideo);
      this.webcamTexture.colorSpace = THREE.SRGBColorSpace;
      this.scene.background = this.webcamTexture;
      this.isWebcamActive = true;

      // Position reticle on virtual desktop plane
      this.reticle.visible = true;
      this.reticle.position.set(0, 0.22, -3.5);

      window.addEventListener('click', this.onWebcamClick);
      return true;
    } catch (err) {
      console.warn('[ARManager] Webcam access unavailable:', err);
      return false;
    }
  }

  private onXRSelect = () => {
    if (this.reticle.visible && this.onPlaceCallback) {
      const pose: ARPlacementResult = {
        position: this.reticle.position.clone(),
        rotation: this.reticle.quaternion.clone(),
      };
      this.onPlaceCallback(pose);
      this.reticle.visible = false;
    }
  };

  private onWebcamClick = (e: MouseEvent) => {
    // Check if clicked outside UI
    const target = e.target as HTMLElement;
    if (target && target.closest('.glass-panel')) return;

    if (this.reticle.visible && this.onPlaceCallback) {
      const pose: ARPlacementResult = {
        position: this.reticle.position.clone(),
        rotation: this.reticle.quaternion.clone(),
      };
      this.onPlaceCallback(pose);
      this.reticle.visible = false;
      window.removeEventListener('click', this.onWebcamClick);
    }
  };

  public update(frame?: any): void {
    if (this.xrHitTestSource && frame && this.xrRefSpace) {
      const hitTestResults = frame.getHitTestResults(this.xrHitTestSource);
      if (hitTestResults.length > 0) {
        const hit = hitTestResults[0];
        const pose = hit.getPose(this.xrRefSpace);
        if (pose) {
          this.reticle.visible = true;
          this.reticle.matrix.fromArray(pose.transform.matrix);
          this.reticle.matrix.decompose(this.reticle.position, this.reticle.quaternion, this.reticle.scale);
        }
      } else {
        this.reticle.visible = false;
      }
    } else if (this.isWebcamActive) {
      // Rotate reticle gently on desk plane
      this.reticle.rotation.y += 0.02;
    }
  }

  private onXREnd = () => {
    this.xrSession = null;
    this.xrHitTestSource = null;
    this.reticle.visible = false;
  };

  public stopAR(): void {
    if (this.xrSession) {
      this.xrSession.end();
    }
    if (this.webcamVideo && this.webcamVideo.srcObject) {
      const tracks = (this.webcamVideo.srcObject as MediaStream).getTracks();
      tracks.forEach((t) => t.stop());
      this.webcamVideo = null;
      this.scene.background = null;
      this.isWebcamActive = false;
    }
    window.removeEventListener('click', this.onWebcamClick);
    this.reticle.visible = false;
  }

  public isWebAR(): boolean {
    return this.isARSupported;
  }
}
