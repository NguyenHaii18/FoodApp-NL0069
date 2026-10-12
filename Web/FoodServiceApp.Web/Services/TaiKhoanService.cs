using Microsoft.EntityFrameworkCore;
using FoodServiceApp.Web.Data;
using FoodServiceApp.Web.Models.Entities;

namespace FoodServiceApp.Web.Services
{
    /// <summary>Nghiệp vụ tài khoản dùng chung cho Website và API: quên mật khẩu, OTP, lịch sử đăng nhập, xóa tài khoản.</summary>
    public class TaiKhoanService
    {
        private readonly AppDbContext _db;
        private readonly OtpService _otp;

        public TaiKhoanService(AppDbContext db, OtpService otp) { _db = db; _otp = otp; }

        public record KetQua(bool Ok, string ThongDiep, string? MaDev = null);

        /// <summary>Tìm tài khoản theo tên đăng nhập / email / số điện thoại (mọi vai trò). Trả cả "liên hệ" để gửi OTP.</summary>
        public async Task<(TaiKhoan? tk, string kenh, string nguoiNhan)> TimAsync(string? nhap)
        {
            nhap = (nhap ?? "").Trim();
            if (nhap.Length == 0) return (null, "email", "");
            var key = nhap.ToLowerInvariant();

            var tk = await _db.TaiKhoans.FirstOrDefaultAsync(t => t.TenDangNhap == nhap);
            if (tk == null)
            {
                var maTK =
                    await _db.KhachHangs.Where(k => k.Email == nhap || k.SDT == nhap).Select(k => (int?)k.MaTK).FirstOrDefaultAsync()
                    ?? await _db.GianHangs.Where(g => g.Email == nhap || g.SDT == nhap).Select(g => (int?)g.MaTK).FirstOrDefaultAsync()
                    ?? await _db.TaiXes.Where(t => t.SDT == nhap).Select(t => (int?)t.MaTK).FirstOrDefaultAsync()
                    ?? await _db.QuanTris.Where(q => q.Email == nhap || q.SDT == nhap).Select(q => (int?)q.MaTK).FirstOrDefaultAsync();
                if (maTK != null) tk = await _db.TaiKhoans.FirstOrDefaultAsync(t => t.MaTK == maTK);
            }
            if (tk == null) return (null, "email", key);
            return (tk, nhap.Contains('@') ? "email" : (nhap.All(char.IsDigit) ? "sms" : "email"), key);
        }

        /// <summary>Bước 1 quên mật khẩu: gửi OTP. Luôn trả thông điệp chung để không lộ tài khoản nào tồn tại.</summary>
        public async Task<KetQua> YeuCauQuenMatKhauAsync(string? nhap)
        {
            var (tk, kenh, nguoiNhan) = await TimAsync(nhap);
            const string chung = "Nếu tài khoản tồn tại, mã xác thực đã được gửi.";
            if (tk == null || !tk.TrangThai) return new KetQua(true, chung);
            var (ok, msg, ma) = await _otp.GuiAsync("QuenMatKhau", kenh, nguoiNhan, tk.MaTK);
            await NhatKy.GhiAsync(_db, null, "Otp", "Yêu cầu quên mật khẩu", tk.MaTK);
            return ok ? new KetQua(true, chung, ma) : new KetQua(false, msg);
        }

        /// <summary>Bước 2 quên mật khẩu: kiểm tra OTP rồi đặt mật khẩu mới (đồng thời mở khóa tạm do sai mật khẩu).</summary>
        public async Task<KetQua> DatLaiMatKhauAsync(string? nhap, string? otp, string? matKhauMoi)
        {
            if (string.IsNullOrEmpty(matKhauMoi) || matKhauMoi.Length < 6) return new KetQua(false, "Mật khẩu mới tối thiểu 6 ký tự.");
            var (tk, _, nguoiNhan) = await TimAsync(nhap);
            if (tk == null) return new KetQua(false, "Mã xác thực không đúng hoặc đã hết hạn.");
            var (ok, msg) = await _otp.KiemTraAsync("QuenMatKhau", nguoiNhan, otp);
            if (!ok) return new KetQua(false, msg);
            tk.MatKhau = MatKhauHelper.Hash(matKhauMoi);
            tk.SoLanDangNhapSai = 0; tk.KhoaDen = null;
            await _db.SaveChangesAsync();
            await NhatKy.GhiAsync(_db, null, "TaiKhoan", "Đặt lại mật khẩu bằng OTP", tk.MaTK);
            return new KetQua(true, "Đã đặt lại mật khẩu, hãy đăng nhập bằng mật khẩu mới.");
        }

        /// <summary>Email/SĐT hiện có của tài khoản (để xác thực).</summary>
        public async Task<(string? email, string? sdt)> LienHeAsync(TaiKhoan tk)
        {
            switch (tk.VaiTro)
            {
                case "KhachHang": { var k = await _db.KhachHangs.FirstOrDefaultAsync(x => x.MaTK == tk.MaTK); return (k?.Email, k?.SDT); }
                case "GianHang": { var g = await _db.GianHangs.FirstOrDefaultAsync(x => x.MaTK == tk.MaTK); return (g?.Email, g?.SDT); }
                case "TaiXe": { var t = await _db.TaiXes.FirstOrDefaultAsync(x => x.MaTK == tk.MaTK); return (null, t?.SDT); }
                case "QuanTri": { var q = await _db.QuanTris.FirstOrDefaultAsync(x => x.MaTK == tk.MaTK); return (q?.Email, q?.SDT); }
            }
            return (null, null);
        }

        /// <summary>Gửi OTP xác thực email hoặc SĐT cho tài khoản đang đăng nhập.</summary>
        public async Task<KetQua> GuiXacThucAsync(int maTK, string kenh)
        {
            var tk = await _db.TaiKhoans.FindAsync(maTK);
            if (tk == null) return new KetQua(false, "Không tìm thấy tài khoản.");
            var (email, sdt) = await LienHeAsync(tk);
            var nguoiNhan = kenh == "sms" ? sdt : email;
            if (string.IsNullOrWhiteSpace(nguoiNhan)) return new KetQua(false, kenh == "sms" ? "Tài khoản chưa có số điện thoại." : "Tài khoản chưa có email.");
            var (ok, msg, ma) = await _otp.GuiAsync("XacNhan", kenh == "sms" ? "sms" : "email", nguoiNhan.Trim().ToLowerInvariant(), maTK);
            return new KetQua(ok, msg, ma);
        }

        public async Task<KetQua> XacNhanXacThucAsync(int maTK, string kenh, string? otp)
        {
            var tk = await _db.TaiKhoans.FindAsync(maTK);
            if (tk == null) return new KetQua(false, "Không tìm thấy tài khoản.");
            var (email, sdt) = await LienHeAsync(tk);
            var nguoiNhan = (kenh == "sms" ? sdt : email)?.Trim().ToLowerInvariant();
            if (string.IsNullOrEmpty(nguoiNhan)) return new KetQua(false, "Thiếu thông tin liên hệ.");
            var (ok, msg) = await _otp.KiemTraAsync("XacNhan", nguoiNhan, otp);
            if (!ok) return new KetQua(false, msg);
            if (kenh == "sms") tk.SdtDaXacThuc = true; else tk.EmailDaXacThuc = true;
            await _db.SaveChangesAsync();
            await NhatKy.GhiAsync(_db, null, "TaiKhoan", $"Xác thực {(kenh == "sms" ? "SĐT" : "email")} thành công", maTK);
            return new KetQua(true, "Đã xác thực.");
        }

        public Task<List<NhatKyHeThong>> LichSuDangNhapAsync(int maTK, int top = 30) =>
            _db.NhatKyHeThongs.Where(n => n.MaTK == maTK && n.Nguon == "Auth")
                .OrderByDescending(n => n.ThoiGian).Take(top).ToListAsync();

        /// <summary>Xóa tài khoản = vô hiệu hóa + ẩn danh thông tin cá nhân (giữ lại đơn hàng cũ để đối soát).</summary>
        public async Task<KetQua> XoaTaiKhoanAsync(int maTK, string? matKhau)
        {
            var tk = await _db.TaiKhoans.FindAsync(maTK);
            if (tk == null) return new KetQua(false, "Không tìm thấy tài khoản.");
            if (tk.VaiTro == "QuanTri") return new KetQua(false, "Tài khoản quản trị không thể tự xóa.");
            if (!MatKhauHelper.Khop(matKhau ?? "", tk.MatKhau)) return new KetQua(false, "Mật khẩu xác nhận không đúng.");

            var dangXuLy = new[] { "ChoXacNhan", "DaXacNhan", "DangGiao" };
            switch (tk.VaiTro)
            {
                case "KhachHang":
                {
                    var k = await _db.KhachHangs.FirstOrDefaultAsync(x => x.MaTK == maTK);
                    if (k != null)
                    {
                        if (await _db.DonHangs.AnyAsync(d => d.MaKH == k.MaKH && dangXuLy.Contains(d.TrangThai)))
                            return new KetQua(false, "Bạn còn đơn hàng đang xử lý, hãy đợi hoàn tất hoặc hủy đơn trước.");
                        _db.DiaChiKhachHangs.RemoveRange(_db.DiaChiKhachHangs.Where(d => d.MaKH == k.MaKH));
                        k.HoTen = "Người dùng đã xóa"; k.SDT = ""; k.Email = null; k.DiaChiGiaoHang = null; k.AnhDaiDien = null;
                    }
                    break;
                }
                case "GianHang":
                {
                    var g = await _db.GianHangs.FirstOrDefaultAsync(x => x.MaTK == maTK);
                    if (g != null)
                    {
                        if (await _db.DonHangs.AnyAsync(d => d.MaGianHang == g.MaGianHang && dangXuLy.Contains(d.TrangThai)))
                            return new KetQua(false, "Gian hàng còn đơn đang xử lý, hãy hoàn tất trước khi xóa.");
                        g.TrangThaiKinhDoanh = "DongCua"; g.Email = null; g.SDT = "";
                    }
                    break;
                }
                case "TaiXe":
                {
                    var t = await _db.TaiXes.FirstOrDefaultAsync(x => x.MaTK == maTK);
                    if (t != null)
                    {
                        if (t.TrangThai == "DangGiao" || await _db.DonHangs.AnyAsync(d => d.MaTaiXe == t.MaTaiXe && (d.TrangThai == "DaXacNhan" || d.TrangThai == "DangGiao")))
                            return new KetQua(false, "Bạn đang giao đơn, hãy hoàn tất trước khi xóa tài khoản.");
                        t.TrangThai = "NgoaiTuyen"; t.SDT = ""; t.AnhDaiDien = null; t.SoCCCD = null; t.SoGPLX = null; t.AnhGPLX = null;
                    }
                    break;
                }
            }
            tk.TrangThai = false;
            tk.TenDangNhap = $"da_xoa_{tk.MaTK}";
            tk.MatKhau = MatKhauHelper.Hash(Guid.NewGuid().ToString("N"));
            await _db.SaveChangesAsync();
            await NhatKy.GhiAsync(_db, null, "TaiKhoan", "Người dùng tự xóa tài khoản", maTK, "Warn");
            return new KetQua(true, "Đã xóa tài khoản.");
        }
    }
}
