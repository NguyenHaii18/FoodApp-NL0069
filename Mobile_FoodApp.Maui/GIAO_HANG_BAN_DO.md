# Giao hàng & bản đồ — phần tài xế

## Chức năng và vị trí trong app
| Chức năng | Ở đâu |
|---|---|
| Địa chỉ lấy hàng / giao hàng (có nút sao chép) | Tài xế → Chi tiết giao hàng → thẻ tuyến đường |
| Khoảng cách, ước tính thời gian, khu vực | Chi tiết giao hàng → thẻ "Khoảng cách / Ước tính / Khu vực"; hộp thoại nhận đơn; khách thấy khi thanh toán |
| Phí giao theo khoảng cách + khu vực | `GiaoHangService.TinhPhi`; tính khi khách thanh toán, tài xế xem bảng phí chi tiết |
| Điều chỉnh phí | Nút "✏ Báo khoảng cách thực tế" → tính lại theo biểu phí |
| Chỉ đường | 🧭 đến quán / đến khách → mở Google Maps (xe máy), điểm đến hiện tại được tô nổi bật |
| Gọi quán / gọi khách | 📞 trên Chi tiết giao hàng (máy tính không gọi được thì sao chép số) |
| Theo dõi tài xế | Tài xế bật "📡 Chia sẻ vị trí" (15 giây/lần); khách thấy ở **Đơn hàng**: tên, biển số, ETA, giờ cập nhật, 📍 Xem vị trí, 📞 Gọi tài xế |
| Xác nhận nhận hàng | Tài xế xác nhận "đã nhận đủ N món" tại quán; khách đọc **mã 4 số** (hiện trên đơn) cho tài xế để xác nhận đã nhận |
| Giao thất bại | Nút "⚠ Giao thất bại" (sau khi lấy hàng) → chọn lý do |

## Biểu phí (sửa ở đầu `Services/GiaoHangService.cs`)
- 15.000đ cho 2 km đầu, +5.000đ mỗi km tiếp theo (làm tròn lên); ngoại thành (Củ Chi, Hóc Môn, Bình Chánh, Nhà Bè, Cần Giờ) +10.000đ; tối đa 70.000đ.
- Ví dụ từ Quận 5: → Quận 10 ≈ 2,9 km, 20.000đ · → Quận 1 ≈ 6,0 km, 35.000đ · → Hóc Môn ≈ 20,5 km, 70.000đ.

## Quy tắc nghiệp vụ đã chọn (có thể đổi)
- **Điều chỉnh phí**: đơn *tiền mặt* tăng/giảm được (khách trả thêm/bớt khi nhận); đơn *đã thanh toán trước* chỉ được giảm, vì không thu thêm được.
- **Giao thất bại**: lỗi do khách (không liên lạc được, từ chối, hẹn lại, sai địa chỉ) → tài xế **vẫn nhận phí giao**; sự cố xe, món hư, lý do khác → **không nhận phí**.
  "Không liên lạc được" yêu cầu tài xế đã bấm gọi khách ít nhất 1 lần. Đơn chuyển sang "Đã hủy" (nhãn "Giao thất bại"); đơn trả trước được đánh dấu hoàn tiền.
- Đơn giao thất bại không tính vào "số đơn hôm nay".

## Giới hạn hiện tại (cần biết khi demo / bảo vệ)
- **Khoảng cách là ƯỚC TÍNH**: nhận quận/huyện trong địa chỉ (ví dụ "Q.5", "Quận 10", "Hóc Môn"), lấy tâm khu vực, đường chim bay × 1,25, tốc độ 20 km/h.
  Địa chỉ không ghi quận/huyện → chưa tính được, dùng phí cơ bản (tài xế có thể báo km thực tế). Khi có Web API + Goong thì thay `GiaoHangService.UocTinh`.
- **Chưa có bản đồ nhúng trong app** (cần gói Maps + khóa API); chỉ đường và xem vị trí mở ứng dụng bản đồ bên ngoài.
- **Theo dõi trực tiếp giữa 2 thiết bị cần server** (`PUT api/mobile/tai-xe/vi-tri` + trả `taiXeLat/taiXeLng` trong đơn của khách). Chạy một máy (demo) thì khách và tài xế dùng chung dữ liệu cục bộ.
- Mã xác nhận chỉ có ở đơn tạo sau bản này; đơn cũ không yêu cầu mã.
- Cần cấp quyền vị trí (Android đã khai báo trong `AndroidManifest.xml`). Windows dùng thiết lập vị trí của hệ thống.

## Đã sửa kèm theo
- Bản sao đơn gửi cho tài xế trước đây **thiếu** tên/SĐT khách, hình thức thanh toán và giảm giá → tài xế thấy sai số tiền cần thu và không gọi được khách. Nay mang đủ (`DonHang.TaoBanSaoChoTaiXe`).
- Mở lại app: đơn tài xế đang giao dở được khôi phục (trước đó biến mất khỏi màn hình tài xế).

## Chưa kiểm chứng
Chưa biên dịch/chạy được trong môi trường soạn thảo (không có .NET SDK). Đã kiểm tra: XAML hợp lệ, mọi trình xử lý sự kiện có hàm tương ứng, ngoặc C# cân, công thức phí/khoảng cách mô phỏng lại bằng Python.
Nên build và chạy thử các kịch bản: đặt đơn → quán xác nhận/bàn giao → tài xế nhận → lấy hàng → giao (nhập mã) và nhánh giao thất bại.
