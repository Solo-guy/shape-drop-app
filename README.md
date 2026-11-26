# Shape Drop (Unity / C#)

Game pixel né tường rơi. Bạn là một hình học ngẫu nhiên và phải chui qua đúng lỗ mang hình của mình.

- **150 màn.** Mỗi bức tường rơi xuống là một màn.
- **Tốc độ khởi đầu theo nhịp Flappy Bird:** tường đi hết màn hình trong khoảng 2 giây, cứ khoảng 1,45 giây lại có tường mới. **Cứ 10 màn tốc độ tăng 8%**, đến màn 150 nhanh gấp khoảng 2,1 lần.
- **Khe thoát hiểm hẹp lại sau mỗi màn.** Đến màn 150 chỉ còn **đúng 1 lỗ vừa khít**.
- **Lỗ xuất hiện ngẫu nhiên theo chiều ngang**, kèm lỗ bẫy sai hình. Ô NEXT báo trước hình bạn sắp biến thành.
- **Thua là chơi lại từ màn 1.** Giao diện có hai ngôn ngữ Việt/Anh. Âm thanh 8-bit và đồ hoạ đều tạo bằng code, dự án không có file ảnh hay âm thanh nào.

## Build

Cần **Unity 6.6 (6000.6.10f1)**. Muốn build thêm bản Android thì cài kèm module **Android Build Support**, gồm cả **OpenJDK** và **Android SDK & NDK Tools**.

Nhấp đúp `BUILD-ShapeDrop.bat`. Script chạy Unity ở chế độ nền (batchmode) và tạo ra:

| File | Dùng để |
|---|---|
| `Builds/Windows/ShapeDrop.exe` | Chơi trên Windows ngay, không cần cài đặt. Khi mang sang máy khác, chép cả thư mục `Windows`. |
| `Builds/Android/ShapeDrop.apk` | Cài lên điện thoại Android. Chỉ có khi đã cài module Android. |
| `Builds/build-report.txt` | Kết quả build. |

Trong Unity Editor cũng build được, qua menu **Shape Drop ▸ Build Windows + Android**.

## Mã nguồn

| File | Nội dung |
|---|---|
| `Assets/Scripts/GameCore.cs` | Lõi game: độ khó 150 màn, tường, va chạm, vẽ pixel |
| `Assets/Scripts/Shapes.cs` | 10 hình học, mặt nạ và sprite pixel |
| `Assets/Scripts/Px.cs` | Bộ đệm pixel, xuất ra Texture2D (lọc Point) |
| `Assets/Scripts/GameApp.cs` | Giao diện uGUI dựng bằng code, điều khiển (kéo, phím, nút Back) |
| `Assets/Scripts/Sfx.cs` | Âm thanh 8-bit tổng hợp bằng code |
| `Assets/Scripts/Loc.cs` | Chữ tiếng Việt / tiếng Anh |
| `Assets/Scripts/Ads.cs` | Chỗ gắn quảng cáo AdMob (chưa bật) |
| `Assets/Editor/ShapeDropBuild.cs` | Cấu hình app (màn hình dọc, icon pixel, IL2CPP ARM64) và build |

Muốn chỉnh độ khó thì sửa lớp `Difficulty` ở đầu file `GameCore.cs`.
