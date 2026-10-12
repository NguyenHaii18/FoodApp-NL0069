using FoodApp.Maui.Views;

namespace FoodApp.Maui;

public partial class AppShell : Shell
{
    private static bool _daDangKyRoute;

    public AppShell()
    {
        InitializeComponent();
        if (_daDangKyRoute) return;   // AppShell được tạo lại khi đổi màu chủ đề — chỉ đăng ký route một lần
        _daDangKyRoute = true;

        // Các trang được điều hướng bằng Shell.Current.GoToAsync (push lên trên,
        // không nằm trong TabBar dưới) — tương đương Navigator.push() bên Flutter.
        Routing.RegisterRoute(nameof(IntroPage), typeof(IntroPage));
        Routing.RegisterRoute(nameof(DriverEarningsPage), typeof(DriverEarningsPage));
        Routing.RegisterRoute(nameof(DriverRatingPage), typeof(DriverRatingPage));
        Routing.RegisterRoute(nameof(DriverNotificationsPage), typeof(DriverNotificationsPage));
        Routing.RegisterRoute(nameof(MenuCartPage), typeof(MenuCartPage));
        Routing.RegisterRoute(nameof(PaymentPage), typeof(PaymentPage));
        Routing.RegisterRoute(nameof(OrderTrackingPage) + "Detail", typeof(OrderTrackingPage));
        Routing.RegisterRoute(nameof(ReviewPage), typeof(ReviewPage));
        Routing.RegisterRoute(nameof(ProfilePage), typeof(ProfilePage));
        Routing.RegisterRoute(nameof(DoiMatKhauPage), typeof(DoiMatKhauPage));
        Routing.RegisterRoute(nameof(TinTucPage), typeof(TinTucPage));
        Routing.RegisterRoute(nameof(CaiDatPage), typeof(CaiDatPage));
        Routing.RegisterRoute(nameof(DiaChiPage), typeof(DiaChiPage));
        Routing.RegisterRoute(nameof(YeuThichPage), typeof(YeuThichPage));

        // Luồng tài xế giao hàng (điều hướng dạng stack, nằm ngoài TabBar khách hàng).
        Routing.RegisterRoute(nameof(DriverLoginPage), typeof(DriverLoginPage));
        Routing.RegisterRoute(nameof(DriverHomePage), typeof(DriverHomePage));
        Routing.RegisterRoute(nameof(DriverProfilePage), typeof(DriverProfilePage));
        Routing.RegisterRoute(nameof(DriverRegisterPage), typeof(DriverRegisterPage));
        Routing.RegisterRoute(nameof(AdminDriverApprovalPage), typeof(AdminDriverApprovalPage));
        Routing.RegisterRoute(nameof(ChatPage), typeof(ChatPage));
        Routing.RegisterRoute(nameof(BanDoPage), typeof(BanDoPage));
        Routing.RegisterRoute(nameof(DriverDeliveryDetailPage), typeof(DriverDeliveryDetailPage));

        // Luồng quản lý gian hàng (đối tác) — điều hướng dạng stack, nằm ngoài TabBar khách hàng.
        Routing.RegisterRoute(nameof(VendorLoginPage), typeof(VendorLoginPage));
        Routing.RegisterRoute(nameof(VendorRegisterPage), typeof(VendorRegisterPage));
        Routing.RegisterRoute(nameof(VendorHomePage), typeof(VendorHomePage));
        Routing.RegisterRoute(nameof(VendorOrdersPage), typeof(VendorOrdersPage));
        Routing.RegisterRoute(nameof(VendorMenuPage), typeof(VendorMenuPage));
        Routing.RegisterRoute(nameof(VendorMenuItemEditPage), typeof(VendorMenuItemEditPage));
        Routing.RegisterRoute(nameof(VendorRevenuePage), typeof(VendorRevenuePage));
        Routing.RegisterRoute(nameof(VendorProfilePage), typeof(VendorProfilePage));
        Routing.RegisterRoute(nameof(VendorPromotionsPage), typeof(VendorPromotionsPage));
        Routing.RegisterRoute(nameof(VendorPromotionEditPage), typeof(VendorPromotionEditPage));
        Routing.RegisterRoute(nameof(QuenMatKhauPage), typeof(QuenMatKhauPage));
        Routing.RegisterRoute(nameof(XacThucOtpPage), typeof(XacThucOtpPage));
        Routing.RegisterRoute(nameof(LichSuDangNhapPage), typeof(LichSuDangNhapPage));
        Routing.RegisterRoute(nameof(AdminStaffPage), typeof(AdminStaffPage));
        Routing.RegisterRoute(nameof(AdminAuditLogPage), typeof(AdminAuditLogPage));
        Routing.RegisterRoute(nameof(VendorReviewsPage), typeof(VendorReviewsPage));
        Routing.RegisterRoute(nameof(VendorNotificationsPage), typeof(VendorNotificationsPage));
        Routing.RegisterRoute(nameof(VendorFinancePage), typeof(VendorFinancePage));

        // Luồng quản lý quản trị — điều hướng dạng stack, nằm ngoài TabBar khách hàng.
        Routing.RegisterRoute(nameof(AdminLoginPage), typeof(AdminLoginPage));
        Routing.RegisterRoute(nameof(AdminHomePage), typeof(AdminHomePage));
        Routing.RegisterRoute(nameof(AdminVendorApprovalPage), typeof(AdminVendorApprovalPage));
        Routing.RegisterRoute(nameof(AdminVendorListPage), typeof(AdminVendorListPage));
        Routing.RegisterRoute(nameof(AdminCustomerListPage), typeof(AdminCustomerListPage));
        Routing.RegisterRoute(nameof(AdminIssuesPage), typeof(AdminIssuesPage));
        Routing.RegisterRoute(nameof(AdminPromotionsPage), typeof(AdminPromotionsPage));
        Routing.RegisterRoute(nameof(AdminPromotionEditPage), typeof(AdminPromotionEditPage));
        Routing.RegisterRoute(nameof(AdminStatisticsPage), typeof(AdminStatisticsPage));
    }

    /// <summary>
    /// Gọi sau khi đăng nhập thành công: ẩn trang đăng nhập, hiện TabBar chính.
    /// Tương đương Navigator.pushReplacement(HomeScreen) bên Flutter.
    /// </summary>
    public static async Task DangNhapThanhCongAsync()
    {
        await Current.GoToAsync(nameof(IntroPage));   // intro 5 giây, xong tự vào trang chủ
    }
}
