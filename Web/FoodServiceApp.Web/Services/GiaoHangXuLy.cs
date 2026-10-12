using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using FoodServiceApp.Web.Data;
using FoodServiceApp.Web.Models.Entities;

namespace FoodServiceApp.Web.Services
{
    public record KetQuaGiao(bool Ok, string ThongDiep);

    /// <summary>
    /// Luồng giao hàng dùng chung cho Web + API: xác nhận nhận hàng bằng mã 4 số, bằng chứng giao hàng,
    /// giao thất bại -> yêu cầu hoàn tiền thống nhất, và vị trí tài xế lưu bền.
    /// </summary>
    public static class GiaoHangXuLy
    {
        public const int ToiDaSaiMa = 5;
        public const int ViTriCuGiay = 90;            // quá ngưỡng này coi là vị trí "cũ"

        // Lý do lỗi do khách -> tài xế vẫn nhận phí giao, hoàn tiền trừ phí giao. Lý do khác: hoàn đủ, tài xế không nhận phí.
        private static readonly string[] LoiDoKhach = { "không liên lạc", "từ chối", "hẹn lại", "sai địa chỉ", "sai hoặc không tìm thấy địa chỉ", "không tìm thấy địa chỉ", "không có nhà", "không nghe máy" };

        // ------------------------------------------------------------------ luồng trạng thái DÙNG CHUNG (Web + API + Mobile)
        // Nhận đơn:   Đơn "DaXacNhan" (quán đang chuẩn bị) + MaTaiXe gán + GiaoHang "DaNhan"  (tài xế đang đến quán)
        // Đã lấy hàng: Đơn "DangGiao" + GiaoHang "DangGiao"                                   (đang giao cho khách)
        // Hoàn thành:  Đơn "DaGiao"  + GiaoHang "GiaoThanhCong"  |  Thất bại: Đơn "DaHuy" + GiaoHang "GiaoThatBai"

        /// <summary>Tài xế còn chuyến chưa xong (đã nhận nhưng chưa lấy hàng, hoặc đang giao).</summary>
        public static Task<bool> DangCoChuyenAsync(AppDbContext db, int maTaiXe) =>
            db.DonHangs.AnyAsync(d => d.MaTaiXe == maTaiXe && (d.TrangThai == "DaXacNhan" || d.TrangThai == "DangGiao"));

        /// <summary>Nhận đơn: chiếm đơn bằng một câu UPDATE có điều kiện (an toàn khi 2 tài xế bấm cùng lúc).</summary>
        public static async Task<KetQuaGiao> NhanDonAsync(AppDbContext db, TaiXe tx, int maDon)
        {
            if (tx.TrangThaiDuyet != "DaDuyet") return new(false, "Hồ sơ tài xế chưa được duyệt.");
            if (await DangCoChuyenAsync(db, tx.MaTaiXe)) return new(false, "Bạn đang có một chuyến giao chưa hoàn thành.");
            var maTx = (int?)tx.MaTaiXe;
            var n = await db.DonHangs.Where(d => d.MaDonHang == maDon && d.TrangThai == "DaXacNhan" && d.MaTaiXe == null)
                .ExecuteUpdateAsync(s => s.SetProperty(d => d.MaTaiXe, maTx));
            if (n == 0) return new(false, "Đơn đã có tài xế khác nhận hoặc không còn sẵn sàng.");

            var now = DateTime.Now;
            var gh = await db.GiaoHangs.FirstOrDefaultAsync(g => g.MaDonHang == maDon);
            if (gh == null) db.GiaoHangs.Add(new GiaoHang { MaDonHang = maDon, MaTaiXe = tx.MaTaiXe, TrangThai = "DaNhan", ThoiGianNhan = now });
            else { gh.MaTaiXe = tx.MaTaiXe; gh.TrangThai = "DaNhan"; gh.ThoiGianNhan = now; }
            tx.TrangThai = "DangGiao";

            var maKH = await db.DonHangs.Where(d => d.MaDonHang == maDon).Select(d => d.MaKH).FirstAsync();
            var maTK = await db.KhachHangs.Where(k => k.MaKH == maKH).Select(k => (int?)k.MaTK).FirstOrDefaultAsync();
            if (maTK != null)
                db.ThongBaos.Add(new ThongBao { MaTK = maTK.Value, Loai = "DonHang", TieuDe = $"Đơn #{maDon} đã có tài xế",
                    NoiDung = $"Tài xế {tx.HoTen} đã nhận đơn và đang đến quán lấy món.", MaThamChieu = maDon.ToString() });
            await db.SaveChangesAsync();
            return new(true, "Đã nhận đơn.");
        }

        /// <summary>Đã lấy hàng -> bắt đầu giao (đơn chuyển sang DangGiao; khách thấy mã xác nhận nhận hàng).</summary>
        public static async Task<KetQuaGiao> DaLayHangAsync(AppDbContext db, DonHang don, GiaoHang gh)
        {
            if (don.TrangThai == "DangGiao" && gh.TrangThai == "DangGiao") return new(true, "Đơn đã ở trạng thái đang giao.");
            if (don.TrangThai != "DaXacNhan" || gh.TrangThai != "DaNhan")
                return new(false, "Đơn không ở bước chờ lấy hàng.");
            don.TrangThai = "DangGiao"; gh.TrangThai = "DangGiao";
            if (string.IsNullOrEmpty(don.MaXacNhanGiao)) don.MaXacNhanGiao = TaoMa();
            await db.SaveChangesAsync();
            return new(true, "Đã lấy hàng, bắt đầu giao.");
        }

        /// <summary>Trả đơn về hàng chờ khi tài xế bị gỡ/đổi: giải phóng tài xế cũ nếu không còn chuyến khác.</summary>
        public static async Task GiaiPhongTaiXeAsync(AppDbContext db, int? maTaiXeCu, int boQuaDon)
        {
            if (maTaiXeCu == null) return;
            var con = await db.DonHangs.AnyAsync(d => d.MaTaiXe == maTaiXeCu && d.MaDonHang != boQuaDon && (d.TrangThai == "DaXacNhan" || d.TrangThai == "DangGiao"));
            if (con) return;
            var tx = await db.TaiXes.FirstOrDefaultAsync(t => t.MaTaiXe == maTaiXeCu);
            if (tx != null && tx.TrangThai == "DangGiao") tx.TrangThai = "SanSang";
        }

        public static string TaoMa() => RandomNumberGenerator.GetInt32(1000, 10000).ToString();

        public static bool LoiDoKhachHang(string lyDo) =>
            LoiDoKhach.Any(k => (lyDo ?? "").Contains(k, StringComparison.OrdinalIgnoreCase));

        private static bool BangNhau(string a, string b) =>
            CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(a), Encoding.UTF8.GetBytes(b));

        /// <summary>Hoàn thành giao hàng: bắt buộc đúng mã khách đọc (đơn cũ không có mã thì bắt buộc có ảnh bằng chứng).</summary>
        public static async Task<KetQuaGiao> HoanThanhAsync(AppDbContext db, DonHang don, GiaoHang gh, TaiXe tx, string? maNhap, double? lat, double? lng)
        {
            if (don.TrangThai != "DangGiao" || gh.TrangThai != "DangGiao")
                return new(false, "Đơn chưa ở trạng thái đang giao (hãy bấm đã lấy hàng trước) hoặc đã được xử lý.");

            if (!string.IsNullOrEmpty(don.MaXacNhanGiao))
            {
                if (don.SoLanSaiMa >= ToiDaSaiMa)
                    return new(false, "Nhập sai mã quá nhiều lần, đơn bị khóa xác nhận. Liên hệ Quản trị để được hỗ trợ.");
                if (string.IsNullOrWhiteSpace(maNhap) || !BangNhau(maNhap.Trim(), don.MaXacNhanGiao))
                {
                    don.SoLanSaiMa++;
                    await db.SaveChangesAsync();
                    var con = ToiDaSaiMa - don.SoLanSaiMa;
                    return new(false, con > 0 ? $"Mã xác nhận không đúng (còn {con} lần thử)." : "Nhập sai mã quá nhiều lần, đơn bị khóa xác nhận. Liên hệ Quản trị.");
                }
            }
            else if (string.IsNullOrEmpty(gh.AnhBangChung))
                return new(false, "Đơn này không có mã xác nhận: cần chụp ảnh bằng chứng giao hàng trước khi hoàn thành.");

            var now = DateTime.Now;
            don.TrangThai = "DaGiao";
            gh.TrangThai = "GiaoThanhCong"; gh.ThoiGianGiao = now; gh.ViDoGiao = lat; gh.KinhDoGiao = lng;
            if (don.ThanhToan != null && don.ThanhToan.TrangThai != "DaThanhToan" && don.ThanhToan.HinhThuc == "TienMat")
            {
                don.ThanhToan.TrangThai = "DaThanhToan"; don.ThanhToan.NgayThanhToan = now;
                gh.TienThuCOD = don.TongTien;
            }
            tx.TrangThai = "SanSang";
            await db.SaveChangesAsync();
            return new(true, "Đã xác nhận giao thành công.");
        }

        /// <summary>
        /// Giao thất bại thống nhất: đơn -> DaHuy; phí tài xế theo lý do (server tự phân loại, không tin cờ từ client);
        /// đơn đã trả online -> tạo YeuCauHoanTien (ChoDuyet) để đi qua quy trình duyệt + hoàn tiền thật qua cổng.
        /// </summary>
        public static async Task<KetQuaGiao> ThatBaiAsync(AppDbContext db, DonHang don, GiaoHang gh, TaiXe tx, string? lyDo, double? lat, double? lng)
        {
            lyDo = (lyDo ?? "").Trim();
            if (lyDo.Length < 3) return new(false, "Nhập lý do giao thất bại.");
            if (don.TrangThai is not ("DaXacNhan" or "DangGiao") || gh.TrangThai is not ("DangGiao" or "DaNhan"))
                return new(false, "Đơn không ở trạng thái có thể báo giao thất bại.");

            var loiKhach = LoiDoKhachHang(lyDo);
            gh.TrangThai = "GiaoThatBai"; gh.ThoiGianGiao = DateTime.Now; gh.ViDoGiao = lat; gh.KinhDoGiao = lng;
            gh.LyDoThatBai = lyDo; gh.TinhPhiThatBai = loiKhach; gh.NguoiGayLoi = loiKhach ? "KhachHang" : "Khac";
            don.TrangThai = "DaHuy";
            don.GhiChu = string.IsNullOrWhiteSpace(don.GhiChu) ? $"Giao thất bại: {lyDo}" : $"{don.GhiChu} | Giao thất bại: {lyDo}";
            tx.TrangThai = "SanSang";

            string tbHoan = "";
            var tt = don.ThanhToan;
            if (tt != null && tt.HinhThuc != "TienMat" && tt.TrangThai == "DaThanhToan")
            {
                var soTien = loiKhach ? Math.Max(0, don.TongTien - don.PhiGiaoHang) : don.TongTien;
                var daCo = await db.YeuCauHoanTiens.AnyAsync(y => y.MaDonHang == don.MaDonHang && (y.TrangThai == "ChoDuyet" || y.TrangThai == "DaDuyet"));
                if (soTien > 0 && !daCo)
                {
                    db.YeuCauHoanTiens.Add(new YeuCauHoanTien
                    {
                        MaDonHang = don.MaDonHang, MaKH = don.MaKH, SoTien = soTien, Nguon = "GiaoThatBai",
                        LyDo = "Giao thất bại: " + lyDo + (loiKhach ? " (lỗi phía khách: giữ lại phí giao)" : ""),
                    });
                    tbHoan = $" Yêu cầu hoàn {soTien:N0}đ đã được tạo và đang chờ duyệt.";
                }
            }
            var maTK = await db.KhachHangs.Where(k => k.MaKH == don.MaKH).Select(k => (int?)k.MaTK).FirstOrDefaultAsync();
            if (maTK != null)
                db.ThongBaos.Add(new ThongBao { MaTK = maTK.Value, Loai = "DonHang", TieuDe = $"Đơn #{don.MaDonHang} giao không thành công",
                    NoiDung = $"Lý do: {lyDo}." + tbHoan, MaThamChieu = don.MaDonHang.ToString() });
            await db.SaveChangesAsync();
            return new(true, "Đã ghi nhận giao thất bại." + tbHoan);
        }

        // ------------------------------------------------------------------ vị trí tài xế
        /// <summary>Lưu vị trí (chặn cập nhật dồn dập và bước nhảy phi thực tế > 150 km/h).</summary>
        public static async Task<KetQuaGiao> LuuViTriAsync(AppDbContext db, int maDon, int maTaiXe, double lat, double lng)
        {
            var now = DateTime.Now;
            var cu = await db.TaiXeViTris.FirstOrDefaultAsync(v => v.MaDonHang == maDon);
            if (cu != null)
            {
                var giay = (now - cu.CapNhatLuc).TotalSeconds;
                if (giay < 2) return new(true, "Bỏ qua (cập nhật quá dày).");
                var km = KhoangCachKm(cu.ViDo, cu.KinhDo, lat, lng);
                if (giay < 120 && km / (giay / 3600.0) > 150) return new(false, "Vị trí nhảy bất thường, bỏ qua.");
                cu.ViDo = lat; cu.KinhDo = lng; cu.CapNhatLuc = now; cu.MaTaiXe = maTaiXe;
            }
            else db.TaiXeViTris.Add(new TaiXeViTri { MaDonHang = maDon, MaTaiXe = maTaiXe, ViDo = lat, KinhDo = lng, CapNhatLuc = now });
            await db.SaveChangesAsync();
            return new(true, "OK");
        }

        public static double KhoangCachKm(double la1, double lo1, double la2, double lo2)
        {
            const double R = 6371;
            double d(double x) => x * Math.PI / 180;
            var a = Math.Pow(Math.Sin(d(la2 - la1) / 2), 2) + Math.Cos(d(la1)) * Math.Cos(d(la2)) * Math.Pow(Math.Sin(d(lo2 - lo1) / 2), 2);
            return 2 * R * Math.Asin(Math.Sqrt(a));
        }
    }
}
