using FoodApp.Maui.Models;
using FoodApp.Maui.Services;

namespace FoodApp.Maui.Views;

/// <summary>
/// Tab "Đơn hàng" của khách: lọc theo trạng thái, xem chi tiết từng đơn (món, phí giao, giảm giá,
/// thanh toán, địa chỉ), thanh tiến trình cho đơn đang xử lý, hủy đơn, đánh giá, đặt lại.
/// </summary>
public partial class OrderTrackingPage : ContentPage
{
    private enum Loc { TatCa, DangXuLy, HoanThanh, DaHuy }

    private Loc _loc = Loc.TatCa;
    private readonly HashSet<string> _moRong = new();   // các đơn đang mở chi tiết

    public OrderTrackingPage()
    {
        InitializeComponent();
        VeThanhLoc();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        VeDanhSach();
        await DuLieuService.TaiDonHangKhachAsync();
        VeDanhSach();
    }

    private async void OnKeoLamMoi(object? sender, EventArgs e)
    {
        await DuLieuService.TaiDonHangKhachAsync();
        VeDanhSach();
        LamMoi.IsRefreshing = false;
    }

    private static Color Mau(string khoa) => (Color)Application.Current!.Resources[khoa];

    /// <summary>Đơn của khách đang đăng nhập (đơn cũ không ghi email thì coi là của mọi người).</summary>
    public static bool DonCuaToi(DonHang d) =>
        string.IsNullOrEmpty(d.KhachEmail) || string.Equals(d.KhachEmail, Phien.Khach?.Email, StringComparison.OrdinalIgnoreCase);

    private static bool DangXuLy(DonHang d) =>
        d.TrangThai is TrangThaiDonHang.ChoXacNhan or TrangThaiDonHang.DangLam or TrangThaiDonHang.DangGiao;

    // ---------- Thanh lọc ----------
    private void VeThanhLoc()
    {
        ThanhLoc.Children.Clear();
        (Loc, string)[] ds = { (Loc.TatCa, "Tất cả"), (Loc.DangXuLy, "Đang xử lý"), (Loc.HoanThanh, "Hoàn thành"), (Loc.DaHuy, "Đã hủy") };
        foreach (var (loc, nhan) in ds)
        {
            var chon = loc == _loc;
            var nut = new Button
            {
                Text = nhan, CornerRadius = 18, HeightRequest = 36, Padding = new Thickness(16, 0), FontSize = 13,
                BackgroundColor = chon ? Mau("Primary") : Mau("PrimarySoft"),
                TextColor = chon ? Colors.White : Mau("Primary"),
            };
            nut.Clicked += (_, _) => { _loc = loc; VeThanhLoc(); VeDanhSach(); };
            ThanhLoc.Children.Add(nut);
        }
    }

    // ---------- Danh sách ----------
    private void VeDanhSach()
    {
        var tatCa = MockData.LichSuDonHang.Where(DonCuaToi).OrderByDescending(d => d.NgayDat).ToList();
        var ds = tatCa.Where(d => _loc switch
        {
            Loc.DangXuLy => DangXuLy(d),
            Loc.HoanThanh => d.TrangThai == TrangThaiDonHang.HoanThanh,
            Loc.DaHuy => d.TrangThai == TrangThaiDonHang.DaHuy,
            _ => true,
        }).ToList();

        // đơn đang xử lý luôn nằm trên cùng
        ds = ds.OrderByDescending(DangXuLy).ThenByDescending(d => d.NgayDat).ToList();

        DanhSach.Children.Clear();
        LblTrong.IsVisible = ds.Count == 0;
        DanhSach.Children.Add(LblTrong);
        foreach (var don in ds) DanhSach.Children.Add(VeThe(don));
    }

    private View VeThe(DonHang don)
    {
        var dangXuLy = DangXuLy(don);
        var mo = _moRong.Contains(don.Id) || dangXuLy;

        var (mauNen, mauChu) = don.TrangThai switch
        {
            TrangThaiDonHang.HoanThanh => (Color.FromArgb("#DCFCE7"), Color.FromArgb("#15803D")),
            TrangThaiDonHang.DaHuy => (Color.FromArgb("#FEE2E2"), Color.FromArgb("#B91C1C")),
            TrangThaiDonHang.ChoXacNhan => (Color.FromArgb("#FEF3C7"), Color.FromArgb("#B45309")),
            _ => (Mau("PrimarySoft"), Mau("Primary")),
        };

        var thanTrong = new VerticalStackLayout { Spacing = 8 };

        // Hàng đầu: tên quán + nhãn trạng thái
        var dau = new Grid { ColumnDefinitions = { new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Auto) } };
        var tieuDe = new VerticalStackLayout { Spacing = 2 };
        tieuDe.Children.Add(new Label { Text = don.TenGianHang, FontAttributes = FontAttributes.Bold, FontSize = 14.5 });
        tieuDe.Children.Add(new Label
        {
            Text = $"Đơn #{don.Id} · {don.NgayDat:dd/MM/yyyy HH:mm}",
            FontSize = 11.5, TextColor = Mau("TextGrey"),
        });
        dau.Add(tieuDe, 0);
        dau.Add(new Frame
        {
            BackgroundColor = mauNen, CornerRadius = 12, Padding = new Thickness(10, 4), HasShadow = false,
            BorderColor = Colors.Transparent, VerticalOptions = LayoutOptions.Start,
            Content = new Label { Text = don.NhanHienThi, FontSize = 11.5, FontAttributes = FontAttributes.Bold, TextColor = mauChu },
        }, 1);
        thanTrong.Children.Add(dau);

        if (don.LaDonHenGio)
            thanTrong.Children.Add(new Label { Text = don.NhanHenGio, FontSize = 12, FontAttributes = FontAttributes.Bold, TextColor = Mau("Primary") });

        if (dangXuLy) thanTrong.Children.Add(VeTienTrinh(don));
        if (dangXuLy && !string.IsNullOrEmpty(don.TaiXeId)) thanTrong.Children.Add(VeTaiXe(don));

        // Tóm tắt món
        var tomTat = string.Join(", ", don.ChiTiet.Select(c => $"{c.SoLuong}× {c.MonAn.TenMon}"));
        thanTrong.Children.Add(new Label { Text = tomTat, FontSize = 12.5, TextColor = Mau("TextGrey"), MaxLines = mo ? 0 : 2, LineBreakMode = LineBreakMode.TailTruncation });

        // Chi tiết
        if (mo)
        {
            thanTrong.Children.Add(new BoxView { HeightRequest = 1, Color = Mau("BorderColor") });
            foreach (var c in don.ChiTiet)
                thanTrong.Children.Add(Dong($"{c.SoLuong} × {c.MonAn.TenMon}", $"{c.ThanhTien:N0}đ"));
            thanTrong.Children.Add(Dong("Tạm tính", $"{don.TongTien:N0}đ", xam: true));
            thanTrong.Children.Add(Dong("Phí giao hàng", $"{don.PhiGiaoHang:N0}đ", xam: true));
            if (don.GiamGia > 0)
                thanTrong.Children.Add(Dong(string.IsNullOrEmpty(don.MaKhuyenMai) ? "Giảm giá" : $"Giảm giá ({don.MaKhuyenMai})", $"-{don.GiamGia:N0}đ", xam: true));
            thanTrong.Children.Add(Dong("Thanh toán", don.HinhThucThanhToan, xam: true));
            thanTrong.Children.Add(Dong("Giao đến", string.IsNullOrWhiteSpace(don.DiaChiGiao) ? "—" : don.DiaChiGiao, xam: true));
            if (!string.IsNullOrEmpty(don.DiaChiLayHang))
                thanTrong.Children.Add(Dong("Lấy hàng tại", don.DiaChiLayHang, xam: true));
            if (don.KhoangCachKm > 0)
                thanTrong.Children.Add(Dong("Khoảng cách", $"{don.KhoangCachHienThi}" + (don.ThoiGianDuKienPhut > 0 ? $" · ~{don.ThoiGianDuKienPhut} phút" : ""), xam: true));
            if (don.PhiDaDieuChinh)
                thanTrong.Children.Add(new Label { Text = $"✏ Phí giao đã điều chỉnh theo khoảng cách thực tế ({don.KhoangCachThucTeKm:0.#} km).", FontSize = 11.5, TextColor = Mau("TextGrey") });
            if (don.GiaoThatBai)
                thanTrong.Children.Add(new Label { Text = $"⚠ Giao thất bại: {don.LyDoThatBai}", FontSize = 12, FontAttributes = FontAttributes.Bold, TextColor = Mau("Danger") });
            if (don.DaHoanTien)
                thanTrong.Children.Add(new Label { Text = "✅ Đã hoàn tiền", FontSize = 12, TextColor = Mau("Success"), FontAttributes = FontAttributes.Bold });
        }

        // Tổng
        thanTrong.Children.Add(new BoxView { HeightRequest = 1, Color = Mau("BorderColor") });
        var tong = new Grid { ColumnDefinitions = { new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Auto) } };
        var nutXem = new Label { Text = mo && !dangXuLy ? "Thu gọn ▲" : (dangXuLy ? "" : "Chi tiết ▼"), FontSize = 12.5, TextColor = Mau("Primary"), VerticalOptions = LayoutOptions.Center };
        if (!dangXuLy)
            nutXem.GestureRecognizers.Add(new TapGestureRecognizer
            {
                Command = new Command(() => { if (!_moRong.Add(don.Id)) _moRong.Remove(don.Id); VeDanhSach(); })
            });
        tong.Add(nutXem, 0);
        tong.Add(new Label { Text = $"Tổng: {don.TongThanhToan:N0}đ", FontAttributes = FontAttributes.Bold, FontSize = 14, TextColor = Mau("Primary") }, 1);
        thanTrong.Children.Add(tong);

        // Nút hành động
        var hang = new HorizontalStackLayout { Spacing = 8 };
        if (don.TrangThai == TrangThaiDonHang.ChoXacNhan)
            hang.Children.Add(TaoNut("Hủy đơn", true, () => HuyAsync(don)));
        else if (don.TrangThai == TrangThaiDonHang.DangLam || don.TrangThai == TrangThaiDonHang.DangGiao)
            hang.Children.Add(TaoNut("Yêu cầu hủy / hoàn tiền", true, () => HuyAsync(don)));
        if (don.TrangThai == TrangThaiDonHang.HoanThanh)
            hang.Children.Add(TaoNut("⭐ Đánh giá", false, () => DanhGiaAsync(don)));
        if (!dangXuLy)
            hang.Children.Add(TaoNut("🔁 Đặt lại", false, () => DatLaiAsync(don)));
        if (hang.Children.Count > 0) thanTrong.Children.Add(hang);

        var the = new Frame
        {
            Style = (Style)Application.Current!.Resources["CardFrame"],
            Content = thanTrong,
        };
        if (!dangXuLy)
            the.GestureRecognizers.Add(new TapGestureRecognizer
            {
                Command = new Command(() => { if (!_moRong.Add(don.Id)) _moRong.Remove(don.Id); VeDanhSach(); })
            });
        return the;
    }

    private static View Dong(string trai, string phai, bool xam = false)
    {
        var g = new Grid { ColumnDefinitions = { new ColumnDefinition(GridLength.Star), new ColumnDefinition(new GridLength(2, GridUnitType.Star)) }, ColumnSpacing = 8 };
        g.Add(new Label { Text = trai, FontSize = 12.5, TextColor = xam ? Mau("TextGrey") : null }, 0);
        g.Add(new Label { Text = phai, FontSize = 12.5, HorizontalTextAlignment = TextAlignment.End }, 1);
        return g;
    }

    private static Button TaoNut(string chu, bool vien, Func<Task> hanhDong)
    {
        var nut = new Button
        {
            Text = chu, FontSize = 12.5, CornerRadius = 18, HeightRequest = 36, Padding = new Thickness(14, 0),
            BackgroundColor = vien ? Colors.Transparent : Mau("Primary"),
            TextColor = vien ? Mau("Danger") : Colors.White,
            BorderColor = vien ? Mau("Danger") : Colors.Transparent, BorderWidth = vien ? 1 : 0,
        };
        nut.Clicked += async (_, _) => await hanhDong();
        return nut;
    }

    private static int BuocTheoDoi(DonHang don) => don.TrangThai switch
    {
        TrangThaiDonHang.ChoXacNhan => 0,
        TrangThaiDonHang.DangLam => string.IsNullOrEmpty(don.TaiXeId) ? 1 : 2,
        TrangThaiDonHang.DangGiao => 3,
        TrangThaiDonHang.HoanThanh => 4,
        _ => 0,
    };

    /// <summary>5 bước: Chờ xác nhận → Đang chuẩn bị → Tài xế nhận → Đang giao → Hoàn thành.</summary>
    private static View VeTienTrinh(DonHang don)
    {
        var buoc = BuocTheoDoi(don);
        string[] nhan = { "Chờ xác nhận", "Chuẩn bị", "Tài xế nhận", "Đang giao", "Hoàn thành" };
        var g = new Grid { ColumnSpacing = 4, RowSpacing = 4, Margin = new Thickness(0, 4) };
        g.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
        g.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
        g.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
        for (int i = 0; i < nhan.Length; i++)
        {
            g.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
            g.Add(new BoxView { HeightRequest = 6, CornerRadius = 3, Color = i <= buoc ? Mau("Primary") : Mau("BorderColor") }, i, 0);
            g.Add(new Label
            {
                Text = nhan[i], FontSize = 10, HorizontalTextAlignment = TextAlignment.Center,
                TextColor = i <= buoc ? Mau("Primary") : Mau("TextGrey"),
                FontAttributes = i == buoc ? FontAttributes.Bold : FontAttributes.None,
            }, i, 1);
        }
        if (don.DangChoTaiXe)
        {
            var goiY = new Label { Text = "🍳 Món đã xong, đang chờ tài xế nhận đơn", FontSize = 12, TextColor = Mau("Primary"), HorizontalTextAlignment = TextAlignment.Center };
            g.Add(goiY, 0, 2);
            Grid.SetColumnSpan(goiY, nhan.Length);
        }
        return g;
    }

    /// <summary>Thẻ tài xế đã nhận đơn: nhắn tin và gọi điện.</summary>
    private static View VeTaiXe(DonHang don)
    {
        var ten = string.IsNullOrWhiteSpace(don.TaiXeTen) ? "Tài xế" : don.TaiXeTen;
        var chu = new VerticalStackLayout { Spacing = 0, VerticalOptions = LayoutOptions.Center };
        chu.Children.Add(new Label { Text = $"🛵 {ten}", FontAttributes = FontAttributes.Bold, FontSize = 13.5, LineBreakMode = LineBreakMode.TailTruncation });
        chu.Children.Add(new Label { Text = "Tài xế giao hàng của bạn", FontSize = 11.5, TextColor = Mau("TextGrey") });

        Button Nut(string t, Func<Task> viec)
        {
            var n = new Button { Text = t, FontSize = 12.5, CornerRadius = 18, HeightRequest = 36, Padding = new Thickness(12, 0),
                                 BackgroundColor = Mau("Primary"), TextColor = Colors.White };
            n.Clicked += async (_, _) => await viec();
            return n;
        }

        var hang = new Grid { ColumnSpacing = 8, ColumnDefinitions = { new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Auto), new ColumnDefinition(GridLength.Auto) } };
        hang.Add(chu, 0);
        hang.Add(Nut("💬", () => ChatPage.MoAsync(don.Id, "KhachHang", ten, don.TaiXeSdt)), 1);
        hang.Add(Nut("📞", () => FoodApp.Maui.Services.LienLac.GoiAsync(don.TaiXeSdt)), 2);

        var st = new VerticalStackLayout { Spacing = 6 };
        st.Children.Add(hang);
        if (don.TrangThai == TrangThaiDonHang.DangGiao || don.TrangThai == TrangThaiDonHang.DangLam)
        {
            string eta = "";
            if (don.TrangThai == TrangThaiDonHang.DangGiao && GiaoHangService.ConLaiDenKhach(don) is { } cl)
                eta = $"Còn khoảng {cl.Km:0.#} km · ~{cl.Phut} phút (ước tính)\n";
            else if (don.ThoiGianDuKienPhut > 0 && don.KhoangCachKm > 0)
                eta = $"Quãng đường giao ~{don.KhoangCachHienThi} · ~{don.ThoiGianDuKienPhut} phút (ước tính)\n";
            var vt = don.ViTriCapNhatLuc is { } luc
                ? $"📍 Vị trí cập nhật {luc:HH:mm:ss}" + (DateTime.Now - luc > TimeSpan.FromMinutes(2) ? " (đã lâu chưa cập nhật)" : "")
                : "📍 Tài xế chưa chia sẻ vị trí";
            st.Children.Add(new Label { Text = eta + vt, FontSize = 11.5, TextColor = Mau("TextGrey") });
            st.Children.Add(Nut("📍 Xem vị trí tài xế", async () =>
            {
                if (don.TaiXeLat is not { } lat || don.TaiXeLng is not { } lng)
                {
                    await Application.Current!.MainPage!.DisplayAlert("Chưa có vị trí", "Tài xế chưa chia sẻ vị trí. Bạn có thể nhắn tin hoặc gọi tài xế.", "OK");
                    return;
                }
                if (!await GiaoHangService.MoViTriAsync(lat, lng))
                    await Application.Current!.MainPage!.DisplayAlert("Không mở được bản đồ", $"Tọa độ tài xế: {lat:0.0000}, {lng:0.0000}", "OK");
            }));
        }
        if (!string.IsNullOrEmpty(don.MaXacNhanGiao) && don.TrangThai == TrangThaiDonHang.DangGiao)
            st.Children.Add(new Label { Text = $"🔐 Mã nhận hàng: {don.MaXacNhanGiao}   (đọc mã này cho tài xế khi nhận món)", FontAttributes = FontAttributes.Bold, FontSize = 12.5 });

        return new Frame { BackgroundColor = Mau("PrimarySoft"), CornerRadius = 14, Padding = new Thickness(12, 8), HasShadow = false, BorderColor = Colors.Transparent, Content = st };
    }

    // ---------- Hành động ----------
    private async Task DanhGiaAsync(DonHang don) =>
        await Shell.Current.GoToAsync(nameof(ReviewPage), new Dictionary<string, object> { { "DonHang", don } });

    /// <summary>Đặt lại: mở menu quán để chọn món (quán còn hoạt động).</summary>
    private async Task DatLaiAsync(DonHang don)
    {
        var gh = MockData.DanhSachGianHang.FirstOrDefault(g => g.Id == don.GianHangId);
        if (gh is null || gh.TaiKhoanBiKhoa)
        {
            await DisplayAlert("Không thể đặt lại", "Quán này hiện không còn hoạt động.", "OK");
            return;
        }
        await Shell.Current.GoToAsync(nameof(MenuCartPage), new Dictionary<string, object> { { "GianHang", gh } });
    }

    /// <summary>
    /// Chờ xác nhận: hủy ngay và hoàn tiền tự động.
    /// Đang chuẩn bị/đang giao: gửi yêu cầu để quản trị viên xử lý (hiện ở trang Vấn đề hệ thống).
    /// </summary>
    private async Task HuyAsync(DonHang don)
    {
        if (don.TrangThai == TrangThaiDonHang.ChoXacNhan)
        {
            if (!await DisplayAlert("Hủy đơn", $"Hủy đơn #{don.Id}? Tiền sẽ được hoàn lại theo hình thức bạn đã thanh toán.", "Hủy đơn", "Giữ đơn")) return;
            if (!await DuLieuService.GhiAsync("PUT", $"api/mobile/don-hang/{don.Id}/huy")) return;
            don.TrangThai = TrangThaiDonHang.DaHuy;
            Services.KhuyenMaiService.HoanSuDung(don.MaKhuyenMai, don.KhachEmail);   // trả lại lượt dùng voucher
            don.DaHoanTien = true;
            if (string.IsNullOrWhiteSpace(don.LyDoTuChoi)) don.LyDoTuChoi = "Khách hàng hủy đơn";
            DoiTacService.GuiThongBao(don.GianHangId, ThongBaoGianHang.HuyDon, "Khách đã hủy đơn",
                $"Đơn #{don.Id} đã bị khách hàng hủy trước khi quán xác nhận.", don.Id);
            VendorLocalStore.Luu();
            VeDanhSach();
            await DisplayAlert("Đã hủy", "Đơn đã hủy và được hoàn tiền.", "OK");
            return;
        }

        if (!await DisplayAlert("Yêu cầu hủy / hoàn tiền",
                "Gian hàng đã nhận đơn. Gửi yêu cầu để quản trị viên xem xét và hoàn tiền theo quy định?", "Gửi yêu cầu", "Đóng")) return;

        var vanDe = new VanDeHeThong
        {
            Id = "vd" + DateTime.Now.ToString("yyMMddHHmmss"),
            TieuDe = $"Yêu cầu hủy/hoàn tiền đơn #{don.Id}",
            MoTa = $"Khách yêu cầu hủy đơn đang ở trạng thái \"{don.TrangThai.Nhan()}\" tại {don.TenGianHang}.",
            NguonGoc = Phien.Khach?.HoTen is { Length: > 0 } ten ? ten : "Khách hàng",
            NgayBaoCao = DateTime.Now,
            TrangThai = TrangThaiVanDe.MoiPhatSinh,
        };
        if (!await DuLieuService.GhiAsync("POST", "api/mobile/van-de",
                new { donHangId = don.Id, tieuDe = vanDe.TieuDe, moTa = vanDe.MoTa })) return;
        MockData.DanhSachVanDe.Insert(0, vanDe);
        await DisplayAlert("Đã gửi", "Yêu cầu đã được gửi tới quản trị viên.", "OK");
    }
}
