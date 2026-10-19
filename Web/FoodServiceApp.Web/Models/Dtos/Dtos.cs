namespace FoodServiceApp.Web.Models.Dtos
{
    public record LoginRequest(string TenDangNhap, string MatKhau);
    public record LoginResponse(string Token, string VaiTro, int MaTK, string HoTenHienThi);

    public record MonAnDto(int MaMonAn, string TenMon, decimal Gia, string? MoTa, string? HinhAnh, bool ConHang);

    public record ChiTietDonHangDto(string TenMon, int SoLuong, decimal DonGia, decimal ThanhTien);

    public record DonHangDto(
        int MaDonHang, string TrangThai, decimal TongTien, DateTime NgayDat,
        string DiaChiGiaoHang, string? GhiChu,
        string TenGianHang, string DiaChiGianHang,
        List<ChiTietDonHangDto> ChiTiet,
        decimal PhiGiaoHang = 0);

    public record VendorHomeDto(
        string TenCuaHang, string TrangThaiKinhDoanh,
        decimal DoanhThuHomNay, int SoDonHomNay, int SoDonChoXacNhan, int SoMonAn);

    public record AdminHomeDto(
        int TongKhachHang, int TongGianHang, int GianHangDangMo,
        int TongDonHomNay, decimal DoanhThuHomNay);

    public record VendorListItemDto(int MaGianHang, string TenCuaHang, string DiaChi, DateTime NgayDangKy, string TrangThaiKinhDoanh, bool TaiKhoanHoatDong);

    public record CustomerListItemDto(int MaKH, string HoTen, string SDT, string? Email, bool TaiKhoanHoatDong);

    public record DriverHomeDto(
        string HoTen, string TrangThai, int SoDonHomNay, decimal ThuNhapHomNay, DonHangDto? DonDangGiao, int SoDonChoNhan);

    public record UpdateOrderStatusRequest(string TrangThaiMoi);


    // ---------- Mobile Khách hàng ----------
    public record GianHangCardDto(
        int MaGianHang, string TenCuaHang, string DiaChi, string SDT,
        string? MoTa, string? HinhAnh, string TrangThaiKinhDoanh,
        double DiemTrungBinh, int SoDanhGia);

    public record CustomerHomeDto(string? TuKhoa, List<GianHangCardDto> DanhSachGianHang);

    public record DanhMucDto(int MaDanhMuc, string TenDanhMuc, List<MonAnDto> MonAns);
    public record StoreReviewDto(int SoSao, string? NoiDung, DateTime NgayDanhGia);
    public record FoodReviewDto(string TenKhach, int SoSao, string? NoiDung, DateTime NgayDanhGia);

    public record CustomerStoreDetailDto(
        int MaGianHang, string TenCuaHang, string DiaChi, string SDT,
        string? MoTa, string? HinhAnh, string TrangThaiKinhDoanh,
        double DiemTrungBinh, int SoDanhGia,
        List<DanhMucDto> DanhMucs, List<StoreReviewDto> DanhGias);

    public record CustomerFoodDetailDto(
        int MaMonAn, string TenMon, decimal Gia, string? MoTa, string? HinhAnh,
        bool ConHang, int MaGianHang, string TenGianHang,
        double DiemTrungBinh, int SoDanhGia, List<FoodReviewDto> DanhGias);

    public record PlaceOrderItemRequest(int MaMonAn, int SoLuong);
    public record PlaceOrderRequest(
        int MaGianHang,
        List<PlaceOrderItemRequest> Items,
        string DiaChiGiaoHang,
        string? GhiChu,
        string? MaKhuyenMai,
        string HinhThucThanhToan = "TienMat", string? MaYeuCauDat = null);

    public record DiaChiDto(int MaDiaChi, string Nhan, string DiaChi, bool MacDinh);
    public record DiaChiRequest(string Nhan, string DiaChi, bool MacDinh = false);

    public record CustomerProfileDto(
        int MaKH, int MaTK, string TenDangNhap, string HoTen,
        string SDT, string? Email, string? DiaChiGiaoHang);

    public record UpdateCustomerProfileRequest(
        string HoTen, string SDT, string? Email, string? DiaChiGiaoHang,
        string? MatKhauHienTai, string? MatKhauMoi);

    public record ReviewFoodItemRequest(int MaMonAn, int SoSao, string? NoiDung);
    public record ReviewOrderRequest(int SoSao, string? NoiDung, List<ReviewFoodItemRequest>? MonAn);
}