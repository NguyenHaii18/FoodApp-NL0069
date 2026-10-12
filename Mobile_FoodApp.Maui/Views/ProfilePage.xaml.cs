using FoodApp.Maui.Models;
using FoodApp.Maui.Services;

namespace FoodApp.Maui.Views;

public partial class ProfilePage : ContentPage
{
    public ProfilePage()
    {
        InitializeComponent();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        NapHoSo();
        VeUuDai();
        ToMauThanhToan();
        _ = DongBoXacThucAsync();
    }

    // Online: lấy trạng thái xác thực email thật từ Web.
    private async Task DongBoXacThucAsync()
    {
        if (!DuLieuService.DangOnline || Phien.Khach is not { } k) return;
        var r = await ApiClient.GetAsync<OtpService.TrangThaiXacThucDto>("api/mobile/tai-khoan/trang-thai-xac-thuc");
        if (!r.ThanhCong || r.DuLieu is null) return;
        k.EmailDaXacThuc = r.DuLieu.EmailDaXacThuc;
        NapHoSo();
    }

    private void NapHoSo()
    {
        var k = Phien.Khach;
        var ten = string.IsNullOrWhiteSpace(k?.HoTen) ? "Khách hàng" : k!.HoTen;
        LblHoTen.Text = ten;
        LblEmail.Text = k?.Email ?? "";
        LblChuCai.Text = ten.Trim().Length > 0 ? ten.Trim()[..1].ToUpperInvariant() : "?";
        ImgAnh.Source = k is { CoAnh: true } ? ImageSource.FromFile(k.AnhPath) : null;
        ImgAnh.IsVisible = k?.CoAnh == true;
        LblChuCai.IsVisible = k?.CoAnh != true;
        LblXacThuc.Text = k is null ? "" : k.EmailDaXacThuc ? "✔ Email đã xác thực" : "⚠ Email chưa xác thực — chạm để xác thực";
        EntryEmail.Text = k?.Email ?? "";
        EntryEmail.IsEnabled = !DuLieuService.DangOnline;   // đổi email khi online thực hiện trên website
        EntryHoTen.Text = k?.HoTen ?? "";
        EntrySdt.Text = k?.SoDienThoai ?? "";
        EntryDiaChi.Text = k?.DiaChi ?? "";

        var don = MockData.LichSuDonHang.Where(OrderTrackingPage.DonCuaToi).ToList();
        var xong = don.Where(d => d.TrangThai == TrangThaiDonHang.HoanThanh).ToList();
        LblSoDon.Text = don.Count.ToString();
        LblHoanThanh.Text = xong.Count.ToString();
        var chi = xong.Sum(d => d.TongThanhToan);
        LblTongChi.Text = chi >= 1_000_000 ? $"{chi / 1_000_000:0.#}tr" : $"{chi / 1000:0.#}k";
    }

    private async void OnLuuHoSoClicked(object? sender, EventArgs e)
    {
        LblLoiHoSo.IsVisible = false;
        var k = Phien.Khach;
        if (k is null) return;
        var ten = EntryHoTen.Text?.Trim() ?? "";
        var sdt = EntrySdt.Text?.Trim() ?? "";
        var dc = EntryDiaChi.Text?.Trim() ?? "";
        var email = EntryEmail.Text?.Trim() ?? "";
        if (ten.Length == 0) { LoiHoSo("Vui lòng nhập họ tên"); return; }
        if (!System.Net.Mail.MailAddress.TryCreate(email, out var mm) || mm.Address != email || !email.Contains('.')) { LoiHoSo("Email không hợp lệ"); return; }
        bool doiEmail = !string.Equals(email, k.Email, StringComparison.OrdinalIgnoreCase);
        if (doiEmail && MockData.DanhSachKhachHang.Any(x => !ReferenceEquals(x, k) && string.Equals(x.Email, email, StringComparison.OrdinalIgnoreCase)))
        { LoiHoSo("Email này đã được tài khoản khác sử dụng"); return; }
        if (sdt.Length > 0 && (sdt.Length < 9 || sdt.Length > 11 || !sdt.All(char.IsDigit))) { LoiHoSo("Số điện thoại phải gồm 9–11 chữ số"); return; }
        if (dc.Length == 0) { LoiHoSo("Vui lòng nhập địa chỉ giao hàng"); return; }

        BtnLuuHoSo.IsEnabled = false;
        try
        {
            // Đổi email phải nhập đúng mã OTP gửi tới email MỚI thì mới có hiệu lực.
            if (doiEmail && !await XacThucOtpPage.CanXacThucAsync(OtpService.DoiEmail, email, "Xác thực email mới"))
            { LoiHoSo("Email mới chưa được xác thực nên chưa được thay đổi"); return; }

            if (!await DuLieuService.GhiAsync("PUT", "api/mobile/khach-hang/ho-so", new { hoTen = ten, sdt, diaChi = dc, email })) return;
            if (doiEmail)
            {
                // Đơn hàng nhận diện khách theo email: chuyển sang email mới để không mất lịch sử đơn.
                var cu = k.Email;
                foreach (var d in MockData.LichSuDonHang.Concat(MockData.DonHangCuaGianHang).Concat(MockData.DonHangChoTaiXe).Concat(MockData.DonHangDaGiaoCuaTaiXe)
                             .Where(d => string.Equals(d.KhachEmail, cu, StringComparison.OrdinalIgnoreCase)))
                    d.KhachEmail = email;
                NhatKyService.Ghi("KhachHang", email, "Đổi email", chiTiet: $"{cu} → {email}");
                k.Email = email;
                k.EmailDaXacThuc = true;
            }
            k.HoTen = ten; k.SoDienThoai = sdt; k.DiaChi = dc;
            var so = TaiKhoanService.LayDiaChi();   // giữ sổ địa chỉ khớp với địa chỉ mặc định vừa sửa
            var mac = so.FirstOrDefault(d => d.MacDinh);
            if (mac is null) so.Add(new DiaChiLuu { Nhan = "Nhà", DiaChi = dc, MacDinh = true }); else mac.DiaChi = dc;
            TaiKhoanService.LuuDiaChi(so);
            VendorLocalStore.Luu();
            NapHoSo();
            await DisplayAlert("Đã lưu", "Thông tin tài khoản đã được cập nhật.", "OK");
        }
        finally { BtnLuuHoSo.IsEnabled = true; }
    }

    // ---------- Ảnh đại diện ----------
    private async void OnDoiAnhTapped(object? sender, EventArgs e)
    {
        var k = Phien.Khach;
        if (k is null) return;

        var chon = k.CoAnh
            ? await DisplayActionSheet("Ảnh đại diện", "Đóng", null, "Chọn từ thư viện", "Chụp ảnh", "Xóa ảnh")
            : await DisplayActionSheet("Ảnh đại diện", "Đóng", null, "Chọn từ thư viện", "Chụp ảnh");
        if (chon is null or "Đóng") return;

        try
        {
            if (chon == "Xóa ảnh")
            {
                try { File.Delete(k.AnhPath); } catch { }
                k.AnhPath = "";
            }
            else
            {
                var path = await AnhService.ChonVaLuuAsync("kh_" + k.Id, "Chọn ảnh đại diện", chup: chon == "Chụp ảnh");
                if (path is null) return;
                if (k.CoAnh) { try { File.Delete(k.AnhPath); } catch { } }
                k.AnhPath = path;
            }
            VendorLocalStore.Luu();
            NapHoSo();
        }
        catch (PermissionException) { await DisplayAlert("Thiếu quyền", "Ứng dụng chưa được cấp quyền camera/ảnh. Hãy cấp quyền trong Cài đặt rồi thử lại.", "OK"); }
        catch (Exception ex) { await DisplayAlert("Không đặt được ảnh", ex.Message, "OK"); }
    }

    // ---------- Xác thực email bằng OTP ----------
    private async void OnXacThucEmailTapped(object? sender, EventArgs e)
    {
        var k = Phien.Khach;
        if (k is null || k.EmailDaXacThuc) return;
        if (await XacThucOtpPage.CanXacThucAsync(OtpService.XacThucEmail, k.Email, "Xác thực email"))
        {
            k.EmailDaXacThuc = true;
            VendorLocalStore.Luu();
            NhatKyService.Ghi("KhachHang", k.Email, "Xác thực email");
            NapHoSo();
        }
    }

    private async void OnLichSuDangNhapTapped(object? sender, EventArgs e) => await Shell.Current.GoToAsync(nameof(LichSuDangNhapPage));

    private void LoiHoSo(string tb) { LblLoiHoSo.Text = tb; LblLoiHoSo.IsVisible = true; }

    private void OnThanhToanClicked(object? sender, EventArgs e)
    {
        if (sender is Button { CommandParameter: string ma })
        {
            Preferences.Default.Set("tt-mac-dinh", ma);
            ToMauThanhToan();
        }
    }

    private void ToMauThanhToan()
    {
        var chinh = (Color)Application.Current!.Resources["Primary"];
        var nhat = (Color)Application.Current.Resources["PrimarySoft"];
        var hien = Preferences.Default.Get("tt-mac-dinh", "TienMat");
        foreach (var (nut, ma) in new[] { (BtnTienMat, "TienMat"), (BtnChuyenKhoan, "ChuyenKhoan"), (BtnViDienTu, "ViDienTu") })
        {
            var chon = hien == ma;
            nut.BackgroundColor = chon ? chinh : nhat;
            nut.TextColor = chon ? Colors.White : chinh;
        }
    }

    /// <summary>Các mã khuyến mãi đang dùng được; chạm vào thẻ để sao chép mã.</summary>
    private void VeUuDai()
    {
        KhungUuDai.Children.Clear();
        var ds = MockData.DanhSachKhuyenMai
            .Where(k => k.DangHoatDong && k.ConHan && !k.ChuaBatDau && k.ConLuot)
            .OrderByDescending(k => k.ApDungToanHeThong).ToList();
        LblKhongUuDai.IsVisible = ds.Count == 0;
        foreach (var km in ds)
        {
            var noiDung = new VerticalStackLayout { Spacing = 2 };
            noiDung.Children.Add(new Label { Text = km.MaCode, FontAttributes = FontAttributes.Bold, FontSize = 15, TextColor = (Color)Application.Current!.Resources["Primary"] });
            noiDung.Children.Add(new Label { Text = km.TenChuongTrinh, FontSize = 12.5 });
            noiDung.Children.Add(new Label
            {
                Text = $"{km.GiaTriGiamHienThi} · {km.PhamViHienThi} · HSD {km.NgayKetThuc:dd/MM/yyyy}",
                FontSize = 11.5, TextColor = (Color)Application.Current.Resources["TextGrey"],
            });
            var the = new Frame { Style = (Style)Application.Current.Resources["CardFrame"], Content = noiDung };
            the.GestureRecognizers.Add(new TapGestureRecognizer
            {
                Command = new Command(async () =>
                {
                    await Clipboard.Default.SetTextAsync(km.MaCode);
                    await DisplayAlert("Đã sao chép", $"Mã {km.MaCode} đã được sao chép — dán vào ô khuyến mãi khi thanh toán.", "OK");
                })
            });
            KhungUuDai.Children.Add(the);
        }
    }

    private async void OnCaiDatTapped(object? s, TappedEventArgs e) => await Shell.Current.GoToAsync(nameof(CaiDatPage));

    private async void OnSoDiaChiTapped(object? s, TappedEventArgs e) => await Shell.Current.GoToAsync(nameof(DiaChiPage));
    private async void OnYeuThichTapped(object? s, TappedEventArgs e) => await Shell.Current.GoToAsync(nameof(YeuThichPage));
    private async void OnTinMoiTapped(object? s, TappedEventArgs e) => await Shell.Current.GoToAsync(nameof(TinTucPage));

    private async void OnGioiThieuTapped(object? s, TappedEventArgs e)
    {
        var ma = TaiKhoanService.MaGioiThieu;
        var noiDung = $"Dùng mã {ma} khi đăng ký ứng dụng đặt món để cùng nhận ưu đãi nhé!";
        var chon = await DisplayActionSheet($"Mã giới thiệu của bạn: {ma}", "Đóng", null, "Sao chép mã", "Chia sẻ");
        if (chon == "Sao chép mã") { await Clipboard.Default.SetTextAsync(ma); await DisplayAlert("Đã sao chép", ma, "OK"); }
        else if (chon == "Chia sẻ") await Share.Default.RequestAsync(new ShareTextRequest { Text = noiDung, Title = "Giới thiệu bạn bè" });
    }

    private async void OnPhanHoiTapped(object? s, TappedEventArgs e)
    {
        var loai = await DisplayActionSheet("Bạn cần hỗ trợ về?", "Đóng", null, "Đơn hàng / thanh toán", "Lỗi ứng dụng", "Góp ý cải thiện", "Khác");
        if (loai is null or "Đóng") return;
        var nd = await DisplayPromptAsync("Gửi phản hồi", $"Mô tả ({loai}):", "Gửi", "Hủy", maxLength: 500, keyboard: Keyboard.Text);
        if (string.IsNullOrWhiteSpace(nd)) return;

        var vd = new VanDeHeThong
        {
            Id = "vd" + DateTime.Now.ToString("yyMMddHHmmss"),
            TieuDe = $"Phản hồi: {loai}",
            MoTa = nd.Trim(),
            NguonGoc = string.IsNullOrWhiteSpace(Phien.Khach?.HoTen) ? "Khách hàng" : Phien.Khach!.HoTen,
            NgayBaoCao = DateTime.Now,
            TrangThai = TrangThaiVanDe.MoiPhatSinh,
        };
        if (!await DuLieuService.GhiAsync("POST", "api/mobile/van-de", new { tieuDe = vd.TieuDe, moTa = vd.MoTa })) return;
        MockData.DanhSachVanDe.Insert(0, vd);
        VendorLocalStore.Luu();
        await DisplayAlert("Đã gửi", "Cảm ơn bạn! Phản hồi đã được gửi tới quản trị viên.", "OK");
    }

    private async void OnVeUngDungTapped(object? s, TappedEventArgs e)
        => await DisplayAlert("Dịch Vụ Ăn Uống", $"Phiên bản {AppInfo.Current.VersionString}\nỨng dụng đặt món và quản lý dịch vụ ăn uống.\nĐồ án CNTT — HUIT.", "Đóng");

    private async void OnXoaTaiKhoanTapped(object? s, TappedEventArgs e)
    {
        var k = Phien.Khach;
        if (k is null) return;
        if (!await DisplayAlert("Xóa tài khoản", "Tài khoản và dữ liệu cá nhân sẽ bị xóa vĩnh viễn và không thể khôi phục. Bạn chắc chắn?", "Xóa", "Hủy")) return;
        var mk = await DisplayPromptAsync("Xác nhận", "Nhập mật khẩu để xóa tài khoản:", "Xóa", "Hủy", keyboard: Keyboard.Text);
        if (mk is null) return;
        if (!DuLieuService.DangOnline && mk != k.MatKhau) { await DisplayAlert("Sai mật khẩu", "Mật khẩu không đúng.", "OK"); return; }

        if (DuLieuService.DangOnline)
        {
            // Web vô hiệu hóa + ẩn danh tài khoản (chặn nếu còn đơn đang xử lý) và kiểm mật khẩu ở phía máy chủ.
            var kq = await ApiClient.PostAsync<object>("api/mobile/tai-khoan/xoa", new { matKhau = mk });
            if (!kq.ThanhCong) { await DisplayAlert("Không xóa được", kq.ThongDiep, "OK"); return; }
        }
        NhatKyService.Ghi("KhachHang", k.Email, "Xóa tài khoản", chiTiet: NhatKyService.ThietBi());
        if (k.CoAnh) { try { File.Delete(k.AnhPath); } catch { } }
        MockData.DanhSachKhachHang.Remove(k);
        TaiKhoanXoaStore.DanhDau(k.Email);     // dữ liệu mẫu không làm tài khoản này "sống lại"
        VendorLocalStore.Luu();
        Phien.DangXuat();
        await Shell.Current.GoToAsync("//dang-nhap");
    }

    private async void OnDoiMatKhauTapped(object sender, EventArgs e)
        => await Shell.Current.GoToAsync($"{nameof(DoiMatKhauPage)}?VaiTro=KhachHang");

    // Bug cũ: nút "Đăng xuất" không có sự kiện nào nên bấm không có tác dụng.
    private async void OnDangXuatTapped(object sender, EventArgs e)
    {
        if (!await DisplayAlert("Đăng xuất", "Bạn có chắc muốn đăng xuất?", "Đăng xuất", "Hủy")) return;
        if (Phien.Khach is { } kh) NhatKyService.Ghi("KhachHang", kh.Email, "Đăng xuất", chiTiet: NhatKyService.ThietBi());
        Phien.DangXuat();
        await Shell.Current.GoToAsync("//dang-nhap");
    }
}
