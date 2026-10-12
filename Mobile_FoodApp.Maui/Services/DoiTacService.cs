using FoodApp.Maui.Models;

namespace FoodApp.Maui.Services;

public enum KyBaoCao { Ngay, Thang, Nam }

/// <summary>
/// Nghiệp vụ dùng chung của phía đối tác (gian hàng): xử lý đơn, thông báo, đánh giá, đối soát tài chính, kỳ báo cáo.
/// Dữ liệu hiện lưu cục bộ (MockData + VendorLocalStore); khi có API thật thì thay phần ghi bằng DuLieuService.GhiAsync.
/// </summary>
public static class DoiTacService
{
    /// <summary>Mức chiết khấu mặc định của nền tảng (báo cáo NV09: tỉ lệ % thỏa thuận lúc đăng ký). Mỗi gian hàng có thể có mức riêng (GianHang.TyLeChietKhau).</summary>
    public const decimal TyLeChietKhauMacDinh = 0.15m;

    public static string TaoId(string tienTo) => tienTo + DateTime.Now.Ticks;

    public static IEnumerable<DonHang> DonCuaGianHang(string gianHangId) =>
        MockData.DonHangCuaGianHang.Where(d => d.GianHangId == gianHangId);

    // ====================== THÔNG BÁO ======================

    public static void GuiThongBao(string gianHangId, string loai, string tieuDe, string noiDung, string donHangId = "")
    {
        if (string.IsNullOrEmpty(gianHangId)) return;
        MockData.DanhSachThongBao.Insert(0, new ThongBaoGianHang
        {
            Id = TaoId("tb"), GianHangId = gianHangId, Loai = loai, TieuDe = tieuDe, NoiDung = noiDung,
            DonHangId = donHangId, Ngay = DateTime.Now, DaDoc = false
        });
        while (MockData.DanhSachThongBao.Count > 300) MockData.DanhSachThongBao.RemoveAt(MockData.DanhSachThongBao.Count - 1);
    }

    public static List<ThongBaoGianHang> ThongBaoCua(string gianHangId) =>
        MockData.DanhSachThongBao.Where(t => t.GianHangId == gianHangId).OrderByDescending(t => t.Ngay).ToList();

    public static int SoThongBaoChuaDoc(string gianHangId) =>
        MockData.DanhSachThongBao.Count(t => t.GianHangId == gianHangId && !t.DaDoc);

    public static void DanhDauDaDocTatCa(string gianHangId)
    {
        foreach (var t in MockData.DanhSachThongBao.Where(t => t.GianHangId == gianHangId && !t.DaDoc)) t.DaDoc = true;
        VendorLocalStore.Luu();
    }

    // ====================== XỬ LÝ ĐƠN ======================

    public static readonly string[] LyDoTuChoiMau =
    {
        "Hết nguyên liệu / hết món",
        "Quán sắp đóng cửa",
        "Quá tải, không kịp chuẩn bị",
        "Ngoài khu vực phục vụ",
    };

    public static async Task XacNhanDonAsync(DonHang don)
    {
        don.TrangThai = TrangThaiDonHang.DangLam;
        don.XacNhanLuc = DateTime.Now;
        DonHangService.DongBo(don);   // đồng bộ sang các bản của khách / tài xế (đồng thời lưu)
        await DuLieuService.GhiAsync("PUT", $"api/mobile/don-hang/{don.Id}/xac-nhan");
    }

    /// <summary>Từ chối đơn: bắt buộc chọn/nhập lý do (NV04). Trả về true nếu đơn đã được từ chối.</summary>
    public static async Task<bool> TuChoiDonAsync(Page trang, DonHang don)
    {
        const string khac = "Lý do khác…";
        var chon = await trang.DisplayActionSheet($"Lý do từ chối đơn #{don.Id}", "Để sau", null,
            LyDoTuChoiMau.Append(khac).ToArray());
        if (string.IsNullOrEmpty(chon) || chon == "Để sau") return false;

        var lyDo = chon;
        if (chon == khac)
        {
            lyDo = await trang.DisplayPromptAsync("Lý do từ chối", "Nhập lý do (bắt buộc, khách hàng sẽ nhìn thấy)",
                "Từ chối", "Hủy", maxLength: 200) ?? "";
            if (string.IsNullOrWhiteSpace(lyDo)) return false;
        }

        don.TrangThai = TrangThaiDonHang.DaHuy;
        don.DaHoanTien = true;                       // hoàn tiền tự động theo quy định
        don.LyDoTuChoi = lyDo.Trim();
        DonHangService.DongBo(don);
        await DuLieuService.GhiAsync("PUT", $"api/mobile/don-hang/{don.Id}/tu-choi", new { lyDo = don.LyDoTuChoi });
        return true;
    }

    /// <summary>
    /// Món đã chuẩn bị xong: bàn giao cho tài xế. Đơn vẫn là "Đang chuẩn bị" (SanSangGiao = true) cho tới khi TÀI XẾ
    /// nhận và lấy hàng; dùng chính đối tượng đơn để thông tin khách, thanh toán, tuyến đường đi theo sang tài xế.
    /// </summary>
    public static async Task BanGiaoTaiXeAsync(DonHang don)
    {
        don.SanSangGiao = true;
        don.SanSangGiaoLuc = DateTime.Now;
        if (!MockData.DonHangChoTaiXe.Contains(don) && MockData.DonHangDaGiaoCuaTaiXe.All(d => d.Id != don.Id))
            MockData.DonHangChoTaiXe.Add(don);
        DonHangService.DongBo(don);
        await DuLieuService.GhiAsync("PUT", $"api/mobile/don-hang/{don.Id}/san-sang-giao");
    }

    /// <summary>
    /// Quy định NV04: quá 5 phút gian hàng không phản hồi thì tự hủy đơn và hoàn tiền cho khách.
    /// Gọi mỗi khi mở/làm mới màn hình đơn hàng. Trả về số đơn đã tự hủy.
    /// </summary>
    public static int HuyDonQuaHan(string gianHangId)
    {
        var quaHan = DonCuaGianHang(gianHangId).Where(d => d.QuaHanPhanHoi).ToList();
        foreach (var d in quaHan)
        {
            d.TrangThai = TrangThaiDonHang.DaHuy;
            d.DaHoanTien = true;
            d.LyDoTuChoi = $"Gian hàng không phản hồi trong {DonHang.PhutPhanHoiToiDa} phút — hệ thống tự hủy";
            GuiThongBao(gianHangId, ThongBaoGianHang.HuyDon, "Đơn bị tự hủy do quá hạn",
                $"Đơn #{d.Id} đã bị hủy vì chưa được phản hồi trong {DonHang.PhutPhanHoiToiDa} phút.", d.Id);
            DonHangService.DongBo(d);
        }
        if (quaHan.Count > 0) VendorLocalStore.Luu();
        return quaHan.Count;
    }

    // ====================== ĐÁNH GIÁ ======================

    public static List<DanhGiaGianHang> DanhGiaCua(string gianHangId) =>
        MockData.DanhSachDanhGia.Where(d => d.GianHangId == gianHangId).OrderByDescending(d => d.Ngay).ToList();

    /// <summary>Lưu đánh giá của khách (gọi từ ReviewPage), cập nhật điểm trung bình của quán và báo cho quán.</summary>
    public static void GhiDanhGia(DonHang don, int saoMon, int saoGiao, IEnumerable<string> the, string noiDung)
    {
        var dg = MockData.DanhSachDanhGia.FirstOrDefault(d => d.DonHangId == don.Id);
        var laMoi = dg is null;
        if (dg is null)
        {
            dg = new DanhGiaGianHang { Id = TaoId("dg"), GianHangId = don.GianHangId, DonHangId = don.Id };
            MockData.DanhSachDanhGia.Insert(0, dg);
        }
        dg.KhachTen = string.IsNullOrWhiteSpace(don.KhachTen) ? (Phien.Khach?.HoTen ?? "Khách hàng") : don.KhachTen;
        dg.SoSaoMonAn = saoMon;
        dg.SoSaoGiaoHang = saoGiao;
        dg.The = the.ToList();
        dg.NoiDung = noiDung?.Trim() ?? "";
        dg.Ngay = DateTime.Now;

        CapNhatDiemTrungBinh(don.GianHangId);
        if (laMoi)
            GuiThongBao(don.GianHangId, ThongBaoGianHang.DanhGiaMoi, $"Đánh giá mới {saoMon}★",
                $"{dg.KhachTen} đánh giá đơn #{don.Id}" + (dg.CoNoiDung ? $": “{dg.NoiDung}”" : "."), don.Id);
        VendorLocalStore.Luu();
    }

    public static void CapNhatDiemTrungBinh(string gianHangId)
    {
        var ds = MockData.DanhSachDanhGia.Where(d => d.GianHangId == gianHangId && d.SoSaoMonAn > 0).ToList();
        var gh = MockData.DanhSachGianHang.FirstOrDefault(g => g.Id == gianHangId);
        if (gh is not null && ds.Count > 0) gh.DanhGia = Math.Round(ds.Average(d => d.SoSaoMonAn), 1);
    }

    // ====================== KỲ BÁO CÁO & TÀI CHÍNH ======================

    /// <summary>Khoảng thời gian [Tu, Den) của kỳ chứa mốc <paramref name="moc"/>.</summary>
    public static (DateTime Tu, DateTime Den) Khoang(KyBaoCao ky, DateTime moc) => ky switch
    {
        KyBaoCao.Ngay => (moc.Date, moc.Date.AddDays(1)),
        KyBaoCao.Thang => (new DateTime(moc.Year, moc.Month, 1), new DateTime(moc.Year, moc.Month, 1).AddMonths(1)),
        _ => (new DateTime(moc.Year, 1, 1), new DateTime(moc.Year, 1, 1).AddYears(1)),
    };

    public static string NhanKy(KyBaoCao ky, DateTime moc) => ky switch
    {
        KyBaoCao.Ngay => moc.Date == DateTime.Today ? "Hôm nay" : moc.ToString("dd/MM/yyyy"),
        KyBaoCao.Thang => moc.ToString("'Tháng' MM/yyyy"),
        _ => $"Năm {moc.Year}",
    };

    public static DateTime LuiKy(KyBaoCao ky, DateTime moc, int soKy) => ky switch
    {
        KyBaoCao.Ngay => moc.AddDays(soKy),
        KyBaoCao.Thang => moc.AddMonths(soKy),
        _ => moc.AddYears(soKy),
    };

    public static List<DonHang> TrongKy(IEnumerable<DonHang> ds, KyBaoCao ky, DateTime moc)
    {
        var (tu, den) = Khoang(ky, moc);
        return ds.Where(d => d.NgayDat >= tu && d.NgayDat < den).ToList();
    }

    public static decimal TyLeChietKhau(GianHang gh) => gh.TyLeChietKhau ?? TyLeChietKhauMacDinh;

    /// <summary>Phần giảm giá do chính quán tài trợ (mã quán tự tạo). Mã toàn hệ thống do nền tảng chịu nên không trừ vào quán.</summary>
    public static decimal GiamGiaQuanChiu(DonHang d, string gianHangId)
    {
        if (d.GiamGia <= 0 || string.IsNullOrWhiteSpace(d.MaKhuyenMai)) return 0;
        var km = MockData.DanhSachKhuyenMai.FirstOrDefault(k => string.Equals(k.MaCode, d.MaKhuyenMai, StringComparison.OrdinalIgnoreCase));
        return km is not null && !km.ApDungToanHeThong && km.GianHangId == gianHangId ? d.GiamGia : 0;
    }

    /// <summary>
    /// Đối soát các đơn HOÀN THÀNH: phí nền tảng = tỉ lệ chiết khấu × (tiền món − giảm giá quán tài trợ);
    /// thực nhận = tiền món − giảm giá quán tài trợ − phí nền tảng; lợi nhuận = thực nhận − giá vốn món.
    /// Phí giao hàng đi thẳng cho tài xế nên không nằm trong doanh thu của quán.
    /// </summary>
    public static KetQuaTaiChinh TinhTaiChinh(GianHang gh, IEnumerable<DonHang> donHoanThanh)
    {
        var kq = new KetQuaTaiChinh();
        var tyLe = TyLeChietKhau(gh);
        foreach (var d in donHoanThanh.Where(x => x.TrangThai == TrangThaiDonHang.HoanThanh).OrderByDescending(x => x.NgayDat))
        {
            var mon = d.TongTien;
            var giam = Math.Min(GiamGiaQuanChiu(d, gh.Id), mon);
            var phi = Math.Round((mon - giam) * tyLe, 0, MidpointRounding.AwayFromZero);

            decimal giaVon = 0;
            var thieu = false;
            foreach (var ct in d.ChiTiet)
            {
                var gv = MockData.DanhSachMonAn.FirstOrDefault(m => m.Id == ct.MonAn.Id)?.GiaVon ?? ct.MonAn.GiaVon;
                if (gv <= 0) thieu = true;
                giaVon += gv * ct.SoLuong;
            }

            kq.SoDon++;
            kq.DoanhThuMon += mon;
            kq.GiamGiaQuanChiu += giam;
            kq.PhiNenTang += phi;
            kq.TongGiaVon += giaVon;
            if (thieu) kq.SoDonThieuGiaVon++;
            kq.ChiTiet.Add(new DongDonTaiChinh { DonHangId = d.Id, Ngay = d.NgayDat, DoanhThuMon = mon, GiamGiaQuanChiu = giam, PhiNenTang = phi });
        }
        return kq;
    }
}
