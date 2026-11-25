# Hướng dẫn đưa Shape Drop lên Google Play (CH Play) và App Store

Tài liệu này đi theo thứ tự bạn sẽ làm. Mỗi bước có ghi rõ chi phí và thời gian chờ.

---

## 0. Tổng quan chi phí và thời gian

| Hạng mục | Chi phí | Ghi chú |
|---|---|---|
| Tài khoản Google Play Console | 25 USD, trả 1 lần | Cần xác minh danh tính (CCCD/hộ chiếu) |
| Tài khoản Apple Developer | 99 USD/năm | Bắt buộc để lên App Store |
| Máy Mac có Xcode | — | Chỉ cần cho bản iOS. Không có Mac thì thuê Mac trên mây (Codemagic, MacinCloud…) |
| AdMob | Miễn phí | Google trả tiền quảng cáo khi số dư đạt ngưỡng 100 USD |
| GitHub | Miễn phí | Dùng để build Android tự động |

**Thời gian thực tế:**
- **Android:** khoảng 3–4 tuần. Tài khoản cá nhân mới bắt buộc phải thử nghiệm kín với **ít nhất 12 người thử, liên tục 14 ngày**, rồi mới được xin phát hành chính thức. Tài khoản tổ chức (doanh nghiệp, cần số D-U-N-S) thì không bị yêu cầu này.
- **iOS:** xét duyệt thường mất 1–3 ngày, sau khi tài khoản Apple Developer được duyệt.

---

## 1. Chuẩn bị dự án (làm 1 lần)

Máy tính cần cài **Node.js 22** (tải ở nodejs.org). Mở terminal trong thư mục dự án và chạy:

```bash
npm install
npm run dev          # chơi thử trên trình duyệt: http://localhost:5173
```

### 1.1 Đặt mã định danh app
Mã định danh có dạng `com.tencuaban.shapedrop`. Nó phải là duy nhất trên toàn store, và **sau khi đã phát hành thì không đổi được nữa**.

```bash
node scripts/set-app-id.mjs com.tencuaban.shapedrop
```

### 1.2 Tên app, phiên bản
- Tên hiển thị: `appName` trong `capacitor.config.json`, và `app_name` trong `android/app/src/main/res/values/strings.xml`.
- Phiên bản: `versionName` trong `android/app/build.gradle`. Còn `versionCode` do quy trình build tự tăng.

### 1.3 Icon và màn hình khởi động
Ảnh gốc nằm trong thư mục `resources/`. Muốn thay thì sửa các ảnh đó, rồi chạy `npm run assets`.

---

## 2. Thiết lập AdMob (nguồn thu nhập)

1. Vào **admob.google.com** và đăng nhập bằng tài khoản Google.
2. **Ứng dụng → Thêm ứng dụng:** tạo 2 app, một cho Android và một cho iOS. Bạn nhận được 2 **App ID**, dạng `ca-app-pub-xxxxxxxx~yyyyyyyy`.
3. Trong từng app, tạo **đơn vị quảng cáo → Quảng cáo xen kẽ (Interstitial)**. Bạn nhận được 2 **Ad unit ID**, dạng `ca-app-pub-xxxxxxxx/zzzzzzzz`.
4. Điền các ID vào dự án:
   - **App ID Android:** trong `android/app/src/main/AndroidManifest.xml`, ở dòng `com.google.android.gms.ads.APPLICATION_ID`.
   - **App ID iOS:** trong `ios/App/App/Info.plist`, ở khoá `GADApplicationIdentifier`.
   - **Ad unit ID:** trong `src/config.js`, ở các mục `admob.android.interstitial` và `admob.ios.interstitial`.
5. **Quyền riêng tư & thông báo:** tạo thông báo **GDPR** (bắt buộc với người dùng châu Âu/Anh). Với iOS, tạo thêm thông báo giải thích **IDFA**. Code trong game đã tự hiện các thông báo này khi cần.
6. **app-ads.txt:** AdMob sẽ cho bạn một dòng nội dung. Đặt nó trong file `app-ads.txt` ở thư mục gốc website nhà phát triển (website khai báo trên store). Thiếu file này thì quảng cáo vẫn chạy, nhưng doanh thu thấp hơn.
7. **Chỉ khi chuẩn bị phát hành:** đổi `ADS_TESTING: true` thành `false` trong `src/config.js`.

> ⚠️ Trong lúc phát triển, **không bao giờ** tự bấm vào quảng cáo thật của chính mình. AdMob coi đó là gian lận và có thể khoá tài khoản vĩnh viễn. Khi đang thử, hãy để `ADS_TESTING: true`.

**Cách game hiện quảng cáo:** sau mỗi 3 lượt thua, khi người chơi bấm *Chơi lại*, game hiện 1 quảng cáo toàn màn hình. Hai quảng cáo cách nhau ít nhất 60 giây. Quảng cáo không bao giờ xuất hiện giữa lúc đang chơi. Bạn chỉnh nhịp này trong `src/config.js`.

---

## 3. Chính sách quyền riêng tư (bắt buộc)

App có quảng cáo thì cả hai store đều yêu cầu có trang chính sách quyền riêng tư.

1. Mở `docs/privacy-policy.html`. Thay `[EMAIL CỦA BẠN]` và `[TÊN NHÀ PHÁT TRIỂN]` bằng thông tin của bạn.
2. Đưa file đó lên mạng. Cách miễn phí: bật **GitHub Pages** cho kho code, chọn thư mục `/docs`. Đường dẫn sẽ có dạng `https://tenban.github.io/shape-drop/privacy-policy.html`.
3. Dán đường dẫn đó vào `privacyUrl` trong `src/config.js`, và vào trang khai báo trên cả hai store.

---

## 4. Android → Google Play

### 4.1 Đưa code lên GitHub
Tạo một kho (repository) **riêng tư** trên GitHub rồi đẩy thư mục dự án lên. Workflow `.github/workflows/android.yml` sẽ tự chạy. Vào tab **Actions → Android build → Artifacts** để tải `shape-drop-debug.apk` về, rồi cài thử lên điện thoại Android. Nếu máy báo chặn, hãy bật cho phép cài app từ nguồn không xác định.

### 4.2 Tạo khoá ký app (làm 1 lần, giữ cẩn thận suốt đời app)
Máy cần có Java (JDK 17 trở lên). Chạy lệnh sau:
```bash
keytool -genkeypair -v -keystore shape-drop-release.keystore -alias shapedrop \
  -keyalg RSA -keysize 2048 -validity 10000
```
Sao lưu file `.keystore` và mật khẩu ở **ít nhất 2 nơi an toàn**. **Không** đưa file này lên GitHub (dự án đã chặn sẵn trong `.gitignore`).

Chuyển file khoá sang chuỗi base64:
- macOS/Linux: `base64 -i shape-drop-release.keystore | pbcopy` (hoặc `base64 -w0 shape-drop-release.keystore`)
- Windows PowerShell: `[Convert]::ToBase64String([IO.File]::ReadAllBytes("shape-drop-release.keystore")) | Set-Clipboard`

Vào GitHub, mở **Settings → Secrets and variables → Actions → New repository secret** và tạo 4 secret:

| Tên secret | Giá trị |
|---|---|
| `ANDROID_KEYSTORE_BASE64` | chuỗi base64 vừa copy |
| `ANDROID_KEYSTORE_PASSWORD` | mật khẩu keystore |
| `ANDROID_KEY_ALIAS` | `shapedrop` |
| `ANDROID_KEY_PASSWORD` | mật khẩu key (thường giống mật khẩu keystore) |

Chạy lại workflow (**Actions → Android build → Run workflow**). Lần này phần Artifacts sẽ có thêm **`shape-drop-release.aab`**. Đây là file để nộp lên Google Play.

### 4.3 Google Play Console
1. Đăng ký tài khoản tại **play.google.com/console** (25 USD) và xác minh danh tính.
2. Chọn **Tạo ứng dụng**: nhập tên *Shape Drop*, chọn loại **Trò chơi**, chọn **Miễn phí**.
3. Hoàn thành mục **Thiết lập ứng dụng**:
   - *Chính sách quyền riêng tư:* dán đường dẫn ở bước 3.
   - *Quảng cáo:* chọn **Có, ứng dụng chứa quảng cáo**.
   - *Quyền truy cập ứng dụng:* chọn mọi chức năng đều dùng được không cần đăng nhập.
   - *Xếp hạng nội dung:* làm bảng câu hỏi IARC. Game không có bạo lực hay nội dung người lớn.
   - *Đối tượng mục tiêu:* chọn **13 tuổi trở lên**. Nếu chọn cả trẻ em, app phải theo chính sách Gia đình với quy định quảng cáo chặt hơn.
   - *An toàn dữ liệu:* xem mục 6 bên dưới.
   - *ID quảng cáo:* chọn **Có** (dùng cho quảng cáo).
4. Điền **Trang thông tin cửa hàng**: nội dung có sẵn trong `docs/store-listing.md`. Bạn cần icon 512×512 (lấy `resources/icon-only.png` rồi thu nhỏ), ảnh bìa 1024×500, và ít nhất 2 ảnh chụp màn hình điện thoại.
5. Vào **Thử nghiệm → Thử nghiệm kín**: tạo bản phát hành, tải file `.aab` lên, rồi thêm **ít nhất 12 người thử** bằng email Gmail của họ. Họ phải bấm tham gia và **giữ tham gia liên tục 14 ngày**, đồng thời thực sự mở game chơi. Nên mời 15–20 người để dự phòng có người bỏ giữa chừng.
6. Sau 14 ngày, chọn **Đăng ký quyền truy cập bản phát hành công khai**, trả lời bảng câu hỏi. Khi được duyệt thì đẩy bản phát hành lên **Bản chính thức (Production)**.

---

## 5. iOS → App Store

1. Đăng ký **Apple Developer Program** tại developer.apple.com (99 USD/năm).
2. Trên máy Mac có **Xcode** (bản mới nhất từ Mac App Store), chạy:
   ```bash
   npm install
   npm run ios          # build web, đồng bộ, rồi mở Xcode
   ```
3. Trong Xcode, chọn target **App**, mở tab **Signing & Capabilities**: chọn *Team* là tài khoản Apple của bạn, và kiểm tra *Bundle Identifier* trùng với mã ở bước 1.1.
4. Cắm iPhone vào máy và bấm ▶ để chạy thử trên máy thật.
5. Vào **App Store Connect** (appstoreconnect.apple.com), chọn **Ứng dụng → +**: tạo app mới với đúng Bundle ID đó.
6. Trong Xcode: chọn thiết bị *Any iOS Device*, rồi vào **Product → Archive → Distribute App → App Store Connect → Upload**.
7. Trong App Store Connect:
   - *Quyền riêng tư của ứng dụng (nhãn dinh dưỡng):* xem mục 6.
   - *Chính sách quyền riêng tư:* dán đường dẫn.
   - Điền mô tả, từ khoá (lấy trong `docs/store-listing.md`), ảnh chụp màn hình cỡ 6,9 inch, độ tuổi 4+ hoặc 9+.
   - Chọn bản build vừa tải lên, rồi bấm **Gửi xét duyệt**.
8. Muốn cho bạn bè thử trước khi phát hành thì dùng **TestFlight** (tab TestFlight trong App Store Connect).

**Không có máy Mac?** Bạn có thể dùng **Codemagic** (codemagic.io). Kết nối kho GitHub, chọn dự án Capacitor/Ionic, khai báo chứng chỉ Apple, và dịch vụ này tự build rồi tải lên App Store Connect. Gói miễn phí có giới hạn số phút build mỗi tháng.

Workflow `.github/workflows/ios-check.yml` chỉ dùng để kiểm tra code iOS biên dịch được trên máy Mac ảo của GitHub. Nó không tạo bản nộp store.

---

## 6. Khai báo dữ liệu (Data safety / App Privacy)

Bản thân game **không** thu thập dữ liệu cá nhân. Kỷ lục, ngôn ngữ và cài đặt âm thanh chỉ lưu trên máy người chơi. Riêng **SDK quảng cáo Google (AdMob)** có thu thập dữ liệu, nên phải khai báo:

| Loại dữ liệu | Mục đích | Ghi chú |
|---|---|---|
| ID thiết bị / ID quảng cáo | Quảng cáo, phân tích, chống gian lận | Có chia sẻ với Google |
| Tương tác ứng dụng (lượt xem/bấm quảng cáo) | Quảng cáo, phân tích | |
| Vị trí gần đúng (suy ra từ IP) | Quảng cáo | |
| Chẩn đoán (nhật ký lỗi, hiệu năng) | Phân tích | |

Dữ liệu được mã hoá khi truyền đi. Người dùng không cần tạo tài khoản. Hướng dẫn khai báo chi tiết của Google cho AdMob: tìm "Google Mobile Ads SDK data disclosure" trên developers.google.com.

---

## 7. Cập nhật game về sau

1. Sửa code, rồi chạy thử bằng `npm run dev`.
2. Tăng `versionName` trong `android/app/build.gradle`, ví dụ từ `1.0` lên `1.1`. Với iOS, tăng *Version* và *Build* trong Xcode.
3. Đẩy code lên GitHub, tải file `.aab` mới, rồi tạo bản phát hành mới trên Play Console. Với iOS, làm lại thao tác Archive → Upload.

---

## 8. Về lợi nhuận — nói thật để bạn lên kế hoạch

- Thu nhập từ quảng cáo **tỉ lệ thuận với số người chơi**. Giá quảng cáo khác nhau rất nhiều giữa các nước: người chơi ở Mỹ/châu Âu mang lại nhiều tiền gấp vài lần người chơi ở Việt Nam. Đừng kỳ vọng có thu nhập đáng kể nếu chỉ có vài trăm lượt tải.
- Lên store xong **chưa có ai tìm thấy ngay**. Việc kéo người chơi mới là phần khó nhất. Những cách nên thử:
  - Làm video ngắn TikTok/Reels/Shorts quay cảnh "chui lỗ sát nút" và "màn 1000 chỉ còn 1 lỗ". Kiểu game này rất hợp với video ngắn.
  - Tối ưu tên, mô tả và từ khoá trên store (ASO), có sẵn trong `docs/store-listing.md`.
  - Ảnh chụp màn hình đầu tiên phải cho thấy ngay cách chơi.
- Theo dõi doanh thu trong AdMob, và số người chơi quay lại (retention) trong Play Console/App Store Connect. Nếu người chơi bỏ nhanh, hãy chỉnh độ khó mấy màn đầu trước.
