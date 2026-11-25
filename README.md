# Shape Drop

Game pixel né tường rơi: bạn là một hình học và phải chui qua đúng lỗ mang hình của mình. Game có 1000 màn, chạy trên Android, iOS và trình duyệt.

Dự án dùng **Capacitor 8 + Vite**, không phụ thuộc game engine nào. Quảng cáo chạy qua **Google AdMob** (dạng quảng cáo xen kẽ).

## Chạy nhanh

```bash
npm install
npm run dev        # chơi thử trên trình duyệt
npm run android    # mở Android Studio (cần cài Android Studio)
npm run ios        # mở Xcode (cần máy Mac)
```

Không cài Android Studio thì đẩy dự án lên GitHub. Workflow trong `.github/workflows/android.yml` sẽ tự build ra file APK để cài thử, và file AAB để nộp lên Google Play.

## Cấu trúc

| Đường dẫn | Nội dung |
|---|---|
| `src/game.js` | Lõi game: hình học, tường, độ khó, vẽ pixel |
| `src/main.js` | Giao diện, điều khiển, lưu kỷ lục, nút Back Android |
| `src/ads.js` | AdMob: hỏi đồng ý GDPR/ATT, quảng cáo xen kẽ |
| `src/config.js` | **ID quảng cáo, nhịp quảng cáo, đường dẫn chính sách**, cần sửa trước khi phát hành |
| `src/i18n.js` | Chữ tiếng Việt / tiếng Anh |
| `resources/` | Ảnh gốc icon và màn hình khởi động (`npm run assets` để tạo lại) |
| `android/`, `ios/` | Project gốc cho từng nền tảng |
| `docs/HUONG-DAN-XUAT-BAN.md` | **Hướng dẫn từng bước đưa app lên CH Play và App Store** |
| `docs/privacy-policy.html` | Chính sách quyền riêng tư (song ngữ) |
| `docs/store-listing.md` | Tên, mô tả, từ khoá cho trang cửa hàng |

## Độ khó

- **Tốc độ khởi đầu** theo nhịp Flappy Bird: tường đi hết màn hình trong khoảng 2 giây, và cứ khoảng 1,45 giây lại có một tường mới.
- **Mỗi 10 màn**, tốc độ rơi tăng 1,2%. Đến màn 1000 tốc độ gấp khoảng 2,2 lần lúc đầu.
- **Mỗi màn**, khe hở quanh lỗ hẹp lại, đến màn 1000 thì vừa khít.
- **Số lỗ:** màn 1–10 có 3 lỗ đúng hình; màn 11–250 có 2 lỗ đúng cộng lỗ bẫy; màn 251–999 có 1 lỗ đúng cộng lỗ bẫy; màn 1000 chỉ còn đúng 1 lỗ.
- Thua là chơi lại từ màn 1.

Các con số này nằm ở phần đầu `src/game.js` (`TRAVEL_SEC`, `INTERVAL_SEC`, `speedMult`, `tolOf`, `layout`).
