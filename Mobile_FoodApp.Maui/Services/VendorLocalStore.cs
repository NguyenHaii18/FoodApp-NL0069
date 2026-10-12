using System.Text.Json;
using FoodApp.Maui.Models;

namespace FoodApp.Maui.Services;

/// <summary>Lưu trạng thái demo của khu vực gian hàng trên thiết bị hiện tại.</summary>
public static class VendorLocalStore
{
    private const string Key = "vendor-workspace-v1";

    private sealed class Snapshot
    {
        public List<GianHang> GianHangs { get; set; } = new();
        public List<KhachHang> KhachHangs { get; set; } = new();
        public List<MonAn> MonAns { get; set; } = new();
        public Dictionary<string, List<string>> DanhMucs { get; set; } = new();
        public List<DonHang> DonHangs { get; set; } = new();
        public List<DonHang> LichSu { get; set; } = new();
        public List<DonHang> ChoTaiXe { get; set; } = new();
        public List<DonHang> TaiXeDaGiao { get; set; } = new();       // lịch sử đơn tài xế đã giao / giao thất bại
        public List<KhuyenMai> KhuyenMais { get; set; } = new();
        public List<VanDeHeThong> VanDes { get; set; } = new();
        public List<DanhGiaGianHang> DanhGias { get; set; } = new();
        public List<ThongBaoGianHang> ThongBaos { get; set; } = new();
    }

    public static void Nap()
    {
        var json = Preferences.Default.Get(Key, string.Empty);
        if (string.IsNullOrWhiteSpace(json)) return;
        try
        {
            var state = JsonSerializer.Deserialize<Snapshot>(json);
            if (state is null) return;
            Copy(state.GianHangs, MockData.DanhSachGianHang, x => x.Id, (from, to) =>
            {
                to.TenGianHang = from.TenGianHang; to.DiaChi = from.DiaChi; to.HinhAnh = from.HinhAnh; to.AnhPath = from.AnhPath;
                to.SoDienThoai = from.SoDienThoai; to.Email = from.Email; to.MoTa = from.MoTa;
                to.ThoiGianGiaoPhut = from.ThoiGianGiaoPhut; to.DangMoCua = from.DangMoCua;
                to.GioMoCua = from.GioMoCua; to.GioDongCua = from.GioDongCua; to.DangNhanDon = from.DangNhanDon;
                to.DanhGia = from.DanhGia;
                // Khóa / duyệt / mật khẩu / giấy tờ cũng phải còn nguyên sau khi tắt app
                to.TaiKhoanBiKhoa = from.TaiKhoanBiKhoa; to.TrangThaiDangKy = from.TrangThaiDangKy; to.LyDoTuChoi = from.LyDoTuChoi;
                if (!string.IsNullOrEmpty(from.MatKhau)) to.MatKhau = from.MatKhau;
                to.SoGiayPhepKinhDoanh = from.SoGiayPhepKinhDoanh; to.MaSoThue = from.MaSoThue;
                to.CccdChuQuan = from.CccdChuQuan; to.AnhGiayPhepPath = from.AnhGiayPhepPath; to.SoGiayChungNhanAttp = from.SoGiayChungNhanAttp;
            });
            Copy(state.KhachHangs, MockData.DanhSachKhachHang, x => x.Email.Trim().ToLowerInvariant(), (from, to) =>
            {
                to.HoTen = from.HoTen; to.SoDienThoai = from.SoDienThoai; to.DiaChi = from.DiaChi;
                if (!string.IsNullOrEmpty(from.MatKhau)) to.MatKhau = from.MatKhau;
                to.TaiKhoanBiKhoa = from.TaiKhoanBiKhoa; to.AnhPath = from.AnhPath; to.EmailDaXacThuc = from.EmailDaXacThuc;
            });
            // Khôi phục tài khoản MỚI đăng ký (gian hàng + khách hàng) mà MockData không có sẵn
            foreach (var gh in state.GianHangs.Where(g => MockData.DanhSachGianHang.All(x => x.Id != g.Id)))
                MockData.DanhSachGianHang.Add(gh);
            foreach (var kh in state.KhachHangs.Where(k => MockData.DanhSachKhachHang.All(x =>
                         !string.Equals(x.Email, k.Email, StringComparison.OrdinalIgnoreCase))))
                MockData.DanhSachKhachHang.Add(kh);
            MockData.DanhSachMonAn.RemoveAll(m => state.MonAns.Any(s => s.GianHangId == m.GianHangId));
            MockData.DanhSachMonAn.AddRange(state.MonAns);
            foreach (var item in state.DanhMucs) MockData.DanhMucTheoGianHang[item.Key] = item.Value;
            // Khôi phục đơn hàng. Cùng một đơn có thể nằm ở 3 danh sách (khách / gian hàng / chờ tài xế)
            // nên dùng chung MỘT đối tượng theo Id để trạng thái đổi theo nhau.
            var theoId = new Dictionary<string, DonHang>();
            DonHang Chung(DonHang d) { if (!theoId.TryGetValue(d.Id, out var x)) theoId[d.Id] = x = d; return x; }
            foreach (var d in state.DonHangs.Concat(state.LichSu).Concat(state.ChoTaiXe)) Chung(d);
            foreach (var d in state.DonHangs) if (MockData.DonHangCuaGianHang.All(x => x.Id != d.Id)) MockData.DonHangCuaGianHang.Add(Chung(d));
            foreach (var d in state.LichSu) if (MockData.LichSuDonHang.All(x => x.Id != d.Id)) MockData.LichSuDonHang.Add(Chung(d));
            foreach (var d in state.ChoTaiXe) if (MockData.DonHangChoTaiXe.All(x => x.Id != d.Id)) MockData.DonHangChoTaiXe.Add(Chung(d));
            foreach (var d in state.TaiXeDaGiao) if (MockData.DonHangDaGiaoCuaTaiXe.All(x => x.Id != d.Id)) MockData.DonHangDaGiaoCuaTaiXe.Add(Chung(d));
            foreach (var k in state.KhuyenMais) if (MockData.DanhSachKhuyenMai.All(x => x.Id != k.Id)) MockData.DanhSachKhuyenMai.Add(k);
            foreach (var v in state.VanDes) if (MockData.DanhSachVanDe.All(x => x.Id != v.Id)) MockData.DanhSachVanDe.Add(v);
            foreach (var d in state.DanhGias) if (MockData.DanhSachDanhGia.All(x => x.Id != d.Id)) MockData.DanhSachDanhGia.Add(d);
            foreach (var t in state.ThongBaos) if (MockData.DanhSachThongBao.All(x => x.Id != t.Id)) MockData.DanhSachThongBao.Add(t);
        }
        catch (JsonException) { Preferences.Default.Remove(Key); }
    }

    public static void Luu()
    {
        var state = new Snapshot
        {
            GianHangs = MockData.DanhSachGianHang,
            KhachHangs = MockData.DanhSachKhachHang.ToList(),
            MonAns = MockData.DanhSachMonAn,
            DanhMucs = MockData.DanhMucTheoGianHang,
            DonHangs = MockData.DonHangCuaGianHang.ToList(),
            LichSu = MockData.LichSuDonHang.ToList(),
            ChoTaiXe = MockData.DonHangChoTaiXe.ToList(),
            TaiXeDaGiao = MockData.DonHangDaGiaoCuaTaiXe.ToList(),
            KhuyenMais = MockData.DanhSachKhuyenMai.ToList(),
            VanDes = MockData.DanhSachVanDe.ToList(),
            DanhGias = MockData.DanhSachDanhGia.ToList(),
            ThongBaos = MockData.DanhSachThongBao.ToList()
        };
        Preferences.Default.Set(Key, JsonSerializer.Serialize(state));
    }

    private static void Copy<T>(IEnumerable<T> source, IEnumerable<T> target, Func<T, string> key, Action<T, T> copy)
    {
        var byKey = target.ToDictionary(key);
        foreach (var item in source)
            if (byKey.TryGetValue(key(item), out var existing)) copy(item, existing);
    }
}
