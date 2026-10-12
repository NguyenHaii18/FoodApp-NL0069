# FoodServiceApp.Web — Chương trình gộp thành 1 project

Đề tài CNTT-KLCN069 — Xây dựng ứng dụng quản lý dịch vụ ăn uống. ASP.NET Core
(C#) duy nhất, dùng chung 1 CSDL SQL Server, gồm 3 phần trước đây tách rời nay
gộp lại:

1. **Website** (Razor MVC, Cookie Auth) — cho Gian hàng, Quản trị, Tài xế dùng trên trình duyệt
2. **Web API** (`Controllers/Api/*`, JWT Auth) — cho **FoodApp (.NET MAUI)** gọi từ mobile
3. **Tích hợp bên thứ 3** — Storage (ảnh/file), Goong Maps (khoảng cách/phí ship), VNPay (thanh toán)

Một Program.cs, một appsettings.json, một AppDbContext — không còn 2 project
riêng như trước.

## 1. Yêu cầu môi trường
- .NET 8 SDK
- SQL Server / LocalDB. Ứng dụng tự thử `(localdb)\MSSQLLocalDB`, `\\SQLEXPRESS` và `localhost`.

## 2. Cấu hình
Sửa `appsettings.json`:
- `ConnectionStrings:DefaultConnection` — khớp SQL Server của bạn
- `Jwt:Key` — đổi thành chuỗi bí mật riêng (≥32 ký tự) trước khi deploy
- `GoongMaps:ApiKey`, `VnPay:TmnCode`/`HashSecret` — điền khi cần dùng Maps/Payment thật

## 3. Chạy
```
cd FoodServiceApp.Web
dotnet restore
dotnet run
```
- Website: `https://localhost:5001`
- API (test bằng Swagger): `https://localhost:5001/swagger`

## 4. Website (Cookie Auth) — dùng trên trình duyệt

### Khách hàng (Quản lý mua hàng) — trang chủ công khai tại `/`
| Trang | Route | Chức năng |
|---|---|---|
| Trang chủ | `/` hoặc `/MuaHang` | Tìm kiếm, xem danh sách gian hàng đang mở |
| Chi tiết gian hàng | `/MuaHang/GianHang/{id}` | Xem thực đơn theo danh mục, thêm vào giỏ hàng |
| Giỏ hàng | `/MuaHang/GioHang` | Xem/sửa số lượng — không cần đăng nhập để xem |
| Thanh toán | `/MuaHang/GioHang/thanh-toan` | Nhập địa chỉ, áp mã khuyến mãi, chọn hình thức thanh toán — **cần đăng nhập** |
| Đăng ký / Đăng nhập | `/MuaHang/Auth/DangKy`, `/MuaHang/Auth/Login` | Khách hàng tự đăng ký tài khoản |
| Đơn hàng đang xử lý | `/MuaHang/DonHang` | Theo dõi trạng thái, huỷ đơn khi còn "Chờ xác nhận" |
| Lịch sử mua hàng | `/MuaHang/DonHang/LichSu` | Đơn đã hoàn tất/huỷ, đánh giá đơn đã giao |
| Hồ sơ | `/MuaHang/HoSo` | Sửa thông tin, đổi mật khẩu |

Giỏ hàng lưu trong Session (không cần bảng CSDL riêng) và chỉ chứa món của **một
gian hàng tại một thời điểm** — giống các app đặt đồ ăn thật; thêm món từ gian
hàng khác sẽ báo lỗi và yêu cầu đặt xong/xoá giỏ hàng hiện tại trước.

### Gian hàng (đối tác)
| Trang | Route | Chức năng |
|---|---|---|
| Đăng nhập | `/Auth/Login` | |
| Trang chủ | `/Dashboard` | Doanh thu hôm nay, đơn chờ xác nhận, bật/tắt mở cửa |
| Đơn hàng | `/DonHang` | Lọc theo trạng thái, xác nhận/hủy đơn |
| Thực đơn | `/MonAn` | CRUD món ăn theo danh mục, upload ảnh |
| Khuyến mãi | `/KhuyenMai` | CRUD mã khuyến mãi riêng của gian hàng |
| Doanh thu | `/DoanhThu` | Biểu đồ 30 ngày (Chart.js) + món bán chạy |
| Hồ sơ | `/HoSo` | Sửa thông tin, đổi mật khẩu, đổi ảnh |

### Quản trị — route `/QuanTri/...`
| Trang | Route | Chức năng |
|---|---|---|
| Đăng nhập | `/AdminAuth/Login` | |
| Trang chủ | `/QuanTri` | Tổng quan hệ thống, top gian hàng |
| Khách hàng | `/QuanTri/KhachHang` | Tìm kiếm, khoá/mở khoá tài khoản |
| Gian hàng | `/QuanTri/GianHang` | Chi tiết doanh thu/đánh giá từng đối tác, khoá/mở khoá |
| Tài xế | `/QuanTri/TaiXe` | **Tạo tài khoản tài xế** (trước đây không có cách nào tạo!), khoá/mở khoá |
| Khuyến mãi toàn hệ thống | `/QuanTri/KhuyenMai` | Tách biệt mã riêng của từng gian hàng |
| Thống kê & báo cáo | `/QuanTri/ThongKe` | Doanh thu hệ thống, món bán chạy, doanh thu theo gian hàng |

### Tài xế — route `/TaiXe/...`
| Trang | Route | Chức năng |
|---|---|---|
| Đăng nhập | `/DriverAuth/Login` | |
| Trang chủ | `/TaiXe` | Bật/tắt sẵn sàng, chuyến giao hiện tại |
| Đơn chờ nhận | `/TaiXe/DonHang/ChoNhan` | Tự nhận đơn đã được gian hàng xác nhận |
| Chuyến hiện tại | `/TaiXe/DonHang/HienTai` | Bắt đầu giao → thành công/thất bại |
| Lịch sử | `/TaiXe/DonHang/LichSu` | Kèm cột thu nhập từng chuyến |
| Thu nhập | `/TaiXe/ThuNhap` | **Mới** — biểu đồ 30 ngày (Chart.js) + tổng thu nhập/số đơn |
| Hồ sơ | `/TaiXe/HoSo` | |

**Luồng trạng thái đơn hàng đã chốt**: `ChoXacNhan` (gian hàng xác nhận) →
`DaXacNhan` (chờ tài xế) → `DangGiao` (**tài xế tự nhận đơn**, không phải gian
hàng bàn giao) → `DaGiao`. Giao thất bại thì `GiaoHang.TrangThai=GiaoThatBai`
nhưng đơn giữ nguyên `DangGiao` để Quản trị/Gian hàng xử lý tiếp thủ công.

**Phí giao hàng & thu nhập tài xế (mới)**: `DonHang.PhiGiaoHang` được tính
theo khoảng cách thật qua `IGoongMapsService` (5.000đ/km, làm tròn lên) ngay
khi khách bấm "Đặt hàng" ở `CustomerCartController.DatHang` — có xem trước
theo thời gian thực ở trang Checkout (gọi `POST /MuaHang/GioHang/uoc-tinh-phi`
khi gõ địa chỉ). Nếu Goong Maps lỗi/chưa có API key thì dùng phí mặc định
15.000đ thay vì chặn đơn. Toàn bộ khoản này là thu nhập của tài xế khi giao
thành công — không có phần platform ăn chia (đơn giản hoá cho phạm vi đồ án).

Cả 3 khu vực dùng chung 1 cookie scheme, phân quyền theo Role claim
(`GianHang`/`QuanTri`/`TaiXe`) — vào nhầm khu vực sẽ tự điều hướng đúng trang
đăng nhập (`Program.cs`, `OnRedirectToLogin`).

## 5. Web API (JWT Auth) — dùng cho FoodApp (MAUI)
Nằm trong `Controllers/Api/*`, tách biệt hoàn toàn với MVC — không dùng Cookie,
chỉ định rõ `AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme`.

### Đăng nhập (dùng chung 4 vai trò)
`POST /api/auth/login/{vaiTro}` — vaiTro = `KhachHang`|`GianHang`|`QuanTri`|`TaiXe`
```json
{ "tenDangNhap": "gianhang01", "matKhau": "..." }
```
→ `{ token, vaiTro, maTK, hoTenHienThi }`. Đính `token` vào header
`Authorization: Bearer {token}` cho các request sau.

### Endpoint theo vai trò
- **Khách hàng** (`Roles="KhachHang"`): `GET /api/customer/home?tuKhoa=`,
  `GET /api/customer/stores/{id}`, `GET /api/customer/foods/{id}`,
  `GET /api/customer/orders?loai=dang-xu-ly|lich-su`, `GET /api/customer/orders/{id}`,
  `POST /api/customer/orders`, `POST /api/customer/orders/{id}/cancel`,
  `POST /api/customer/orders/{id}/review`, `GET /api/customer/addresses`,
  `POST /api/customer/addresses`, `POST /api/customer/addresses/{id}/default`,
  `DELETE /api/customer/addresses/{id}`, `GET /api/customer/profile`,
  `PUT /api/customer/profile`
- **Gian hàng** (`[Authorize(Roles="GianHang")]`): `GET /api/vendor/home`,
  `POST /api/vendor/toggle-mo-cua`, `GET /api/vendor/orders?trangThai=`,
  `POST /api/vendor/orders/{id}/status`, `GET /api/vendor/menu`
- **Quản trị** (`Roles="QuanTri"`): `GET /api/admin/home`,
  `GET /api/admin/vendors?timKiem=`, `GET /api/admin/customers?timKiem=`,
  `POST /api/admin/accounts/{maTK}/toggle-lock`
- **Tài xế** (`Roles="TaiXe"`): `GET /api/driver/home`,
  `POST /api/driver/toggle-san-sang`, `GET /api/driver/available-orders`,
  `POST /api/driver/orders/{id}/accept|start|complete|fail`
- **Dùng chung JWT**: `POST /api/upload/monan|gianhang`,
  `POST /api/map/phi-giao-hang`, `GET /api/payment/vnpay/return|ipn`.
  Riêng `POST /api/payment/vnpay/tao-url` yêu cầu JWT vai trò `KhachHang`.
  IPN vẫn để VNPay gọi server-to-server và tự cập nhật `ThanhToan`/`DonHang`
  khi xác nhận thanh toán thành công.

> **Tổng số API endpoint hiện tại: 37** (1 auth + 14 khách hàng + 5 gian hàng + 4 quản trị + 7 tài xế + 1 map + 3 payment + 2 upload).

### Nối vào FoodApp (MAUI)
Xem bộ `FoodApp_MauiIntegration.zip` (gửi trước đó) — có sẵn `ApiClient.cs`,
`VendorApiService.cs`, và 1 trang mẫu (`VendorOrdersPage`) đã nối API thật hoàn
chỉnh làm pattern. Đổi `BaseUrl` trong `ApiClient.cs` thành
`https://<địa chỉ máy chạy project này>:5001`.

## 6. Việc CẦN LÀM trước khi nộp báo cáo / demo thật
1. ~~**Hash mật khẩu**~~ ✅ **Đã xong.** Toàn bộ 8 nơi xử lý mật khẩu (đăng
   nhập Khách hàng/Gian hàng/Quản trị/Tài xế/API, đăng ký, đổi mật khẩu) giờ
   dùng `Services/MatKhauHelper.cs` (BCrypt.Net-Next). Mật khẩu mới tạo/đổi
   luôn được hash; đăng nhập vẫn nhận diện được tài khoản mẫu cũ nếu seed sẵn
   dạng plaintext trong file `.sql` (không cần sửa lại data mẫu) — xem
   comment trong `MatKhauHelper.cs`.
2. ~~**Đăng ký gian hàng mới**~~ ✅ Đã có sẵn (`AuthController.DangKy` +
   `Views/Auth/DangKy.cshtml`, trạng thái `ChoDuyet` → `Views/Dashboard/ChoDuyet.cshtml`
   khi chưa được Quản trị duyệt). Mục này trong bản README trước ghi nhầm là
   chưa có.
3. ~~**Thanh toán online thật**~~ ✅ **Đã nối.** Chọn "VNPay" ở Checkout giờ
   redirect thật sang cổng VNPay (`CustomerCartController.DatHang`), có
   route riêng `MuaHang/GioHang/vnpay-return` xử lý kết quả trả về và cập
   nhật `ThanhToan`/`DonHang` (không đụng tới `Controllers/Api/PaymentController`
   để không ảnh hưởng luồng mobile đang dùng chung `IVnPayService`). Chọn
   "Momo" sẽ báo lỗi rõ ràng vì chưa có service tương ứng — muốn dùng thật
   cần viết `IMomoService` theo đúng pattern của `IVnPayService`.
4. Điền `GoongMaps:ApiKey` và `VnPay:TmnCode`/`HashSecret` thật trong
   `appsettings.json` để 2 tích hợp này hoạt động (hiện là placeholder).
5. FoodApp (.NET MAUI) có thể nối vai trò Khách hàng sang API thật bằng
   `CustomerApiController`: xem quán/món, đặt đơn, theo dõi đơn, huỷ đơn,
   quản lý địa chỉ, hồ sơ và đánh giá. Dữ liệu giá món, trạng thái còn hàng,
   khuyến mãi và phí giao hàng được server kiểm tra/tính lại, không tin giá
   do mobile gửi lên.
6. `FoodServiceApp.Web.csproj` vừa thêm `PackageReference BCrypt.Net-Next` —
   chạy `dotnet restore` trước khi build để tải gói này.
7. ~~**Quản lý tài khoản tài xế**~~ ✅ **Đã xong.** Trước bản này hệ thống
   không có cách nào tạo tài khoản tài xế (không tự đăng ký như Gian hàng,
   admin cũng chưa có nơi tạo) — chỉ có thể insert thẳng vào CSDL. Giờ có
   `/QuanTri/TaiXe` (danh sách, tạo mới, khoá/mở khoá).
8. ~~**Phí giao hàng / thu nhập tài xế**~~ ✅ **Đã xong.** Trước bản này
   `DonHang` không có cột phí giao hàng nào cả — tài xế giao hàng nhưng
   không có cơ chế tính thu nhập. Đã thêm `DonHang.PhiGiaoHang` (tính theo
   khoảng cách qua Goong Maps), trang `/TaiXe/ThuNhap`, và cập nhật API
   mobile (`DriverHomeDto.ThuNhapHomNay`, `DonHangDto.PhiGiaoHang`).
9. **⚠️ Vừa đổi schema** (`DonHang.PhiGiaoHang` mới) — nếu database đã tồn
   tại từ trước (kể cả từ lần chạy trước bản này), `EnsureCreated()` sẽ
   **không tự thêm cột mới**, gây lỗi `Invalid column name 'PhiGiaoHang'`
   giống hệt lỗi `TrangThaiDuyet` đã gặp trước đây. Cách sửa: xoá database
   `QuanLyDichVuAnUong` (`sqlcmd -S "(localdb)\MSSQLLocalDB" -Q "DROP DATABASE QuanLyDichVuAnUong"`)
   rồi chạy lại `dotnet run` để tạo lại đúng schema mới nhất. Đây cũng là lý
   do nên chuyển sang EF Core Migrations thật (`dotnet ef migrations add ...`)
   thay vì `EnsureCreated()` nếu còn tiếp tục sửa entity về sau.
10. ~~**Chỉ đường cho tài xế**~~ ✅ **Đã xong**, nhưng theo hướng khác ban
    đầu dự tính: thay vì nhúng Goong Maps JS SDK (cần API key thật mới chạy
    được, khó demo khi key còn là placeholder), dùng thẳng link
    `https://www.google.com/maps/dir/?api=1&destination=...` — chỉ cần địa
    chỉ dạng text, **không cần geocode**, không phụ thuộc API key nào, và mở
    thẳng ra app Google Maps thật trên điện thoại tài xế (tự dùng GPS hiện
    tại làm điểm xuất phát). Có ở cả `/TaiXe/DonHang/ChoNhan` (xem trước
    quãng đường khi cân nhắc nhận đơn) và `/TaiXe/DonHang/HienTai` (chỉ
    đường đến gian hàng hoặc đến khách tuỳ trạng thái chuyến giao).

## 7. Ghi chú thiết kế
- Giao diện Website dùng đúng bảng màu trong `food_service_app_demo.html` đã
  gửi trước đó (primary `#1E6FA5`, accent `#E88C00`).
- Ảnh món ăn/gian hàng lưu vào `wwwroot/uploads/` (`LocalFileStorageService`) —
  dùng chung cho cả Website (form upload) và API (`/api/upload/*` cho mobile).
- Vì sandbox không có .NET SDK, project này **chưa được `dotnet build`/test
  thật** — hãy build thử ngay khi tải về để bắt lỗi cú pháp trước khi demo.


### Xử lý lỗi không kết nối được SQL Server
Ứng dụng hiện tự thử các SQL Server instance phổ biến trên Windows và ưu tiên instance đã có database `QuanLyDichVuAnUong`. Nếu chưa có database nhưng có instance đang chạy, EF Core sẽ tự tạo schema. Để có dữ liệu mẫu, chạy `QuanLyDichVuAnUong_Full.sql` nếu bạn có file này.

Có thể ép connection string bằng biến môi trường `FOOD_DB_CONNECTION`.

## Bản tích hợp (Shopee UI + BCrypt + VNPay + Goong Maps + Tài xế đăng ký/duyệt)

Bản này gộp hai nhánh code thành một chương trình duy nhất:

- **Giao diện kiểu Shopee** (header tìm kiếm, tab đơn mua, bảng màu mới, `wwwroot/css/site.css`) cho khách hàng, gian hàng, quản trị, tài xế.
- **Tài xế tự đăng ký** tại `/DriverAuth/DangKy` (trạng thái `ChoDuyet`) và **Quản trị duyệt/từ chối** tại `/QuanTri/TaiXe/ChoDuyet`; tài xế do Quản trị tạo trực tiếp thì kích hoạt ngay.
- **Hồ sơ & đổi mật khẩu của Quản trị viên** tại `/QuanTri/HoSo` (mật khẩu được băm BCrypt).
- Giữ nguyên các nâng cấp của bản trước: BCrypt (`MatKhauHelper`), thanh toán VNPay thật + `vnpay-return`, phí giao hàng theo khoảng cách (Goong Maps, 5.000đ/km, mặc định 15.000đ), trang Thu nhập tài xế, nút chỉ đường Google Maps.

> Lưu ý: bảng `TaiXe` có thêm 2 cột `TrangThaiDuyet`, `NgayDangKy`. Vì CSDL tạo bằng `EnsureCreated()`, nếu đã có CSDL cũ cần xoá CSDL `QuanLyDichVuAnUong` để ứng dụng tạo lại. Dự án chưa được `dotnet build` trong môi trường tạo bản này.

## Chức năng khách hàng bổ sung (bản này)

| Chức năng | Vị trí |
|---|---|
| Tìm kiếm theo **tên gian hàng hoặc tên món ăn** | `/MuaHang?tuKhoa=...` |
| **Trang chi tiết món ăn** (ảnh, mô tả, giá, điểm và nhận xét riêng của món, thêm vào giỏ) | `/MuaHang/Mon/{id}` |
| **Đánh giá từng món** ngoài đánh giá chung gian hàng/dịch vụ (sau khi đơn "Đã giao") | `/MuaHang/DonHang/{id}/danh-gia` |
| **Sổ địa chỉ giao hàng** (thêm, xóa, đặt mặc định, chọn nhanh ở Checkout; tối đa 10) | `/MuaHang/DiaChi` |
| **Gửi phản hồi / góp ý / khiếu nại** (gắn được với đơn hàng) và xem trả lời | `/MuaHang/PhanHoi` |
| Quản trị **xử lý phản hồi** (lọc, trả lời, đánh dấu đã xử lý) | `/QuanTri/PhanHoi` |
| **Theo dõi đơn có thanh tiến trình** và thông tin tài xế (tên, xe, biển số, gọi điện) | `/MuaHang/DonHang/{id}` |

## CSDL
Dùng file `MotFileDuyNhat.sql` nằm cạnh thư mục dự án (16 bảng + dữ liệu mẫu; chạy trong SSMS/sqlcmd trước khi `dotnet run`). Khớp với các entity trong `Models/Entities/Entities.cs`, gồm `DonHang.PhiGiaoHang`, `TaiXe.TrangThaiDuyet/NgayDangKy` và 3 bảng mới `DanhGiaMon`, `DiaChiKhachHang`, `PhanHoi`.
Tài khoản mẫu: khách/gian hàng/tài xế mật khẩu `123456`, quản trị `admin123` (dữ liệu mẫu dạng thường; đăng nhập xong nên đổi mật khẩu để lưu dạng BCrypt).

## API cho FoodApp (.NET MAUI)

Controller tương thích app nằm tại `Controllers/Api/MobileApiController.cs` với base route `/api/mobile`. Tổng cộng **42 endpoint** (catalog, auth, khách hàng, gian hàng, quản trị, tài xế, bản đồ và VNPay).

Tài liệu contract đầy đủ nằm trong `Mobile/API_MOBILE.md` ở bộ mã tích hợp app. API dùng JWT; response đơn hàng được map về đúng model MAUI (`chiTiet[].monAn` và các trường giao hàng).
