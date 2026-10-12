using Microsoft.AspNetCore.Http;

namespace FoodServiceApp.Web.Services
{
    public interface IFileStorageService
    {
        /// <summary>
        /// Lưu file vào thư mục con chỉ định (vd: "monan", "gianhang").
        /// Trả về đường dẫn URL tương đối để lưu vào CSDL (cột HinhAnh).
        /// </summary>
        Task<string> SaveFileAsync(IFormFile file, string subFolder);

        /// <summary>
        /// Xóa file theo đường dẫn URL tương đối đã lưu trong CSDL.
        /// </summary>
        void DeleteFile(string relativeUrl);
    }
}
