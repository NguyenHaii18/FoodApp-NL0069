using System.Globalization;
using FoodApp.Maui.Models;

namespace FoodApp.Maui.Converters;

/// <summary>True khi đơn đang ở trạng thái "Chờ xác nhận" — dùng để hiện nút Xác nhận/Từ chối phía gian hàng.</summary>
public class TrangThaiToChoXacNhanConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is TrangThaiDonHang t && t == TrangThaiDonHang.ChoXacNhan;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
