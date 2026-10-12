/* ============================================================
   NÂNG CẤP v11: chức năng KHÁCH HÀNG (chạy NHIỀU LẦN an toàn, không xóa dữ liệu)
   - Đồng ý điều khoản, phiên đăng nhập (đăng xuất thiết bị khác)
   - Sổ địa chỉ có tỉnh/quận/phường, hướng dẫn giao, tọa độ ghim bản đồ
   - Cài đặt thông báo, thông báo (bảng ThongBao đã có từ v9)
   - Yêu thích (quán/món), báo cáo đánh giá
   - Tùy chọn món trong đơn (ghi chú, size/topping), chống tạo đơn trùng
   Yêu cầu đã chạy: NangCap_v8 -> NangCap_v9_Backend -> 04_BoSung_DoiTac (hoặc tạo mới bằng 01_TaoMoi_Full.sql)
   ============================================================ */
USE QuanLyDichVuAnUong;
GO

IF COL_LENGTH('TaiKhoan', 'DongYDieuKhoanLuc') IS NULL ALTER TABLE TaiKhoan ADD DongYDieuKhoanLuc DATETIME NULL;
GO

/* ---- Sổ địa chỉ ---- */
IF COL_LENGTH('DiaChiKhachHang', 'Tinh')     IS NULL ALTER TABLE DiaChiKhachHang ADD Tinh     NVARCHAR(80)  NULL;
IF COL_LENGTH('DiaChiKhachHang', 'Quan')     IS NULL ALTER TABLE DiaChiKhachHang ADD Quan     NVARCHAR(80)  NULL;
IF COL_LENGTH('DiaChiKhachHang', 'Phuong')   IS NULL ALTER TABLE DiaChiKhachHang ADD Phuong   NVARCHAR(80)  NULL;
IF COL_LENGTH('DiaChiKhachHang', 'HuongDan') IS NULL ALTER TABLE DiaChiKhachHang ADD HuongDan NVARCHAR(300) NULL;
IF COL_LENGTH('DiaChiKhachHang', 'Lat')      IS NULL ALTER TABLE DiaChiKhachHang ADD Lat DECIMAL(9,6) NULL;
IF COL_LENGTH('DiaChiKhachHang', 'Lng')      IS NULL ALTER TABLE DiaChiKhachHang ADD Lng DECIMAL(9,6) NULL;
GO

/* ---- Cài đặt thông báo ---- */
IF COL_LENGTH('KhachHang', 'NhanTbDonHang')  IS NULL ALTER TABLE KhachHang ADD NhanTbDonHang  BIT NOT NULL CONSTRAINT DF_KH_TbDon DEFAULT 1;
IF COL_LENGTH('KhachHang', 'NhanTbThanhToan') IS NULL ALTER TABLE KhachHang ADD NhanTbThanhToan BIT NOT NULL CONSTRAINT DF_KH_TbTT  DEFAULT 1;
IF COL_LENGTH('KhachHang', 'NhanTbKhuyenMai') IS NULL ALTER TABLE KhachHang ADD NhanTbKhuyenMai BIT NOT NULL CONSTRAINT DF_KH_TbKM  DEFAULT 1;
GO

/* ---- Đơn hàng: chống tạo trùng + tùy chọn món ---- */
IF COL_LENGTH('DonHang', 'MaYeuCauDat') IS NULL ALTER TABLE DonHang ADD MaYeuCauDat VARCHAR(40) NULL;
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_DonHang_MaYeuCauDat')
    CREATE UNIQUE INDEX UX_DonHang_MaYeuCauDat ON DonHang(MaYeuCauDat) WHERE MaYeuCauDat IS NOT NULL;
GO
IF COL_LENGTH('ChiTietDonHang', 'GhiChu')  IS NULL ALTER TABLE ChiTietDonHang ADD GhiChu  NVARCHAR(255) NULL;
IF COL_LENGTH('ChiTietDonHang', 'TuyChon') IS NULL ALTER TABLE ChiTietDonHang ADD TuyChon NVARCHAR(500) NULL;   -- "Lớn; Trân châu"
GO

/* ---- Yêu thích ---- */
IF OBJECT_ID('YeuThich', 'U') IS NULL
BEGIN
    CREATE TABLE YeuThich (
        MaYeuThich  INT IDENTITY(1,1) PRIMARY KEY,
        MaKH        INT NOT NULL REFERENCES KhachHang(MaKH) ON DELETE CASCADE,
        Loai        NVARCHAR(10) NOT NULL CHECK (Loai IN (N'GianHang', N'MonAn')),
        MaThamChieu INT NOT NULL,
        NgayTao     DATETIME NOT NULL DEFAULT GETDATE(),
        CONSTRAINT UQ_YeuThich UNIQUE (MaKH, Loai, MaThamChieu)
    );
END
GO

/* ---- Phiên đăng nhập (xem / đăng xuất thiết bị khác) ---- */
IF OBJECT_ID('PhienDangNhap', 'U') IS NULL
BEGIN
    CREATE TABLE PhienDangNhap (
        MaPhien     UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
        MaTK        INT NOT NULL REFERENCES TaiKhoan(MaTK) ON DELETE CASCADE,
        Kenh        VARCHAR(10) NOT NULL DEFAULT 'web',      -- web | app
        ThietBi     NVARCHAR(200) NULL,
        DiaChiIP    NVARCHAR(45) NULL,
        NgayTao     DATETIME NOT NULL DEFAULT GETDATE(),
        LanCuoi     DATETIME NOT NULL DEFAULT GETDATE(),
        ThuHoiLuc   DATETIME NULL
    );
    CREATE INDEX IX_PhienDangNhap_MaTK ON PhienDangNhap(MaTK, ThuHoiLuc);
END
GO

/* ---- Báo cáo đánh giá không phù hợp ---- */
IF OBJECT_ID('BaoCaoDanhGia', 'U') IS NULL
BEGIN
    CREATE TABLE BaoCaoDanhGia (
        MaBaoCao   INT IDENTITY(1,1) PRIMARY KEY,
        MaKH       INT NOT NULL REFERENCES KhachHang(MaKH),
        MaDanhGia  INT NOT NULL REFERENCES DanhGia(MaDanhGia) ON DELETE CASCADE,
        LyDo       NVARCHAR(255) NOT NULL,
        NgayTao    DATETIME NOT NULL DEFAULT GETDATE(),
        TrangThai  NVARCHAR(15) NOT NULL DEFAULT N'ChoXuLy' CHECK (TrangThai IN (N'ChoXuLy', N'DaXuLy')),
        CONSTRAINT UQ_BaoCaoDanhGia UNIQUE (MaKH, MaDanhGia)
    );
END
GO
