using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FoodServiceApp.Web.Data;
using FoodServiceApp.Web.Models.Entities;
using FoodServiceApp.Web.Services;

namespace FoodServiceApp.Web.Controllers
{
    /// <summary>Báo cáo & thống kê theo khoảng thời gian, xuất CSV (Excel mở được).</summary>
    [Route("QuanTri/BaoCao")]
    public class AdminBaoCaoController : AdminOpsBase
    {
        private readonly CauHinhService _cauHinh;
        public AdminBaoCaoController(AppDbContext db, CauHinhService cauHinh) : base(db) { _cauHinh = cauHinh; }

        private static (DateTime tu, DateTime den) Khoang(DateTime? tu, DateTime? den)
        {
            var d = (den ?? DateTime.Today).Date.AddDays(1);
            var t = (tu ?? DateTime.Today.AddDays(-29)).Date;
            return (t, d);
        }

        // ---- Các bộ số liệu dùng chung cho trang HTML và CSV ----
        private async Task<List<DonHang>> DonAsync(DateTime t, DateTime d) =>
            await Db.DonHangs.AsNoTracking().Include(x => x.GianHang).Include(x => x.ThanhToan).Include(x => x.GiaoHang).ThenInclude(g => g!.TaiXe)
                .Where(x => x.NgayDat >= t && x.NgayDat < d).ToListAsync();

        public record DongQuan(string Quan, int SoDon, decimal TienHang, decimal PhiNenTang, decimal ThucNhan);
        public record DongTaiXe(string TaiXe, int SoDon, decimal ThuNhap);
        public record DongKm(string Ma, string Ten, int Luot, decimal TongGiam, int SoKhach);

        private static List<DongQuan> TheoQuan(List<DonHang> hoanThanh) =>
            hoanThanh.GroupBy(x => x.GianHang?.TenCuaHang ?? "?").Select(g =>
            {
                var hang = g.Sum(x => x.TongTien - x.PhiGiaoHang);
                var phi = Math.Round(g.Sum(x => (x.TongTien - x.PhiGiaoHang) * (x.GianHang?.TyLeChietKhau ?? 0)), 0);
                return new DongQuan(g.Key, g.Count(), hang, phi, hang - phi);
            }).OrderByDescending(x => x.TienHang).ToList();

        private static List<DongTaiXe> TheoTaiXe(List<DonHang> hoanThanh) =>
            hoanThanh.Where(x => x.GiaoHang?.TaiXe != null).GroupBy(x => x.GiaoHang!.TaiXe!.HoTen)
                .Select(g => new DongTaiXe(g.Key, g.Count(), g.Sum(x => x.PhiGiaoHang))).OrderByDescending(x => x.ThuNhap).ToList();

        private async Task<List<DongKm>> KhuyenMaiAsync(DateTime t, DateTime d)
        {
            var don = await Db.DonHangs.AsNoTracking().Where(x => x.NgayDat >= t && x.NgayDat < d && x.MaKhuyenMaiApDung != null && x.TrangThai != "DaHuy").ToListAsync();
            var ids = don.Select(x => x.MaKhuyenMaiApDung!.Value).Distinct().ToList();
            var km = await Db.KhuyenMais.AsNoTracking().Where(k => ids.Contains(k.MaKhuyenMai)).ToDictionaryAsync(k => k.MaKhuyenMai);
            return don.GroupBy(x => x.MaKhuyenMaiApDung!.Value).Select(g => new DongKm(km[g.Key].MaCode, km[g.Key].TenChuongTrinh ?? km[g.Key].MoTa ?? "", g.Count(), g.Sum(x => x.TienGiam), g.Select(x => x.MaKH).Distinct().Count()))
                .OrderByDescending(x => x.TongGiam).ToList();
        }

        [HttpGet("")]
        public async Task<IActionResult> Index(DateTime? tu, DateTime? den)
        {
            var (t, d) = Khoang(tu, den);
            var don = await DonAsync(t, d);
            var hoanThanh = don.Where(x => x.TrangThai == "DaGiao").ToList();

            ViewBag.Tu = t.ToString("yyyy-MM-dd"); ViewBag.Den = d.AddDays(-1).ToString("yyyy-MM-dd");
            ViewBag.TongDon = don.Count; ViewBag.HoanThanh = hoanThanh.Count;
            ViewBag.Huy = don.Count(x => x.TrangThai == "DaHuy");
            ViewBag.ThatBai = don.Count(x => x.GiaoHang?.TrangThai == "GiaoThatBai");
            ViewBag.DoanhThu = hoanThanh.Sum(x => x.TongTien);
            ViewBag.Quan = TheoQuan(hoanThanh);
            ViewBag.TaiXe = TheoTaiXe(hoanThanh);
            ViewBag.GiaoDich = don.Where(x => x.ThanhToan != null).GroupBy(x => (x.ThanhToan!.HinhThuc, x.ThanhToan.TrangThai))
                .Select(g => (g.Key.HinhThuc, g.Key.TrangThai, g.Count(), g.Sum(x => x.TongTien))).OrderBy(x => x.HinhThuc).ToList();
            var hoan = await Db.YeuCauHoanTiens.AsNoTracking().Where(y => y.NgayTao >= t && y.NgayTao < d).ToListAsync();
            ViewBag.HoanSo = hoan.Count; ViewBag.HoanDuyet = hoan.Where(y => y.TrangThai == "DaDuyet").Sum(y => y.SoTien);
            ViewBag.HoanCho = hoan.Count(y => y.TrangThai == "ChoDuyet");
            ViewBag.KhuyenMai = await KhuyenMaiAsync(t, d);

            var dg = await Db.DanhGias.AsNoTracking().Where(x => x.NgayDanhGia >= t && x.NgayDanhGia < d).ToListAsync();
            ViewBag.DgSo = dg.Count; ViewBag.DgTB = dg.Any() ? dg.Average(x => x.SoSao) : 0; ViewBag.DgAn = dg.Count(x => x.DaAn);
            var ph = await Db.PhanHois.AsNoTracking().Where(p => p.NgayGui >= t && p.NgayGui < d).ToListAsync();
            ViewBag.PhSo = ph.Count; ViewBag.PhKhieuNai = ph.Count(p => p.Loai == "KhieuNai"); ViewBag.PhCho = ph.Count(p => p.TrangThai == "ChuaXuLy");
            var xong = ph.Where(p => p.NgayXuLy != null).ToList();
            ViewBag.PhGioTB = xong.Any() ? xong.Average(p => (p.NgayXuLy!.Value - p.NgayGui).TotalHours) : 0;
            return View();
        }

        [HttpGet("csv/{loai}")]
        public async Task<IActionResult> Xuat(string loai, DateTime? tu, DateTime? den)
        {
            var (t, d) = Khoang(tu, den);
            var don = await DonAsync(t, d);
            var ht = don.Where(x => x.TrangThai == "DaGiao").ToList();
            switch (loai)
            {
                case "tong-don":
                    return Csv("BaoCao_TongDon", new[] { "Trạng thái", "Số đơn" }, don.GroupBy(x => x.TrangThai).Select(g => new object?[] { g.Key, g.Count() })
                        .Append(new object?[] { "GiaoThatBai", don.Count(x => x.GiaoHang?.TrangThai == "GiaoThatBai") }));
                case "doanh-thu-ngay":
                    return Csv("BaoCao_DoanhThuNgay", new[] { "Ngày", "Số đơn", "Doanh thu" }, ht.GroupBy(x => x.NgayDat.Date).OrderBy(g => g.Key).Select(g => new object?[] { g.Key.ToString("yyyy-MM-dd"), g.Count(), g.Sum(x => x.TongTien) }));
                case "gian-hang":
                    return Csv("BaoCao_GianHang_PhiNenTang", new[] { "Gian hàng", "Số đơn", "Tiền hàng", "Phí nền tảng", "Thực nhận" }, TheoQuan(ht).Select(x => new object?[] { x.Quan, x.SoDon, x.TienHang, x.PhiNenTang, x.ThucNhan }));
                case "tai-xe":
                    return Csv("BaoCao_ThuNhapTaiXe", new[] { "Tài xế", "Số đơn", "Thu nhập" }, TheoTaiXe(ht).Select(x => new object?[] { x.TaiXe, x.SoDon, x.ThuNhap }));
                case "giao-dich":
                    return Csv("BaoCao_GiaoDich", new[] { "Mã đơn", "Ngày", "Hình thức", "Trạng thái", "Mã giao dịch", "Số tiền" },
                        don.Where(x => x.ThanhToan != null).Select(x => new object?[] { x.MaDonHang, x.NgayDat, x.ThanhToan!.HinhThuc, x.ThanhToan.TrangThai, x.ThanhToan.MaGiaoDich, x.TongTien }));
                case "hoan-tien":
                    var hoan = await Db.YeuCauHoanTiens.AsNoTracking().Where(y => y.NgayTao >= t && y.NgayTao < d).OrderBy(y => y.NgayTao).ToListAsync();
                    return Csv("BaoCao_HoanTien", new[] { "Mã YC", "Mã đơn", "Nguồn", "Số tiền", "Trạng thái", "Lý do", "Kết quả", "Ngày tạo", "Ngày xử lý" }, hoan.Select(y => new object?[] { y.MaYeuCau, y.MaDonHang, y.Nguon, y.SoTien, y.TrangThai, y.LyDo, y.KetQua, y.NgayTao, y.NgayXuLy }));
                case "khuyen-mai":
                    return Csv("BaoCao_KhuyenMai", new[] { "Mã", "Chương trình", "Lượt dùng", "Tổng giảm", "Số khách" }, (await KhuyenMaiAsync(t, d)).Select(x => new object?[] { x.Ma, x.Ten, x.Luot, x.TongGiam, x.SoKhach }));
                case "danh-gia":
                    var dg = await Db.DanhGias.AsNoTracking().Where(x => x.NgayDanhGia >= t && x.NgayDanhGia < d).OrderBy(x => x.NgayDanhGia).ToListAsync();
                    return Csv("BaoCao_DanhGia", new[] { "Ngày", "Mã đơn", "Sao quán", "Sao tài xế", "Nội dung", "Đã ẩn" }, dg.Select(x => new object?[] { x.NgayDanhGia, x.MaDonHang, x.SoSao, x.SoSaoGiaoHang, x.NoiDung, x.DaAn ? "Có" : "" }));
                case "khieu-nai":
                    var ph = await Db.PhanHois.AsNoTracking().Where(p => p.NgayGui >= t && p.NgayGui < d).OrderBy(p => p.NgayGui).ToListAsync();
                    return Csv("BaoCao_KhieuNai", new[] { "Mã", "Ngày gửi", "Loại", "Tiêu đề", "Trạng thái", "Giờ giải quyết" }, ph.Select(p => new object?[] { p.MaPhanHoi, p.NgayGui, p.Loai, p.TieuDe, p.TrangThai, p.NgayXuLy == null ? null : Math.Round((p.NgayXuLy.Value - p.NgayGui).TotalHours, 1) }));
            }
            return NotFound();
        }
    }
}
