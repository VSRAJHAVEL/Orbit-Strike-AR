import missionsData from '../data/missions.json';
import { ROCKET_ARCHETYPES, RocketStats } from '../rocket/RocketStats';
import { CHAPTERS } from '../world/ChapterConfig';
import { gameStore, GamePhase } from '../core/GameState';

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
      <div class="pointer-auto absolute top-3 left-3 right-3 flex flex-col md:flex-row items-start md:items-center justify-between gap-3 p-3.5 rounded-xl glass-panel">
        <div class="flex items-center gap-3">
          <button id="btn-missions" class="btn-cyber px-3 py-1.5 rounded-lg text-xs font-orbitron flex items-center gap-1.5">
            <span>☰ MISSIONS</span>
            <span class="text-amber-400 font-bold">${currentMission.id}/30</span>
          </button>
          <div>
            <div class="text-[11px] tracking-widest text-cyan-400 font-bold font-orbitron">SKYFALL // ORBITAL DEFENSE</div>
            <h1 id="mission-title" class="text-sm md:text-base font-bold font-orbitron text-white glow-cyan truncate max-w-xs md:max-w-md">
              M${currentMission.id}: ${currentMission.title}
            </h1>
          </div>
        </div>

        <!-- STATS & TELEMETRY -->
        <div class="flex flex-wrap items-center gap-2 text-xs font-semibold">
          <div class="px-2.5 py-1 rounded-lg bg-black/50 border border-amber-500/30 text-amber-300">
            SCORE: <span id="stat-score" class="text-white font-orbitron font-bold">${state.totalScore}</span>
          </div>
          <div class="px-2.5 py-1 rounded-lg bg-black/50 border border-yellow-500/30 text-yellow-300">
            CREDITS: <span id="stat-coins" class="text-white font-orbitron font-bold">${state.coins} CR</span>
          </div>
          <div class="px-2.5 py-1 rounded-lg bg-black/50 border border-cyan-500/30 text-cyan-300">
            GRAV: <span id="stat-grav" class="text-white font-orbitron">-${currentChapter.gravity.toFixed(2)}</span>
          </div>
          <div class="px-2.5 py-1 rounded-lg bg-black/50 border border-emerald-500/30 text-emerald-300">
            WIND: <span id="stat-wind" class="text-white font-orbitron">${currentMission.wind[0]} - ${currentMission.wind[1]} m/s</span>
          </div>
          <button id="btn-ar-toggle" class="btn-cyber px-2.5 py-1 rounded-lg text-xs font-orbitron text-cyan-300 border-cyan-500/50">
            📱 AR MODE
          </button>
        </div>
      </div>

      <!-- LEFT ROCKET HANGAR DECK -->
      <div class="pointer-auto absolute left-3 bottom-24 md:bottom-6 w-48 md:w-56 p-3 rounded-xl glass-panel flex flex-col gap-2">
        <div class="text-xs font-orbitron text-cyan-400 font-bold tracking-wider border-b border-cyan-500/20 pb-1 flex justify-between items-center">
          <span>HANGAR ARSENAL</span>
          <span class="text-[10px] text-slate-400">[1-4]</span>
        </div>

        <div class="flex flex-col gap-1.5">
          ${Object.values(ROCKET_ARCHETYPES)
            .map(
              (r) => `
            <button
              data-rocket="${r.id}"
              class="rocket-card p-2 rounded-lg text-left text-xs transition-all flex flex-col ${
                r.id === selectedRocket.id
                  ? 'bg-cyan-500/20 border border-cyan-400 text-white shadow-md shadow-cyan-500/30'
                  : 'bg-slate-900/60 border border-slate-700/60 text-slate-300 hover:bg-slate-800'
              }"
            >
              <div class="font-orbitron font-bold text-[11px] flex justify-between">
                <span>${r.name}</span>
                <span class="text-amber-400">${r.directDamage} DMG</span>
              </div>
              <div class="text-[10px] text-slate-400 mt-0.5">
                Mass: ${r.mass} | Burn: ${r.burnDuration}s
              </div>
            </button>
          `
            )
            .join('')}
        </div>
      </div>

      <!-- RIGHT SLIDERS & LAUNCH CONTROL -->
      <div class="pointer-auto absolute right-3 bottom-3 md:bottom-6 w-72 md:w-80 p-3.5 rounded-xl glass-panel flex flex-col gap-3">
        <div class="text-xs font-orbitron text-cyan-400 font-bold tracking-wider border-b border-cyan-500/20 pb-1 flex justify-between">
          <span>BALLISTIC AIMING</span>
          <span class="text-amber-400 font-bold" id="target-dist-label">TARGET: ${currentMission.distance}m</span>
        </div>

        <!-- Pitch Slider -->
        <div class="flex flex-col gap-1">
          <div class="flex justify-between text-xs font-semibold text-slate-300">
            <span>ELEVATION PITCH:</span>
            <span id="label-pitch" class="font-orbitron text-cyan-400 font-bold">${state.pitchAngleDeg}°</span>
          </div>
          <input
            id="slider-pitch"
            type="range"
            min="15"
            max="85"
            step="1"
            value="${state.pitchAngleDeg}"
            class="w-full h-2 bg-slate-800 rounded-lg appearance-none cursor-pointer accent-cyan-400"
          />
        </div>

        <!-- Yaw Slider -->
        <div class="flex flex-col gap-1">
          <div class="flex justify-between text-xs font-semibold text-slate-300">
            <span>AZIMUTH YAW:</span>
            <span id="label-yaw" class="font-orbitron text-cyan-400 font-bold">${state.yawAngleDeg}°</span>
          </div>
          <input
            id="slider-yaw"
            type="range"
            min="-55"
            max="55"
            step="1"
            value="${state.yawAngleDeg}"
            class="w-full h-2 bg-slate-800 rounded-lg appearance-none cursor-pointer accent-cyan-400"
          />
        </div>

        <!-- Force Power Slider -->
        <div class="flex flex-col gap-1">
          <div class="flex justify-between text-xs font-semibold text-slate-300">
            <span>LAUNCH FORCE:</span>
            <span id="label-force" class="font-orbitron text-amber-400 font-bold">${state.forcePercent}%</span>
          </div>
          <input
            id="slider-force"
            type="range"
            min="50"
            max="250"
            step="2"
            value="${state.forcePercent}"
            class="w-full h-2 bg-slate-800 rounded-lg appearance-none cursor-pointer accent-amber-400"
          />
        </div>

        <!-- Primary Launch Button -->
        <button
          id="btn-launch"
          class="w-full py-3 rounded-lg bg-gradient-to-r from-orange-600 to-amber-500 hover:from-orange-500 hover:to-amber-400 text-white font-orbitron font-extrabold text-sm md:text-base tracking-widest shadow-lg shadow-orange-500/40 transition-all active:scale-95 cursor-pointer flex items-center justify-center gap-2"
        >
          <span>🚀 IGNITE & LAUNCH</span>
          <span class="text-xs bg-black/30 px-1.5 py-0.5 rounded text-amber-200">[SPACE]</span>
        </button>
      </div>

      <!-- MISSION BRIEFING MODAL CONTAINER -->
      <div id="modal-container" class="pointer-auto"></div>

      <!-- FLOATING COMBAT TEXT CONTAINER -->
      <div id="floating-combat-container" class="pointer-events-none absolute inset-0 overflow-hidden z-20"></div>
    `;

    this.bindEvents();
  }

  private bindEvents(): void {
    // 1. Rocket Selection
    const rocketBtns = this.container.querySelectorAll<HTMLButtonElement>('.rocket-card');
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

    // 4. AR Toggle Button
    const arBtn = this.container.querySelector<HTMLButtonElement>('#btn-ar-toggle');
    arBtn?.addEventListener('click', () => {
      this.callbacks.onARStart();
    });

    // 5. Missions Modal Button
    const missionsBtn = this.container.querySelector<HTMLButtonElement>('#btn-missions');
    missionsBtn?.addEventListener('click', () => {
      this.showMissionsModal();
    });
  }

  public showCountdown(count: number): void {
    if (!this.countdownOverlay) {
      this.countdownOverlay = document.createElement('div');
      this.countdownOverlay.className =
        'pointer-events-none absolute inset-0 flex items-center justify-center z-50';
      this.container.appendChild(this.countdownOverlay);
    }

    if (count > 0) {
      this.countdownOverlay.innerHTML = `
        <div class="text-7xl md:text-9xl font-black font-orbitron text-amber-400 drop-shadow-[0_0_35px_rgba(251,191,36,0.9)] animate-ping">
          ${count}
        </div>
      `;
    } else {
      this.countdownOverlay.innerHTML = `
        <div class="text-6xl md:text-8xl font-black font-orbitron text-orange-500 drop-shadow-[0_0_45px_rgba(249,115,22,1)] animate-pulse">
          IGNITION!
        </div>
      `;
      setTimeout(() => {
        if (this.countdownOverlay) {
          this.container.removeChild(this.countdownOverlay);
          this.countdownOverlay = null;
        }
      }, 750);
    }
  }

  public showCombatText(text: string, isBullseye = false): void {
    const parent = document.getElementById('floating-combat-container');
    if (!parent) return;

    const el = document.createElement('div');
    el.className = `absolute top-1/3 left-1/2 -translate-x-1/2 font-black font-orbitron text-2xl md:text-4xl tracking-wider transition-all duration-1000 ${
      isBullseye ? 'text-amber-400 glow-gold scale-125' : 'text-cyan-400 glow-cyan'
    }`;
    el.textContent = text;
    parent.appendChild(el);

    // Float upward and fade out
    requestAnimationFrame(() => {
      el.style.transform = 'translate(-50%, -80px) scale(1.1)';
      el.style.opacity = '0';
    });

    setTimeout(() => {
      if (el.parentElement) el.parentElement.removeChild(el);
    }, 1100);
  }

  public showMissionsModal(): void {
    const modalContainer = document.getElementById('modal-container');
    if (!modalContainer) return;

    modalContainer.innerHTML = `
      <div class="fixed inset-0 bg-black/80 backdrop-blur-md flex items-center justify-center p-4 z-50">
        <div class="w-full max-w-2xl max-h-[85vh] rounded-2xl glass-panel p-6 flex flex-col gap-4 overflow-hidden border border-cyan-500/30">
          <div class="flex justify-between items-center border-b border-cyan-500/20 pb-3">
            <div>
              <div class="text-xs font-orbitron text-cyan-400 font-bold">CAMPAIGN OPERATIONS</div>
              <h2 class="text-xl font-bold font-orbitron text-white">SELECT MISSION (1 - 30)</h2>
            </div>
            <button id="modal-close" class="btn-cyber px-3 py-1.5 rounded-lg text-xs font-orbitron">✕ CLOSE</button>
          </div>

          <div class="grid grid-cols-1 md:grid-cols-2 gap-2.5 overflow-y-auto pr-1">
            ${missionsData
              .map(
                (m, idx) => `
              <button
                data-idx="${idx}"
                class="mission-select-btn p-3 rounded-xl text-left bg-slate-900/70 hover:bg-slate-800 border ${
                  idx === gameStore.getState().currentMissionIndex
                    ? 'border-cyan-400 bg-cyan-950/40 text-white'
                    : 'border-slate-700/60 text-slate-300'
                } transition-all flex flex-col gap-1"
              >
                <div class="flex justify-between text-xs font-orbitron font-bold">
                  <span class="text-cyan-400">MISSION ${m.id}</span>
                  <span class="text-amber-400">${m.distance}m [${m.targetTier.toUpperCase()}]</span>
                </div>
                <div class="text-sm font-bold text-white">${m.title}</div>
                <div class="text-xs text-slate-400 line-clamp-1">${m.briefing}</div>
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

    const missionBtns = modalContainer.querySelectorAll<HTMLButtonElement>('.mission-select-btn');
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
