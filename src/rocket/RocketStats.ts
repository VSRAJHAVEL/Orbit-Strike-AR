export interface RocketStats {
  id: string;
  name: string;
  archetype: 'basic' | 'heavy' | 'speedster' | 'precision';
  mass: number; // kg normalized
  thrustMultiplier: number;
  burnDuration: number; // seconds
  blastRadius: number; // meters
  directDamage: number;
  dragCoefficient: number;
  primaryColor: number;
  glowColor: number;
  accentColor: number;
  description: string;
}

export const ROCKET_ARCHETYPES: Record<string, RocketStats> = {
  viper_basic: {
    id: 'viper_basic',
    name: 'VIPER-I MK4',
    archetype: 'basic',
    mass: 1.0,
    thrustMultiplier: 1.0,
    burnDuration: 1.25,
    blastRadius: 2.8,
    directDamage: 100,
    dragCoefficient: 0.28,
    primaryColor: 0xe2e8f0,
    glowColor: 0x00f0ff,
    accentColor: 0x0284c7,
    description: 'Standard issue multi-role ballistic interceptor with balanced aerodynamics.',
  },
  titan_heavy: {
    id: 'titan_heavy',
    name: 'TITAN-HVY SIEGE',
    archetype: 'heavy',
    mass: 1.85,
    thrustMultiplier: 0.9,
    burnDuration: 1.6,
    blastRadius: 5.2,
    directDamage: 240,
    dragCoefficient: 0.45,
    primaryColor: 0x334155,
    glowColor: 0xf97316,
    accentColor: 0xea580c,
    description: 'Reinforced dual-stage warhead built for heavy fortification and boss destruction.',
  },
  mach_speedster: {
    id: 'mach_speedster',
    name: 'MACH-X STRIKER',
    archetype: 'speedster',
    mass: 0.65,
    thrustMultiplier: 1.45,
    burnDuration: 0.75,
    blastRadius: 2.2,
    directDamage: 85,
    dragCoefficient: 0.16,
    primaryColor: 0xdc2626,
    glowColor: 0xfacc15,
    accentColor: 0x7f1d1d,
    description: 'Hypersonic needle-point dart capable of penetrating strong crosswinds.',
  },
  ion_precision: {
    id: 'ion_precision',
    name: 'ION-PRO GUIDED',
    archetype: 'precision',
    mass: 0.9,
    thrustMultiplier: 1.15,
    burnDuration: 2.1,
    blastRadius: 3.2,
    directDamage: 140,
    dragCoefficient: 0.22,
    primaryColor: 0x059669,
    glowColor: 0x10b981,
    accentColor: 0x064e3b,
    description: 'Extended plasma burn thruster engineered for long-distance orbital parabolic trajectories.',
  },
};

