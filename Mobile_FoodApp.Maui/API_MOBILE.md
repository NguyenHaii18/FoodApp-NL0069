# Hợp đồng API mà app MAUI gọi (đặt trong FoodServiceApp.Web, MobileApi.cs)

Địa chỉ gốc lấy từ `Resources/Raw/appsettings.json`. JSON camelCase; Id có thể là số hoặc chuỗi (app tự đổi sang chuỗi).
Nếu GET api/mobile/gian-hang không trả được, app tự chuyển sang dữ liệu mẫu (chế độ demo).

| Method | URL | Body | Trả về |
|---|---|---|---|
| GET | api/mobile/gian-hang | | [GianHang] |
| GET | api/mobile/mon-an | | [MonAn] (có gianHangId, danhMuc, conHang) |
| GET | api/mobile/khuyen-mai | | [KhuyenMai] |
| POST | api/mobile/khach-hang/dang-nhap | {email, matKhau} | {token, khachHang} |
| POST | api/mobile/khach-hang/dang-ky | {hoTen, sdt, email, matKhau} | {token, khachHang} |
| GET | api/mobile/khach-hang/don-hang | | [DonHang] |
| POST | api/mobile/don-hang | {gianHangId, diaChiGiao, hinhThucThanhToan, maKhuyenMai, chiTiet:[{monAnId, soLuong}]} | {id} |
| POST | api/mobile/gian-hang/dang-nhap | {email, matKhau} | {token, gianHang} |
| GET | api/mobile/gian-hang/{id}/don-hang | | [DonHang] |
| PUT | api/mobile/gian-hang/{id}/trang-thai-kinh-doanh | {dangMoCua} | 200 |
| PUT | api/mobile/don-hang/{id}/xac-nhan, tu-choi, san-sang-giao | | 200 |
| POST | api/mobile/admin/dang-nhap | {email, matKhau} | {token} |
| GET | api/mobile/admin/khach-hang, van-de | | [KhachHang], [VanDeHeThong] |
| PUT | api/mobile/admin/gian-hang/{id}/duyet, tu-choi, khoa, mo-khoa | | 200 |
| PUT | api/mobile/admin/khach-hang/{id}/khoa, mo-khoa | | 200 |
| PUT | api/mobile/admin/van-de/{id}/dang-xu-ly, giai-quyet | | 200 |

Lỗi: trả 4xx kèm {"thongDiep": "..."} để app hiển thị.

| PUT | api/mobile/tai-xe/ho-so | {hoTen, soDienThoai, bienSoXe, loaiXe} | 200 (ảnh đại diện hiện chỉ lưu trên máy) |

## Giao hàng & bản đồ (phần tài xế) — endpoint bổ sung
Mọi lời gọi đều **tùy chọn**: offline app tự lưu cục bộ. Lỗi mạng khi gửi vị trí được bỏ qua (lần cập nhật sau gửi lại).

| Method | URL | Body | Ghi chú |
|---|---|---|---|
| PUT | api/mobile/tai-xe/vi-tri | {donHangId, viDo, kinhDo} | gửi mỗi ~15 giây khi tài xế bật chia sẻ vị trí |
| PUT | api/mobile/don-hang/{id}/da-lay-hang | | tài xế xác nhận đã nhận đủ hàng từ quán |
| PUT | api/mobile/don-hang/{id}/hoan-thanh | {maXacNhan} | server nên kiểm tra mã 4 số khách nhận được |
| PUT | api/mobile/don-hang/{id}/giao-that-bai | {lyDo, tinhPhi} | tinhPhi = true nếu lỗi do khách; đơn trả trước cần hoàn tiền |
| PUT | api/mobile/don-hang/{id}/phi-giao-hang | {khoangCachKm, phiGiaoHang} | tài xế báo khoảng cách thực tế → phí tính lại |

`DonHang` trả về nên có thêm (camelCase, tùy chọn): `khoangCachKm`, `thoiGianDuKienPhut`, `khuVucLay`, `khuVucGiao`, `ngoaiThanh`,
`phiKhoangCach`, `phuPhiKhuVuc`, `phiDaDieuChinh`, `taiXeId`, `taiXeTen`, `taiXeSdt`, `taiXeBienSo`, `daLayHang`, `taiXeLat`, `taiXeLng`,
`viTriCapNhatLuc`, `maXacNhanGiao`, `giaoThatBai`, `lyDoGiaoThatBai`. Thiếu trường nào app dùng giá trị ước tính/bỏ qua trường đó.
Khi nối server, **server nên tự tính phí giao** (Goong DistanceMatrix) và trả về `phiGiaoHang` thay cho ước tính của app.

## Đối tác / Gian hàng — endpoint bổ sung (bản bổ sung chức năng đối tác)
Mọi lời gọi đều **tùy chọn**: offline app tự lưu cục bộ. Các mục "chưa có API" hiện chỉ lưu trên máy (VendorLocalStore).

| Method | URL | Body | Ghi chú |
|---|---|---|---|
| POST | api/mobile/gian-hang/quen-mat-khau | {email, soDienThoai, matKhauMoi} | xác minh email + SĐT đã đăng ký; server nên gửi OTP/email thật |
| PUT | api/mobile/gian-hang/{id}/gio-hoat-dong | {gioMoCua, gioDongCua, dangNhanDon, dangMoCua} | "HH:mm"; 00:00–23:59 = cả ngày |
| PUT | api/mobile/don-hang/{id}/tu-choi | {lyDo} | **lyDo bắt buộc** (NV04) |

`GianHang` nên có thêm: `gioMoCua`, `gioDongCua`, `dangNhanDon`, `tyLeChietKhau`.
`MonAn` nên có thêm: `moTa`, `tamNgungBan`, `giaVon`, `choGhiChu`, `tuyChon:[{loai: Size|Topping|MucCay, ten, giaThem}]`.
`DonHang` nên có thêm: `lyDoTuChoi`.
Chưa có API (chỉ lưu cục bộ): CRUD món/danh mục/thứ tự danh mục, đánh giá + phản hồi, thông báo, khuyến mãi.
Quy tắc: đơn chờ xác nhận quá 5 phút → server tự hủy + hoàn tiền (app cũng tự hủy khi đối tác mở màn hình đơn).
Phí nền tảng = tyLeChietKhau (mặc định 15%) × (tiền món − giảm giá do quán tài trợ), tính trên đơn hoàn thành.
CSDL: chạy `SQL/04_BoSung_DoiTac.sql` và `SQL/05_BoSung_GiaoHang_BanDo.sql`.
