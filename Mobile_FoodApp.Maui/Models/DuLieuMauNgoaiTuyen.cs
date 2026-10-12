namespace FoodApp.Maui.Models;

/// <summary>
/// Dữ liệu mẫu dự phòng — CHỈ nạp khi <see cref="Services.DuLieuService.DangOnline"/> = false
/// (không gọi được API thật, ví dụ lúc demo đồ án không có mạng/chưa bật server SQL Server).
/// Nếu không có lớp này, màn hình Trang chủ sẽ TRẮNG HOÀN TOÀN khi mất kết nối vì
/// <see cref="MockData"/> giờ khởi tạo rỗng theo thiết kế (xem ghi chú trên class MockData).
/// Có 10 loại món (PhanLoai), MỖI loại có 2 gian hàng khác nhau cùng bán — để khách có
/// thể so sánh nhiều lựa chọn khi tìm theo 1 loại món, giống trải nghiệm app giao đồ ăn thật.
/// </summary>
public static class DuLieuMauNgoaiTuyen
{
    private static bool _daNap;

    public static void NapNeuChuaCo()
    {
        if (_daNap || MockData.DanhSachGianHang.Count > 0) return;
        _daNap = true;

        NapGianHangVaMonAn();
        NapTaiXe();
        NapQuanTri();
        NapKhachHang();
        NapKhuyenMai();

        MockData.GianHangHienTai = MockData.DanhSachGianHang[0];
        Services.TaiKhoanXoaStore.ApDung();   // không "sống lại" tài khoản mẫu mà khách đã xóa
    }

    private static void NapGianHangVaMonAn()
    {
        var gh = new (string Id, string Ten, string DiaChi, string Anh, string Loai, double Sao, int PhutGiao)[]
        {
            ("gh1",  "Cơm Tấm Sài Gòn",         "12 Nguyễn Trãi, Q.1",          "🍚", "com",   4.7, 20),
            ("gh11", "Cơm Tấm Bụi Sài Gòn",     "230 Nguyễn Trãi, Q.1",         "🍛", "com",   4.5, 18),
            ("gh2",  "Trà Sữa Bốn Mùa",         "45 Lê Lợi, Q.1",               "🧋", "tra",   4.6, 15),
            ("gh12", "Trà Sữa Chất Lừ",         "15 Phan Xích Long, Q.Phú Nhuận","🧋", "tra",   4.6, 18),
            ("gh3",  "Bún Bò Huế Cô Ba",        "23 Phan Xích Long, Q.Phú Nhuận","🍜", "bun",   4.6, 25),
            ("gh13", "Bún Bò Mệ Ba",            "88 Phan Đăng Lưu, Q.Phú Nhuận","🍜", "bun",   4.6, 28),
            ("gh4",  "Pizza Ý Ngon",            "78 Điện Biên Phủ, Q.Bình Thạnh","🍕", "pizza", 4.5, 30),
            ("gh14", "Pizza Ý Mama Mia",        "50 Nguyễn Văn Trỗi, Q.Phú Nhuận","🍕","pizza", 4.5, 32),
            ("gh5",  "Gà Rán Cô Út",            "5 Cách Mạng Tháng 8, Q.3",     "🍗", "ga",    4.4, 22),
            ("gh15", "Gà Rán Phố",              "77 Lê Hồng Phong, Q.10",       "🍗", "ga",    4.4, 25),
            ("gh6",  "Phở Hà Nội Gia Truyền",   "67 Điện Biên Phủ, Q.Bình Thạnh","🍲", "pho",   4.8, 22),
            ("gh16", "Phở Sáng Sài Gòn",        "19 Lý Chính Thắng, Q.3",       "🍲", "pho",   4.6, 20),
            ("gh7",  "Bánh Mì Hội An 24h",      "140 Nguyễn Thị Minh Khai, Q.3","🥖", "banh",  4.6, 12),
            ("gh17", "Bánh Mì Bà Huynh",        "200 Xô Viết Nghệ Tĩnh, Q.Bình Thạnh","🥖","banh",4.7, 10),
            ("gh8",  "Sushi Sakura",            "30 Lê Thánh Tôn, Q.1",         "🍣", "nhat",  4.7, 35),
            ("gh18", "Umi Sushi",               "12 Thái Văn Lung, Q.1",        "🍱", "nhat",  4.5, 30),
            ("gh9",  "Cà Phê Muối Sài Gòn",     "5 Pasteur, Q.1",               "☕", "cafe",  4.5, 15),
            ("gh19", "Highland Góc Phố",        "66 Đồng Khởi, Q.1",            "☕", "cafe",  4.6, 12),
            ("gh10", "Chè Dì Năm",              "92 Hồ Thị Kỷ, Q.10",           "🍧", "che",   4.4, 18),
            ("gh20", "Chè Khúc Bạch Cô Hai",    "5 Nguyễn Tri Phương, Q.5",     "🍮", "che",   4.5, 15),
        };

        var sdt = 1000000;
        foreach (var g in gh)
        {
            sdt++;
            MockData.DanhSachGianHang.Add(new GianHang
            {
                Id = g.Id, TenGianHang = g.Ten, DiaChi = g.DiaChi, HinhAnh = g.Anh, PhanLoai = g.Loai,
                DanhGia = g.Sao, ThoiGianGiaoPhut = g.PhutGiao, DangMoCua = true,
                SoDienThoai = $"09{sdt:D8}",
                Email = $"{g.Id}@gianhang.vn", MatKhau = "123456",
                TrangThaiDangKy = TrangThaiDangKyGianHang.DaDuyet,
            });
        }

        var mon = new (string Id, string Ten, decimal Gia, string Anh, string DanhMuc, string GhId)[]
        {
            ("ma1","Cơm sườn bì chả",45000,"🍛","Món chính","gh1"), ("ma2","Cơm gà nướng",42000,"🍛","Món chính","gh1"),
            ("ma40","Cơm tấm sườn trứng ốp la",38000,"🍛","Món chính","gh11"), ("ma41","Cơm tấm bì chả",32000,"🍚","Món chính","gh11"),
            ("ma3","Trà sữa trân châu đường đen",35000,"🧋","Trà sữa","gh2"), ("ma4","Hồng trà đào cam sả",32000,"🍹","Trà trái cây","gh2"),
            ("ma43","Trà sữa trân châu đường đen",36000,"🧋","Trà sữa","gh12"), ("ma44","Hồng trà kem cheese",38000,"🍵","Trà sữa","gh12"),
            ("ma5","Bún bò Huế đặc biệt",49000,"🍜","Món chính","gh3"), ("ma6","Bún bò giò heo",42000,"🍜","Món chính","gh3"),
            ("ma46","Bún bò Huế đặc biệt",58000,"🍜","Món chính","gh13"), ("ma47","Bún bò chay",42000,"🥬","Món chính","gh13"),
            ("ma7","Pizza hải sản",129000,"🍕","Pizza","gh4"), ("ma8","Pizza bò phô mai",119000,"🍕","Pizza","gh4"),
            ("ma48","Pizza bò phô mai",119000,"🍕","Pizza","gh14"), ("ma49","Pizza rau củ",99000,"🍕","Pizza","gh14"),
            ("ma9","Combo 2 miếng gà + cơm",89000,"🍗","Combo","gh5"), ("ma10","Gà rán giòn cay",79000,"🍗","Gà rán","gh5"),
            ("ma51","Gà rán giòn 3 miếng",79000,"🍗","Gà rán","gh15"), ("ma52","Combo gà + khoai + nước",89000,"🍟","Combo","gh15"),
            ("ma11","Phở đặc biệt",55000,"🍲","Phở","gh6"), ("ma12","Phở tái nạm",52000,"🍲","Phở","gh6"),
            ("ma53","Phở bò tái",52000,"🍲","Phở","gh16"), ("ma54","Phở bò viên",50000,"🍜","Phở","gh16"),
            ("ma13","Bánh mì thịt nướng",25000,"🥖","Bánh mì","gh7"), ("ma14","Bánh mì chả cá",22000,"🥪","Bánh mì","gh7"),
            ("ma56","Bánh mì pate thịt nguội",23000,"🥖","Bánh mì","gh17"), ("ma57","Bánh mì xíu mại",24000,"🥪","Bánh mì","gh17"),
            ("ma15","Sushi cá hồi",95000,"🍣","Sushi","gh8"), ("ma16","Cơm cuộn trứng",65000,"🍱","Cơm Nhật","gh8"),
            ("ma58","Cơm bento gà teriyaki",89000,"🍱","Cơm Nhật","gh18"), ("ma60","Sushi cuộn trứng",65000,"🍣","Sushi","gh18"),
            ("ma17","Cà phê muối",35000,"☕","Cà phê","gh9"), ("ma18","Bạc xỉu",29000,"🥛","Cà phê","gh9"),
            ("ma61","Cà phê đen đá",22000,"☕","Cà phê","gh19"), ("ma62","Freeze trà xanh",45000,"🥤","Đá xay","gh19"),
            ("ma19","Chè thái",25000,"🍨","Chè","gh10"), ("ma20","Chè khúc bạch",28000,"🍮","Chè","gh10"),
            ("ma63","Chè khúc bạch hạnh nhân",29000,"🍮","Chè","gh20"), ("ma64","Chè đậu xanh nước cốt dừa",22000,"🍵","Chè","gh20"),
        };

        foreach (var m in mon)
            MockData.DanhSachMonAn.Add(new MonAn
            {
                Id = m.Id, TenMon = m.Ten, Gia = m.Gia, HinhAnh = m.Anh, DanhMuc = m.DanhMuc, GianHangId = m.GhId, ConHang = true,
            });
    }

    private static void NapTaiXe()
    {
        // Hồ sơ tài xế mẫu: đã duyệt sẵn, đăng nhập bằng SĐT 0909888777 / mật khẩu 123456
        // (v8 chỉ cho đăng nhập các hồ sơ nằm trong DanhSachTaiXe, mật khẩu lưu dạng băm PBKDF2).
        if (Services.TaiXeStore.TimTheoSdt("0909888777") is not null) return;
        var t = new TaiXe
        {
            Id = "tx1", HoTen = "Nguyễn Văn Hải", SoDienThoai = "0909888777",
            LoaiXe = "Xe máy", BienSoXe = "59-P1 123.45",
            SoCccd = "079090001234", SoGplx = "790012345678",
            DanhGia = 4.9, DangHoatDong = true,
            TrangThaiDangKy = TrangThaiDangKyGianHang.DaDuyet, NgayDangKy = DateTime.Now,
        };
        Services.TaiXeStore.DatMatKhau(t, "123456", luu: false);
        MockData.DanhSachTaiXe.Add(t);
        Services.TaiXeStore.Luu();
    }

    private static void NapQuanTri()
    {
        Services.QuanTriStore.TaoMacDinhNeuTrong();   // Admin tổng + nhân viên hỗ trợ mẫu
    }

    private static void NapKhachHang()
    {
        var kh = new (string Id, string Ten, string Email, string Sdt, int SoDon)[]
        {
            ("kh1", "Nguyễn Thị Lan",   "lan.nguyen@gmail.com",  "0901234567", 12),
            ("kh2", "Trần Văn Nam",     "nam.tran@gmail.com",    "0912345678", 5),
            ("kh3", "Lê Thị Hương",     "huong.le@gmail.com",    "0923456789", 8),
            ("kh4", "Phạm Minh Đức",    "duc.pham@gmail.com",    "0909123456", 1),
            ("kh5", "Võ Thị Kim Ngân",  "ngan.vo@gmail.com",     "0933567890", 6),
            ("kh6", "Đặng Quốc Bảo",    "bao.dang@gmail.com",    "0966234567", 2),
        };
        foreach (var k in kh)
            MockData.DanhSachKhachHang.Add(new KhachHang
            {
                Id = k.Id, HoTen = k.Ten, Email = k.Email, MatKhau = "123456", SoDienThoai = k.Sdt, TongSoDon = k.SoDon,
            });
    }

    private static void NapKhuyenMai()
    {
        MockData.DanhSachKhuyenMai.Add(new KhuyenMai
        {
            Id = "km1", MaCode = "CHAOMUNG", TenChuongTrinh = "Chào mừng khách mới", MoTa = "Giảm 20% cho đơn đầu tiên",
            LoaiGiam = LoaiGiamGia.PhanTram, GiaTriGiam = 20, GiamToiDa = 30000, GiaTriDonToiThieu = 50000,
            GianHangId = null, SoLuongToiDa = 1000, SoLuongDaDung = 230, DangHoatDong = true,
            NgayBatDau = DateTime.Now.AddMonths(-1), NgayKetThuc = DateTime.Now.AddMonths(2),
        });
        MockData.DanhSachKhuyenMai.Add(new KhuyenMai
        {
            Id = "km2", MaCode = "COMTAM10K", TenChuongTrinh = "Ưu đãi Cơm Tấm Sài Gòn", MoTa = "Giảm 10.000đ",
            LoaiGiam = LoaiGiamGia.SoTien, GiaTriGiam = 10000, GiaTriDonToiThieu = 40000,
            GianHangId = "gh1", SoLuongToiDa = 200, SoLuongDaDung = 54, DangHoatDong = true,
            NgayBatDau = DateTime.Now.AddDays(-10), NgayKetThuc = DateTime.Now.AddDays(20),
        });
        MockData.DanhSachKhuyenMai.Add(new KhuyenMai
        {
            Id = "km3", MaCode = "FREESHIP", TenChuongTrinh = "Miễn phí giao hàng", MoTa = "Giảm phí ship tối đa 15.000đ",
            LoaiGiam = LoaiGiamGia.SoTien, GiaTriGiam = 15000, GiaTriDonToiThieu = 0,
            GianHangId = null, SoLuongToiDa = 500, SoLuongDaDung = 412, DangHoatDong = true,
            NgayBatDau = DateTime.Now.AddMonths(-2), NgayKetThuc = DateTime.Now.AddDays(5),
        });
        MockData.DanhSachKhuyenMai.Add(new KhuyenMai
        {
            Id = "km4", MaCode = "TRASUA15", TenChuongTrinh = "Ưu đãi Trà Sữa Bốn Mùa", MoTa = "Giảm 15%",
            LoaiGiam = LoaiGiamGia.PhanTram, GiaTriGiam = 15, GiamToiDa = 20000, GiaTriDonToiThieu = 30000,
            GianHangId = "gh2", SoLuongToiDa = 150, SoLuongDaDung = 150, DangHoatDong = true,
            NgayBatDau = DateTime.Now.AddDays(-30), NgayKetThuc = DateTime.Now.AddDays(-1), // hết hạn — demo trạng thái "Đã hết hạn"
        });
        MockData.DanhSachKhuyenMai.Add(new KhuyenMai
        {
            Id = "km5", MaCode = "SALE50", TenChuongTrinh = "Flash sale cuối tuần", MoTa = "Giảm 50% tối đa 40.000đ",
            LoaiGiam = LoaiGiamGia.PhanTram, GiaTriGiam = 50, GiamToiDa = 40000, GiaTriDonToiThieu = 80000,
            GianHangId = null, SoLuongToiDa = 100, SoLuongDaDung = 10, DangHoatDong = false, // demo trạng thái "Đã tắt"
            NgayBatDau = DateTime.Now.AddDays(-3), NgayKetThuc = DateTime.Now.AddDays(3),
        });
    }
}
