import missionsData from '../data/missions.json';
import { ROCKET_ARCHETYPES, RocketStats } from '../rocket/RocketStats';
import { CHAPTERS } from '../world/ChapterConfig';
import { gameStore } from '../core/GameState';

export interface CommandCallbacks {
  onRocketSelect: (stats: RocketStats) => void;
  onAimChange: (pitch: number, yaw: number, force: number) => void;
  onLaunch: () => void;
  onReset: () => void;
  onARStart: () => void;
  onMissionSelect: (missionIndex: number) => void;
  onChapterSelect: (chapterIndex: number) => void;
}

export class TacticalCommandHUD {
  private container: HTMLElement;
  private callbacks: CommandCallbacks;
  private countdownOverlay: HTMLElement | null = null;

  constructor(rootId: string, callbacks: CommandCallbacks) {
    const el = document.getElementById(rootId);
    if (!el) throw new Error(`HUD root #${rootId} not found`);
    this.container = el;
    this.callbacks = callbacks;

    this.render();
  }

  public render(): void {
    const state = gameStore.getState();
    const currentMission = missionsData[state.currentMissionIndex] || missionsData[0];
    const currentChapter = CHAPTERS[state.currentChapterIndex] || CHAPTERS[0];
    const selectedRocket = ROCKET_ARCHETYPES[state.selectedRocketId] || ROCKET_ARCHETYPES.viper_basic;

    this.container.innerHTML = `
      <!-- TOP COMMAND BAR -->
      <div class="top-command-bar glass-panel pointer-auto">
        <div class="title-group">
          <div class="pulse-dot"></div>
          <button id="btn-missions" class="btn-cyber">
            <span>☰ MISSIONS</span>
            <span style="color: var(--amber-glow);">${currentMission.id}/30</span>
          </button>
          <div>
            <div class="title-sub">SKYFALL // ORBITAL DEFENSE</div>
            <div class="title-main glow-cyan">
              M${currentMission.id}: ${currentMission.title}
            </div>
          </div>
        </div>

        <!-- TELEMETRY BADGES -->
        <div class="telemetry-deck">
          <div class="telemetry-chip">
            <span>SCORE:</span>
            <span id="stat-score" class="telemetry-val" style="color: var(--amber-glow);">${state.totalScore}</span>
          </div>
          <div class="telemetry-chip">
            <span>CREDITS:</span>
            <span id="stat-coins" class="telemetry-val" style="color: #fde047;">${state.coins} CR</span>
          </div>
          <div class="telemetry-chip">
            <span>GRAV:</span>
            <span id="stat-grav" class="telemetry-val" style="color: var(--cyan-glow);">-${currentChapter.gravity.toFixed(2)}</span>
          </div>
          <div class="telemetry-chip">
            <span>WIND:</span>
            <span id="stat-wind" class="telemetry-val" style="color: #4ade80;">${currentMission.wind[0]} - ${currentMission.wind[1]} m/s</span>
          </div>
          <button id="btn-ar-toggle" class="btn-cyber">
            📱 AR MODE
          </button>
        </div>
      </div>

      <!-- FLOATING COMBAT TEXT CONTAINER -->
      <div id="floating-combat-container" style="position: absolute; inset: 0; pointer-events: none; z-index: 20;"></div>

      <!-- MAIN BOTTOM DECK (HANGAR ON LEFT, CHAPTERS IN CENTER, SLIDERS ON RIGHT) -->
      <div class="main-deck-container pointer-auto">
        <!-- LEFT HANGAR -->
        <div class="hangar-panel glass-panel">
          <div class="panel-header">
            <span>HANGAR ARSENAL</span>
            <span style="color: #94a3b8;">[KEYS 1-4]</span>
          </div>
          <div class="rocket-list">
            ${Object.values(ROCKET_ARCHETYPES)
              .map(
                (r) => `
              <button
                data-rocket="${r.id}"
                class="rocket-btn ${r.id === selectedRocket.id ? 'active' : ''}"
              >
                <div class="rocket-title-row">
                  <span>${r.name}</span>
                  <span class="rocket-dmg">${r.directDamage} DMG</span>
                </div>
                <div class="rocket-stats-row">
                  Mass: ${r.mass} | Thrust: ${r.thrustMultiplier}x | Burn: ${r.burnDuration}s
                </div>
              </button>
            `
              )
              .join('')}
          </div>
        </div>

        <!-- CENTER CHAPTER CHIPS -->
        <div class="bottom-chapter-bar glass-panel">
          ${CHAPTERS.map(
            (ch, i) => `
            <button
              data-chapter="${i}"
              class="chapter-chip ${i === state.currentChapterIndex ? 'active' : ''}"
            >
              ${ch.name.split(':')[1]?.trim() || ch.name}
            </button>
          `
          ).join('')}
        </div>

        <!-- RIGHT BALLISTIC SLIDERS & LAUNCH -->
        <div class="aiming-panel glass-panel">
          <div class="panel-header">
            <span>BALLISTIC AIMING</span>
            <span style="color: var(--amber-glow); font-family: 'Orbitron'; font-weight: 700;">
              TARGET: ${currentMission.distance}m
            </span>
          </div>

          <!-- Pitch Slider -->
          <div class="slider-group">
            <div class="slider-label-row">
              <span>ELEVATION PITCH:</span>
              <span id="label-pitch" class="slider-val">${state.pitchAngleDeg}°</span>
            </div>
            <input
              id="slider-pitch"
              type="range"
              min="15"
              max="85"
              step="1"
              value="${state.pitchAngleDeg}"
            />
          </div>

          <!-- Yaw Slider -->
          <div class="slider-group">
            <div class="slider-label-row">
              <span>AZIMUTH YAW:</span>
              <span id="label-yaw" class="slider-val">${state.yawAngleDeg}°</span>
            </div>
            <input
              id="slider-yaw"
              type="range"
              min="-55"
              max="55"
              step="1"
              value="${state.yawAngleDeg}"
            />
          </div>

          <!-- Force Slider -->
          <div class="slider-group">
            <div class="slider-label-row">
              <span>LAUNCH FORCE:</span>
              <span id="label-force" class="slider-val force">${state.forcePercent}%</span>
            </div>
            <input
              id="slider-force"
              type="range"
              min="50"
              max="250"
              step="2"
              value="${state.forcePercent}"
              class="force-range"
            />
          </div>

          <!-- Primary Launch Button -->
          <button id="btn-launch" class="btn-launch-primary">
            <span>🚀 IGNITE & LAUNCH</span>
            <span class="shortcut-chip">[SPACE]</span>
          </button>
        </div>
      </div>

      <!-- MODAL CONTAINER -->
      <div id="modal-container" class="pointer-auto"></div>
    `;

    this.bindEvents();
  }

  private bindEvents(): void {
    // 1. Rocket Selection
    const rocketBtns = this.container.querySelectorAll<HTMLButtonElement>('.rocket-btn');
    rocketBtns.forEach((btn) => {
      btn.addEventListener('click', () => {
        const id = btn.getAttribute('data-rocket');
        if (id && ROCKET_ARCHETYPES[id]) {
          gameStore.getState().selectedRocketId = id;
          this.callbacks.onRocketSelect(ROCKET_ARCHETYPES[id]);
          this.render();
        }
      });
    });

    // 2. Sliders
    const pitchSlider = this.container.querySelector<HTMLInputElement>('#slider-pitch');
    const yawSlider = this.container.querySelector<HTMLInputElement>('#slider-yaw');
    const forceSlider = this.container.querySelector<HTMLInputElement>('#slider-force');

    const onAimInput = () => {
      const pitch = parseFloat(pitchSlider?.value || '55');
      const yaw = parseFloat(yawSlider?.value || '0');
      const force = parseFloat(forceSlider?.value || '120');

      const pitchLabel = this.container.querySelector('#label-pitch');
      const yawLabel = this.container.querySelector('#label-yaw');
      const forceLabel = this.container.querySelector('#label-force');

      if (pitchLabel) pitchLabel.textContent = `${pitch}°`;
      if (yawLabel) yawLabel.textContent = `${yaw}°`;
      if (forceLabel) forceLabel.textContent = `${force}%`;

      gameStore.getState().setAim(pitch, yaw, force);
      this.callbacks.onAimChange(pitch, yaw, force);
    };

    pitchSlider?.addEventListener('input', onAimInput);
    yawSlider?.addEventListener('input', onAimInput);
    forceSlider?.addEventListener('input', onAimInput);

    // 3. Launch Button
    const launchBtn = this.container.querySelector<HTMLButtonElement>('#btn-launch');
    launchBtn?.addEventListener('click', () => {
      this.callbacks.onLaunch();
    });

    // Keyboard Shortcuts: Space = Launch, 1-4 = Rocket select
    window.addEventListener('keydown', (e) => {
      if (e.code === 'Space' && !e.repeat) {
        e.preventDefault();
        this.callbacks.onLaunch();
      } else if (e.key === '1') {
        this.callbacks.onRocketSelect(ROCKET_ARCHETYPES.viper_basic);
        this.render();
      } else if (e.key === '2') {
        this.callbacks.onRocketSelect(ROCKET_ARCHETYPES.titan_heavy);
        this.render();
      } else if (e.key === '3') {
        this.callbacks.onRocketSelect(ROCKET_ARCHETYPES.mach_speedster);
        this.render();
      } else if (e.key === '4') {
        this.callbacks.onRocketSelect(ROCKET_ARCHETYPES.ion_precision);
        this.render();
      }
    });

    // 4. Chapter Chips
    const chapterChips = this.container.querySelectorAll<HTMLButtonElement>('.chapter-chip');
    chapterChips.forEach((chip) => {
      chip.addEventListener('click', () => {
        const idx = parseInt(chip.getAttribute('data-chapter') || '0', 10);
        this.callbacks.onChapterSelect(idx);
        this.render();
      });
    });

    // 5. AR Button
    const arBtn = this.container.querySelector<HTMLButtonElement>('#btn-ar-toggle');
    arBtn?.addEventListener('click', () => {
      this.callbacks.onARStart();
    });

    // 6. Missions Modal Button
    const missionsBtn = this.container.querySelector<HTMLButtonElement>('#btn-missions');
    missionsBtn?.addEventListener('click', () => {
      this.showMissionsModal();
    });
  }

  public showCountdown(count: number): void {
    if (!this.countdownOverlay) {
      this.countdownOverlay = document.createElement('div');
      this.countdownOverlay.className = 'countdown-banner';
      this.container.appendChild(this.countdownOverlay);
    }

    if (count > 0) {
      this.countdownOverlay.textContent = `${count}`;
    } else {
      this.countdownOverlay.textContent = 'IGNITION!';
      setTimeout(() => {
        if (this.countdownOverlay && this.countdownOverlay.parentElement) {
          this.countdownOverlay.parentElement.removeChild(this.countdownOverlay);
          this.countdownOverlay = null;
        }
      }, 700);
    }
  }

  public showCombatText(text: string, isBullseye = false): void {
    const parent = document.getElementById('floating-combat-container');
    if (!parent) return;

    const el = document.createElement('div');
    el.className = `floating-text ${isBullseye ? 'glow-gold' : 'glow-cyan'}`;
    el.style.color = isBullseye ? 'var(--amber-glow)' : 'var(--cyan-glow)';
    el.textContent = text;
    parent.appendChild(el);

    setTimeout(() => {
      if (el.parentElement) el.parentElement.removeChild(el);
    }, 1200);
  }

  public showMissionsModal(): void {
    const modalContainer = document.getElementById('modal-container');
    if (!modalContainer) return;

    modalContainer.innerHTML = `
      <div class="modal-overlay">
        <div class="modal-content glass-panel">
          <div style="display: flex; justify-content: space-between; align-items: center; border-bottom: 1px solid rgba(0, 240, 255, 0.25); padding-bottom: 12px;">
            <div>
              <div class="title-sub">CAMPAIGN OPERATIONS</div>
              <div class="title-main">SELECT MISSION (1 - 30)</div>
            </div>
            <button id="modal-close" class="btn-cyber">✕ CLOSE</button>
          </div>

          <div class="missions-grid">
            ${missionsData
              .map(
                (m, idx) => `
              <button
                data-idx="${idx}"
                class="mission-card ${idx === gameStore.getState().currentMissionIndex ? 'active' : ''}"
              >
                <div style="display: flex; justify-content: space-between; font-size: 11px; font-family: 'Orbitron'; font-weight: 700;">
                  <span style="color: var(--cyan-glow);">MISSION ${m.id}</span>
                  <span style="color: var(--amber-glow);">${m.distance}m [${m.targetTier.toUpperCase()}]</span>
                </div>
                <div style="font-weight: 700; color: #fff; font-size: 14px;">${m.title}</div>
                <div style="font-size: 12px; color: #94a3b8; line-height: 1.3;">${m.briefing}</div>
              </button>
            `
              )
              .join('')}
          </div>
        </div>
      </div>
    `;

    modalContainer.querySelector('#modal-close')?.addEventListener('click', () => {
      modalContainer.innerHTML = '';
    });

    const missionBtns = modalContainer.querySelectorAll<HTMLButtonElement>('.mission-card');
    missionBtns.forEach((btn) => {
      btn.addEventListener('click', () => {
        const idx = parseInt(btn.getAttribute('data-idx') || '0', 10);
        this.callbacks.onMissionSelect(idx);
        modalContainer.innerHTML = '';
        this.render();
      });
    });
  }

  public updateStats(score: number, coins: number): void {
    const scoreEl = this.container.querySelector('#stat-score');
    const coinsEl = this.container.querySelector('#stat-coins');
    if (scoreEl) scoreEl.textContent = `${score}`;
    if (coinsEl) coinsEl.textContent = `${coins} CR`;
  }
}
