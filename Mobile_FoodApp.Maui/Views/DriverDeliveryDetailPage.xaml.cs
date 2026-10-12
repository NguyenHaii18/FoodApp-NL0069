using System.Globalization;
using FoodApp.Maui.Models;
using FoodApp.Maui.Services;

namespace FoodApp.Maui.Views;

public partial class DriverDeliveryDetailPage : ContentPage
{
    private DonHang? _don;

    public DriverDeliveryDetailPage()
    {
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        _don = MockData.DonHangDangGiaoCuaTaiXe;

        if (_don is null)
        {
            // Không còn đơn đang giao (đã hoàn tất) -> quay lại trang chủ tài xế.
            await Shell.Current.GoToAsync("..");
            return;
        }

        if (_don.TrangThai == TrangThaiDonHang.DaHuy && !_don.GiaoThatBai)       // khách hủy trong lúc tài xế đang thực hiện
        {
            var huy = _don;
            Dispatcher.Dispatch(async () =>
            {
                await DisplayAlert("Khách đã hủy đơn", $"Đơn #{huy.Id} đã bị hủy. Nếu bạn đã lấy hàng, hãy liên hệ hỗ trợ.", "OK");
                MockData.DonHangDangGiaoCuaTaiXe = null;
                MockData.DonHangChoTaiXe.Remove(huy);
                await Shell.Current.GoToAsync("..");
            });
            return;
        }

        GiaoHangService.BoSungTuyenNeuThieu(_don);

        LblMaDon.Text = $"Đơn #{_don.Id}";
        LblGianHang.Text = _don.TenGianHang;
        LblDiaChiLay.Text = _don.DiaChiLayHang;
        LblDiaChiGiao.Text = _don.DiaChiGiao;
        DsMon.ItemsSource = _don.ChiTiet;

        LblKhachTen.Text = string.IsNullOrWhiteSpace(_don.KhachTen) ? (LayKhach()?.HoTen is { Length: > 0 } t ? t : "Khách hàng") : _don.KhachTen;
        LblKhachSdt.Text = string.IsNullOrWhiteSpace(LaySdtKhach()) ? "Khách chưa cung cấp số điện thoại" : LaySdtKhach();

        HienThiTuyenVaPhi();
        CapNhatThuHo();
        CapNhatTheoTrangThai();

        // Chia sẻ vị trí cho khách theo dõi: theo lựa chọn của tài xế (mặc định bật)
        SwChiaSeViTri.Toggled -= OnChiaSeViTriToggled;
        SwChiaSeViTri.IsToggled = ViTriTaiXeService.DuocPhepChiaSe;
        SwChiaSeViTri.Toggled += OnChiaSeViTriToggled;
        ViTriTaiXeService.DaCapNhat -= HienThiTrangThaiViTri;
        ViTriTaiXeService.DaCapNhat += HienThiTrangThaiViTri;
        if (ViTriTaiXeService.DuocPhepChiaSe) await ViTriTaiXeService.BatDauAsync(_don);
        HienThiTrangThaiViTri();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        ViTriTaiXeService.DaCapNhat -= HienThiTrangThaiViTri;   // vẫn chia sẻ nền cho tới khi giao xong
    }

    private bool TienMat => _don is not null && _don.HinhThucThanhToan.Contains("mặt", StringComparison.OrdinalIgnoreCase);

    private void HienThiTuyenVaPhi()
    {
        if (_don is null) return;
        LblPhiGiao.Text = $"+{_don.PhiGiaoHang:N0}đ";
        LblKhoangCach.Text = _don.KhoangCachHienThi;
        LblThoiGian.Text = _don.ThoiGianDuKienPhut > 0 && _don.KhoangCachKm > 0 ? $"~{_don.ThoiGianDuKienPhut} phút" : "—";
        LblKhuVuc.Text = string.IsNullOrEmpty(_don.KhuVucGiao) ? "Chưa rõ"
            : (string.IsNullOrEmpty(_don.KhuVucLay) || _don.KhuVucLay == _don.KhuVucGiao ? _don.KhuVucGiao : $"{_don.KhuVucLay} → {_don.KhuVucGiao}");
        LblGhiChuUocTinh.Text = _don.PhiDaDieuChinh
            ? $"✏ Đã điều chỉnh theo khoảng cách thực tế {_don.KhoangCachThucTeKm:0.#} km."
            : _don.KhoangCachKm > 0
                ? "Ước tính theo khu vực trong địa chỉ, có thể lệch so với đường đi thực tế — dùng \"Chỉ đường\" để xem lộ trình."
                : "Chưa nhận ra khu vực từ địa chỉ nên chưa tính được khoảng cách. Hãy báo khoảng cách thực tế nếu xa hơn.";
        LblNhanPhiKm.Text = _don.KhoangCachKm > 0 ? $"Theo khoảng cách ({_don.KhoangCachHienThi})" : "Phí cơ bản";
        LblPhiKm.Text = $"{_don.PhiKhoangCach:N0}đ";
        RowPhuPhi.IsVisible = _don.PhuPhiKhuVuc > 0;
        LblNhanPhuPhi.Text = $"Phụ phí khu vực ({(string.IsNullOrEmpty(_don.KhuVucGiao) ? "ngoại thành" : _don.KhuVucGiao)})";
        LblPhuPhi.Text = $"+{_don.PhuPhiKhuVuc:N0}đ";
        LblPhiTong.Text = $"{_don.PhiGiaoHang:N0}đ";
        LblGhiChuPhi.IsVisible = true;
        LblGhiChuPhi.Text = $"{GiaoHangService.PhiCoBan:N0}đ cho {GiaoHangService.KmMienPhi:0} km đầu, +{GiaoHangService.PhiMoiKmThem:N0}đ mỗi km tiếp theo; ngoại thành +{GiaoHangService.PhuPhiNgoaiThanh:N0}đ; tối đa {GiaoHangService.PhiToiDa:N0}đ.";
    }

    private void CapNhatThuHo()
    {
        if (_don is null) return;
        LblIconThuHo.Text = TienMat ? "💵" : "✅";
        LblThuHoTieuDe.Text = TienMat ? "Thu tiền mặt từ khách" : $"Khách đã thanh toán ({_don.HinhThucThanhToan})";
        LblThuHoSoTien.Text = TienMat ? $"{_don.TongThanhToan:N0}đ" : "Không cần thu tiền";
    }

    private void HienThiTrangThaiViTri() => MainThread.BeginInvokeOnMainThread(() =>
        LblTrangThaiViTri.Text = ViTriTaiXeService.DuocPhepChiaSe ? ViTriTaiXeService.TrangThai : "Đang tắt — khách không thấy vị trí của bạn");

    private async void OnChiaSeViTriToggled(object? sender, ToggledEventArgs e)
    {
        ViTriTaiXeService.DuocPhepChiaSe = e.Value;
        if (!e.Value) { ViTriTaiXeService.Dung("Đang tắt — khách không thấy vị trí của bạn"); return; }
        if (_don is not null) await ViTriTaiXeService.BatDauAsync(_don);
        HienThiTrangThaiViTri();
    }

    private async void OnGoiQuanClicked(object? sender, EventArgs e)
    {
        if (_don is null) return;
        await GiaoHangService.GoiDienAsync(this, GiaoHangService.SdtQuan(_don), "Quán");
    }

    private async void OnDieuChinhPhiClicked(object? sender, EventArgs e)
    {
        if (_don is null) return;
        var nhap = await DisplayPromptAsync("Khoảng cách thực tế",
            $"Nhập số km thực tế quán → khách (hiện ước tính: {_don.KhoangCachHienThi}).\nPhí được tính lại theo biểu phí khoảng cách và khu vực.",
            "Tính lại", "Hủy", "VD: 4.5", 6, Keyboard.Numeric);
        if (string.IsNullOrWhiteSpace(nhap)) return;
        if (!double.TryParse(nhap.Trim().Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out var km) || km < 0.3 || km > 60)
        {
            await DisplayAlert("Số km không hợp lệ", "Nhập khoảng cách từ 0,3 đến 60 km.", "Đóng");
            return;
        }
        km = Math.Round(km, 1);
        var (phiKm, phuPhi, tong) = GiaoHangService.TinhPhi(km, _don.NgoaiThanh);
        if (tong == _don.PhiGiaoHang) { await DisplayAlert("Phí không đổi", $"Với {km:0.#} km, phí vẫn là {tong:N0}đ.", "OK"); return; }
        if (!TienMat && tong > _don.PhiGiaoHang)
        {
            await DisplayAlert("Không thể tăng phí",
                $"Đơn đã thanh toán trước ({_don.HinhThucThanhToan}) nên không thu thêm của khách được.\nPhí tính lại: {tong:N0}đ (hiện {_don.PhiGiaoHang:N0}đ). Nếu cần hỗ trợ, báo cho quản trị viên.", "Đóng");
            return;
        }
        var ghiChu = TienMat && tong > _don.PhiGiaoHang ? $"\nKhách sẽ trả thêm {tong - _don.PhiGiaoHang:N0}đ khi nhận hàng."
            : (tong < _don.PhiGiaoHang ? $"\nKhách được giảm {_don.PhiGiaoHang - tong:N0}đ." : "");
        if (!await DisplayAlert("Điều chỉnh phí giao hàng",
                $"Khoảng cách {km:0.#} km → phí giao {tong:N0}đ (đang là {_don.PhiGiaoHang:N0}đ).{ghiChu}\nCập nhật?", "Cập nhật", "Hủy")) return;
        if (!await DuLieuService.GhiAsync("PUT", $"api/mobile/don-hang/{_don.Id}/phi-giao-hang", new { khoangCachKm = km, phiGiaoHang = tong })) return;
        var phut = GiaoHangService.UocTinhPhut(km);
        GiaoHangService.CapNhatMoiBanSao(_don.Id, d =>
        {
            d.KhoangCachKm = km; d.KhoangCachThucTeKm = km; d.ThoiGianDuKienPhut = phut;
            d.PhiKhoangCach = phiKm; d.PhuPhiKhuVuc = phuPhi; d.PhiGiaoHang = tong; d.PhiDaDieuChinh = true;
        });
        VendorLocalStore.Luu();
        HienThiTuyenVaPhi();
        CapNhatThuHo();
    }

    private static Color Res(string k) => (Color)Application.Current!.Resources[k];
    private static Color MauCho => Color.FromArgb("#66FFFFFF");

    private void TonBuoc(Frame dot, Label lbl, bool xong, bool hienTai)
    {
        dot.BackgroundColor = xong ? Colors.White : (hienTai ? Color.FromArgb("#33FFFFFF") : Color.FromArgb("#1AFFFFFF"));
        dot.BorderColor = hienTai || xong ? Colors.White : Color.FromArgb("#55FFFFFF");
        lbl.TextColor = xong ? Res("Primary") : Colors.White;
        if (xong) lbl.Text = "✓";
    }

    private void CapNhatBuoc(int buoc)
    {
        // buoc 1 = đang đến quán lấy hàng, 2 = đang giao
        TonBuoc(Dot1, LblDot1, true, false);
        TonBuoc(Dot2, LblDot2, buoc >= 2, buoc == 1);
        TonBuoc(Dot3, LblDot3, false, buoc == 2);
        Line1.Color = buoc >= 1 ? Colors.White : MauCho;
        Line2.Color = buoc >= 2 ? Colors.White : MauCho;
        if (buoc < 2) LblDot2.Text = "2";
    }

    private async void OnSaoChepLayTapped(object? sender, EventArgs e)
    {
        if (_don is null) return;
        await Clipboard.Default.SetTextAsync(_don.DiaChiLayHang);
        await DisplayAlert("Đã sao chép", "Địa chỉ lấy hàng đã được sao chép.", "OK");
    }

    private async void OnSaoChepGiaoTapped(object? sender, EventArgs e)
    {
        if (_don is null) return;
        await Clipboard.Default.SetTextAsync(_don.DiaChiGiao);
        await DisplayAlert("Đã sao chép", "Địa chỉ giao hàng đã được sao chép.", "OK");
    }

    private void CapNhatTheoTrangThai()
    {
        if (_don is null) return;

        if (_don.TrangThai == TrangThaiDonHang.DangLam)
        {
            if (_don.BuocTaiXe == 0)
            {
                LblTrangThaiHienTai.Text = "📍 Đã nhận đơn — đang đến quán lấy hàng";
                BtnHanhDongChinh.Text = "Đã đến quán";
            }
            else
            {
                LblTrangThaiHienTai.Text = "🍳 Đang lấy hàng tại quán";
                BtnHanhDongChinh.Text = "Đã lấy hàng, bắt đầu giao";
            }
            CapNhatBuoc(1);
        }
        else
        {
            LblTrangThaiHienTai.Text = "🛵 Đang trên đường giao đến khách hàng";
            BtnHanhDongChinh.Text = "Đã giao thành công";
            CapNhatBuoc(2);
        }
        BtnGiaoThatBai.IsVisible = _don.TrangThai == TrangThaiDonHang.DangGiao;   // chỉ báo thất bại sau khi đã lấy hàng
    }

    private async void OnHanhDongChinhClicked(object sender, EventArgs e)
    {
        if (_don is null) return;

        if (_don.TrangThai == TrangThaiDonHang.DangLam)
        {
            if (_don.BuocTaiXe == 0)                         // đã nhận → đến quán, bắt đầu lấy hàng
            {
                _don.BuocTaiXe = 1;
                Services.DonHangService.DongBo(_don);
                await Services.DuLieuService.GhiAsync("PUT", $"api/mobile/don-hang/{_don.Id}/tai-xe-den-quan");
                CapNhatTheoTrangThai();
                return;
            }

            if (!await DisplayAlert("Xác nhận lấy hàng", $"Bạn đã nhận đủ {_don.ChiTiet.Sum(x => x.SoLuong)} món từ {_don.TenGianHang}?", "Đã nhận đủ", "Chưa"))
                return;
            _don.DaLayHang = true; _don.LayHangLuc = DateTime.Now;
            _don.TrangThai = TrangThaiDonHang.DangGiao;      // đã lấy hàng → khách thấy "Đang giao"
            Services.DonHangService.DongBo(_don);
            await Services.DuLieuService.GhiAsync("PUT", $"api/mobile/don-hang/{_don.Id}/da-lay-hang");
            CapNhatTheoTrangThai();
            return;
        }

        // Hoàn tất giao hàng: thu tiền mặt (nếu có) + mã 4 số của khách, rồi ảnh bằng chứng (có thể bỏ qua)
        if (TienMat && !await DisplayAlert("Thu tiền mặt", $"Bạn đã thu đủ {_don.TongThanhToan:N0}đ từ khách?", "Đã thu đủ", "Chưa thu"))
            return;
        // Online: server giữ mã và là nơi duy nhất kiểm tra (tài xế không nhận được mã). Offline demo: so với mã cục bộ.
        string? maNhap = null;
        if (Services.DuLieuService.DangOnline || !string.IsNullOrEmpty(_don.MaXacNhanGiao))
        {
            while (true)
            {
                var ma = await DisplayPromptAsync("Khách xác nhận đã nhận hàng",
                    "Nhập mã 4 số hiển thị trên đơn hàng của khách (Đơn hàng → đơn đang giao).", "Xác nhận", "Hủy", "____", 4, Keyboard.Numeric);
                if (ma is null) return;
                maNhap = ma.Trim();
                if (Services.DuLieuService.DangOnline || maNhap == _don.MaXacNhanGiao) break;
                await DisplayAlert("Mã không đúng", "Mã xác nhận không khớp. Nhờ khách kiểm tra lại mã trên đơn hàng.", "Nhập lại");
            }
        }

        var anh = await DisplayActionSheet("Ảnh bằng chứng giao hàng", "Hủy", null, "Chụp ảnh", "Chọn từ thư viện", "Bỏ qua");
        if (anh is null or "Hủy") return;
        if (anh != "Bỏ qua")
        {
            var path = await Services.AnhService.ChonVaLuuAsync("giao_" + _don.Id, "Ảnh giao hàng", chup: anh == "Chụp ảnh");
            if (path is null) { await DisplayAlert("Chưa có ảnh", "Bạn chưa chọn ảnh. Chọn lại hoặc bấm \"Bỏ qua\".", "OK"); return; }
            _don.AnhGiaoPath = path;
        }

        // Gọi server TRƯỚC khi đổi trạng thái cục bộ: sai mã / đơn bị khóa thì dừng lại (GhiAsync tự báo lỗi).
        var viTri = Services.ViTriTaiXeService.ViTriCuoi;
        if (!await Services.DuLieuService.GhiAsync("PUT", $"api/mobile/don-hang/{_don.Id}/hoan-thanh",
                new { maXacNhan = maNhap, viDo = viTri?.Lat, kinhDo = viTri?.Lng, phuCap = Services.ThuNhapService.PhuCapDon(_don) })) return;

        _don.TrangThai = TrangThaiDonHang.HoanThanh;
        _don.GiaoLuc = DateTime.Now;
        _don.PhuCap = Services.ThuNhapService.PhuCapDon(_don);

        MockData.DonHangChoTaiXe.Remove(_don);
        MockData.DonHangDaGiaoCuaTaiXe.Add(_don);
        MockData.DonHangDangGiaoCuaTaiXe = null;

        // Đồng bộ lại cho khách và quán: đơn đã giao xong.
        foreach (var d in MockData.LichSuDonHang.Where(x => x.Id == _don.Id)) d.TrangThai = TrangThaiDonHang.HoanThanh;
        foreach (var d in MockData.DonHangCuaGianHang.Where(x => x.Id == _don.Id)) d.TrangThai = TrangThaiDonHang.HoanThanh;
        Services.DonHangService.DongBo(_don);
        Services.ThuNhapService.CapNhatChiSo();            // thu nhập hôm nay tính lại từ lịch sử (gồm phụ cấp + thưởng)
        Services.TaiXeStore.Luu();
        ViTriTaiXeService.Dung();

        await DisplayAlert("Hoàn tất", $"Đã giao đơn #{_don.Id} thành công. Bạn nhận được {_don.PhiGiaoHang + _don.PhuCap:N0}đ" +
                                       (_don.PhuCap > 0 ? $" (gồm phụ cấp đêm {_don.PhuCap:N0}đ)." : "."), "OK");

        await Shell.Current.GoToAsync("..");
    }

    private static readonly (string Nhan, bool LoiKhach)[] DsLyDoThatBai =
    {
        ("Không liên lạc được với khách", true),
        ("Khách từ chối nhận hàng", true),
        ("Khách hẹn lại / không có nhà", true),
        ("Sai hoặc không tìm thấy địa chỉ", true),
        ("Sự cố xe / tai nạn trên đường", false),
        ("Món bị đổ / hư hỏng", false),
        ("Lý do khác", false),
    };

    private async void OnThatBaiClicked(object sender, EventArgs e)
    {
        if (_don is null) return;
        var chon = await DisplayActionSheet("Lý do giao không thành công", "Đóng", null, DsLyDoThatBai.Select(x => x.Nhan).ToArray());
        if (chon is null or "Đóng") return;
        var (nhan, loiKhach) = DsLyDoThatBai.First(x => x.Nhan == chon);
        if (nhan.StartsWith("Không liên lạc") && _don.SoLanGoiKhach == 0)
        {
            await DisplayAlert("Hãy gọi khách trước", "Bạn cần gọi khách ít nhất 1 lần trước khi báo không liên lạc được.", "Đóng");
            return;
        }
        var ly = nhan;
        if (nhan == "Lý do khác")
        {
            var tuNhap = await DisplayPromptAsync("Lý do khác", "Mô tả ngắn lý do giao không thành công.", "Gửi", "Hủy", "VD: khu vực bị ngập", 120);
            if (string.IsNullOrWhiteSpace(tuNhap)) return;
            ly = tuNhap.Trim();
        }
        var phi = loiKhach ? $"Bạn vẫn nhận phí giao {_don.PhiGiaoHang:N0}đ (lỗi do khách)." : "Bạn sẽ không nhận phí giao cho đơn này.";
        var tienTxt = TienMat ? "Đơn thanh toán tiền mặt: không thu tiền của khách." : "Đơn đã thanh toán trước: khách sẽ được hoàn tiền.";
        if (!await DisplayAlert("Xác nhận giao thất bại", $"Lý do: {ly}\n{phi}\n{tienTxt}\nĐơn sẽ bị hủy. Hãy mang món về trả quán hoặc liên hệ quán để xử lý.", "Xác nhận", "Hủy"))
            return;
        // Server quyết định phí/hoàn tiền theo lý do và tạo yêu cầu hoàn tiền; dừng nếu server từ chối.
        var viTriTb = Services.ViTriTaiXeService.ViTriCuoi;
        if (!await Services.DuLieuService.GhiAsync("PUT", $"api/mobile/don-hang/{_don.Id}/giao-that-bai", new { lyDo = ly.Trim(), tinhPhi = loiKhach, viDo = viTriTb?.Lat, kinhDo = viTriTb?.Lng })) return;
        _don.TinhPhiKhiThatBai = loiKhach;
        _don.GiaoThatBaiLuc = DateTime.Now;

        bool tienMat = _don.HinhThucThanhToan.Contains("mặt", StringComparison.OrdinalIgnoreCase);
        _don.LyDoThatBai = ly.Trim();
        _don.TrangThai = TrangThaiDonHang.DaHuy;
        _don.GiaoLuc = DateTime.Now;
        _don.DaHoanTien = !tienMat;
        _don.PhuCap = 0;

        MockData.DonHangChoTaiXe.Remove(_don);
        MockData.DonHangDaGiaoCuaTaiXe.Add(_don);
        MockData.DonHangDangGiaoCuaTaiXe = null;
        Services.DonHangService.DongBo(_don);
        Services.KhuyenMaiService.HoanSuDung(_don.MaKhuyenMai, _don.KhachEmail);     // trả lại lượt dùng voucher
        Services.ThuNhapService.CapNhatChiSo();
        Services.TaiXeStore.Luu();
        ViTriTaiXeService.Dung();

        await DisplayAlert("Đã ghi nhận", "Đơn đã được hủy và báo cho khách hàng." + (tienMat ? "" : " Khoản thanh toán trước sẽ được hoàn lại."), "OK");
        await Shell.Current.GoToAsync("..");
    }

    // Đơn cũ chưa lưu SĐT thì tra theo email khách đặt đơn.
    private KhachHang? LayKhach() =>
        string.IsNullOrEmpty(_don?.KhachEmail) ? null
        : MockData.DanhSachKhachHang.FirstOrDefault(k => string.Equals(k.Email, _don.KhachEmail, StringComparison.OrdinalIgnoreCase));

    private string LaySdtKhach()
    {
        if (_don is null) return "";
        return !string.IsNullOrWhiteSpace(_don.KhachSdt) ? _don.KhachSdt.Trim() : (LayKhach()?.SoDienThoai ?? "").Trim();
    }

    private async void OnGoiKhachClicked(object sender, EventArgs e)
    {
        if (_don is null) return;
        GiaoHangService.CapNhatMoiBanSao(_don.Id, d => d.SoLanGoiKhach++);   // điều kiện "không liên lạc được"
        VendorLocalStore.Luu();
        await Services.LienLac.GoiAsync(LaySdtKhach());
    }

    private async void OnNhanTinClicked(object sender, EventArgs e)
    {
        if (_don is null) return;
        await ChatPage.MoAsync(_don.Id, "TaiXe", string.IsNullOrWhiteSpace(_don.KhachTen) ? "Khách hàng" : _don.KhachTen, LaySdtKhach());
    }

    private async void OnBanDoLayTapped(object? sender, EventArgs e)
    {
        if (_don is not null) await BanDoPage.MoAsync($"{_don.DiaChiLayHang}");
    }

    private async void OnBanDoGiaoTapped(object? sender, EventArgs e)
    {
        if (_don is not null) await BanDoPage.MoAsync(_don.DiaChiGiao);
    }

    /// <summary>Mở Google Maps chỉ đường từ quán tới địa chỉ khách (đang giao) hoặc tới quán (đang đi lấy hàng).</summary>
    private async void OnChiDuongClicked(object sender, EventArgs e)
    {
        if (_don is null) return;
        var den = _don.TrangThai == TrangThaiDonHang.DangGiao ? _don.DiaChiGiao : _don.DiaChiLayHang;
        if (await GiaoHangService.MoChiDuongAsync(den)) return;
        await Clipboard.Default.SetTextAsync(den);
        await DisplayAlert("Không mở được bản đồ", "Đã sao chép địa chỉ để bạn dán vào ứng dụng bản đồ.", "OK");
    }
}
