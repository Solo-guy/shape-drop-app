using System.Collections.Generic;

namespace ShapeDrop
{
    /// <summary>Chữ tiếng Việt / tiếng Anh.</summary>
    public static class Loc
    {
        public static string Lang = "vi";

        static readonly Dictionary<string, string[]> T = new Dictionary<string, string[]>
        {
            // key            tiếng Việt                                   English
            { "tag",        new[] { "Chui đúng lỗ — vượt qua 150 màn",       "Fit the hole — survive 150 levels" } },
            { "lvl",        new[] { "MÀN",                                    "LEVEL" } },
            { "spd",        new[] { "TỐC ĐỘ",                                 "SPEED" } },
            { "best",       new[] { "KỶ LỤC",                                 "BEST" } },
            { "tap",        new[] { "CHẠM ĐỂ CHƠI",                           "TAP TO PLAY" } },
            { "how",        new[] { "Cách chơi",                              "How to play" } },
            { "langBtn",    new[] { "English",                                "Tiếng Việt" } },
            { "soundOn",    new[] { "Âm thanh: Bật",                          "Sound: On" } },
            { "soundOff",   new[] { "Âm thanh: Tắt",                          "Sound: Off" } },
            { "pause",      new[] { "Tạm dừng",                               "Paused" } },
            { "resume",     new[] { "TIẾP TỤC",                               "RESUME" } },
            { "menu",       new[] { "Menu",                                   "Menu" } },
            { "overT",      new[] { "Vỡ rồi!",                                "Shattered!" } },
            { "overS",      new[] { "Bạn dừng ở màn",                         "You reached level" } },
            { "newRec",     new[] { "Kỷ lục mới!",                            "New record!" } },
            { "recIs",      new[] { "Kỷ lục: màn ",                           "Best: level " } },
            { "again",      new[] { "CHƠI LẠI",                               "PLAY AGAIN" } },
            { "winT",       new[] { "Chinh phục 150 màn!",                    "All 150 levels cleared!" } },
            { "winS",       new[] { "Bạn đã chui qua lỗ duy nhất, vừa khít, ở tốc độ tối đa.", "You squeezed through the single, tight hole at top speed." } },
            { "endless",    new[] { "CHƠI TIẾP",                              "KEEP GOING" } },
            { "endlessT",   new[] { "Chế độ vô tận",                          "Endless mode" } },
            { "faster",     new[] { "Tốc độ tăng: x",                         "Speed up: x" } },
            { "last",       new[] { "Màn 150 — chỉ còn 1 lỗ!",                "Level 150 — only one hole left!" } },
            { "close",      new[] { "ĐÓNG",                                   "CLOSE" } },
            { "quit",       new[] { "Thoát",                                  "Quit" } },
            { "howText",    new[] {
                "• Bạn là một hình học. Kéo chuột/ngón tay sang trái–phải để di chuyển (máy tính: phím ← → hoặc A D, giữ Shift để đi chậm).\n\n" +
                "• Mỗi bức tường rơi là một màn. Chỉ lỗ ĐÚNG HÌNH bạn mới cho đi qua — lỗ khác là bẫy.\n\n" +
                "• Ô NEXT góc trên cho biết bạn sẽ biến thành hình gì sau bức tường này.\n\n" +
                "• Cứ 10 màn tốc độ rơi tăng một bậc. Mỗi màn khe hở quanh lỗ hẹp lại; màn 150 chỉ còn đúng 1 lỗ vừa khít.\n\n" +
                "• Thua là chơi lại từ màn 1.",
                "• You are a shape. Drag the mouse/your finger left–right to move (PC: ← → or A D, hold Shift to move slowly).\n\n" +
                "• Each falling wall is one level. Only the hole MATCHING YOUR SHAPE lets you through — the others are traps.\n\n" +
                "• The NEXT box shows the shape you will become after this wall.\n\n" +
                "• Every 10 levels the walls fall faster. Each level the clearance around the hole shrinks; level 150 has a single perfect-fit hole.\n\n" +
                "• Lose and you start again from level 1." } },
        };

        public static string Get(string key)
        {
            string[] v;
            if (!T.TryGetValue(key, out v)) return key;
            return Lang == "en" ? v[1] : v[0];
        }
    }
}
