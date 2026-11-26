using System;

namespace ShapeDrop
{
    /// <summary>
    /// Điểm gắn quảng cáo. Hiện tại KHÔNG hiển thị quảng cáo nào (bản chơi thử).
    /// Khi muốn kiếm tiền: cài gói Google Mobile Ads cho Unity, thêm "ADMOB" vào
    /// Player Settings > Scripting Define Symbols, rồi điền mã ở nhánh #if ADMOB.
    /// Nhịp gợi ý: cứ 3 lượt thua mới hiện 1 quảng cáo xen kẽ, cách nhau tối thiểu 60 giây.
    /// </summary>
    public static class Ads
    {
        public const int EveryRuns = 3;
        public const float MinGapSec = 60f;
        static int runs;

        public static void BeforeRetry(Action continueGame)
        {
            runs++;
#if ADMOB
            // TODO: nếu runs % EveryRuns == 0 và đủ MinGapSec, hiện InterstitialAd rồi gọi continueGame khi đóng.
#endif
            continueGame();
        }
    }
}
