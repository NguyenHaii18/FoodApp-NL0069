using FoodApp.Maui.Models;

namespace FoodApp.Maui.Services;

/// <summary>Người dùng đang đăng nhập (phía khách hàng) và thao tác đăng xuất.</summary>
public static class Phien
{
    public static KhachHang? Khach { get; set; }

    public static void DangXuat()
    {
        ApiClient.Token = null;
        Khach = null;
    }
}
