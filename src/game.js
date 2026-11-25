// Shape Drop — game engine (canvas pixel art, không phụ thuộc thư viện)
export const W = 180, S = 17, N = 15, WH = 34, MAXL = 1000, MAXM = 7;
// Nhịp Flappy Bird: vật cản đi hết màn hình trong ~2 giây, cứ ~1,45 giây lại có một cái mới.
const TRAVEL_SEC = 2.0, INTERVAL_SEC = 1.45;

const ab = Math.abs, rand = n => Math.floor(Math.random() * n);
const mk = (w, h) => { const c = document.createElement('canvas'); c.width = w; c.height = h; return c; };
const rgb = h => { const n = parseInt(h.slice(1), 16); return [n >> 16, n >> 8 & 255, n & 255]; };

/* ---------- hình học ---------- */
const poly = pts => (x, y) => { let c = false; for (let i = 0, j = pts.length - 1; i < pts.length; j = i++) { const [xi, yi] = pts[i], [xj, yj] = pts[j]; if ((yi > y) !== (yj > y) && x < (xj - xi) * (y - yi) / (yj - yi) + xi) c = !c; } return c; };
const reg = (n, r, rot) => Array.from({ length: n }, (_, i) => { const a = rot + i * 2 * Math.PI / n; return [Math.cos(a) * r, Math.sin(a) * r]; });
const star = Array.from({ length: 10 }, (_, i) => { const a = -Math.PI / 2 + i * Math.PI / 5, r = i % 2 ? .44 : 1.02; return [Math.cos(a) * r, Math.sin(a) * r + .1]; });

export const SHAPES = [
  { n: { vi: 'Hình tròn', en: 'Circle' }, c: '#41a6f6', f: (x, y) => x * x + y * y <= .92 },
  { n: { vi: 'Hình vuông', en: 'Square' }, c: '#e0505f', f: (x, y) => ab(x) <= .8 && ab(y) <= .8 },
  { n: { vi: 'Tam giác', en: 'Triangle' }, c: '#38b764', f: poly([[0, -.95], [.98, .82], [-.98, .82]]) },
  { n: { vi: 'Hình thoi', en: 'Diamond' }, c: '#ffcd75', f: (x, y) => ab(x) + ab(y) <= 1 },
  { n: { vi: 'Ngôi sao', en: 'Star' }, c: '#ef7d57', f: poly(star) },
  { n: { vi: 'Lục giác', en: 'Hexagon' }, c: '#73eff7', f: poly(reg(6, 1, 0)) },
  { n: { vi: 'Chữ thập', en: 'Cross' }, c: '#f4f4f4', f: (x, y) => (ab(x) <= .34 && ab(y) <= .95) || (ab(y) <= .34 && ab(x) <= .95) },
  { n: { vi: 'Trái tim', en: 'Heart' }, c: '#ff77a8', f: (x, y) => { const X = x * 1.18, Y = -y * 1.2 + .15, q = X * X + Y * Y - 1; return q * q * q - X * X * Y * Y * Y <= 0; } },
  { n: { vi: 'Ngũ giác', en: 'Pentagon' }, c: '#a7f070', f: poly(reg(5, 1, -Math.PI / 2)) },
  { n: { vi: 'Trăng khuyết', en: 'Crescent' }, c: '#c2c3c7', f: (x, y) => x * x + y * y <= .92 && (x - .42) ** 2 + (y + .22) ** 2 > .5 },
];

const shade = (hex, a) => { const f = v => Math.max(0, Math.min(255, Math.round(a > 0 ? v + (255 - v) * a : v * (1 + a)))); const [r, g, b] = rgb(hex); return `rgb(${f(r)},${f(g)},${f(b)})`; };
const masks = SHAPES.map(s => { const m = new Uint8Array(N * N); for (let y = 0; y < N; y++) for (let x = 0; x < N; x++) m[y * N + x] = s.f((x + .5) / N * 2 - 1, (y + .5) / N * 2 - 1) ? 1 : 0; return m; });
const inM = (m, x, y) => x >= 0 && y >= 0 && x < N && y < N && m[y * N + x] === 1;
const near = (m, x, y, r) => { for (let j = -r; j <= r; j++) for (let i = -r; i <= r; i++) if (inM(m, x + i, y + j)) return true; return false; };

function sprite(i, flat) {
  const c = mk(S, S), g = c.getContext('2d'), m = masks[i], col = SHAPES[i].c;
  for (let py = 0; py < S; py++) for (let px = 0; px < S; px++) {
    const x = px - 1, y = py - 1; let f = null;
    if (inM(m, x, y)) { f = flat || col; if (!flat) { if (!inM(m, x, y - 1) || !inM(m, x - 1, y)) f = shade(col, .45); else if (!inM(m, x, y + 1) || !inM(m, x + 1, y)) f = shade(col, -.35); } }
    else if (near(m, x, y, 1)) f = flat || '#0d0e17';
    if (f) { g.fillStyle = f; g.fillRect(px, py, 1, 1); }
  }
  if (!flat) {
    g.fillStyle = 'rgba(255,255,255,.85)';
    outer: for (let y = 0; y < N; y++) for (let x = 0; x < N; x++) if (inM(m, x, y) && inM(m, x + 1, y) && inM(m, x, y + 1) && inM(m, x + 1, y + 1) && inM(m, x - 1, y) && inM(m, x, y - 1)) { g.fillRect(x + 2, y + 2, 2, 1); g.fillRect(x + 2, y + 3, 1, 1); break outer; }
  }
  return c;
}
export const SPR = SHAPES.map((_, i) => sprite(i));
const WHITE = SHAPES.map((_, i) => sprite(i, '#f4f4f4'));

/* lỗ = chính hình đó phóng to thêm m pixel mỗi phía */
const DC = {};
function cut(s, m) {
  const k = s + '_' + m; if (DC[k]) return DC[k];
  const G = S + 2 * m, I = G - 2, a = new Uint8Array(G * G), f = SHAPES[s].f, ins = new Uint8Array(I * I);
  for (let y = 0; y < I; y++) for (let x = 0; x < I; x++) ins[y * I + x] = f((x + .5) / I * 2 - 1, (y + .5) / I * 2 - 1) ? 1 : 0;
  const at = (x, y) => x >= 0 && y >= 0 && x < I && y < I && ins[y * I + x] === 1;
  for (let y = 0; y < G; y++) for (let x = 0; x < G; x++) { let o = 0; for (let j = -1; j <= 1 && !o; j++) for (let i = -1; i <= 1; i++) if (at(x - 1 + i, y - 1 + j)) { o = 1; break; } a[y * G + x] = o; }
  return DC[k] = { G, a };
}

/* ---------- tường: màu gạch đổi mỗi 100 màn ---------- */
const WORLDS = [['#566c86', '#94b0c2', '#333c57'], ['#257179', '#38b764', '#163f45'], ['#8a5a44', '#c28569', '#4e3330'], ['#3b5dc9', '#41a6f6', '#29366f'], ['#5d275d', '#b13e53', '#371737'],
  ['#5a6b2e', '#a7f070', '#2f3a18'], ['#b13e53', '#ef7d57', '#5d275d'], ['#a0785a', '#ffcd75', '#5c4033'], ['#2a3048', '#566c86', '#12131d'], ['#b8862e', '#ffe08a', '#6b4e1e']].map(w => w.map(rgb));
const DARK = rgb('#0d0e17'), LIP = rgb('#f4f4f4');
function buildWall(holes, L) {
  const c = mk(W, WH), g = c.getContext('2d'), img = g.createImageData(W, WH), d = img.data;
  const [B, Li, M] = WORLDS[Math.min(9, Math.floor((L - 1) / 100))];
  const put = (x, y, col, a = 255) => { if (x < 0 || y < 0 || x >= W || y >= WH) return; const o = (y * W + x) * 4; d[o] = col[0]; d[o + 1] = col[1]; d[o + 2] = col[2]; d[o + 3] = a; };
  for (let y = 0; y < WH; y++) for (let x = 0; x < W; x++) {
    const r = Math.floor((y + 1) / 6), yy = (y + 1) % 6, xx = (x + (r % 2 ? 6 : 0)) % 12;
    let col = B; if (yy === 5 || xx === 11) col = M; else if (yy === 0 || xx === 0) col = Li; else if ((x * 7 + y * 13) % 11 === 0) col = M;
    if (y === 0) col = Li; if (y === WH - 1) col = DARK; put(x, y, col);
  }
  for (const h of holes) {
    const { G, a } = cut(h.s, h.m), ox = h.x - (S >> 1) - h.m, oy = Math.round(WH / 2 - G / 2);
    const at = (i, j) => i >= 0 && j >= 0 && i < G && j < G && a[j * G + i] === 1;
    for (let j = -1; j <= G; j++) for (let i = -1; i <= G; i++) {
      if (at(i, j)) put(ox + i, oy + j, DARK, 0);
      else if (at(i - 1, j) || at(i + 1, j) || at(i, j - 1) || at(i, j + 1)) put(ox + i, oy + j, j < G / 2 ? DARK : LIP);
    }
  }
  g.putImageData(img, 0, 0); return c;
}

/* ---------- độ khó ---------- */
const lvC = L => Math.min(L, MAXL);
export const tier = L => Math.floor((lvC(L) - 1) / 10);
export const speedMult = L => 1 + tier(L) * 0.012;      // +1,2% mỗi 10 màn → x2,19 ở màn 1000

export const tolOf = L => 1.2 + MAXM * (1 - (lvC(L) - 1) / (MAXL - 1)); // hẹp dần mỗi màn
function layout(L) { if (L >= MAXL) return [1, 0]; if (L <= 10) return [3, 0]; if (L <= 40) return [2, 1]; if (L <= 250) return [2, 2]; if (L <= 500) return [1, 2]; return [1, 1]; }

/* ---------- chữ pixel 3x5 ---------- */
const FONT = { N: '101111111101101', E: '111100110100111', X: '101101010101101', T: '111010010010010', ' ': '000000000000000' };

export function createGame(cv, hooks) {
  const ctx = cv.getContext('2d');
  let H = 300, PY = 262, BG = null, stars = [], BASE = 150, SPACING = 218;
  const fallSpeed = L => BASE * speedMult(L);
  const moveSpeed = L => 190 * speedMult(L);

  function resize(h) {
    H = Math.max(300, Math.min(420, Math.round(h))); PY = H - 38;
    BASE = (PY + WH) / TRAVEL_SEC; SPACING = Math.round(BASE * INTERVAL_SEC);
    cv.width = W; cv.height = H; ctx.imageSmoothingEnabled = false;
    BG = mk(W, H); const g = BG.getContext('2d'); let seed = 7; const rnd = () => (seed = (seed * 16807) % 2147483647) / 2147483647;
    const bands = ['#1a1c2c', '#1e2237', '#232a48', '#29366f'];
    for (let y = 0; y < H; y++) for (let x = 0; x < W; x++) { const t = y / (PY - 40) * 3, b = Math.min(3, Math.floor(t)), fr = t - b; const k = fr > .5 && (x + y) % 2 === 0 && b < 3 ? b + 1 : b; g.fillStyle = bands[k]; g.fillRect(x, y, 1, 1); }
    let x = 0; while (x < W) {
      const bw = 8 + Math.floor(rnd() * 14), bh = 24 + Math.floor(rnd() * 56);
      g.fillStyle = '#333c57'; g.fillRect(x, PY + 12 - bh, bw, bh); g.fillStyle = '#29366f'; g.fillRect(x + bw - 1, PY + 12 - bh, 1, bh);
      for (let wy = PY + 15 - bh; wy < PY + 6; wy += 4) for (let wx = x + 2; wx < x + bw - 2; wx += 3) if (rnd() < .22) { g.fillStyle = rnd() < .7 ? '#ffcd75' : '#73eff7'; g.fillRect(wx, wy, 1, 2); }
      x += bw + 1;
    }
    g.fillStyle = '#257179'; g.fillRect(0, PY + 12, W, H - PY - 12); g.fillStyle = '#38b764'; g.fillRect(0, PY + 12, W, 1);
    g.fillStyle = '#1a4a52'; for (let y = PY + 15; y < H; y += 3) for (let x2 = (y % 2) * 2; x2 < W; x2 += 4) g.fillRect(x2, y, 1, 1);
    stars = Array.from({ length: Math.round(H / 6) }, () => ({ x: Math.random() * W, y: Math.random() * (PY - 60), b: Math.random() }));
  }

  let state = 'menu', walls = [], queue = [], spawnIdx = 0, passIdx = 0, px = W / 2, shape = rand(SHAPES.length);
  let parts = [], shake = 0, flash = 0, morphT = 0, dieT = 0, clock = 0, demoT = 0, dir = 0, slow = false;
  const level = () => passIdx + 1;

  function shapeAt(i) {
    while (queue.length <= i) {
      const prev = queue[queue.length - 1], p = Math.min(.7, .3 + queue.length * .0005);
      if (prev === undefined || Math.random() < p) { let s; do s = rand(SHAPES.length); while (s === prev); queue.push(s); } else queue.push(prev);
    }
    return queue[i];
  }
  function spawnWall(y0 = -WH) {
    const L = spawnIdx + 1, s = shapeAt(spawnIdx++), tol = tolOf(L), m = Math.max(0, Math.round(tol - 1.2)), [k, dk] = layout(L);
    const hw = S + 2 * m + 2, holes = []; const fits = x => holes.every(h => ab(h.x - x) >= (hw + h.w) / 2 + 3);
    const place = (sh, ok) => { for (let t = 0; t < 80; t++) { const x = Math.round(hw / 2 + 2 + Math.random() * (W - hw - 4)); if (fits(x)) { holes.push({ x, s: sh, m, ok, w: hw }); return; } } };
    for (let i = 0; i < k; i++) place(s, true);
    const pool = [...Array(SHAPES.length).keys()].filter(i => i !== s).sort(() => Math.random() - .5);
    for (let i = 0; i < dk; i++) place(pool[i], false);
    walls.push({ y: y0, L, shape: s, holes, tol, cv: buildWall(holes, L), cleared: false });
  }
  function burst(x, y, col, n, sp) { for (let i = 0; i < n; i++) { const a = Math.random() * Math.PI * 2, v = sp * (.3 + Math.random()); parts.push({ x, y, vx: Math.cos(a) * v, vy: Math.sin(a) * v - 20, l: .4 + Math.random() * .5, c: col }); } }

  function start() {
    walls = []; queue = []; passIdx = spawnIdx = 0; px = W / 2; parts = []; dir = 0;
    shape = shapeAt(0); morphT = .4; state = 'play';
    spawnWall(26);                       // tường đầu tiên xuất hiện ngay, không phải chờ
    hooks.onHud(level());
  }
  function die() {
    state = 'dying'; dieT = .65; shake = .45; flash = .25; hooks.onHit();
    const x0 = Math.round(px) - 8, y0 = PY - 8, m = masks[shape];
    for (let y = 0; y < N; y++) for (let x = 0; x < N; x++) if (m[y * N + x]) { const a = Math.atan2(y - 7, x - 7), v = 30 + Math.random() * 70; parts.push({ x: x0 + x + 1, y: y0 + y + 1, vx: Math.cos(a) * v, vy: Math.sin(a) * v - 40, l: .8 + Math.random() * .6, c: Math.random() < .3 ? '#f4f4f4' : SHAPES[shape].c, g: 1 }); }
  }
  function passed(w) {
    passIdx++; const L = level();
    burst(Math.round(px), PY, SHAPES[w.shape].c, 12, 70);
    if (passIdx === MAXL) { state = 'won'; hooks.onWin(); hooks.onHud(MAXL); return; }
    hooks.onPass(L);
    const ns = shapeAt(passIdx); if (ns !== shape) { shape = ns; morphT = .35; hooks.onMorph(); burst(Math.round(px), PY, '#f4f4f4', 10, 40); }
    hooks.onHud(L);
  }

  function update(dt) {
    clock += dt; const L = level(), fall = fallSpeed(L);
    for (const s of stars) { s.y += (state === 'play' ? fall * .12 : 6) * dt; if (s.y > PY - 60) { s.y = 0; s.x = Math.random() * W; } }
    parts.forEach(p => { p.x += p.vx * dt; p.y += p.vy * dt; p.vy += (p.g ? 160 : 60) * dt; p.l -= dt; }); parts = parts.filter(p => p.l > 0);
    shake = Math.max(0, shake - dt); flash = Math.max(0, flash - dt); morphT = Math.max(0, morphT - dt);
    if (state === 'menu') { demoT -= dt; if (demoT <= 0) { demoT = 1.1; shape = (shape + 1 + rand(SHAPES.length - 1)) % SHAPES.length; morphT = .25; } px = W / 2 + Math.sin(clock * 1.3) * 60; return; }
    if (state === 'dying') { dieT -= dt; if (dieT <= 0) { state = 'over'; hooks.onDie(level()); } return; }
    if (state !== 'play') return;
    if (dir) px += dir * moveSpeed(L) * (slow ? .3 : 1) * dt;
    px = Math.max(S / 2, Math.min(W - S / 2, px));
    const lw = walls[walls.length - 1]; if (!lw || lw.y >= -WH + SPACING) spawnWall();
    for (const w of walls) {
      w.y += fall * dt;
      if (w.cleared) continue;
      if (w.y + WH >= PY - 7 && w.y <= PY + 8) { if (!w.holes.some(h => h.ok && ab(px - h.x) <= w.tol)) { die(); return; } }
      else if (w.y > PY + 8) { w.cleared = true; passed(w); if (state !== 'play') return; }
    }
    walls = walls.filter(w => w.y < H + 2);
  }

  function text(s, x, y, col) { ctx.fillStyle = col; [...s].forEach((ch, i) => { const gl = FONT[ch] || FONT[' ']; for (let p = 0; p < 15; p++) if (gl[p] === '1') ctx.fillRect(x + i * 4 + p % 3, y + Math.floor(p / 3), 1, 1); }); }
  function draw() {
    ctx.save();
    if (shake > 0) ctx.translate(Math.round((Math.random() - .5) * shake * 14), Math.round((Math.random() - .5) * shake * 14));
    ctx.drawImage(BG, 0, 0);
    for (const s of stars) { ctx.fillStyle = Math.sin(clock * 3 + s.b * 9) > .3 ? '#f4f4f4' : '#566c86'; ctx.fillRect(Math.round(s.x), Math.round(s.y), 1, 1); }
    for (const w of walls) { const y = Math.round(w.y); ctx.fillStyle = 'rgba(13,14,23,.35)'; ctx.fillRect(0, y + WH, W, 2); ctx.drawImage(w.cv, 0, y); }
    if (state !== 'dying' && state !== 'over') {
      const x0 = Math.round(px) - 8, bob = state === 'menu' ? Math.round(Math.sin(clock * 4)) : 0;
      ctx.fillStyle = 'rgba(13,14,23,.45)'; ctx.fillRect(x0 + 3, PY + 11, 11, 2);
      const blink = morphT > 0 && Math.floor(morphT * 20) % 2 === 0;
      ctx.drawImage(blink ? WHITE[shape] : SPR[shape], x0, PY - 8 + bob);
    }
    for (const p of parts) { ctx.fillStyle = p.c; ctx.fillRect(Math.round(p.x), Math.round(p.y), 1, 1); }
    if (state !== 'menu') {
      const nx = shapeAt(passIdx + 1), chg = nx !== shape, bx = W - 25, by = 3;
      ctx.fillStyle = '#0d0e17'; ctx.fillRect(bx, by, 22, 30); ctx.fillStyle = chg && Math.floor(clock * 4) % 2 ? '#ffcd75' : '#566c86';
      ctx.fillRect(bx, by, 22, 1); ctx.fillRect(bx, by + 29, 22, 1); ctx.fillRect(bx, by, 1, 30); ctx.fillRect(bx + 21, by, 1, 30);
      text('NEXT', bx + 4, by + 3, '#94b0c2'); ctx.drawImage(SPR[nx], bx + 2, by + 11);
    }
    ctx.restore();
    if (flash > 0) { ctx.fillStyle = `rgba(177,62,83,${flash * 2})`; ctx.fillRect(0, 0, W, H); }
  }

  let last = performance.now();
  function loop(t) { const dt = Math.min(.05, (t - last) / 1000); last = t; update(dt); draw(); requestAnimationFrame(loop); }
  resize(300); requestAnimationFrame(loop);

  return {
    start, resize,
    get state() { return state; }, set state(v) { state = v; },
    level, height: () => H,
    setDir(d) { dir = d; }, setSlow(v) { slow = v; },
    dragBy(dx) { if (state === 'play') px = Math.max(S / 2, Math.min(W - S / 2, px + dx)); },
    pause() { if (state === 'play') { state = 'pause'; dir = 0; return true; } return false; },
    resume() { if (state === 'pause') state = 'play'; },
    endless() { if (state === 'won') state = 'play'; },
    toMenu() { walls = []; parts = []; dir = 0; state = 'menu'; },
  };
}
