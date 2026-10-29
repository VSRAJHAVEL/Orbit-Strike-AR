export interface ChapterConfig {
  id: string;
  name: string;
  subtitle: string;
  timeOfDay: string;
  gravity: number; // m/s^2
  windRange: [number, number]; // min, max m/s
  skyColors: {
    top: number;
    bottom: number;
    horizon: number;
    sun: number;
  };
  sunDirection: [number, number, number];
  sunIntensity: number;
  fogColor: number;
  fogDensity: number;
  terrainColor: number;
  terrainRoughness: number;
  terrainMetalness: number;
  ambientColor: number;
  ambientIntensity: number;
}

export const CHAPTERS: ChapterConfig[] = [
  {
    id: 'desert',
    name: 'CHAPTER 1: MOJAVE SPACEPORT',
    subtitle: 'Golden Hour Orbital Corridor',
    timeOfDay: 'Golden Hour (18:45)',
    gravity: 9.81,
    windRange: [1.2, 3.5],
    skyColors: {
      top: 0x1a2e4a,
      bottom: 0xeb7a34,
      horizon: 0xffaa44,
      sun: 0xffe2a4,
    },
    sunDirection: [18, 9, -22],
    sunIntensity: 2.8,
    fogColor: 0xe08544,
    fogDensity: 0.008,
    terrainColor: 0xc48c5a,
    terrainRoughness: 0.88,
    terrainMetalness: 0.05,
    ambientColor: 0x3d3028,
    ambientIntensity: 0.75,
  },
  {
    id: 'coast',
    name: 'CHAPTER 2: COASTAL BLUFFS',
    subtitle: 'Pacific Ocean Satellite Uplink',
    timeOfDay: 'Late Afternoon (16:30)',
    gravity: 9.81,
    windRange: [4.0, 8.5],
    skyColors: {
      top: 0x0c3c78,
      bottom: 0x5aa2db,
      horizon: 0x9ed3ff,
      sun: 0xfff3d4,
    },
    sunDirection: [24, 16, -14],
    sunIntensity: 3.2,
    fogColor: 0x76a9cc,
    fogDensity: 0.006,
    terrainColor: 0x546b50,
    terrainRoughness: 0.82,
    terrainMetalness: 0.08,
    ambientColor: 0x223e59,
    ambientIntensity: 0.85,
  },
  {
    id: 'arctic',
    name: 'CHAPTER 3: ARCTIC PERMAFROST',
    subtitle: 'Station Svalbard Defense Grid',
    timeOfDay: 'Sub-Zero Overcast (12:00)',
    gravity: 9.81,
    windRange: [6.5, 14.0],
    skyColors: {
      top: 0x364654,
      bottom: 0x7b93a8,
      horizon: 0xc4d7e8,
      sun: 0xe8f2ff,
    },
    sunDirection: [10, 12, 16],
    sunIntensity: 2.2,
    fogColor: 0x8ba6bc,
    fogDensity: 0.012,
    terrainColor: 0xd8e6f0,
    terrainRoughness: 0.65,
    terrainMetalness: 0.15,
    ambientColor: 0x2e3c4a,
    ambientIntensity: 0.9,
  },
  {
    id: 'city',
    name: 'CHAPTER 4: NEO-METROPOLIS',
    subtitle: 'Cyberpunk Skyline Interceptor',
    timeOfDay: 'Midnight Rain (01:15)',
    gravity: 9.81,
    windRange: [3.0, 7.0],
    skyColors: {
      top: 0x05030f,
      bottom: 0x1b0a2e,
      horizon: 0x34124f,
      sun: 0x00f0ff,
    },
    sunDirection: [-15, 18, -10],
    sunIntensity: 1.6,
    fogColor: 0x170b28,
    fogDensity: 0.015,
    terrainColor: 0x161c28,
    terrainRoughness: 0.35,
    terrainMetalness: 0.8,
    ambientColor: 0x2a1445,
    ambientIntensity: 0.6,
  },
  {
    id: 'moon',
    name: 'CHAPTER 5: LUNAR BASE ALPHA',
    subtitle: 'Vacuum Orbital Defense Perimeter',
    timeOfDay: 'Deep Space Vacuum (Continuous)',
    gravity: 1.62,
    windRange: [0.0, 0.0], // Vacuum = 0 wind!
    skyColors: {
      top: 0x000103,
      bottom: 0x010308,
      horizon: 0x050914,
      sun: 0xffffff,
    },
    sunDirection: [25, 20, -5],
    sunIntensity: 4.5,
    fogColor: 0x000000,
    fogDensity: 0.0001, // Zero atmospheric fog in space
    terrainColor: 0x5a5e66,
    terrainRoughness: 0.95,
    terrainMetalness: 0.02,
    ambientColor: 0x0c121e,
    ambientIntensity: 0.3,
  },
];

