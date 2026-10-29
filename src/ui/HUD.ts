import { CHAPTERS, ChapterConfig } from '../world/ChapterConfig';
import { gameStore, GraphicsQuality } from '../core/GameState';

export class HUD {
  private container: HTMLElement;
  private chapterCallback: (index: number) => void;
  private qualityCallback: (quality: GraphicsQuality) => void;

  constructor(
    rootId: string,
    onChapterChange: (index: number) => void,
    onQualityChange: (quality: GraphicsQuality) => void
  ) {
    const el = document.getElementById(rootId);
    if (!el) throw new Error(`HUD root #${rootId} not found`);
    this.container = el;
    this.chapterCallback = onChapterChange;
    this.qualityCallback = onQualityChange;

    this.render();
  }

  private render(): void {
    const state = gameStore.getState();
    const currentChapter = CHAPTERS[state.currentChapterIndex];

    this.container.innerHTML = `
      <!-- TOP COMMAND BAR -->
      <div class="pointer-auto absolute top-4 left-4 right-4 flex flex-col md:flex-row items-start md:items-center justify-between gap-3 p-4 rounded-xl glass-panel">
        <div class="flex items-center gap-3">
          <div class="w-3.5 h-3.5 rounded-full bg-cyan-400 animate-pulse shadow-lg shadow-cyan-500/50"></div>
          <div>
            <div class="text-xs tracking-widest text-cyan-400 font-bold font-orbitron">SKYFALL // AR ROCKET COMMAND</div>
            <h1 id="chapter-title" class="text-lg md:text-xl font-bold font-orbitron text-white glow-cyan">
              ${currentChapter.name}
            </h1>
          </div>
        </div>

        <!-- TELEMETRY BADGES -->
        <div class="flex flex-wrap items-center gap-2 text-xs md:text-sm font-semibold">
          <div class="px-3 py-1.5 rounded-lg bg-black/40 border border-amber-500/30 text-amber-300">
            TIME: <span id="telemetry-time" class="text-white">${currentChapter.timeOfDay}</span>
          </div>
          <div class="px-3 py-1.5 rounded-lg bg-black/40 border border-cyan-500/30 text-cyan-300">
            GRAVITY: <span id="telemetry-grav" class="text-white font-orbitron">-${currentChapter.gravity.toFixed(2)} m/s²</span>
          </div>
          <div class="px-3 py-1.5 rounded-lg bg-black/40 border border-emerald-500/30 text-emerald-300">
            WIND: <span id="telemetry-wind" class="text-white font-orbitron">${currentChapter.windRange[0]} - ${currentChapter.windRange[1]} m/s</span>
          </div>
          <div class="px-2.5 py-1.5 rounded-lg bg-black/40 border border-slate-600 text-slate-300 font-orbitron text-xs">
            FPS: <span id="telemetry-fps" class="text-emerald-400">60</span>
          </div>
        </div>
      </div>

      <!-- BOTTOM ENVIRONMENT & CHAPTER SWITCHER -->
      <div class="pointer-auto absolute bottom-5 left-1/2 -translate-x-1/2 w-full max-w-4xl px-4 flex flex-col items-center gap-3">
        <!-- Chapter selection chips -->
        <div class="flex flex-wrap items-center justify-center gap-2 p-2 rounded-xl glass-panel">
          ${CHAPTERS.map(
            (ch, i) => `
            <button
              data-chapter="${i}"
              class="chapter-btn px-3 py-2 rounded-lg text-xs md:text-sm font-bold font-orbitron transition-all ${
                i === state.currentChapterIndex
                  ? 'bg-cyan-500 text-black shadow-lg shadow-cyan-500/40'
                  : 'bg-slate-800/80 hover:bg-slate-700 text-slate-300 border border-slate-700'
              }"
            >
              ${ch.name.split(':')[1] || ch.name}
            </button>
          `
          ).join('')}
        </div>

        <!-- Camera instruction bar -->
        <div class="px-4 py-1.5 rounded-full bg-black/60 border border-cyan-500/20 text-xs text-slate-300 font-medium tracking-wide flex items-center gap-3 shadow-lg">
          <span><span class="text-cyan-400 font-bold">Orbit:</span> Left Click + Drag</span>
          <span class="text-slate-600">•</span>
          <span><span class="text-cyan-400 font-bold">Pan:</span> Right Click + Drag</span>
          <span class="text-slate-600">•</span>
          <span><span class="text-cyan-400 font-bold">Zoom:</span> Scroll Wheel</span>
          <span class="text-slate-600">•</span>
          <span><span class="text-cyan-400 font-bold">Move:</span> W A S D</span>
        </div>
      </div>
    `;

    this.bindEvents();
  }

  private bindEvents(): void {
    const buttons = this.container.querySelectorAll<HTMLButtonElement>('.chapter-btn');
    buttons.forEach((btn) => {
      btn.addEventListener('click', () => {
        const idx = parseInt(btn.getAttribute('data-chapter') || '0', 10);
        this.chapterCallback(idx);
        this.updateChapter(idx);
      });
    });
  }

  public updateChapter(index: number): void {
    const ch = CHAPTERS[index];
    if (!ch) return;

    gameStore.getState().setChapter(index);

    const titleEl = document.getElementById('chapter-title');
    if (titleEl) titleEl.textContent = ch.name;

    const timeEl = document.getElementById('telemetry-time');
    if (timeEl) timeEl.textContent = ch.timeOfDay;

    const gravEl = document.getElementById('telemetry-grav');
    if (gravEl) gravEl.textContent = `-${ch.gravity.toFixed(2)} m/s²`;

    const windEl = document.getElementById('telemetry-wind');
    if (windEl) windEl.textContent = `${ch.windRange[0]} - ${ch.windRange[1]} m/s`;

    // Update button active state styles
    const buttons = this.container.querySelectorAll<HTMLButtonElement>('.chapter-btn');
    buttons.forEach((btn, i) => {
      if (i === index) {
        btn.className =
          'chapter-btn px-3 py-2 rounded-lg text-xs md:text-sm font-bold font-orbitron transition-all bg-cyan-500 text-black shadow-lg shadow-cyan-500/40';
      } else {
        btn.className =
          'chapter-btn px-3 py-2 rounded-lg text-xs md:text-sm font-bold font-orbitron transition-all bg-slate-800/80 hover:bg-slate-700 text-slate-300 border border-slate-700';
      }
    });
  }

  public updateFPS(fps: number): void {
    const fpsEl = document.getElementById('telemetry-fps');
    if (fpsEl) fpsEl.textContent = `${Math.round(fps)}`;
  }
}

