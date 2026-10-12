using System.Globalization;
using FoodApp.Maui.Models;

namespace FoodApp.Maui.Converters;

/// <summary>Nhận vào GianHangId (string), trả về chuỗi doanh thu ước tính đã định dạng — dùng trong màn hình quản trị.</summary>
public class GianHangIdToDoanhThuConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not string gianHangId) return "0đ";
        return $"{MockData.DoanhThuUocTinh(gianHangId):N0}đ";
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
