import { createStore } from 'zustand/vanilla';

export enum GamePhase {
  MENU = 'MENU',
  BRIEFING = 'BRIEFING',
  PLACEMENT = 'PLACEMENT',
  AIMING = 'AIMING',
  LAUNCH = 'LAUNCH',
  RESULT = 'RESULT',
}

export type GraphicsQuality = 'LOW' | 'MEDIUM' | 'HIGH' | 'ULTRA';

export interface GameStateData {
  phase: GamePhase;
  currentChapterIndex: number;
  currentMissionIndex: number;
  totalScore: number;
  coins: number;
  selectedRocketId: string;
  forcePercent: number; // 50% to 250%
  pitchAngleDeg: number; // 15 to 85 deg
  yawAngleDeg: number; // -60 to 60 deg
  graphicsQuality: GraphicsQuality;
  isARActive: boolean;
  isHandTrackingActive: boolean;
  fps: number;

  // Actions
  setPhase: (phase: GamePhase) => void;
  setChapter: (index: number) => void;
  setMission: (index: number) => void;
  setAim: (pitch: number, yaw: number, force: number) => void;
  addScore: (points: number) => void;
  addCoins: (amount: number) => void;
  setGraphicsQuality: (quality: GraphicsQuality) => void;
  setARActive: (active: boolean) => void;
  setFPS: (fps: number) => void;
}

export const gameStore = createStore<GameStateData>((set) => ({
  phase: GamePhase.AIMING, // Start directly in Aiming for Milestone 1 exploration
  currentChapterIndex: 0,
  currentMissionIndex: 0,
  totalScore: 0,
  coins: 200,
  selectedRocketId: 'viper_basic',
  forcePercent: 120,
  pitchAngleDeg: 55,
  yawAngleDeg: 0,
  graphicsQuality: 'HIGH',
  isARActive: false,
  isHandTrackingActive: false,
  fps: 60,

  setPhase: (phase) => set({ phase }),
  setChapter: (currentChapterIndex) => set({ currentChapterIndex }),
  setMission: (currentMissionIndex) => set({ currentMissionIndex }),
  setAim: (pitchAngleDeg, yawAngleDeg, forcePercent) => set({ pitchAngleDeg, yawAngleDeg, forcePercent }),
  addScore: (points) => set((s) => ({ totalScore: s.totalScore + points })),
  addCoins: (amount) => set((s) => ({ coins: s.coins + amount })),
  setGraphicsQuality: (graphicsQuality) => set({ graphicsQuality }),
  setARActive: (isARActive) => set({ isARActive }),
  setFPS: (fps) => set({ fps }),
}));

