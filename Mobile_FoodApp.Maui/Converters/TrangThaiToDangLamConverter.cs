using System.Globalization;
using FoodApp.Maui.Models;

namespace FoodApp.Maui.Converters;

/// <summary>True khi đơn đang ở trạng thái "Đang chuẩn bị" — dùng để hiện nút bàn giao tài xế phía gian hàng.</summary>
public class TrangThaiToDangLamConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is TrangThaiDonHang t && t == TrangThaiDonHang.DangLam;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
