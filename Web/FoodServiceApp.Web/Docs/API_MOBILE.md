# API cho FoodApp (.NET MAUI)

API nằm trong project `FoodServiceApp.Web`, controller `Controllers/Api/MobileApiController.cs`, base route:

`/api/mobile`

JSON dùng camelCase. API dùng JWT cho các nghiệp vụ sau đăng nhập; lỗi trả `4xx` dạng `{ "thongDiep": "..." }`.

## Catalog

| Method | URL | Auth | Trả về |
|---|---|---|---|
| GET | `/api/mobile/gian-hang` | Không | danh sách gian hàng đang mở/đã duyệt |
| GET | `/api/mobile/mon-an` | Không | danh sách món còn bán, có `gianHangId`, `danhMuc` |
| GET | `/api/mobile/khuyen-mai` | Không | mã khuyến mãi đang hiệu lực |

## Đăng nhập / đăng ký

| Method | URL | Body |
|---|---|---|
| POST | `/api/mobile/khach-hang/dang-nhap` | `{email, matKhau}` |
| POST | `/api/mobile/khach-hang/dang-ky` | `{hoTen, sdt, diaChi, email, matKhau}` |
| POST | `/api/mobile/gian-hang/dang-nhap` | `{email, matKhau}` |
| POST | `/api/mobile/gian-hang/dang-ky` | `{tenGianHang, diaChi, sdt, email, matKhau, moTa, hinhAnh}` |
| POST | `/api/mobile/tai-xe/dang-nhap` | `{sdt, matKhau}` |
| POST | `/api/mobile/admin/dang-nhap` | `{email, matKhau}` |

Tài khoản gian hàng seed cũ dùng username như `gianhang01`; API chấp nhận thêm dạng `gianhang01@gianhang.vn` để khớp ô Email của app.

## Khách hàng

| Method | URL |
|---|---|
| GET | `/api/mobile/khach-hang/don-hang` |
| POST | `/api/mobile/don-hang` |
| PUT | `/api/mobile/khach-hang/ho-so` |
| POST | `/api/mobile/van-de` |
| POST | `/api/mobile/danh-gia` |
| PUT | `/api/mobile/don-hang/{id}/huy` |

### Đặt hàng
Body:
```json
{
  "gianHangId": 1,
  "diaChiGiao": "123 Lê Lợi, Q.1, TP.HCM",
  "hinhThucThanhToan": "TienMat",
  "maKhuyenMai": "CODE10",
  "chiTiet": [
    { "monAnId": 10, "soLuong": 2 }
  ]
}
```

Server tự kiểm tra món thuộc cùng gian hàng, còn hàng, giá hiện tại, mã giảm giá và tự tính phí giao hàng. Response có `id`, `tongTien`, `phiGiaoHang`, `khoangCachKm`, `thoiGianDuKienPhut`, `maXacNhanGiao`; với VNPay có thêm `paymentUrl`.

## Gian hàng

| Method | URL |
|---|---|
| GET | `/api/mobile/gian-hang/{id}/don-hang` |
| PUT | `/api/mobile/gian-hang/{id}/trang-thai-kinh-doanh` |
| PUT | `/api/mobile/don-hang/{id}/xac-nhan` |
| PUT | `/api/mobile/don-hang/{id}/tu-choi` |
| PUT | `/api/mobile/don-hang/{id}/san-sang-giao` |

## Quản trị

| Method | URL |
|---|---|
| GET | `/api/mobile/admin/khach-hang` |
| GET | `/api/mobile/admin/van-de` |
| PUT | `/api/mobile/admin/gian-hang/{id}/duyet` |
| PUT | `/api/mobile/admin/gian-hang/{id}/tu-choi` |
| PUT | `/api/mobile/admin/gian-hang/{id}/khoa` |
| PUT | `/api/mobile/admin/gian-hang/{id}/mo-khoa` |
| PUT | `/api/mobile/admin/khach-hang/{id}/khoa` |
| PUT | `/api/mobile/admin/khach-hang/{id}/mo-khoa` |
| PUT | `/api/mobile/admin/van-de/{id}/dang-xu-ly` |
| PUT | `/api/mobile/admin/van-de/{id}/giai-quyet` |

## Tài xế / giao hàng

| Method | URL | Body |
|---|---|---|
| PUT | `/api/mobile/tai-xe/ho-so` | `{hoTen, soDienThoai, bienSoXe, loaiXe}` |
| GET | `/api/mobile/tai-xe/don-cho-nhan` | |
| PUT | `/api/mobile/tai-xe/don-hang/{id}/nhan` | |
| GET | `/api/mobile/tai-xe/dang-giao` | |
| PUT | `/api/mobile/tai-xe/vi-tri` | `{donHangId, viDo, kinhDo}` |
| PUT | `/api/mobile/don-hang/{id}/da-lay-hang` | |
| PUT | `/api/mobile/don-hang/{id}/hoan-thanh` | `{maXacNhan}` |
| PUT | `/api/mobile/don-hang/{id}/giao-that-bai` | `{lyDo, tinhPhi}` |
| PUT | `/api/mobile/don-hang/{id}/phi-giao-hang` | `{khoangCachKm, phiGiaoHang}` |

API kiểm tra tài xế đúng người nhận đơn; mã giao 4 số được server sinh khi tạo đơn. Vị trí tài xế hiện lưu trong memory của Web process vì CSDL tích hợp chưa có bảng vị trí thời gian thực.

## Bổ trợ

| Method | URL |
|---|---|
| PUT | `/api/mobile/doi-mat-khau` |
| POST | `/api/mobile/map/phi-giao-hang` |
| POST | `/api/mobile/payment/vnpay/tao-url` |

## Cấu trúc `DonHang` app nhận

Response đã được map đúng model MAUI, trong đó `chiTiet[]` có object `monAn` lồng bên trong. Các trường giao hàng bổ sung gồm:

`khoangCachKm`, `thoiGianDuKienPhut`, `khuVucLay`, `khuVucGiao`, `ngoaiThanh`, `phiKhoangCach`, `phuPhiKhuVuc`, `phiDaDieuChinh`, `taiXeId`, `taiXeTen`, `taiXeSdt`, `taiXeBienSo`, `daLayHang`, `taiXeLat`, `taiXeLng`, `viTriCapNhatLuc`, `maXacNhanGiao`, `giaoThatBai`, `lyDoGiaoThatBai`.


## Bổ sung Đối tác / Gian hàng (VendorMobileApiController)
| Method | URL | Body | Ghi chú |
|---|---|---|---|
| PUT | api/mobile/gian-hang/{id}/gio-hoat-dong | {gioMoCua:"HH:mm", gioDongCua, dangNhanDon, dangMoCua} | Giờ mở/đóng cửa, tạm dừng nhận đơn. Đặt hàng bị chặn khi ngoài giờ / tạm dừng |
| GET | api/mobile/gian-hang/{id}/danh-gia | | Đánh giá của gian hàng kèm phản hồi |
| PUT | api/mobile/gian-hang/{id}/danh-gia/{maDanhGia}/phan-hoi | {noiDung} | Gian hàng phản hồi đánh giá (≤ 500 ký tự) |
| GET | api/mobile/gian-hang/{id}/thong-bao | | Đơn mới / đơn hủy / đánh giá mới trong 7 ngày |
| GET | api/mobile/gian-hang/{id}/tai-chinh?tu=&den= | | Doanh thu món, phí nền tảng (TyLeChietKhau, mặc định 15%), thực nhận, giá vốn, lợi nhuận |
| POST | api/mobile/gian-hang/quen-mat-khau | {email, soDienThoai, matKhauMoi} | Đặt lại mật khẩu (giới hạn 5 lần/15 phút; nên thay bằng OTP/email khi triển khai thật) |
| PUT | api/mobile/don-hang/{id}/tu-choi | {lyDo} | **Bắt buộc lý do** (NV04); đơn đã thanh toán được hoàn tiền |


## Bổ sung: Tài khoản & bảo mật (TaiKhoanApiController)
| Method | URL | Quyền | Ghi chú |
|---|---|---|---|
| POST | api/mobile/tai-khoan/quen-mat-khau `{taiKhoan}` | công khai | taiKhoan = tên đăng nhập / email / SĐT. Gửi OTP, luôn trả thông điệp chung. Chưa cấu hình SMTP + `Otp:HienMaDev=true` thì trả `maDev` để demo |
| POST | api/mobile/tai-khoan/dat-lai-mat-khau `{taiKhoan, otp, matKhauMoi}` | công khai | OTP 6 số, hiệu lực 5 phút, sai tối đa 5 lần |
| POST | api/mobile/tai-khoan/xac-thuc/gui-otp `{kenh: "email"\|"sms"}` | đã đăng nhập | |
| POST | api/mobile/tai-khoan/xac-thuc/xac-nhan `{kenh, otp}` | đã đăng nhập | |
| GET | api/mobile/tai-khoan/trang-thai-xac-thuc | đã đăng nhập | |
| GET | api/mobile/tai-khoan/lich-su-dang-nhap | đã đăng nhập | 30 sự kiện gần nhất |
| POST | api/mobile/tai-khoan/xoa `{matKhau}` | khách/gian hàng/tài xế | vô hiệu hóa + ẩn danh, chặn nếu còn đơn đang xử lý |
| PUT | api/mobile/tai-khoan/anh-dai-dien `{url}` | khách, tài xế | |
| PUT | api/mobile/gian-hang/giay-to `{giayPhepKinhDoanh, giayChungNhanAttp}` | gian hàng | |
| PUT | api/mobile/tai-xe/trang-thai `{trangThai: "SanSang"\|"NgoaiTuyen"}` | tài xế | |
| GET | api/mobile/admin/nhat-ky?nguon=&maTK=&trang=&kichThuoc= | Admin tổng | |
| GET/PUT | api/mobile/khach-hang/gio-hang | khách | PUT `{maGianHang, chiTiet:[{maMonAn,soLuong,ghiChu,tuyChon[]}]}`, chi tiết rỗng = xóa giỏ |

Đăng nhập sai 5 lần liên tiếp -> khóa tạm 15 phút (mọi vai trò). Mọi thao tác ghi (POST/PUT/DELETE) của người dùng đã đăng nhập tự vào bảng NhatKyHeThong.
Phân quyền quản trị: tài khoản `NhanVien` chỉ xem + xử lý phản hồi/sự cố; mọi thao tác duyệt/khóa/khuyến mãi (Web + API) cần `SuperAdmin`.
