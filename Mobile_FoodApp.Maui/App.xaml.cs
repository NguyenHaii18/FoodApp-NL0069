using FoodApp.Maui.Services;

namespace FoodApp.Maui;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();
        ThemeColors.Ap();                // màu chủ đề người dùng đã chọn
        ThemeService.Ap();               // Sáng / Tối / Theo hệ thống (người dùng chọn ở trang Tài khoản)
        VendorLocalStore.Nap();
        TaiXeStore.Nap();                // hồ sơ + ảnh đại diện tài xế
        QuanTriStore.Nap();              // tài khoản quản trị (Admin tổng / nhân viên hỗ trợ)
        TaiKhoanXoaStore.ApDung();       // bỏ các tài khoản khách đã xóa
        GiaoHangService.KhoiPhucDonDangGiao();   // đơn tài xế đã nhận mà chưa giao xong
        MainPage = new AppShell();
        _ = KhoiTaoAsync();
    }

    private static async Task KhoiTaoAsync()
    {
        await AppSettings.NapAsync();
        var online = await DuLieuService.TaiDanhMucAsync();

        // An toàn khi demo: nếu không gọi được API thật (chưa bật SQL Server/ASP.NET Core,
        // hoặc máy demo không có mạng), nạp dữ liệu mẫu ngoại tuyến thay vì để trắng màn hình.
        if (!online) Models.DuLieuMauNgoaiTuyen.NapNeuChuaCo();
    }
}
