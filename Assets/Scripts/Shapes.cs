using System;
using UnityEngine;

namespace ShapeDrop
{
    /// <summary>10 hình học, mặt nạ pixel 15x15 và sprite 17x17 (có viền + đổ bóng).</summary>
    public static class Shapes
    {
        public const int N = 15;      // kích thước hình
        public const int S = 17;      // hình + viền 1px

        public struct Def
        {
            public string vi, en;
            public Color32 color;
            public Func<float, float, bool> inside;
        }

        public static readonly Def[] All;
        public static readonly bool[][] Masks;
        public static readonly Color32[][] Sprites;
        public static readonly Color32[][] White;

        static Def D(string vi, string en, string hex, Func<float, float, bool> f)
        {
            return new Def { vi = vi, en = en, color = Px.Hex(hex), inside = f };
        }

        static Func<float, float, bool> Poly(float[,] p)
        {
            int n = p.GetLength(0);
            return (x, y) =>
            {
                bool c = false;
                for (int i = 0, j = n - 1; i < n; j = i++)
                {
                    float xi = p[i, 0], yi = p[i, 1], xj = p[j, 0], yj = p[j, 1];
                    if ((yi > y) != (yj > y) && x < (xj - xi) * (y - yi) / (yj - yi) + xi) c = !c;
                }
                return c;
            };
        }

        static float[,] Regular(int n, float r, float rot)
        {
            var p = new float[n, 2];
            for (int i = 0; i < n; i++)
            {
                float a = rot + i * 2f * Mathf.PI / n;
                p[i, 0] = Mathf.Cos(a) * r; p[i, 1] = Mathf.Sin(a) * r;
            }
            return p;
        }

        static float[,] Star()
        {
            var p = new float[10, 2];
            for (int i = 0; i < 10; i++)
            {
                float a = -Mathf.PI / 2 + i * Mathf.PI / 5, r = i % 2 == 1 ? .44f : 1.02f;
                p[i, 0] = Mathf.Cos(a) * r; p[i, 1] = Mathf.Sin(a) * r + .1f;
            }
            return p;
        }

        static Shapes()
        {
            All = new[]
            {
                D("Hình tròn", "Circle", "#41a6f6", (x, y) => x * x + y * y <= .92f),
                D("Hình vuông", "Square", "#e0505f", (x, y) => Mathf.Abs(x) <= .8f && Mathf.Abs(y) <= .8f),
                D("Tam giác", "Triangle", "#38b764", Poly(new float[,] { { 0, -.95f }, { .98f, .82f }, { -.98f, .82f } })),
                D("Hình thoi", "Diamond", "#ffcd75", (x, y) => Mathf.Abs(x) + Mathf.Abs(y) <= 1f),
                D("Ngôi sao", "Star", "#ef7d57", Poly(Star())),
                D("Lục giác", "Hexagon", "#73eff7", Poly(Regular(6, 1f, 0f))),
                D("Chữ thập", "Cross", "#f4f4f4", (x, y) => (Mathf.Abs(x) <= .34f && Mathf.Abs(y) <= .95f) || (Mathf.Abs(y) <= .34f && Mathf.Abs(x) <= .95f)),
                D("Trái tim", "Heart", "#ff77a8", (x, y) =>
                {
                    float X = x * 1.18f, Y = -y * 1.2f + .15f, q = X * X + Y * Y - 1f;
                    return q * q * q - X * X * Y * Y * Y <= 0f;
                }),
                D("Ngũ giác", "Pentagon", "#a7f070", Poly(Regular(5, 1f, -Mathf.PI / 2))),
                D("Trăng khuyết", "Crescent", "#c2c3c7", (x, y) => x * x + y * y <= .92f && (x - .42f) * (x - .42f) + (y + .22f) * (y + .22f) > .5f),
            };

            Masks = new bool[All.Length][];
            Sprites = new Color32[All.Length][];
            White = new Color32[All.Length][];
            for (int i = 0; i < All.Length; i++)
            {
                var m = new bool[N * N];
                for (int y = 0; y < N; y++)
                    for (int x = 0; x < N; x++)
                        m[y * N + x] = All[i].inside((x + .5f) / N * 2 - 1, (y + .5f) / N * 2 - 1);
                Masks[i] = m;
                Sprites[i] = MakeSprite(i, false);
                White[i] = MakeSprite(i, true);
            }
        }

        public static bool In(bool[] m, int x, int y) { return x >= 0 && y >= 0 && x < N && y < N && m[y * N + x]; }

        static bool Near(bool[] m, int x, int y, int r)
        {
            for (int j = -r; j <= r; j++)
                for (int i = -r; i <= r; i++)
                    if (In(m, x + i, y + j)) return true;
            return false;
        }

        /// <summary>Sprite S x S, hàng 0 ở TRÊN cùng.</summary>
        static Color32[] MakeSprite(int i, bool flat)
        {
            var px = new Color32[S * S];
            var m = Masks[i];
            Color32 col = All[i].color, light = Px.Shade(col, .45f), dark = Px.Shade(col, -.35f);
            Color32 white = Px.Hex("#f4f4f4"), outline = Px.Hex("#0d0e17");
            for (int py = 0; py < S; py++)
                for (int pxx = 0; pxx < S; pxx++)
                {
                    int x = pxx - 1, y = py - 1;
                    Color32 c = Px.Clear;
                    if (In(m, x, y))
                    {
                        c = flat ? white : col;
                        if (!flat)
                        {
                            if (!In(m, x, y - 1) || !In(m, x - 1, y)) c = light;
                            else if (!In(m, x, y + 1) || !In(m, x + 1, y)) c = dark;
                        }
                    }
                    else if (Near(m, x, y, 1)) c = flat ? white : outline;
                    px[py * S + pxx] = c;
                }
            if (!flat)
            {
                // điểm bóng sáng
                for (int y = 0; y < N; y++)
                    for (int x = 0; x < N; x++)
                        if (In(m, x, y) && In(m, x + 1, y) && In(m, x, y + 1) && In(m, x + 1, y + 1) && In(m, x - 1, y) && In(m, x, y - 1))
                        {
                            Color32 sh = new Color32(255, 255, 255, 220);
                            px[(y + 2) * S + x + 2] = sh; px[(y + 2) * S + x + 3] = sh; px[(y + 3) * S + x + 2] = sh;
                            return px;
                        }
            }
            return px;
        }

        /// <summary>Lỗ trên tường: chính hình đó phóng to thêm m px mỗi phía (giữ nguyên dáng).</summary>
        public static bool[] Cut(int s, int m, out int G)
        {
            G = S + 2 * m;
            int I = G - 2;
            var ins = new bool[I * I];
            var f = All[s].inside;
            for (int y = 0; y < I; y++)
                for (int x = 0; x < I; x++)
                    ins[y * I + x] = f((x + .5f) / I * 2 - 1, (y + .5f) / I * 2 - 1);
            var a = new bool[G * G];
            for (int y = 0; y < G; y++)
                for (int x = 0; x < G; x++)
                {
                    bool o = false;
                    for (int j = -1; j <= 1 && !o; j++)
                        for (int i = -1; i <= 1 && !o; i++)
                        {
                            int X = x - 1 + i, Y = y - 1 + j;
                            if (X >= 0 && Y >= 0 && X < I && Y < I && ins[Y * I + X]) o = true;
                        }
                    a[y * G + x] = o;
                }
            return a;
        }
    }
}
