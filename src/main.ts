import * as THREE from 'three';
import { Engine } from './core/Engine';
import { PhysicsWorld } from './physics/PhysicsWorld';
import { EnvironmentManager } from './world/EnvironmentManager';
import { TerrainGenerator } from './world/TerrainGenerator';
import { LaunchPad } from './rocket/LaunchPad';
import { RocketModel } from './rocket/RocketModel';
import { TrajectorySimulator } from './physics/TrajectorySimulator';
import { ParticleSystem } from './visual-effects/ParticleSystem';
import { SoundManager } from './audio/SoundManager';
import { TargetManager } from './targets/TargetManager';
import { ARManager } from './ar/ARManager';
import { CameraController } from './camera/CameraController';
import { TacticalCommandHUD } from './ui/TacticalCommandHUD';
import { gameStore } from './core/GameState';
import { ROCKET_ARCHETYPES, RocketStats } from './rocket/RocketStats';
import { CHAPTERS } from './world/ChapterConfig';
import missionsData from './data/missions.json';

async function bootstrap(): Promise<void> {
  // 1. Core Three.js Engine & Renderer
  const engine = new Engine('canvas-container');

  // 2. Rapier 3D Physics World
  const physics = new PhysicsWorld();
  await physics.init(CHAPTERS[0].gravity);

  // 3. Procedural Audio Synthesizer
  const sound = new SoundManager();

  // 4. Atmospheric Sky & Lighting
  const envManager = new EnvironmentManager(engine.scene);

  // 5. Procedural Dunes Landscape & Launch Apron
  const terrain = new TerrainGenerator(engine.scene, envManager.getCurrentConfig());

  // 6. Launch Pad & Rocket Assembly
  const launchPad = new LaunchPad(engine.scene);
  launchPad.setPosition(new THREE.Vector3(0, 0.45, 0));

  let currentStats: RocketStats = ROCKET_ARCHETYPES.viper_basic;
  const rocket = new RocketModel(currentStats);
  launchPad.getMountPoint().add(rocket.mesh);

  // 7. Live Predictive Trajectory Line & GPU Particle System
  const trajectory = new TrajectorySimulator(engine.scene);
  const particles = new ParticleSystem(engine.scene);

  // 8. Multi-Tier Target System
  const targetManager = new TargetManager(engine.scene, physics);

  // 9. Camera Controller (Orbit + WASD)
  const cameraController = new CameraController(engine.camera, engine.renderer.domElement);
  const arManager = new ARManager(engine.renderer, engine.scene);

  // Flight State
  let isFlightActive = false;
  let isCountingDown = false;
  let currentWind = new THREE.Vector3(1.5, 0, 0.5);

  // Helper to load mission
  const loadMission = (missionIdx: number) => {
    const mission = missionsData[missionIdx] || missionsData[0];
    gameStore.getState().setMission(missionIdx);
    gameStore.getState().setChapter(mission.chapter);

    // Apply Chapter Environment
    envManager.applyChapter(mission.chapter);
    const chapterConfig = envManager.getCurrentConfig();
    terrain.updateForChapter(chapterConfig);
    physics.setGravity(chapterConfig.gravity);

    // Setup Wind Vector
    const windSpeed = (mission.wind[0] + mission.wind[1]) * 0.5;
    currentWind.set(windSpeed * 0.8, 0, (Math.random() - 0.5) * windSpeed * 0.4);
    physics.setWind(currentWind);

    // Spawn Mission Target placed firmly on top of terrain
    const targetZ = -mission.distance;
    const targetX = (Math.random() - 0.5) * (mission.distance * 0.18);
    const groundElevation = terrain.getTerrainHeight(targetX, targetZ);
    const targetY = (mission.targetTier === 'drone' ? 8.5 : mission.targetTier === 'boss' ? 5.2 : 0.2) + groundElevation;

    targetManager.spawnTarget(mission.targetTier as any, new THREE.Vector3(targetX, targetY, targetZ));

    // Reset Rocket to Launch Pad
    resetRocketToPad();
    updateLiveTrajectory();
  };

  // Helper to reset Rocket onto Pad
  const resetRocketToPad = () => {
    isFlightActive = false;
    isCountingDown = false;
    physics.despawnRocket();
    sound.stopEngineRumble();
    rocket.setEngineBurning(false);

    // Reattach rocket mesh to launchpad rail mount
    if (rocket.mesh.parent !== launchPad.getMountPoint()) {
      engine.scene.remove(rocket.mesh);
      launchPad.getMountPoint().add(rocket.mesh);
      rocket.mesh.position.set(0, 0, 0);
      rocket.mesh.rotation.set(0, 0, 0);
    }

    trajectory.setVisible(true);
    cameraController.controls.enabled = true;
    cameraController.controls.target.set(0, 2.2, -4.0);
    engine.camera.position.set(-3.8, 2.8, 6.2);
    engine.camera.lookAt(0, 2.2, -4.0);
  };

  // Recalculate predictive trajectory arc
  const updateLiveTrajectory = () => {
    if (isFlightActive) return;

    const state = gameStore.getState();
    launchPad.setAim(state.pitchAngleDeg, state.yawAngleDeg);

    const { position, direction } = launchPad.getLaunchTransform();
    const chapterConfig = envManager.getCurrentConfig();

    trajectory.updateTrajectory(
      position,
      direction,
      state.forcePercent * 0.24,
      currentStats,
      chapterConfig.gravity,
      currentWind
    );
  };

  // Trigger Launch Sequence
  const triggerLaunch = () => {
    if (isFlightActive || isCountingDown) return;

    isCountingDown = true;
    trajectory.setVisible(false);

    let count = 3;
    hud.showCountdown(count);
    sound.playCountdown(count);

    const timer = setInterval(() => {
      count--;
      if (count > 0) {
        hud.showCountdown(count);
        sound.playCountdown(count);
      } else {
        clearInterval(timer);
        hud.showCountdown(0);
        sound.playCountdown(0);
        executeIgnition();
      }
    }, 600);
  };

  const executeIgnition = () => {
    isCountingDown = false;
    isFlightActive = true;

    const state = gameStore.getState();
    const { position, direction } = launchPad.getLaunchTransform();

    // Detach rocket from pad mount into world scene coordinates
    launchPad.getMountPoint().remove(rocket.mesh);
    engine.scene.add(rocket.mesh);
    rocket.mesh.position.copy(position);

    // Spawn Rapier physics dynamic body
    const forceMag = state.forcePercent * 0.24;
    physics.spawnRocket(position, direction, forceMag, currentStats);

    // Audio & Visual Effects
    sound.startEngineRumble();
    rocket.setEngineBurning(true);
    particles.emitExhaust(position, direction, currentStats.glowColor, 50);
  };

  // 10. Connect HUD Callbacks
  const hud = new TacticalCommandHUD('hud-root', {
    onRocketSelect: (newStats: RocketStats) => {
      currentStats = newStats;
      rocket.setArchetype(newStats);
      updateLiveTrajectory();
      sound.playBeep(980, 0.08);
    },
    onAimChange: () => {
      updateLiveTrajectory();
    },
    onLaunch: () => {
      triggerLaunch();
    },
    onReset: () => {
      resetRocketToPad();
      sound.playBeep(640, 0.1);
    },
    onARStart: async () => {
      const success = await arManager.startAR((pose) => {
        launchPad.setPosition(pose.position);
        updateLiveTrajectory();
        sound.playBeep(1046, 0.15);
      });
      if (success) {
        hud.showCombatText('AR RETICLE ACTIVE: TAP TO PLACE PAD');
      }
    },
    onMissionSelect: (idx) => {
      loadMission(idx);
      sound.playBeep(1100, 0.1);
    },
    onChapterSelect: (idx) => {
      envManager.applyChapter(idx);
      const ch = envManager.getCurrentConfig();
      terrain.updateForChapter(ch);
      physics.setGravity(ch.gravity);
      updateLiveTrajectory();
    },
  });

  // Touch & Mouse Drag-to-Aim on Viewport
  let isDraggingAim = false;
  let lastPointerX = 0;
  let lastPointerY = 0;

  window.addEventListener('pointerdown', (e) => {
    const target = e.target as HTMLElement;
    if (target.closest('.glass-panel') || target.closest('button') || target.closest('input')) return;
    if (e.button === 0 && !isFlightActive) {
      isDraggingAim = true;
      lastPointerX = e.clientX;
      lastPointerY = e.clientY;
    }
  });

  window.addEventListener('pointermove', (e) => {
    if (!isDraggingAim || isFlightActive) return;

    const deltaX = e.clientX - lastPointerX;
    const deltaY = e.clientY - lastPointerY;
    lastPointerX = e.clientX;
    lastPointerY = e.clientY;

    const state = gameStore.getState();
    const newYaw = Math.max(-55, Math.min(55, state.yawAngleDeg + deltaX * 0.2));
    const newPitch = Math.max(15, Math.min(85, state.pitchAngleDeg - deltaY * 0.2));

    gameStore.getState().setAim(newPitch, newYaw, state.forcePercent);
    updateLiveTrajectory();

    // Sync sliders
    const pitchSlider = document.querySelector<HTMLInputElement>('#slider-pitch');
    const yawSlider = document.querySelector<HTMLInputElement>('#slider-yaw');
    const pitchLabel = document.querySelector('#label-pitch');
    const yawLabel = document.querySelector('#label-yaw');

    if (pitchSlider) pitchSlider.value = `${Math.round(newPitch)}`;
    if (yawSlider) yawSlider.value = `${Math.round(newYaw)}`;
    if (pitchLabel) pitchLabel.textContent = `${Math.round(newPitch)}°`;
    if (yawLabel) yawLabel.textContent = `${Math.round(newYaw)}°`;
  });

  window.addEventListener('pointerup', () => {
    isDraggingAim = false;
  });

  // Load Mission 1 initially
  loadMission(0);

  // 11. Master Tick Loop
  engine.addTickCallback((delta, elapsed) => {
    // 1. Advance Physics World
    physics.step(delta);

    // 2. Animate Environmental Elements & Targets
    terrain.update(delta);
    targetManager.update(delta, elapsed, engine.camera.position);
    particles.update(delta, currentWind);
    arManager.update();

    // 3. Rocket In-Flight Tracking & Collision Handling
    if (isFlightActive) {
      const flightState = physics.getRocketState();

      if (flightState) {
        // Sync Three.js rocket mesh with Rapier rigid body
        rocket.mesh.position.copy(flightState.position);
        rocket.mesh.quaternion.copy(flightState.quaternion);

        // Rocket exhaust plume while motor burns
        if (flightState.isBurning) {
          const forward = new THREE.Vector3(0, 1, 0).applyQuaternion(flightState.quaternion);
          const exhaustOrigin = flightState.position.clone().addScaledVector(forward, -0.4);
          particles.emitExhaust(exhaustOrigin, forward, currentStats.glowColor, 10);
          rocket.setEngineBurning(true, flightState.fuelRemainingPct);
        } else {
          rocket.setEngineBurning(false);
          sound.stopEngineRumble();
        }

        // Camera Follow Mode: smooth camera chase behind rocket
        const rocketPos = flightState.position;
        const forward = flightState.velocity.clone().normalize();
        const desiredCamPos = rocketPos.clone().addScaledVector(forward, -8.0).add(new THREE.Vector3(0, 3.2, 0));

        engine.camera.position.lerp(desiredCamPos, Math.min(1.0, delta * 5.0));
        engine.camera.lookAt(rocketPos.clone().addScaledVector(forward, 5.0));
        cameraController.controls.enabled = false;

        // Check for collision event
        if (flightState.hasCollided && flightState.collisionPoint) {
          isFlightActive = false;
          sound.stopEngineRumble();
          rocket.setEngineBurning(false);

          const hitPoint = flightState.collisionPoint;
          const target = targetManager.getCurrentTarget();
          const isBoss = target?.tier === 'boss';

          // Trigger explosion VFX & audio
          particles.triggerExplosion(hitPoint, isBoss);
          sound.playExplosion(isBoss);

          // Calculate Damage & Scoring
          if (flightState.collisionTargetId && flightState.collisionTargetId.startsWith('target_')) {
            const hitResult = targetManager.registerDamage(hitPoint, currentStats.directDamage);

            sound.playTargetHit(hitResult.isBullseye);
            hud.showCombatText(hitResult.hitLabel, hitResult.isBullseye);

            gameStore.getState().addScore(hitResult.points);
            gameStore.getState().addCoins(Math.round(hitResult.points * 0.4));
            hud.updateStats(gameStore.getState().totalScore, gameStore.getState().coins);

            // If destroyed, advance to next mission after celebration delay
            if (hitResult.destroyed) {
              setTimeout(() => {
                const nextIdx = (gameStore.getState().currentMissionIndex + 1) % missionsData.length;
                loadMission(nextIdx);
                hud.render();
              }, 2200);
            } else {
              setTimeout(resetRocketToPad, 1800);
            }
          } else {
            // Ground impact (Miss)
            hud.showCombatText('SURFACE IMPACT // RE-CALIBRATE', false);
            setTimeout(resetRocketToPad, 1600);
          }
        }
      }
    } else {
      // Normal aiming & orbit camera navigation
      cameraController.update(delta);
    }
  });

  // Start Engine Loop
  engine.start();

  console.log('[SKYFALL] Engine, Controls, Physics & Heroic Visuals active.');
}

window.addEventListener('DOMContentLoaded', () => {
  bootstrap().catch((err) => console.error('[SKYFALL] Startup error:', err));
});
