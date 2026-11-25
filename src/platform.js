// Lớp nền tảng: lưu trữ, rung, thanh trạng thái, nút Back, âm thanh
import { Capacitor } from '@capacitor/core';
import { Preferences } from '@capacitor/preferences';
import { Haptics, ImpactStyle, NotificationType } from '@capacitor/haptics';
import { StatusBar } from '@capacitor/status-bar';
import { SplashScreen } from '@capacitor/splash-screen';
import { App } from '@capacitor/app';

export const native = Capacitor.isNativePlatform();

export const storage = {
  async get(key, def) {
    try { const { value } = await Preferences.get({ key }); return value == null ? def : JSON.parse(value); }
    catch (e) { return def; }
  },
  set(key, v) { Preferences.set({ key, value: JSON.stringify(v) }).catch(() => {}); },
};

export const haptic = {
  on: true,
  tap() { if (native && this.on) Haptics.impact({ style: ImpactStyle.Light }).catch(() => {}); },
  hit() { if (native && this.on) Haptics.notification({ type: NotificationType.Error }).catch(() => {}); },
  win() { if (native && this.on) Haptics.notification({ type: NotificationType.Success }).catch(() => {}); },
};

export async function nativeSetup({ onPause, onBack }) {
  if (!native) return;
  StatusBar.hide().catch(() => {});
  App.addListener('pause', onPause);
  App.addListener('backButton', onBack);
  setTimeout(() => SplashScreen.hide().catch(() => {}), 150);
}
export const exitApp = () => { if (native) App.exitApp(); };

/* ---------- âm thanh chiptune tổng hợp, không cần file ---------- */
let ac = null;
export const audio = {
  muted: false,
  unlock() { try { ac = ac || new (window.AudioContext || window.webkitAudioContext)(); if (ac.state === 'suspended') ac.resume(); } catch (e) { /* */ } },
  tone(f, d, type = 'square', v = .05, slide = 0, delay = 0) {
    if (this.muted) return;
    try {
      this.unlock(); const t = ac.currentTime + delay, o = ac.createOscillator(), g = ac.createGain();
      o.type = type; o.frequency.setValueAtTime(f, t); if (slide) o.frequency.exponentialRampToValueAtTime(Math.max(30, f + slide), t + d);
      g.gain.setValueAtTime(v, t); g.gain.exponentialRampToValueAtTime(.0001, t + d);
      o.connect(g); g.connect(ac.destination); o.start(t); o.stop(t + d + .03);
    } catch (e) { /* */ }
  },
  start() { this.tone(523, .06); this.tone(784, .1, 'square', .05, 0, .07); },
  pass() { this.tone(660, .06); this.tone(990, .09, 'square', .05, 0, .06); },
  morph() { this.tone(330, .2, 'triangle', .08, 500); },
  up() { [523, 659, 784, 1046].forEach((f, i) => this.tone(f, .08, 'square', .045, 0, i * .07)); },
  win() { [392, 523, 659, 784, 1046, 1318].forEach((f, i) => this.tone(f, .09, 'triangle', .07, 0, i * .08)); },
  die() { this.tone(220, .5, 'sawtooth', .08, -180); this.tone(110, .6, 'square', .05, -70, .05); },
};
