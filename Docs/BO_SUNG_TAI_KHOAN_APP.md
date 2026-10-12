# Đối chiếu yêu cầu "Tài khoản & phân quyền" với app MAUI (bản v4)

| Nhóm | Yêu cầu | App | Ghi chú |
|---|---|---|---|
| Khách | Đăng ký, đăng nhập, đăng xuất, đổi mật khẩu | Có | |
| Khách | Quên mật khẩu | Có | OTP; online: `tai-khoan/quen-mat-khau` + `dat-lai-mat-khau` |
| Khách | Xác thực OTP/email | Có | Sau đăng ký hoặc trong Hồ sơ; online: `xac-thuc/gui-otp` + `xac-nhan` |
| Khách | Khóa/mở tài khoản | Có | Quản trị; ghi nhật ký |
| Khách | Cập nhật họ tên, SĐT, email, ảnh đại diện | Có | Email đổi bằng OTP (offline) hoặc trên website (online); ảnh lưu trên máy |
| Khách | Lịch sử đăng nhập | Có | Online: `tai-khoan/lich-su-dang-nhap`; offline: nhật ký trên máy |
| Khách | Xóa tài khoản | Có | Online: `tai-khoan/xoa` |
| Gian hàng | Đăng ký, đăng nhập, đăng xuất, quên/đổi mật khẩu | Có | |
| Gian hàng | Gửi hồ sơ, chờ duyệt/từ chối (lý do)/duyệt, khóa/mở | Có | |
| Gian hàng | Cập nhật thông tin, giấy tờ, mở/đóng cửa, giờ hoạt động | Có | Giấy tờ: xem giới hạn trong HUONG_DAN_CHAY.txt |
| Tài xế | Đăng ký, đăng nhập, đăng xuất, quên/đổi mật khẩu | Có | |
| Tài xế | Xác thực hồ sơ, duyệt/khóa | Có | CCCD, giấy phép lái xe + ảnh |
| Tài xế | Thông tin cá nhân, phương tiện, biển số | Có | |
| Tài xế | Online/offline, sẵn sàng/không sẵn sàng | Có | 2 công tắc riêng; Web nhận SanSang / NgoaiTuyen |
| Quản trị | Đăng nhập admin, phân quyền, Admin tổng, Nhân viên hỗ trợ | Có | `PhanQuyenService` |
| Quản trị | Quản lý khách hàng, gian hàng, tài xế, khóa/mở | Có | Nhân viên hỗ trợ chỉ xem + xử lý khiếu nại/hoàn tiền/thống kê |
| Quản trị | Nhật ký thao tác | Có | Chỉ Admin tổng, chỉ đọc |

Ma trận quyền: Admin tổng = tất cả. Nhân viên hỗ trợ = xem tài khoản, xử lý khiếu nại, hoàn tiền, xem thống kê.
