using System;
using System.Collections.Generic;
using UnityEngine;

namespace ShapeDrop
{
    public enum State { Menu, Play, Pause, Dying, Over, Won }

    /// <summary>Độ khó: 150 màn. Tăng tốc mỗi 10 màn, lỗ hẹp dần mỗi màn.</summary>
    public static class Difficulty
    {
        public const int MaxLevel = 150;
        public const int MaxMargin = 7;              // khe hở lớn nhất (px mỗi phía) ở màn 1
        public const float TravelSec = 2.0f;         // nhịp Flappy Bird: ~2 giây đi hết màn hình
        public const float IntervalSec = 1.45f;      // ~1,45 giây lại có một tường mới

        static int Lc(int L) { return Mathf.Min(L, MaxLevel); }
        public static int Tier(int L) { return (Lc(L) - 1) / 10; }
        public static float SpeedMult(int L) { return 1f + Tier(L) * 0.08f; }          // x1,00 → x2,12 ở màn 150
        public static float Tolerance(int L) { return 1.2f + MaxMargin * (1f - (Lc(L) - 1) / (float)(MaxLevel - 1)); }
        public static int Margin(int L) { return Mathf.Max(0, Mathf.RoundToInt(Tolerance(L) - 1.2f)); }

        /// <summary>(số lỗ đúng hình, số lỗ bẫy)</summary>
        public static void Layout(int L, out int ok, out int traps)
        {
            if (L >= MaxLevel) { ok = 1; traps = 0; }
            else if (L <= 5) { ok = 3; traps = 0; }
            else if (L <= 20) { ok = 2; traps = 1; }
            else if (L <= 50) { ok = 2; traps = 2; }
            else if (L <= 100) { ok = 1; traps = 2; }
            else { ok = 1; traps = 1; }
        }
    }

    public class GameCore
    {
        public const int W = 180, WH = 34;
        const int S = Shapes.S, N = Shapes.N;

        public int H = 300, PY = 262;
        float baseSpeed = 150, spacing = 218;

        public State state = State.Menu;
        public Px px;

        // sự kiện gửi ra giao diện
        public Action<int> OnHud, OnPass, OnDie;
        public Action OnMorph, OnHit, OnWin;

        class Hole { public int x, s, m, w; public bool ok; }
        class Wall { public float y; public int L, shape; public float tol; public List<Hole> holes; public Color32[] img; public bool cleared; }
        struct Part { public float x, y, vx, vy, life; public Color32 c; public bool heavy; }
        struct Star { public float x, y, b; }

        readonly List<Wall> walls = new List<Wall>();
        readonly List<int> queue = new List<int>();
        readonly List<Part> parts = new List<Part>();
        Star[] stars = new Star[0];
        Color32[] bg;
        int spawnIdx, passIdx, shape;
        float playerX = W / 2f, shake, flash, morphT, dieT, clock, demoT;
        float dir; bool slow;
        readonly System.Random rng = new System.Random();

        static readonly Color32 Dark = Px.Hex("#0d0e17"), Lip = Px.Hex("#f4f4f4");
        static readonly string[][] Worlds =
        {
            new[] { "#566c86", "#94b0c2", "#333c57" }, new[] { "#257179", "#38b764", "#163f45" }, new[] { "#8a5a44", "#c28569", "#4e3330" },
            new[] { "#3b5dc9", "#41a6f6", "#29366f" }, new[] { "#5d275d", "#b13e53", "#371737" }, new[] { "#5a6b2e", "#a7f070", "#2f3a18" },
            new[] { "#b13e53", "#ef7d57", "#5d275d" }, new[] { "#a0785a", "#ffcd75", "#5c4033" }, new[] { "#2a3048", "#566c86", "#12131d" },
            new[] { "#b8862e", "#ffe08a", "#6b4e1e" },
        };
        readonly Dictionary<string, bool[]> cutCache = new Dictionary<string, bool[]>();

        public int Level { get { return passIdx + 1; } }
        float Rnd() { return (float)rng.NextDouble(); }
        int RndI(int n) { return rng.Next(n); }

        public GameCore() { shape = RndI(Shapes.All.Length); Resize(300); }

        /* ---------------- kích thước & nền ---------------- */
        public void Resize(float h)
        {
            H = Mathf.Clamp(Mathf.RoundToInt(h), 300, 420);
            PY = H - 38;
            baseSpeed = (PY + WH) / Difficulty.TravelSec;
            spacing = Mathf.Round(baseSpeed * Difficulty.IntervalSec);
            if (px != null && px.tex != null) UnityEngine.Object.Destroy(px.tex);
            px = new Px(W, H);
            BuildBackground();
        }

        void BuildBackground()
        {
            bg = new Color32[W * H];
            var bands = new[] { Px.Hex("#1a1c2c"), Px.Hex("#1e2237"), Px.Hex("#232a48"), Px.Hex("#29366f") };
            for (int y = 0; y < H; y++)
                for (int x = 0; x < W; x++)
                {
                    float t = y / (float)(PY - 40) * 3f; int b = Mathf.Min(3, (int)t); float fr = t - b;
                    int k = fr > .5f && (x + y) % 2 == 0 && b < 3 ? b + 1 : b;
                    bg[y * W + x] = bands[k];
                }
            int seed = 7;
            Func<float> r = () => { seed = (int)((seed * 16807L) % 2147483647); return seed / 2147483647f; };
            Color32 bld = Px.Hex("#333c57"), edge = Px.Hex("#29366f"), win1 = Px.Hex("#ffcd75"), win2 = Px.Hex("#73eff7");
            int xx = 0;
            while (xx < W)
            {
                int bw = 8 + (int)(r() * 14), bh = 24 + (int)(r() * 56), top = PY + 12 - bh;
                for (int y = top; y < PY + 12; y++)
                    for (int x = xx; x < xx + bw && x < W; x++)
                        if (y >= 0) bg[y * W + x] = x == xx + bw - 1 ? edge : bld;
                for (int wy = PY + 15 - bh; wy < PY + 6; wy += 4)
                    for (int wx = xx + 2; wx < xx + bw - 2 && wx < W; wx += 3)
                        if (r() < .22f) { var c = r() < .7f ? win1 : win2; if (wy >= 0) { bg[wy * W + wx] = c; bg[(wy + 1) * W + wx] = c; } }
                xx += bw + 1;
            }
            Color32 grass = Px.Hex("#257179"), line = Px.Hex("#38b764"), dot = Px.Hex("#1a4a52");
            for (int y = PY + 12; y < H; y++)
                for (int x = 0; x < W; x++)
                    bg[y * W + x] = y == PY + 12 ? line : ((y - PY - 15) % 3 == 0 && y > PY + 14 && (x - (y % 2) * 2) % 4 == 0 ? dot : grass);
            stars = new Star[H / 6];
            for (int i = 0; i < stars.Length; i++) stars[i] = new Star { x = Rnd() * W, y = Rnd() * (PY - 60), b = Rnd() };
        }

        /* ---------------- màn chơi ---------------- */
        int ShapeAt(int i)
        {
            while (queue.Count <= i)
            {
                int prev = queue.Count > 0 ? queue[queue.Count - 1] : -1;
                float p = Mathf.Min(.7f, .3f + queue.Count * .004f);
                if (prev < 0 || Rnd() < p) { int s; do s = RndI(Shapes.All.Length); while (s == prev); queue.Add(s); }
                else queue.Add(prev);
            }
            return queue[i];
        }

        bool[] CutFor(int s, int m, out int G)
        {
            string k = s + "_" + m;
            bool[] a;
            if (!cutCache.TryGetValue(k, out a)) { a = Shapes.Cut(s, m, out G); cutCache[k] = a; }
            G = S + 2 * m;
            return a;
        }

        void SpawnWall(float y0)
        {
            int L = spawnIdx + 1, s = ShapeAt(spawnIdx++), m = Difficulty.Margin(L), ok, traps;
            Difficulty.Layout(L, out ok, out traps);
            int hw = S + 2 * m + 2;
            var holes = new List<Hole>();
            Func<int, bool> fits = x => { foreach (var h in holes) if (Mathf.Abs(h.x - x) < (hw + h.w) / 2f + 3) return false; return true; };
            Action<int, bool> place = (sh, good) =>
            {
                for (int t = 0; t < 80; t++)
                {
                    int x = Mathf.RoundToInt(hw / 2f + 2 + Rnd() * (W - hw - 4));
                    if (fits(x)) { holes.Add(new Hole { x = x, s = sh, m = m, ok = good, w = hw }); return; }
                }
            };
            for (int i = 0; i < ok; i++) place(s, true);
            var pool = new List<int>();
            for (int i = 0; i < Shapes.All.Length; i++) if (i != s) pool.Add(i);
            for (int i = 0; i < traps && pool.Count > 0; i++) { int k = RndI(pool.Count); place(pool[k], false); pool.RemoveAt(k); }
            walls.Add(new Wall { y = y0, L = L, shape = s, holes = holes, tol = Difficulty.Tolerance(L), img = BuildWall(holes, L) });
        }

        Color32[] BuildWall(List<Hole> holes, int L)
        {
            var w = Worlds[Mathf.Min(Worlds.Length - 1, (L - 1) / 15)];   // màu gạch đổi mỗi 15 màn
            Color32 B = Px.Hex(w[0]), Li = Px.Hex(w[1]), M = Px.Hex(w[2]);
            var img = new Color32[W * WH];
            for (int y = 0; y < WH; y++)
                for (int x = 0; x < W; x++)
                {
                    int r = (y + 1) / 6, yy = (y + 1) % 6, xx = (x + (r % 2 == 1 ? 6 : 0)) % 12;
                    Color32 c = B;
                    if (yy == 5 || xx == 11) c = M; else if (yy == 0 || xx == 0) c = Li; else if ((x * 7 + y * 13) % 11 == 0) c = M;
                    if (y == 0) c = Li; if (y == WH - 1) c = Dark;
                    img[y * W + x] = c;
                }
            foreach (var h in holes)
            {
                int G; var a = CutFor(h.s, h.m, out G);
                int ox = h.x - (S >> 1) - h.m, oy = Mathf.RoundToInt(WH / 2f - G / 2f);
                Func<int, int, bool> at = (i, j) => i >= 0 && j >= 0 && i < G && j < G && a[j * G + i];
                for (int j = -1; j <= G; j++)
                    for (int i = -1; i <= G; i++)
                    {
                        int X = ox + i, Y = oy + j;
                        if (X < 0 || Y < 0 || X >= W || Y >= WH) continue;
                        if (at(i, j)) img[Y * W + X] = Px.Clear;
                        else if (at(i - 1, j) || at(i + 1, j) || at(i, j - 1) || at(i, j + 1)) img[Y * W + X] = j < G / 2 ? Dark : Lip;
                    }
            }
            return img;
        }

        void Burst(float x, float y, Color32 c, int n, float sp)
        {
            for (int i = 0; i < n; i++)
            {
                float a = Rnd() * Mathf.PI * 2, v = sp * (.3f + Rnd());
                parts.Add(new Part { x = x, y = y, vx = Mathf.Cos(a) * v, vy = Mathf.Sin(a) * v - 20, life = .4f + Rnd() * .5f, c = c });
            }
        }

        public void Start()
        {
            walls.Clear(); queue.Clear(); parts.Clear();
            passIdx = spawnIdx = 0; playerX = W / 2f; dir = 0;
            shape = ShapeAt(0); morphT = .4f; state = State.Play;
            SpawnWall(26);                       // tường đầu tiên xuất hiện ngay
            if (OnHud != null) OnHud(Level);
        }

        public void ToMenu() { walls.Clear(); parts.Clear(); dir = 0; state = State.Menu; }
        public bool Pause() { if (state != State.Play) return false; state = State.Pause; dir = 0; return true; }
        public void Resume() { if (state == State.Pause) state = State.Play; }
        public void Endless() { if (state == State.Won) state = State.Play; }
        public void SetDir(float d) { dir = d; }
        public void SetSlow(bool s) { slow = s; }
        public void DragBy(float dx) { if (state == State.Play) playerX = Mathf.Clamp(playerX + dx, S / 2f, W - S / 2f); }

        void Die()
        {
            state = State.Dying; dieT = .65f; shake = .45f; flash = .25f;
            if (OnHit != null) OnHit();
            int x0 = Mathf.RoundToInt(playerX) - 8, y0 = PY - 8; var m = Shapes.Masks[shape];
            for (int y = 0; y < N; y++)
                for (int x = 0; x < N; x++)
                    if (m[y * N + x])
                    {
                        float a = Mathf.Atan2(y - 7, x - 7), v = 30 + Rnd() * 70;
                        parts.Add(new Part { x = x0 + x + 1, y = y0 + y + 1, vx = Mathf.Cos(a) * v, vy = Mathf.Sin(a) * v - 40, life = .8f + Rnd() * .6f, c = Rnd() < .3f ? Lip : Shapes.All[shape].color, heavy = true });
                    }
        }

        void Passed(Wall w)
        {
            passIdx++;
            Burst(playerX, PY, Shapes.All[w.shape].color, 12, 70);
            if (passIdx == Difficulty.MaxLevel)
            {
                state = State.Won;
                if (OnWin != null) OnWin();
                if (OnHud != null) OnHud(Difficulty.MaxLevel);
                return;
            }
            if (OnPass != null) OnPass(Level);
            int ns = ShapeAt(passIdx);
            if (ns != shape) { shape = ns; morphT = .35f; if (OnMorph != null) OnMorph(); Burst(playerX, PY, Lip, 10, 40); }
            if (OnHud != null) OnHud(Level);
        }

        /* ---------------- cập nhật ---------------- */
        public void Update(float dt)
        {
            clock += dt;
            int L = Level; float fall = baseSpeed * Difficulty.SpeedMult(L);
            for (int i = 0; i < stars.Length; i++)
            {
                stars[i].y += (state == State.Play ? fall * .12f : 6f) * dt;
                if (stars[i].y > PY - 60) { stars[i].y = 0; stars[i].x = Rnd() * W; }
            }
            for (int i = parts.Count - 1; i >= 0; i--)
            {
                var p = parts[i];
                p.x += p.vx * dt; p.y += p.vy * dt; p.vy += (p.heavy ? 160 : 60) * dt; p.life -= dt;
                if (p.life <= 0) parts.RemoveAt(i); else parts[i] = p;
            }
            shake = Mathf.Max(0, shake - dt); flash = Mathf.Max(0, flash - dt); morphT = Mathf.Max(0, morphT - dt);

            if (state == State.Menu)
            {
                demoT -= dt;
                if (demoT <= 0) { demoT = 1.1f; shape = (shape + 1 + RndI(Shapes.All.Length - 1)) % Shapes.All.Length; morphT = .25f; }
                playerX = W / 2f + Mathf.Sin(clock * 1.3f) * 60; return;
            }
            if (state == State.Dying) { dieT -= dt; if (dieT <= 0) { state = State.Over; if (OnDie != null) OnDie(L); } return; }
            if (state != State.Play) return;

            if (dir != 0) playerX += dir * 190f * Difficulty.SpeedMult(L) * (slow ? .3f : 1f) * dt;
            playerX = Mathf.Clamp(playerX, S / 2f, W - S / 2f);

            if (walls.Count == 0 || walls[walls.Count - 1].y >= -WH + spacing) SpawnWall(-WH);
            for (int i = 0; i < walls.Count; i++)
            {
                var w = walls[i];
                w.y += fall * dt;
                if (w.cleared) continue;
                if (w.y + WH >= PY - 7 && w.y <= PY + 8)
                {
                    bool safe = false;
                    foreach (var h in w.holes) if (h.ok && Mathf.Abs(playerX - h.x) <= w.tol) { safe = true; break; }
                    if (!safe) { Die(); return; }
                }
                else if (w.y > PY + 8) { w.cleared = true; Passed(w); if (state != State.Play) return; }
            }
            walls.RemoveAll(w => w.y >= H + 2);
        }

        /* ---------------- vẽ ---------------- */
        static readonly Color32 StarOn = Px.Hex("#f4f4f4"), StarOff = Px.Hex("#566c86"), Shadow = new Color32(13, 14, 23, 110);
        static readonly Color32 Mute = Px.Hex("#94b0c2"), Hot = Px.Hex("#ffcd75"), Edge = Px.Hex("#566c86");

        public void Render()
        {
            px.Copy(bg);
            int ox = 0, oy = 0;
            if (shake > 0) { ox = Mathf.RoundToInt((Rnd() - .5f) * shake * 14); oy = Mathf.RoundToInt((Rnd() - .5f) * shake * 14); }
            foreach (var s in stars) px.Set((int)s.x, (int)s.y, Mathf.Sin(clock * 3 + s.b * 9) > .3f ? StarOn : StarOff);
            foreach (var w in walls)
            {
                int y = Mathf.RoundToInt(w.y) + oy;
                px.Rect(0, y + WH, W, 2, Shadow, true);
                px.Blit(w.img, W, WH, ox, y);
            }
            if (state != State.Dying && state != State.Over)
            {
                int x0 = Mathf.RoundToInt(playerX) - 8 + ox, bob = state == State.Menu ? Mathf.RoundToInt(Mathf.Sin(clock * 4)) : 0;
                px.Rect(x0 + 3, PY + 11, 11, 2, Shadow, true);
                bool blink = morphT > 0 && ((int)(morphT * 20)) % 2 == 0;
                px.Blit(blink ? Shapes.White[shape] : Shapes.Sprites[shape], S, S, x0, PY - 8 + bob + oy);
            }
            foreach (var p in parts) px.Set(Mathf.RoundToInt(p.x), Mathf.RoundToInt(p.y), p.c);
            if (state != State.Menu)
            {
                int nx = ShapeAt(passIdx + 1), bx = W - 25, by = 3;
                bool chg = nx != shape;
                px.Rect(bx, by, 22, 30, Dark);
                var bc = chg && ((int)(clock * 4)) % 2 == 1 ? Hot : Edge;
                px.Rect(bx, by, 22, 1, bc); px.Rect(bx, by + 29, 22, 1, bc); px.Rect(bx, by, 1, 30, bc); px.Rect(bx + 21, by, 1, 30, bc);
                px.Text("NEXT", bx + 4, by + 3, Mute);
                px.Blit(Shapes.Sprites[nx], S, S, bx + 2, by + 11);
            }
            if (flash > 0) px.Rect(0, 0, W, H, new Color32(177, 62, 83, (byte)Mathf.Clamp(flash * 2 * 255, 0, 255)), true);
            px.Upload();
        }
    }
}
