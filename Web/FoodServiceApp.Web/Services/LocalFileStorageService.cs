using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;

namespace FoodServiceApp.Web.Services
{
    /// <summary>
    /// Lưu ảnh/file trực tiếp vào wwwroot/uploads/{subFolder}.
    /// Phù hợp cho đồ án: không cần đăng ký dịch vụ ngoài, chạy được ngay trên localhost.
    /// Nếu sau này cần scale, chỉ cần thay implementation này bằng CloudinaryStorageService
    /// mà không đổi interface IFileStorageService.
    /// </summary>
    public class LocalFileStorageService : IFileStorageService
    {
        private readonly IWebHostEnvironment _env;
        private static readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".png", ".webp", ".pdf" };
        private const long MaxFileSizeBytes = 5 * 1024 * 1024; // 5MB

        public LocalFileStorageService(IWebHostEnvironment env)
        {
            _env = env;
        }

        public async Task<string> SaveFileAsync(IFormFile file, string subFolder)
        {
            if (file == null || file.Length == 0)
                throw new ArgumentException("File không hợp lệ hoặc rỗng.");

            if (file.Length > MaxFileSizeBytes)
                throw new ArgumentException("File vượt quá dung lượng cho phép (5MB).");

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!AllowedExtensions.Contains(extension))
                throw new ArgumentException("Chỉ chấp nhận file ảnh: .jpg, .jpeg, .png, .webp");

            var uploadsRoot = Path.Combine(_env.WebRootPath ?? "wwwroot", "uploads", subFolder);
            Directory.CreateDirectory(uploadsRoot);

            var fileName = $"{Guid.NewGuid()}{extension}";
            var fullPath = Path.Combine(uploadsRoot, fileName);

            using (var stream = new FileStream(fullPath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            // Đường dẫn URL tương đối để lưu vào cột HinhAnh trong CSDL
            return $"/uploads/{subFolder}/{fileName}";
        }

        public void DeleteFile(string relativeUrl)
        {
            if (string.IsNullOrWhiteSpace(relativeUrl))
                return;

            var fullPath = Path.Combine(_env.WebRootPath ?? "wwwroot", relativeUrl.TrimStart('/'));
            if (File.Exists(fullPath))
                File.Delete(fullPath);
        }
    }
}
