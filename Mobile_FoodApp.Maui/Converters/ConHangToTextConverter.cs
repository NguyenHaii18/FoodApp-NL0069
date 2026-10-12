using System.Globalization;

namespace FoodApp.Maui.Converters;

/// <summary>Chuyển cờ ConHang (bool) của món ăn thành nhãn "Còn hàng" / "Hết hàng".</summary>
public class ConHangToTextConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is true ? "Còn hàng" : "Hết hàng";

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
