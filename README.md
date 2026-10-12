# FoodApp-NL0069

## Chức năng hợp nhất từ FoodServiceApp_HoanChinh

- Quản lý chiến dịch quảng cáo theo gian hàng trên Web và MAUI.
- Hỗ trợ vị trí `Banner`, `NoiBat`, `GoiY`, thời gian chạy, ngân sách, trạng thái và lượt tương tác.
- API chỉ cho phép tài khoản gian hàng quản lý chiến dịch thuộc gian hàng của mình.
- API công khai trả các chiến dịch đang chạy của gian hàng đã duyệt và đang mở cửa.
- Sửa lỗi biên dịch báo cáo tài chính trong `VendorMobileApiController`.

## Cập nhật database

Database mới: chạy `Database/01_TaoMoi_Full.sql`.

Database đã tồn tại: chạy một trong hai file tương đương sau:

```powershell
sqlcmd -S "(localdb)\MSSQLLocalDB" -E -b -i "Database\NangCap_v16_ChienDichQuangCao.sql"
sqlcmd -S "(localdb)\MSSQLLocalDB" -E -b -i "SQL\Script.sql"
```

`SQL/Script.sql` là script nâng cấp tổng hợp hiện tại, có thể chạy lặp lại và không xóa dữ liệu nghiệp vụ.
