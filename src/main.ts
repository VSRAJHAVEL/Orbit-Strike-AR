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
import { gameStore, GamePhase } from './core/GameState';
import { ROCKET_ARCHETYPES, RocketStats } from './rocket/RocketStats';
import { CHAPTERS } from './world/ChapterConfig';
import missionsData from './data/missions.json';

async function bootstrap(): Promise<void> {
  // 1. Initialize Core Engine & Renderer
  const engine = new Engine('canvas-container');

  // 2. Initialize Physics World (Rapier 3D)
  const physics = new PhysicsWorld();
  await physics.init(CHAPTERS[0].gravity);

  // 3. Initialize Audio Synthesizer & Sound Bank
  const sound = new SoundManager();

  // 4. Initialize Environmental Systems & Sky
  const envManager = new EnvironmentManager(engine.scene);

  // 5. Initialize PBR Landscape & Spaceport Apron
  const terrain = new TerrainGenerator(engine.scene, envManager.getCurrentConfig());

  // 6. Initialize Launch Pad & Rocket Assembly
  const launchPad = new LaunchPad(engine.scene);
  launchPad.setPosition(new THREE.Vector3(0, 0.2, 0));

  let currentStats: RocketStats = ROCKET_ARCHETYPES.viper_basic;
  const rocket = new RocketModel(currentStats);
  launchPad.getMountPoint().add(rocket.mesh);

  // 7. Initialize Trajectory Predictor & Particle Engine
  const trajectory = new TrajectorySimulator(engine.scene);
  const particles = new ParticleSystem(engine.scene);

  // 8. Initialize Multi-Tier Targets
  const targetManager = new TargetManager(engine.scene, physics);

  // 9. Initialize Camera & AR Passthrough Manager
  const cameraController = new CameraController(engine.camera, engine.renderer.domElement);
  const arManager = new ARManager(engine.renderer, engine.scene);

  // Flight & State Tracking
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

    // Spawn Mission Target
    const targetZ = -mission.distance;
    const targetX = (Math.random() - 0.5) * (mission.distance * 0.22);
    const targetY = mission.targetTier === 'drone' ? 8.5 : mission.targetTier === 'boss' ? 4.5 : 0.22;
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
    cameraController.controls.target.set(0, 1.5, 0);
  };

  // Helper to recalculate predictive trajectory arc
  const updateLiveTrajectory = () => {
    if (isFlightActive) return;

    const state = gameStore.getState();
    launchPad.setAim(state.pitchAngleDeg, state.yawAngleDeg);

    const { position, direction } = launchPad.getLaunchTransform();
    const chapterConfig = envManager.getCurrentConfig();

    trajectory.updateTrajectory(
      position,
      direction,
      state.forcePercent * 0.22,
      currentStats,
      chapterConfig.gravity,
      currentWind
    );
  };

  // Trigger Launch Sequence with 3-2-1 Countdown
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
    }, 650);
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
    const forceMag = state.forcePercent * 0.22;
    physics.spawnRocket(position, direction, forceMag, currentStats);

    // Audio & Visual Effects
    sound.startEngineRumble();
    rocket.setEngineBurning(true);
    particles.emitExhaust(position, direction, currentStats.glowColor, 40);
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
          const exhaustOrigin = flightState.position.clone().addScaledVector(forward, -0.2);
          particles.emitExhaust(exhaustOrigin, forward, currentStats.glowColor, 8);
          rocket.setEngineBurning(true, flightState.fuelRemainingPct);
        } else {
          rocket.setEngineBurning(false);
          sound.stopEngineRumble();
        }

        // Camera Follow Mode: smooth camera chase behind rocket
        const rocketPos = flightState.position;
        const forward = flightState.velocity.clone().normalize();
        const desiredCamPos = rocketPos.clone().addScaledVector(forward, -6.0).add(new THREE.Vector3(0, 2.5, 0));

        engine.camera.position.lerp(desiredCamPos, Math.min(1.0, delta * 5.0));
        engine.camera.lookAt(rocketPos.clone().addScaledVector(forward, 4.0));
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

  console.log('[SKYFALL] All 8 Milestones successfully wired and operational.');
}

window.addEventListener('DOMContentLoaded', () => {
  bootstrap().catch((err) => console.error('[SKYFALL] Startup error:', err));
});
