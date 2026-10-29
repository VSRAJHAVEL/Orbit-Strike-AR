# SKYFALL: AR ROCKET COMMAND

**SKYFALL: AR Rocket Command** is a cinematic, realistic browser-based ballistic rocket launch simulation game built with **Three.js, Rapier.js (3D physics), WebXR / Webcam Passthrough AR, and Howler.js / Web Audio API**.

---

## 🚀 Scenario & Story

You are the lead launch engineer stationed at a remote desert spaceport at golden hour. Rogue satellite debris and hostile reconnaissance drones are threatening the orbital corridor. You deploy a mobile launch pad, configure your rocket's ballistics (thrust, angle, yaw), calculate environmental forces (drag, mass, wind, variable planetary gravity), and launch precision strikes to eliminate threats.

### 5 Story Chapters:
1. **Chapter 1: Mojave Spaceport** — Golden hour, standard Earth gravity ($-9.81\,\text{m/s}^2$), calm desert breeze.
2. **Chapter 2: Coastal Bluffs** — Pacific maritime mist, strong onshore crosswinds ($4.0\text{--}8.5\,\text{m/s}$).
3. **Chapter 3: Arctic Permafrost** — High-density cold air drag, sub-zero blizzard gusts ($6.5\text{--}14.0\,\text{m/s}$).
4. **Chapter 4: Neo-Metropolis** — Cyberpunk midnight skyline, high-rise obstacles, glowing urban interceptor drones.
5. **Chapter 5: Lunar Base Alpha** — Deep space vacuum, moon gravity ($-1.62\,\text{m/s}^2$), zero atmospheric drag.

---

## 🛠️ Tech Stack (Zero Install-Heavy Native Tools)

- **Engine & Graphics**: Vite 5 + TypeScript 5 + Three.js (`MeshPhysicalMaterial`, ACES Tone Mapping, Soft Shadows).
- **Physics**: Rapier.js 3D (`@dimforge/rapier3d-compat`) with fixed timestep integration and real aerodynamic drag ($F = -\frac{1}{2}\rho v^2 C_d A \hat{v}$).
- **Augmented Reality**: WebXR `immersive-ar` hit-test for mobile devices (ARCore / ARKit) + Laptop webcam passthrough AR fallback with virtual desk reticle.
- **Audio**: Web Audio API procedural synthesis (countdown beeps, layered engine rumble with brown noise low-pass filter, sub-bass explosions).
- **Interface**: Tailwind CSS + Glassmorphism panels + GSAP animations + Zustand state store.

---

## 🎮 Controls

| Action | Laptop / Desktop | Mobile / Touch |
| :--- | :--- | :--- |
| **Orbit Camera** | Left-Click + Drag | Single Finger Drag |
| **Pan Camera** | Right-Click + Drag | Two Finger Drag |
| **Zoom Camera** | Mouse Scroll Wheel | Pinch to Zoom |
| **Fly / Move Camera** | `W`, `A`, `S`, `D` & `Q`, `E` | Touch Pan |
| **Adjust Elevation Pitch** | Pitch Slider ($15^\circ$ to $85^\circ$) | Touch Slider |
| **Adjust Azimuth Yaw** | Yaw Slider ($-55^\circ$ to $+55^\circ$) | Touch Slider |
| **Adjust Launch Force** | Force Slider ($50\%$ to $250\%$) | Touch Slider |
| **Select Rocket** | Keys `1`, `2`, `3`, `4` or click Hangar card | Tap Hangar Card |
| **Ignite & Launch** | **Spacebar** or Orange "IGNITE & LAUNCH" | Tap "IGNITE & LAUNCH" |
| **Open Missions Modal** | Click `☰ MISSIONS` button | Tap `☰ MISSIONS` |
| **Enter AR Mode** | Click `📱 AR MODE` button | Tap `📱 AR MODE` |

---

## 📋 5 Core AR Steps Test Checklist

| Step | Feature | Verification Checklist |
| :---: | :--- | :--- |
| **1** | **Place Launch Pad** | Click `📱 AR MODE`. Point phone at floor or laptop webcam at desk. Animated holographic cyan reticle appears. Tap screen or click desk to anchor launch pad. |
| **2** | **Create Rocket** | Click any of the 4 archetypes (Viper-I, Titan-Heavy, Mach-X, Ion-Precision). Rocket mesh procedural PBR geometry mounts instantly on the launch rail. |
| **3** | **Force Control** | Adjust Pitch, Yaw, and Force sliders. The mechanical gantry and hydraulic arm articulate in real time, and the cyan predictive trajectory arc recalculates live matching physics. |
| **4** | **Launch** | Press Spacebar or tap "IGNITE & LAUNCH". 3-2-1 countdown overlay fires with beeps, engine ignites with audio rumble and particle plume, and camera switches into tracking follow mode. |
| **5** | **Target Collision** | Rocket impacts bullseye or ground. Massive particle explosion and shockwave trigger. Damage is calculated (Outer, Mid, Direct Bullseye +500). 3D floating score appears. Next mission unlocks. |

---

## 📦 How to Run Locally

```bash
# 1. Install dependencies
npm install

# 2. Run local development server
npm run dev

# 3. Open in browser:
http://localhost:3000/

# 4. Build for static production hosting:
npm run build
```

---

## 📜 Asset & Technology Credits

- **Physics**: [Rapier.js](https://rapier.rs/) by Dimforge (Apache 2.0).
- **Graphics & 3D Math**: [Three.js](https://threejs.org/) by Ricardo Cabello & Contributors (MIT).
- **Audio Synthesis**: Native Web Audio API procedural synthesizer (CC0).
- **Fonts**: [Orbitron](https://fonts.google.com/specimen/Orbitron) by Matt McInerney & [Rajdhani](https://fonts.google.com/specimen/Rajdhani) by Indian Type Foundry (OFL).
- **Icons & UI**: [Lucide](https://lucide.dev/) (ISC) & Tailwind CSS.
