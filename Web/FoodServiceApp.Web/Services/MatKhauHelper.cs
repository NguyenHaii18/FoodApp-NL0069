namespace FoodServiceApp.Web.Services
{
    /// <summary>
    /// Hash và kiểm tra mật khẩu bằng BCrypt. <see cref="Khop"/> tương thích ngược với dữ liệu
    /// mẫu cũ còn lưu plaintext (ví dụ tài khoản seed sẵn trong file .sql) — hash BCrypt luôn có
    /// tiền tố "$2" nên có thể phân biệt được, tránh phải sửa lại toàn bộ dữ liệu mẫu khi nâng
    /// cấp. Tài khoản plaintext cũ vẫn đăng nhập được; mật khẩu mới tạo/đổi luôn được hash.
    /// </summary>
    public static class MatKhauHelper
    {
        public static string Hash(string matKhauGoc) => BCrypt.Net.BCrypt.HashPassword(matKhauGoc);

        public static bool Khop(string matKhauNhap, string matKhauLuu)
        {
            if (string.IsNullOrEmpty(matKhauLuu)) return false;

            // Hash BCrypt luôn có dạng "$2a$", "$2b$" hoặc "$2y$" ở đầu.
            if (matKhauLuu.StartsWith("$2"))
            {
                try { return BCrypt.Net.BCrypt.Verify(matKhauNhap, matKhauLuu); }
                catch { return false; } // hash lỗi định dạng -> coi như không khớp thay vì crash
            }

            // Dữ liệu mẫu cũ chưa hash (DEMO/seed) -> so sánh trực tiếp như trước đây.
            return matKhauNhap == matKhauLuu;
        }
    }
}
