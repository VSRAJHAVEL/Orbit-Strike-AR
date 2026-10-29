export class SoundManager {
  private ctx: AudioContext | null = null;
  private isMuted = false;
  private engineRumbleNode: AudioBufferSourceNode | null = null;
  private engineGainNode: GainNode | null = null;

  constructor() {
    // AudioContext will be initialized on first user interaction
    const initAudio = () => {
      if (!this.ctx) {
        const AudioCtx = window.AudioContext || (window as unknown as { webkitAudioContext: typeof AudioContext }).webkitAudioContext;
        this.ctx = new AudioCtx();
      }
      if (this.ctx.state === 'suspended') {
        this.ctx.resume();
      }
      window.removeEventListener('click', initAudio);
      window.removeEventListener('touchstart', initAudio);
      window.removeEventListener('keydown', initAudio);
    };

    window.addEventListener('click', initAudio);
    window.addEventListener('touchstart', initAudio);
    window.addEventListener('keydown', initAudio);
  }

  private ensureContext(): boolean {
    if (!this.ctx) {
      try {
        const AudioCtx = window.AudioContext || (window as unknown as { webkitAudioContext: typeof AudioContext }).webkitAudioContext;
        this.ctx = new AudioCtx();
      } catch {
        return false;
      }
    }
    if (this.ctx.state === 'suspended') {
      this.ctx.resume();
    }
    return true;
  }

  public playBeep(freq = 880, duration = 0.12, type: OscillatorType = 'sine'): void {
    if (this.isMuted || !this.ensureContext() || !this.ctx) return;

    const osc = this.ctx.createOscillator();
    const gain = this.ctx.createGain();

    osc.type = type;
    osc.frequency.setValueAtTime(freq, this.ctx.currentTime);

    gain.gain.setValueAtTime(0.2, this.ctx.currentTime);
    gain.gain.exponentialRampToValueAtTime(0.001, this.ctx.currentTime + duration);

    osc.connect(gain);
    gain.connect(this.ctx.destination);

    osc.start();
    osc.stop(this.ctx.currentTime + duration);
  }

  public playCountdown(num: number): void {
    const freq = num === 0 ? 1760 : 880;
    const dur = num === 0 ? 0.35 : 0.15;
    this.playBeep(freq, dur, 'triangle');
  }

  public startEngineRumble(): void {
    if (this.isMuted || !this.ensureContext() || !this.ctx) return;
    this.stopEngineRumble();

    const bufferSize = this.ctx.sampleRate * 2;
    const noiseBuffer = this.ctx.createBuffer(1, bufferSize, this.ctx.sampleRate);
    const output = noiseBuffer.getChannelData(0);

    let lastOut = 0.0;
    for (let i = 0; i < bufferSize; i++) {
      const white = Math.random() * 2 - 1;
      output[i] = (lastOut + 0.02 * white) / 1.02; // Brown noise approximation
      lastOut = output[i];
    }

    const whiteNoise = this.ctx.createBufferSource();
    whiteNoise.buffer = noiseBuffer;
    whiteNoise.loop = true;

    // Low-pass filter for thunderous engine roar
    const filter = this.ctx.createBiquadFilter();
    filter.type = 'lowpass';
    filter.frequency.setValueAtTime(140, this.ctx.currentTime);

    this.engineGainNode = this.ctx.createGain();
    this.engineGainNode.gain.setValueAtTime(0.01, this.ctx.currentTime);
    this.engineGainNode.gain.linearRampToValueAtTime(0.45, this.ctx.currentTime + 0.3);

    whiteNoise.connect(filter);
    filter.connect(this.engineGainNode);
    this.engineGainNode.connect(this.ctx.destination);

    whiteNoise.start();
    this.engineRumbleNode = whiteNoise;
  }

  public stopEngineRumble(): void {
    if (this.engineGainNode && this.ctx) {
      try {
        this.engineGainNode.gain.linearRampToValueAtTime(0.001, this.ctx.currentTime + 0.2);
        setTimeout(() => {
          if (this.engineRumbleNode) {
            this.engineRumbleNode.stop();
            this.engineRumbleNode.disconnect();
            this.engineRumbleNode = null;
          }
        }, 220);
      } catch {
        this.engineRumbleNode = null;
      }
    }
  }

  public playExplosion(isBoss = false): void {
    if (this.isMuted || !this.ensureContext() || !this.ctx) return;

    const duration = isBoss ? 1.4 : 0.8;
    const bufferSize = this.ctx.sampleRate * duration;
    const buffer = this.ctx.createBuffer(1, bufferSize, this.ctx.sampleRate);
    const data = buffer.getChannelData(0);

    // Deep sub-bass boom + crunchy blast noise
    for (let i = 0; i < bufferSize; i++) {
      const t = i / this.ctx.sampleRate;
      const decay = Math.pow(1 - t / duration, 2.2);
      const sub = Math.sin(2 * Math.PI * (65 - t * 35) * t);
      const noise = (Math.random() * 2 - 1) * 0.7;
      data[i] = (sub * 0.5 + noise * 0.5) * decay * 0.8;
    }

    const source = this.ctx.createBufferSource();
    source.buffer = buffer;

    const gain = this.ctx.createGain();
    gain.gain.setValueAtTime(isBoss ? 0.7 : 0.5, this.ctx.currentTime);

    source.connect(gain);
    gain.connect(this.ctx.destination);

    source.start();
  }

  public playTargetHit(isBullseye = false): void {
    if (isBullseye) {
      this.playBeep(1200, 0.1, 'sine');
      setTimeout(() => this.playBeep(1800, 0.25, 'sine'), 80);
    } else {
      this.playBeep(920, 0.12, 'sine');
    }
  }

  public toggleMute(): boolean {
    this.isMuted = !this.isMuted;
    if (this.isMuted) this.stopEngineRumble();
    return this.isMuted;
  }
}

