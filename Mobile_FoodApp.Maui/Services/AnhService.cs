namespace FoodApp.Maui.Services;

/// <summary>Cho gian hàng chọn ảnh từ máy và lưu vào thư mục dữ liệu của app.</summary>
public static class AnhService
{
    /// <returns>Đường dẫn file ảnh đã lưu, hoặc null nếu người dùng hủy.</returns>
    public static async Task<string?> ChonVaLuuAsync(string tienTo, string tieuDe = "Chọn ảnh cửa hàng", bool chup = false)
    {
        FileResult? file;
        if (chup)
        {
            if (!MediaPicker.Default.IsCaptureSupported) return null;   // máy không có camera
            file = await MediaPicker.Default.CapturePhotoAsync(new MediaPickerOptions { Title = tieuDe });
        }
        else
        {
            file = await MediaPicker.Default.PickPhotoAsync(new MediaPickerOptions { Title = tieuDe });
        }
        if (file is null) return null;

        var ext = Path.GetExtension(file.FileName);
        if (string.IsNullOrEmpty(ext)) ext = ".jpg";
        var dich = Path.Combine(FileSystem.AppDataDirectory, $"{tienTo}_{DateTime.Now:yyyyMMddHHmmss}{ext}");

        await using var nguon = await file.OpenReadAsync();
        await using var ra = File.Create(dich);
        await nguon.CopyToAsync(ra);
        return dich;
    }
}
