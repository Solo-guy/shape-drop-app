import '@fontsource/press-start-2p/400.css';
import '@fontsource/be-vietnam-pro/400.css';
import '@fontsource/be-vietnam-pro/600.css';
import '@fontsource/be-vietnam-pro/800.css';
import './style.css';
import { createGame, SHAPES, SPR, S, W, MAXL, speedMult, tolOf, MAXM } from './game.js';
import { TX } from './i18n.js';
import { ads } from './ads.js';
import { CONFIG } from './config.js';
import { storage, haptic, audio, nativeSetup, exitApp } from './platform.js';

const $ = id => document.getElementById(id);
const cv = $('c'), stage = $('stage');
let lang = 'vi', best = 0, scale = 1, overAt = 0;
const t = k => TX[lang][k];

/* ---------------- game ---------------- */
const game = createGame(cv, {
  onHud: L => hud(L),
  onPass: L => {
    audio.pass(); haptic.tap();
    if (L > best) { best = L; storage.set('best', best); }
    if ((L - 1) % 10 === 0) { audio.up(); toast(L === MAXL ? t('last') : t('faster') + speedMult(L).toFixed(2)); }
  },
  onMorph: () => audio.morph(),
  onHit: () => { audio.die(); haptic.hit(); },
  onDie: L => gameOver(L),
  onWin: () => { best = Math.max(best, MAXL); storage.set('best', best); audio.win(); haptic.win(); show('win'); },
});

function hud(L = game.level()) {
  $('lv').textContent = L;
  $('spd').textContent = 'x' + speedMult(L).toFixed(2);
  $('bs').textContent = best;
  $('prog').style.width = Math.min(100, (L - 1) / (MAXL - 1) * 100) + '%';
  $('gap').style.width = Math.max(1, (tolOf(L) - 1.2) / MAXM * 100) + '%';
}
function toast(s) { const el = $('toast'); el.textContent = s; el.classList.remove('on'); void el.offsetWidth; el.classList.add('on'); }
const OVS = ['menu', 'pause', 'over', 'win', 'how'];
function show(id) { OVS.forEach(k => { $(k).hidden = k !== id; }); $('pBtn').style.visibility = (id === null || id === 'pause') ? 'visible' : 'hidden'; }

function play() {
  audio.unlock(); audio.start(); haptic.tap();
  show(null); fit(true); game.start();
}
async function retry() {
  if (performance.now() - overAt < 350) return;   // tránh chạm nhầm ngay khi vừa thua
  await ads.beforeRetry();
  play();
}
function gameOver(L) {
  overAt = performance.now();
  const rec = L >= best && L > 1;
  if (L > best) { best = L; storage.set('best', best); }
  $('finalLv').textContent = L;
  const p = $('overMsg'); p.className = rec ? 'rec' : ''; p.textContent = rec ? t('newRec') : t('recIs') + best;
  hud(L); show('over');
}
function toMenu() { game.toMenu(); show('menu'); hud(1); fit(true); }
function pause() { if (game.pause()) show('pause'); }
function resume() { game.resume(); show(null); }

/* ---------------- kích thước: canvas lấp đầy màn hình dọc ---------------- */
function fit(force) {
  const r = stage.getBoundingClientRect();
  if (!r.width || !r.height) return;
  if (force || game.state === 'menu' || game.state === 'over') game.resize(W * r.height / r.width);
  const H = game.height();
  scale = Math.min(r.width / W, r.height / H);
  cv.style.width = W * scale + 'px'; cv.style.height = H * scale + 'px';
}
new ResizeObserver(() => fit(false)).observe(stage);

/* ---------------- ngôn ngữ ---------------- */
function applyLang() {
  document.documentElement.lang = lang;
  document.querySelectorAll('[data-i]').forEach(e => { e.innerHTML = t(e.dataset.i); });
  $('langBtn').textContent = lang === 'vi' ? 'English' : 'Tiếng Việt';
  $('ppLink').href = CONFIG.privacyUrl;
  const lg = $('legend'); lg.innerHTML = '';
  SHAPES.forEach((s, i) => {
    const li = document.createElement('li'), c = document.createElement('canvas'); c.width = c.height = S;
    c.getContext('2d').drawImage(SPR[i], 0, 0);
    const sp = document.createElement('span'); sp.textContent = s.n[lang]; li.append(c, sp); lg.append(li);
  });
  if (game.state === 'over') { const p = $('overMsg'); p.textContent = p.className === 'rec' ? t('newRec') : t('recIs') + best; }
}
function setSoundIcon() {
  $('sndIco').innerHTML = audio.muted
    ? '<path d="M1 3h2V2h1V1h1v6H4V6H3V5H1z"/><path d="M6 3h1v1H6zM7 2h1v1H7zM7 4h1v1H7z"/>'
    : '<path d="M1 3h2V2h1V1h1v6H4V6H3V5H1z"/><path d="M6 2h1v4H6z"/>';
}

/* ---------------- điều khiển ---------------- */
// Chạm vào menu (ngoài nút) là chơi ngay
$('menu').addEventListener('click', e => { if (!e.target.closest('button,a')) play(); });
$('langBtn').onclick = () => { lang = lang === 'vi' ? 'en' : 'vi'; storage.set('lang', lang); applyLang(); };
$('sndBtn').onclick = () => { audio.muted = !audio.muted; storage.set('muted', audio.muted); setSoundIcon(); };
$('howBtn').onclick = () => show('how');
$('howClose').onclick = () => show('menu');
$('pvBtn').onclick = () => ads.showPrivacyOptions();
$('pBtn').onclick = () => { if (game.state === 'pause') resume(); else pause(); };
$('resumeBtn').onclick = resume;
$('pMenuBtn').onclick = toMenu; $('oMenuBtn').onclick = toMenu; $('wMenuBtn').onclick = toMenu;
$('againBtn').onclick = retry;
$('endlessBtn').onclick = () => { game.endless(); show(null); toast(t('endlessT')); };

// Kéo ngón tay: nhân vật đi theo đúng khoảng cách ngón tay (chính xác cả ở lỗ vừa khít)
let dragId = null, dragX = 0;
stage.addEventListener('pointerdown', e => {
  if (game.state !== 'play' || e.target.closest('button,a,.ov')) return;
  dragId = e.pointerId; dragX = e.clientX; try { stage.setPointerCapture(e.pointerId); } catch (_) { /* */ }
});
stage.addEventListener('pointermove', e => { if (e.pointerId !== dragId) return; game.dragBy((e.clientX - dragX) / scale); dragX = e.clientX; });
const endDrag = e => { if (e.pointerId === dragId) dragId = null; };
stage.addEventListener('pointerup', endDrag); stage.addEventListener('pointercancel', endDrag);

// Bàn phím (máy tính)
const keys = { l: 0, r: 0 };
const syncDir = () => game.setDir(keys.r - keys.l);
addEventListener('keydown', e => {
  const k = e.key.toLowerCase();
  if (['arrowleft', 'arrowright', ' ', 'arrowup', 'arrowdown'].includes(k)) e.preventDefault();
  if (k === 'arrowleft' || k === 'a') { keys.l = 1; syncDir(); }
  else if (k === 'arrowright' || k === 'd') { keys.r = 1; syncDir(); }
  else if (k === 'shift') game.setSlow(true);
  else if (e.repeat) return;
  else if (k === ' ' || k === 'enter') { if (game.state === 'menu') play(); else if (game.state === 'over') retry(); else if (game.state === 'pause') resume(); }
  else if (k === 'p' || k === 'escape') { if (game.state === 'pause') resume(); else pause(); }
});
addEventListener('keyup', e => {
  const k = e.key.toLowerCase();
  if (k === 'arrowleft' || k === 'a') { keys.l = 0; syncDir(); }
  else if (k === 'arrowright' || k === 'd') { keys.r = 0; syncDir(); }
  else if (k === 'shift') game.setSlow(false);
});
addEventListener('blur', () => { keys.l = keys.r = 0; syncDir(); pause(); });
document.addEventListener('visibilitychange', () => { if (document.hidden) pause(); });

/* ---------------- khởi động ---------------- */
(async function boot() {
  const saved = await storage.get('lang', null);
  lang = saved === 'vi' || saved === 'en' ? saved : ((navigator.language || 'vi').toLowerCase().startsWith('vi') ? 'vi' : 'en');
  best = await storage.get('best', 0);
  audio.muted = await storage.get('muted', false);
  applyLang(); setSoundIcon(); hud(1); fit(true); show('menu');
  await nativeSetup({
    onPause: () => pause(),
    onBack: () => {
      const s = game.state;
      if (s === 'play') pause();
      else if (s === 'pause' || s === 'over' || s === 'won') toMenu();
      else if (!$('how').hidden) show('menu');
      else exitApp();
    },
  });
  ads.init().then(() => { $('pvBtn').hidden = !ads.state.privacyRequired; });
})();
