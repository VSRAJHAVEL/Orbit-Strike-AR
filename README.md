# Orbit Strike AR — Mobile AR Ballistic Rocket Launcher

**Orbit Strike AR** is a mobile Augmented Reality physics launch game built in Unity (URP) with AR Foundation (ARCore & ARKit). Players detect surfaces in their real-world environment, place and orient a holographic launch pad, configure aerodynamic rocket ballistics (thrust, angle, yaw), account for dynamic wind vectors and planetary gravity, and launch precision strikes against multi-tiered interactive targets.

---

## Key Features

- **Realistic Ballistics Engine**: Real-time simulation of engine burn thrust, aerodynamic fin stabilization torque, atmospheric drag, wind drift, and custom planetary gravity ($g_{\text{Earth}} = -9.81\,\text{m/s}^2$, $g_{\text{Moon}} = -1.62\,\text{m/s}^2$, $g_{\text{Mars}} = -3.71\,\text{m/s}^2$).
- **AR Interaction**: Surface detection via `ARRaycastManager`, animated cyan holographic reticle, pinch-to-scale ($0.2\times$ to $2.0\times$), two-finger twist-to-rotate, and an animated hydraulic launch pad deploy sequence.
- **Dynamic Trajectory Predictor**: Real-time physics simulation rendered via dotted `LineRenderer` with difficulty-gated dot limits (Casual: 45 dots, Standard: 20 dots, Expert: 7 dots).
- **Launch Sequence**: 3-2-1-Liftoff countdown with audio beeps, pad warning beacons, pre-ignition flame burst, screen shake, launch shockwaves, and Cinemachine camera tracking.
- **Target Arsenal**: Concentric Bullseye targets (100, 50, 25 points), Shielded forcefield targets (2 hits), Moving hover drones, and Stage 30 Boss Dreadnought with health bars.
- **30 Levels Across 5 Worlds**:
  1. *Mojave Range (Desert)*: Earth gravity, calm breeze, trajectory fundamentals.
  2. *Frostpeak Glacier (Arctic)*: Severe crosswinds and shielded targets.
  3. *Neo-Metropolis (City)*: High-rise obstacles and moving hover drones.
  4. *Canopy Deep (Jungle)*: Dense mountain terrain and parabolic arc shots.
  5. *Lunar Base Alpha (Moon)*: Low-gravity orbital parabolas and Level 30 Boss Dreadnought.
- **Hangar & Upgrade Shop**: 4 distinct rocket archetypes (Basic, Heavy, Speedster, Precision) with purchasable upgrades (+Thrust, +Blast, +Fuel) using coins earned from star ratings.
- **Accessibility & UX**: Futuristic neon HUD with left-handed mode toggle, sound and haptics controls, and an interactive 5-step tutorial.

---

## Directory Structure

```
d:\rocket\
├── Assets\
│   ├── OrbitStrike.asmdef           # Unified Assembly Definition
│   ├── Animations\                  # Launch pad and UI animations
│   ├── Materials\                   # Neon glow, glass panels, holographic scanlines
│   ├── Prefabs\                     # AR reticle, launch pad, rockets, targets, VFX
│   ├── Resources\                   # Audio clips and ScriptableObjects
│   ├── Scenes\
│   │   ├── GameplayAR.unity         # Core AR game scene
│   │   └── MainMenu.unity           # Title, Hangar, Level Select
│   ├── Scripts\
│   │   ├── AR\                      # ARPlacementManager, SurfaceReticle, Gestures, LaunchPadController
│   │   ├── Audio\                   # AudioManager, AudioMixer routing
│   │   ├── Core\                    # GameManager, GameStateMachine, EventBus, ProceduralAssetFactory
│   │   ├── Data\                    # SaveManager, PlayerData, RocketData, LevelData, LevelDatabase
│   │   ├── Environment\             # WindSystem, GravityController, WorldDioramaManager
│   │   ├── Rocket\                  # RocketController, TrajectoryPredictor, LaunchSequencer, Spawner
│   │   ├── Systems\                 # ObjectPooler, CameraShake, HapticsManager, CinemachineFollower
│   │   ├── Targets\                 # TargetBase, BullseyeTarget, ShieldedTarget, MovingTarget, BossTarget
│   │   ├── UI\                      # UIManager, HUDController, ForceAngleControls, ShopUI, Tutorial
│   │   └── VFX\                     # LaunchShockwave, CraterDecal, DebrisPhysics
│   └── Shaders\                     # Custom shaders
├── Packages\
│   └── manifest.json                # URP, AR Foundation 5.x, TextMeshPro, Cinemachine
├── ProjectSettings\                 # TagManager, QualitySettings, EditorBuildSettings
└── README.md
```

---

## Setup & Build Instructions

### Prerequisites
- **Unity 2022.3 LTS** or newer (Universal Render Pipeline).
- **AR Support Packages** (already included in `Packages/manifest.json`):
  - AR Foundation 5.1+
  - ARCore XR Plugin (for Android)
  - ARKit XR Plugin (for iOS)
  - Cinemachine & TextMeshPro

### Unity Editor Setup
1. Launch Unity Hub, click **Add**, and select `d:\rocket`.
2. Allow Unity to import packages and compile the `OrbitStrike.asmdef` assembly.
3. Open `Assets/Scenes/GameplayAR.unity`.
4. Press **Play**. In the Editor:
   - **Raycasting**: Automatically falls back to a virtual ground plane ($Y = 0$).
   - **Click**: Tap to place launch pad and diorama.
   - **Mouse Scroll**: Pinch-to-scale playspace ($0.2\times$ to $2.0\times$).
   - **Right-Click Drag**: Twist-to-rotate playspace azimuth.
   - **Left-Click Drag**: Aim launch yaw direction.

### Android (ARCore) Build Steps
1. In Unity, go to **File > Build Settings...** and switch platform to **Android**.
2. Go to **Edit > Project Settings > XR Plug-in Management**.
3. Under the Android tab, check **Google ARCore**.
4. In **Player Settings**:
   - Minimum API Level: Android 7.0 (API Level 24) or higher.
   - Scripting Backend: IL2CPP, Target Architectures: ARM64.
5. Click **Build and Run**.

### iOS (ARKit) Build Steps
1. In **Build Settings**, switch platform to **iOS**.
2. Go to **Project Settings > XR Plug-in Management** and check **Apple ARKit**.
3. In **Player Settings**:
   - Camera Usage Description: *"Required for augmented reality surface detection and rocket launch gameplay."*
   - Target minimum iOS version: 12.0 or higher.
4. Click **Build** to generate the Xcode project, open in Xcode, and deploy to your iOS device.

---

## How to Add New Content

### Adding a New Rocket Archetype
1. In the Project window, right-click in `Assets/Resources/ScriptableObjects/Rockets/` and choose **Create > OrbitStrike > Rocket Data**.
2. Specify stats:
   - `rocketName`: e.g., *"Hyperion-7"*
   - `mass`: Inertial mass in kg (affects ballistic acceleration).
   - `maxThrust`: Peak thrust force in Newtons.
   - `burnDuration`: Engine burn time in seconds.
   - `linearDrag` & `finStabilityTorque`: Aerodynamic resistance and fin alignment.
   - `blastRadius` & `directHitDamage`: Detonation power.
3. Open `Assets/Scripts/Core/GameEnums.cs` and add your archetype identifier to `RocketType`.
4. Register the new ScriptableObject inside `RocketSpawner`'s `availableRockets` list.

### Adding a New Campaign Level
1. In `Assets/Resources/ScriptableObjects/Levels/`, right-click and choose **Create > OrbitStrike > Level Data**.
2. Configure:
   - `targetTier`: Bullseye, Shield, Moving Drone, or Boss.
   - `targetLocalPosition`: Spatial offset relative to launch pad.
   - `hasObstacle`, `obstacleLocalPosition`, `obstacleScale`: Protective barriers.
   - `windDirection` & `windSpeed`: Atmospheric crosswinds.
   - `customGravity`: $-9.81$ (Earth), $-1.62$ (Moon), $-3.71$ (Mars).
3. Add the level entry to `LevelDatabase.cs`.

---

## Core 5-Step Verification & Test Checklist

| Step | Feature | Acceptance Criteria | Tested / Verified |
|---|---|---|---|
| **1. AR Placement** | Plane Detection & Reticle | Reticle tracks surface, rotates concentric rings, glows green on valid plane. | [x] Verified |
| | Pad Deployment | Single tap places launch pad; pinch scales ($0.2\times-2.0\times$), twist rotates. Gantry deploy animation unfolds. | [x] Verified |
| **2. Rocket Creation** | Archetype Spawning | Rocket materializes on launch mount with bounce build-up animation. | [x] Verified |
| | Physics Parameters | Rigidbody configured with mass, linear/angular drag, and aerodynamic fin torque. | [x] Verified |
| **3. Controls & Trajectory**| Dual Sliders | Force slider adjusts thrust percentage (with live bar). Angle slider tilts pad barrel real-time. | [x] Verified |
| | Yaw & Trajectory Line | Screen drag aims azimuth. Dotted trajectory line simulates arc including gravity, burn, and wind. | [x] Verified |
| **4. Launch Sequence** | Countdown & Ignition | Launch button triggers 3-2-1-Liftoff with audio beeps, pad lights flashing, and screen shake. | [x] Verified |
| | Flight & Camera | Shockwave and smoke plume fire; rocket flies with continuous thrust; Cinemachine camera tracks rocket. | [x] Verified |
| **5. Collision & Scoring**| Target Destruction | Direct hits trigger explosion VFX, debris physics, camera shake, and floating score popups (+100/+50/+25). | [x] Verified |
| | Miss Feedback | Misses leave charred crater decal on surface with "Try Again" prompt. | [x] Verified |

