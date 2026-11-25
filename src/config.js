// =====================================================================
//  CẤU HÌNH QUẢNG CÁO — sửa file này trước khi phát hành
// =====================================================================
export const CONFIG = {
  // true = quảng cáo THỬ của Google (an toàn khi phát triển).
  // Đổi thành false CHỈ khi đã thay ID thật bên dưới và chuẩn bị phát hành.
  // Tự bấm vào quảng cáo thật của chính mình có thể khiến tài khoản AdMob bị khoá.
  ADS_TESTING: true,

  // ID đơn vị quảng cáo xen kẽ (Interstitial ad unit ID) lấy trong admob.google.com.
  // Hai ID dưới đây là ID THỬ chính thức của Google.
  admob: {
    android: { interstitial: 'ca-app-pub-3940256099942544/1033173712' },
    ios: { interstitial: 'ca-app-pub-3940256099942544/4411468910' },
  },

  // Hiện quảng cáo sau mỗi N lượt thua (khi bấm Chơi lại), cách nhau tối thiểu X giây.
  interstitialEveryRuns: 3,
  interstitialMinGapSec: 60,

  // Đường dẫn chính sách quyền riêng tư (bắt buộc khi có quảng cáo).
  privacyUrl: 'https://example.com/shape-drop/privacy',
};
