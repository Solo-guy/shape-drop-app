using UnityEngine;

namespace ShapeDrop
{
    /// <summary>Bộ đệm pixel tự vẽ (hàng 0 ở TRÊN), đẩy lên Texture2D lọc Point để giữ nét pixel.</summary>
    public class Px
    {
        public static readonly Color32 Clear = new Color32(0, 0, 0, 0);

        public int W, H;
        public Color32[] buf;
        public Texture2D tex;
        Color32[] flipped;

        public Px(int w, int h)
        {
            W = w; H = h;
            buf = new Color32[w * h];
            flipped = new Color32[w * h];
            tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            tex.wrapMode = TextureWrapMode.Clamp;
        }

        public static Color32 Hex(string h)
        {
            int n = System.Convert.ToInt32(h.Substring(1), 16);
            return new Color32((byte)(n >> 16), (byte)((n >> 8) & 255), (byte)(n & 255), 255);
        }

        public static Color32 Shade(Color32 c, float a)
        {
            System.Func<byte, byte> f = v =>
            {
                float r = a > 0 ? v + (255 - v) * a : v * (1 + a);
                return (byte)Mathf.Clamp(Mathf.RoundToInt(r), 0, 255);
            };
            return new Color32(f(c.r), f(c.g), f(c.b), 255);
        }

        public void Set(int x, int y, Color32 c)
        {
            if (x < 0 || y < 0 || x >= W || y >= H) return;
            buf[y * W + x] = c;
        }

        public void Blend(int x, int y, Color32 c)
        {
            if (x < 0 || y < 0 || x >= W || y >= H) return;
            int i = y * W + x; var d = buf[i]; float a = c.a / 255f;
            buf[i] = new Color32((byte)(d.r + (c.r - d.r) * a), (byte)(d.g + (c.g - d.g) * a), (byte)(d.b + (c.b - d.b) * a), 255);
        }

        public void Rect(int x, int y, int w, int h, Color32 c, bool blend = false)
        {
            for (int j = y; j < y + h; j++)
                for (int i = x; i < x + w; i++)
                    if (blend) Blend(i, j, c); else Set(i, j, c);
        }

        /// <summary>Vẽ ảnh nguồn (bỏ qua pixel trong suốt).</summary>
        public void Blit(Color32[] src, int sw, int sh, int dx, int dy)
        {
            for (int y = 0; y < sh; y++)
            {
                int ty = dy + y; if (ty < 0 || ty >= H) continue;
                for (int x = 0; x < sw; x++)
                {
                    int tx = dx + x; if (tx < 0 || tx >= W) continue;
                    var c = src[y * sw + x];
                    if (c.a == 0) continue;
                    if (c.a == 255) buf[ty * W + tx] = c; else Blend(tx, ty, c);
                }
            }
        }

        public void Copy(Color32[] src) { System.Array.Copy(src, buf, buf.Length); }

        /// <summary>Đẩy lên GPU (Texture2D có hàng 0 ở DƯỚI nên lật dọc).</summary>
        public void Upload()
        {
            for (int y = 0; y < H; y++) System.Array.Copy(buf, y * W, flipped, (H - 1 - y) * W, W);
            tex.SetPixels32(flipped);
            tex.Apply(false);
        }

        // chữ pixel 3x5 tối giản cho ô NEXT
        static readonly System.Collections.Generic.Dictionary<char, string> Font = new System.Collections.Generic.Dictionary<char, string>
        {
            { 'N', "101111111101101" }, { 'E', "111100110100111" }, { 'X', "101101010101101" }, { 'T', "111010010010010" },
        };

        public void Text(string s, int x, int y, Color32 c)
        {
            for (int k = 0; k < s.Length; k++)
            {
                string g;
                if (!Font.TryGetValue(s[k], out g)) continue;
                for (int p = 0; p < 15; p++) if (g[p] == '1') Set(x + k * 4 + p % 3, y + p / 3, c);
            }
        }
    }
}
