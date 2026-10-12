/* ============================================================
   CƠ SỞ DỮ LIỆU: QuanLyDichVuAnUong (FILE DUY NHẤT - TRỌN BỘ)
   Đề tài: CNTT-KLCN069 - Xây dựng ứng dụng quản lý dịch vụ ăn uống
   Hệ quản trị: SQL Server

   File này gộp toàn bộ, chạy 1 lần là xong, không cần file phụ nào khác:
     PHẦN 1: Tạo DB + 16 bảng (KhachHang/GianHang/QuanTri + Shipper)
     PHẦN 2-5: Dữ liệu mẫu nhỏ (8 gian hàng, 5 khách hàng, 4 tài xế,
               2 quản trị viên, khuyến mãi, đơn hàng mẫu)
     PHẦN 6: Đặt mật khẩu demo = 123456 (quản trị = admin123)
     PHẦN 7: Sinh thêm dữ liệu lớn — 100 khách hàng, 30 quán ăn
             (kèm thực đơn), 40 tài xế — mật khẩu cũng = 123456
   ============================================================ */

IF DB_ID('QuanLyDichVuAnUong') IS NOT NULL
    DROP DATABASE QuanLyDichVuAnUong;
GO

CREATE DATABASE QuanLyDichVuAnUong;
GO

USE QuanLyDichVuAnUong;
GO

/* ============================================================
   PHẦN 1: TẠO BẢNG
   ============================================================ */

CREATE TABLE TaiKhoan (
    MaTK            INT IDENTITY(1,1) PRIMARY KEY,
    TenDangNhap     NVARCHAR(50)  NOT NULL UNIQUE,
    MatKhau         NVARCHAR(255) NOT NULL,
    VaiTro          NVARCHAR(20)  NOT NULL
        CHECK (VaiTro IN (N'KhachHang', N'GianHang', N'QuanTri', N'TaiXe')),
    NgayTao         DATETIME      NOT NULL DEFAULT GETDATE(),
    TrangThai       BIT           NOT NULL DEFAULT 1
);
GO

CREATE TABLE KhachHang (
    MaKH            INT IDENTITY(1,1) PRIMARY KEY,
    MaTK            INT NOT NULL UNIQUE REFERENCES TaiKhoan(MaTK),
    HoTen           NVARCHAR(100) NOT NULL,
    SDT             VARCHAR(15)   NOT NULL,
    Email           VARCHAR(100)  NULL,
    DiaChiGiaoHang  NVARCHAR(255) NULL
);
GO

CREATE TABLE GianHang (
    MaGianHang        INT IDENTITY(1,1) PRIMARY KEY,
    MaTK              INT NOT NULL UNIQUE REFERENCES TaiKhoan(MaTK),
    TenCuaHang        NVARCHAR(150) NOT NULL,
    DiaChi            NVARCHAR(255) NOT NULL,
    SDT               VARCHAR(15)   NOT NULL,
    MoTa              NVARCHAR(500) NULL,
    HinhAnh           NVARCHAR(500) NULL,
    TrangThaiKinhDoanh NVARCHAR(20) NOT NULL DEFAULT N'DangMo'
        CHECK (TrangThaiKinhDoanh IN (N'DangMo', N'DongCua', N'TamNgung')),
    TrangThaiDuyet    NVARCHAR(20) NOT NULL DEFAULT N'ChoDuyet'
        CHECK (TrangThaiDuyet IN (N'ChoDuyet', N'DaDuyet', N'TuChoi')),
    NgayDangKy        DATETIME NOT NULL DEFAULT GETDATE()
);
GO

CREATE TABLE QuanTri (
    MaQuanTri       INT IDENTITY(1,1) PRIMARY KEY,
    MaTK            INT NOT NULL UNIQUE REFERENCES TaiKhoan(MaTK),
    HoTen           NVARCHAR(100) NOT NULL,
    SDT             VARCHAR(15)   NOT NULL,
    Email           VARCHAR(100)  NULL,
    CapQuyenHan     NVARCHAR(30)  NOT NULL DEFAULT N'NhanVien'
        CHECK (CapQuyenHan IN (N'SuperAdmin', N'QuanLy', N'NhanVien')),
    PhongBanPhuTrach NVARCHAR(100) NULL
);
GO

CREATE TABLE TaiXe (
    MaTaiXe         INT IDENTITY(1,1) PRIMARY KEY,
    MaTK            INT NOT NULL UNIQUE REFERENCES TaiKhoan(MaTK),
    HoTen           NVARCHAR(100) NOT NULL,
    SDT             VARCHAR(15)   NOT NULL,
    PhuongTien      NVARCHAR(50)  NULL,
    BienSo          VARCHAR(20)   NULL,
    KhuVucHoatDong  NVARCHAR(150) NULL,
    TrangThai       NVARCHAR(20)  NOT NULL DEFAULT N'SanSang'
        CHECK (TrangThai IN (N'SanSang', N'DangGiao', N'NgoaiTuyen')),
    TrangThaiDuyet  NVARCHAR(20)  NOT NULL DEFAULT N'DaDuyet'
        CHECK (TrangThaiDuyet IN (N'ChoDuyet', N'DaDuyet', N'TuChoi')),
    NgayDangKy      DATETIME      NOT NULL DEFAULT GETDATE()
);
GO

CREATE TABLE DanhMuc (
    MaDanhMuc       INT IDENTITY(1,1) PRIMARY KEY,
    MaGianHang      INT NOT NULL REFERENCES GianHang(MaGianHang),
    TenDanhMuc      NVARCHAR(100) NOT NULL
);
GO

CREATE TABLE MonAn (
    MaMonAn         INT IDENTITY(1,1) PRIMARY KEY,
    MaDanhMuc       INT NOT NULL REFERENCES DanhMuc(MaDanhMuc),
    TenMon          NVARCHAR(150) NOT NULL,
    Gia             DECIMAL(12,0) NOT NULL CHECK (Gia >= 0),
    MoTa            NVARCHAR(500) NULL,
    HinhAnh         NVARCHAR(255) NULL,
    TinhTrang       NVARCHAR(20) NOT NULL DEFAULT N'ConHang'
        CHECK (TinhTrang IN (N'ConHang', N'HetHang'))
);
GO

CREATE TABLE DonHang (
    MaDonHang       INT IDENTITY(1,1) PRIMARY KEY,
    MaKH            INT NOT NULL REFERENCES KhachHang(MaKH),
    MaGianHang      INT NOT NULL REFERENCES GianHang(MaGianHang),
    MaTaiXe         INT NULL REFERENCES TaiXe(MaTaiXe),
    NgayDat         DATETIME NOT NULL DEFAULT GETDATE(),
    TrangThai       NVARCHAR(30) NOT NULL DEFAULT N'ChoXacNhan'
        CHECK (TrangThai IN (N'ChoXacNhan', N'DaXacNhan', N'DangGiao', N'DaGiao', N'DaHuy')),
    TongTien        DECIMAL(12,0) NOT NULL DEFAULT 0,
    PhiGiaoHang     DECIMAL(12,0) NOT NULL DEFAULT 0, -- đã gồm trong TongTien; là thu nhập tài xế
    DiaChiGiaoHang  NVARCHAR(255) NOT NULL,
    GhiChu          NVARCHAR(255) NULL,
    MaKhuyenMaiApDung INT NULL -- FK thêm bằng ALTER TABLE bên dưới, sau khi có bảng KhuyenMai
);
GO

CREATE TABLE ChiTietDonHang (
    MaCTDH          INT IDENTITY(1,1) PRIMARY KEY,
    MaDonHang       INT NOT NULL REFERENCES DonHang(MaDonHang),
    MaMonAn         INT NOT NULL REFERENCES MonAn(MaMonAn),
    SoLuong         INT NOT NULL CHECK (SoLuong > 0),
    DonGia          DECIMAL(12,0) NOT NULL CHECK (DonGia >= 0)
);
GO

CREATE TABLE ThanhToan (
    MaThanhToan     INT IDENTITY(1,1) PRIMARY KEY,
    MaDonHang       INT NOT NULL UNIQUE REFERENCES DonHang(MaDonHang),
    HinhThuc        NVARCHAR(30) NOT NULL
        CHECK (HinhThuc IN (N'TienMat', N'VNPay', N'Momo')),
    TrangThai       NVARCHAR(20) NOT NULL DEFAULT N'ChoThanhToan'
        CHECK (TrangThai IN (N'ChoThanhToan', N'DaThanhToan', N'ThatBai', N'DaHoanTien')),
    MaGiaoDich      VARCHAR(100) NULL,
    NgayThanhToan   DATETIME NULL
);
GO

CREATE TABLE GiaoHang (
    MaGiaoHang      INT IDENTITY(1,1) PRIMARY KEY,
    MaDonHang       INT NOT NULL UNIQUE REFERENCES DonHang(MaDonHang),
    MaTaiXe         INT NOT NULL REFERENCES TaiXe(MaTaiXe),
    TrangThai       NVARCHAR(20) NOT NULL DEFAULT N'DaNhan'
        CHECK (TrangThai IN (N'DaNhan', N'DangGiao', N'GiaoThanhCong', N'GiaoThatBai')),
    ThoiGianNhan    DATETIME NULL,
    ThoiGianGiao    DATETIME NULL
    -- Không có cột PhiGiaoHang ở đây: phí giao hàng (= thu nhập tài xế) được
    -- tính và lưu thẳng vào DonHang.PhiGiaoHang ngay lúc khách đặt hàng
    -- (CustomerCartController), không phải lúc tài xế nhận đơn.
);
GO

CREATE TABLE KhuyenMai (
    MaKhuyenMai     INT IDENTITY(1,1) PRIMARY KEY,
    MaGianHang      INT NULL REFERENCES GianHang(MaGianHang),
    MaMonAn         INT NULL REFERENCES MonAn(MaMonAn),   -- NULL = giảm cả đơn; có giá trị = chỉ giảm trên món này
    MaCode          VARCHAR(30) NOT NULL UNIQUE,
    MoTa            NVARCHAR(255) NULL,
    PhanTramGiam    DECIMAL(5,2) NULL CHECK (PhanTramGiam BETWEEN 0 AND 100),
    SoTienGiam      DECIMAL(12,0) NULL,
    DieuKienApDung  NVARCHAR(255) NULL,
    NgayBatDau      DATETIME NOT NULL,
    NgayKetThuc     DATETIME NOT NULL,
    TrangThai       BIT NOT NULL DEFAULT 1
);
GO

ALTER TABLE DonHang ADD CONSTRAINT FK_DonHang_KhuyenMai
    FOREIGN KEY (MaKhuyenMaiApDung) REFERENCES KhuyenMai(MaKhuyenMai);
GO

CREATE TABLE DanhGia (
    MaDanhGia       INT IDENTITY(1,1) PRIMARY KEY,
    MaKH            INT NOT NULL REFERENCES KhachHang(MaKH),
    MaGianHang      INT NOT NULL REFERENCES GianHang(MaGianHang),
    MaDonHang       INT NOT NULL REFERENCES DonHang(MaDonHang),
    SoSao           INT NOT NULL CHECK (SoSao BETWEEN 1 AND 5),
    NoiDung         NVARCHAR(500) NULL,
    NgayDanhGia     DATETIME NOT NULL DEFAULT GETDATE()
);
GO

CREATE TABLE DanhGiaMon (
    MaDanhGiaMon    INT IDENTITY(1,1) PRIMARY KEY,
    MaKH            INT NOT NULL REFERENCES KhachHang(MaKH),
    MaMonAn         INT NOT NULL REFERENCES MonAn(MaMonAn),
    MaDonHang       INT NOT NULL REFERENCES DonHang(MaDonHang),
    SoSao           INT NOT NULL CHECK (SoSao BETWEEN 1 AND 5),
    NoiDung         NVARCHAR(500) NULL,
    NgayDanhGia     DATETIME NOT NULL DEFAULT GETDATE(),
    CONSTRAINT UQ_DanhGiaMon UNIQUE (MaDonHang, MaMonAn)
);
GO

CREATE TABLE DiaChiKhachHang (
    MaDiaChi        INT IDENTITY(1,1) PRIMARY KEY,
    MaKH            INT NOT NULL REFERENCES KhachHang(MaKH),
    Nhan            NVARCHAR(50)  NOT NULL,
    DiaChi          NVARCHAR(255) NOT NULL,
    MacDinh         BIT NOT NULL DEFAULT 0
);
GO

CREATE TABLE PhanHoi (
    MaPhanHoi       INT IDENTITY(1,1) PRIMARY KEY,
    MaKH            INT NOT NULL REFERENCES KhachHang(MaKH),
    MaDonHang       INT NULL REFERENCES DonHang(MaDonHang),
    MaGianHang      INT NULL REFERENCES GianHang(MaGianHang),
    Loai            NVARCHAR(20) NOT NULL DEFAULT N'GopY'
        CHECK (Loai IN (N'GopY', N'KhieuNai', N'Khac')),
    TieuDe          NVARCHAR(150) NOT NULL,
    NoiDung         NVARCHAR(1000) NOT NULL,
    TrangThai       NVARCHAR(20) NOT NULL DEFAULT N'ChuaXuLy'
        CHECK (TrangThai IN (N'ChuaXuLy', N'DaXuLy')),
    TraLoi          NVARCHAR(1000) NULL,
    NgayGui         DATETIME NOT NULL DEFAULT GETDATE(),
    NgayXuLy        DATETIME NULL
);
GO

/* ---- Tài xế từ chối đơn giao (ẩn đơn khỏi danh sách của riêng tài xế đó) ---- */
IF OBJECT_ID('TaiXeTuChoiDon', 'U') IS NULL
CREATE TABLE TaiXeTuChoiDon (
    MaTuChoi    INT IDENTITY(1,1) PRIMARY KEY,
    MaTaiXe     INT NOT NULL REFERENCES TaiXe(MaTaiXe),
    MaDonHang   INT NOT NULL REFERENCES DonHang(MaDonHang),
    NgayTuChoi  DATETIME NOT NULL DEFAULT GETDATE(),
    CONSTRAINT UQ_TaiXeTuChoiDon UNIQUE (MaTaiXe, MaDonHang)
);
GO


/* ============================================================
   PHẦN 2: NGUỒN DỮ LIỆU LỚN - TÀI KHOẢN
   5 khách hàng, 8 gian hàng, 2 quản trị viên, 4 tài xế
   ============================================================ */

INSERT INTO TaiKhoan (TenDangNhap, MatKhau, VaiTro) VALUES
(N'khachhang01', N'$2b$11$mLzNATvh4ekIRUaoX0zQQ.vsKJ5gCrdGNEsQ3YZaAG.GoRnAuRhiu', N'KhachHang'),
(N'khachhang02', N'$2b$11$mLzNATvh4ekIRUaoX0zQQ.vsKJ5gCrdGNEsQ3YZaAG.GoRnAuRhiu', N'KhachHang'),
(N'khachhang03', N'$2b$11$mLzNATvh4ekIRUaoX0zQQ.vsKJ5gCrdGNEsQ3YZaAG.GoRnAuRhiu', N'KhachHang'),
(N'khachhang04', N'$2b$11$mLzNATvh4ekIRUaoX0zQQ.vsKJ5gCrdGNEsQ3YZaAG.GoRnAuRhiu', N'KhachHang'),
(N'khachhang05', N'$2b$11$mLzNATvh4ekIRUaoX0zQQ.vsKJ5gCrdGNEsQ3YZaAG.GoRnAuRhiu', N'KhachHang'),
(N'gianhang01', N'$2b$11$mLzNATvh4ekIRUaoX0zQQ.vsKJ5gCrdGNEsQ3YZaAG.GoRnAuRhiu', N'GianHang'),
(N'gianhang02', N'$2b$11$mLzNATvh4ekIRUaoX0zQQ.vsKJ5gCrdGNEsQ3YZaAG.GoRnAuRhiu', N'GianHang'),
(N'gianhang03', N'$2b$11$mLzNATvh4ekIRUaoX0zQQ.vsKJ5gCrdGNEsQ3YZaAG.GoRnAuRhiu', N'GianHang'),
(N'gianhang04', N'$2b$11$mLzNATvh4ekIRUaoX0zQQ.vsKJ5gCrdGNEsQ3YZaAG.GoRnAuRhiu', N'GianHang'),
(N'gianhang05', N'$2b$11$mLzNATvh4ekIRUaoX0zQQ.vsKJ5gCrdGNEsQ3YZaAG.GoRnAuRhiu', N'GianHang'),
(N'gianhang06', N'$2b$11$mLzNATvh4ekIRUaoX0zQQ.vsKJ5gCrdGNEsQ3YZaAG.GoRnAuRhiu', N'GianHang'),
(N'gianhang07', N'$2b$11$mLzNATvh4ekIRUaoX0zQQ.vsKJ5gCrdGNEsQ3YZaAG.GoRnAuRhiu', N'GianHang'),
(N'gianhang08', N'$2b$11$mLzNATvh4ekIRUaoX0zQQ.vsKJ5gCrdGNEsQ3YZaAG.GoRnAuRhiu', N'GianHang'),
(N'quantri01', N'$2b$11$4W4e54I7Wm8IZICGB5WG5eJicrEVilueK2EWGeKHOeTK/8ZNZnzzW', N'QuanTri'),
(N'quantri02', N'$2b$11$4W4e54I7Wm8IZICGB5WG5eJicrEVilueK2EWGeKHOeTK/8ZNZnzzW', N'QuanTri'),
(N'taixe01', N'$2b$11$mLzNATvh4ekIRUaoX0zQQ.vsKJ5gCrdGNEsQ3YZaAG.GoRnAuRhiu', N'TaiXe'),
(N'taixe02', N'$2b$11$mLzNATvh4ekIRUaoX0zQQ.vsKJ5gCrdGNEsQ3YZaAG.GoRnAuRhiu', N'TaiXe'),
(N'taixe03', N'$2b$11$mLzNATvh4ekIRUaoX0zQQ.vsKJ5gCrdGNEsQ3YZaAG.GoRnAuRhiu', N'TaiXe'),
(N'taixe04', N'$2b$11$mLzNATvh4ekIRUaoX0zQQ.vsKJ5gCrdGNEsQ3YZaAG.GoRnAuRhiu', N'TaiXe');
GO

INSERT INTO QuanTri (MaTK, HoTen, SDT, Email, CapQuyenHan, PhongBanPhuTrach)
SELECT MaTK, HoTen, SDT, Email, CapQuyen, PhongBan FROM (VALUES
    (N'quantri01', N'Nguyễn Thị Quản Lý', '0920000001', 'quanly@huit.edu.vn', N'SuperAdmin', N'Điều hành hệ thống'),
    (N'quantri02', N'Trần Văn Giám Sát', '0921000002', 'giamsat@huit.edu.vn', N'NhanVien', N'Giám sát đối tác & khuyến mãi')
) AS d(TenDN, HoTen, SDT, Email, CapQuyen, PhongBan)
JOIN TaiKhoan t ON t.TenDangNhap = d.TenDN;
GO

INSERT INTO KhachHang (MaTK, HoTen, SDT, Email, DiaChiGiaoHang)
SELECT MaTK, HoTen, SDT, Email, DiaChi FROM (VALUES
    (N'khachhang01', N'Nguyễn Văn A', '0901000001', 'a@gmail.com', N'123 Lê Lợi, Q.1, TP.HCM'),
    (N'khachhang02', N'Phạm Thị D', '0907000007', 'd@gmail.com', N'56 Lý Thường Kiệt, Q.10, TP.HCM'),
    (N'khachhang03', N'Trần Văn E', '0908000008', 'e@gmail.com', N'89 Nguyễn Thị Minh Khai, Q.3, TP.HCM'),
    (N'khachhang04', N'Lê Thị F', '0909000009', 'f@gmail.com', N'12 Điện Biên Phủ, Bình Thạnh, TP.HCM'),
    (N'khachhang05', N'Hoàng Văn G', '0910000010', 'g@gmail.com', N'34 Cộng Hòa, Tân Bình, TP.HCM')
) AS d(TenDN, HoTen, SDT, Email, DiaChi)
JOIN TaiKhoan t ON t.TenDangNhap = d.TenDN;
GO

/* ---------- 8 gian hàng thuộc nhiều nhóm ẩm thực khác nhau ---------- */
INSERT INTO GianHang (MaTK, TenCuaHang, DiaChi, SDT, MoTa, TrangThaiKinhDoanh, TrangThaiDuyet)
SELECT MaTK, TenCuaHang, DiaChi, SDT, MoTa, N'DangMo', N'DaDuyet' FROM (VALUES
    (N'gianhang01', N'Quán Cơm Tấm Cô Ba', N'45 Nguyễn Trãi, Q.5, TP.HCM', '0902000002', N'Cơm tấm sườn bì chả truyền thống'),
    (N'gianhang02', N'Bún Bò Huế Mệ Ba', N'12 Trần Hưng Đạo, Q.1, TP.HCM', '0904000004', N'Bún bò chuẩn vị Huế, mở cửa từ 6h sáng'),
    (N'gianhang03', N'Trà Sữa Mộc Trà', N'88 Cách Mạng Tháng 8, Q.3, TP.HCM', '0905000005', N'Trà sữa, trà trái cây tươi mỗi ngày'),
    (N'gianhang04', N'Phở Hà Nội Xưa', N'20 Hai Bà Trưng, Q.1, TP.HCM', '0911000011', N'Phở bò/gà theo công thức Hà Nội gốc'),
    (N'gianhang05', N'Bánh Mì Bà Huynh', N'5 Nguyễn Đình Chiểu, Q.3, TP.HCM', '0912000012', N'Bánh mì thịt nướng, pate nhà làm'),
    (N'gianhang06', N'Gà Rán Cô Út', N'150 Ba Tháng Hai, Q.10, TP.HCM', '0913000013', N'Gà rán giòn, combo học sinh sinh viên'),
    (N'gianhang07', N'Hủ Tiếu Nam Vang Sáu Sài Gòn', N'77 Trần Phú, Q.5, TP.HCM', '0914000014', N'Hủ tiếu Nam Vang, hủ tiếu khô'),
    (N'gianhang08', N'Chè Ba Miền', N'22 Sư Vạn Hạnh, Q.10, TP.HCM', '0915000015', N'Chè Bắc - Trung - Nam đủ loại')
) AS d(TenDN, TenCuaHang, DiaChi, SDT, MoTa)
JOIN TaiKhoan t ON t.TenDangNhap = d.TenDN;
GO

INSERT INTO TaiXe (MaTK, HoTen, SDT, PhuongTien, KhuVucHoatDong)
SELECT MaTK, HoTen, SDT, PhuongTien, KhuVuc FROM (VALUES
    (N'taixe01', N'Trần Văn B', '0903000003', N'Xe máy', N'Quận 1, Quận 5'),
    (N'taixe02', N'Lê Thị C', '0906000006', N'Xe máy', N'Quận 3, Quận 10'),
    (N'taixe03', N'Đỗ Văn H', '0916000016', N'Xe máy', N'Bình Thạnh, Quận 1'),
    (N'taixe04', N'Ngô Thị K', '0917000017', N'Xe đạp điện', N'Quận 5, Quận 10')
) AS d(TenDN, HoTen, SDT, PhuongTien, KhuVuc)
JOIN TaiKhoan t ON t.TenDangNhap = d.TenDN;
GO

/* ============================================================
   PHẦN 3: DANH MỤC + MÓN ĂN CHO TỪNG GIAN HÀNG (~45 món)
   ============================================================ */

DECLARE @gh1 INT = (SELECT MaGianHang FROM GianHang WHERE TenCuaHang = N'Quán Cơm Tấm Cô Ba');
DECLARE @gh2 INT = (SELECT MaGianHang FROM GianHang WHERE TenCuaHang = N'Bún Bò Huế Mệ Ba');
DECLARE @gh3 INT = (SELECT MaGianHang FROM GianHang WHERE TenCuaHang = N'Trà Sữa Mộc Trà');
DECLARE @gh4 INT = (SELECT MaGianHang FROM GianHang WHERE TenCuaHang = N'Phở Hà Nội Xưa');
DECLARE @gh5 INT = (SELECT MaGianHang FROM GianHang WHERE TenCuaHang = N'Bánh Mì Bà Huynh');
DECLARE @gh6 INT = (SELECT MaGianHang FROM GianHang WHERE TenCuaHang = N'Gà Rán Cô Út');
DECLARE @gh7 INT = (SELECT MaGianHang FROM GianHang WHERE TenCuaHang = N'Hủ Tiếu Nam Vang Sáu Sài Gòn');
DECLARE @gh8 INT = (SELECT MaGianHang FROM GianHang WHERE TenCuaHang = N'Chè Ba Miền');

-- Cơm Tấm Cô Ba
INSERT INTO DanhMuc (MaGianHang, TenDanhMuc) VALUES (@gh1, N'Cơm phần'), (@gh1, N'Món thêm'), (@gh1, N'Nước uống');
DECLARE @gh1_com INT = (SELECT TOP 1 MaDanhMuc FROM DanhMuc WHERE MaGianHang=@gh1 AND TenDanhMuc=N'Cơm phần');
DECLARE @gh1_them INT = (SELECT TOP 1 MaDanhMuc FROM DanhMuc WHERE MaGianHang=@gh1 AND TenDanhMuc=N'Món thêm');
DECLARE @gh1_nuoc INT = (SELECT TOP 1 MaDanhMuc FROM DanhMuc WHERE MaGianHang=@gh1 AND TenDanhMuc=N'Nước uống');
INSERT INTO MonAn (MaDanhMuc, TenMon, Gia, MoTa) VALUES
(@gh1_com, N'Cơm sườn bì chả', 45000, N'Sườn nướng, bì, chả trứng'),
(@gh1_com, N'Cơm sườn trứng', 40000, NULL),
(@gh1_com, N'Cơm sườn ốp la', 42000, NULL),
(@gh1_com, N'Cơm gà nướng', 48000, NULL),
(@gh1_them, N'Trứng ốp la', 8000, N'Trứng chiên lòng đào'),
(@gh1_them, N'Chả trứng hấp', 15000, NULL),
(@gh1_nuoc, N'Trà đá', 5000, NULL),
(@gh1_nuoc, N'Nước ngọt lon', 12000, NULL);

-- Bún Bò Huế Mệ Ba
INSERT INTO DanhMuc (MaGianHang, TenDanhMuc) VALUES (@gh2, N'Món chính'), (@gh2, N'Nước uống');
DECLARE @gh2_chinh INT = (SELECT TOP 1 MaDanhMuc FROM DanhMuc WHERE MaGianHang=@gh2 AND TenDanhMuc=N'Món chính');
DECLARE @gh2_nuoc INT = (SELECT TOP 1 MaDanhMuc FROM DanhMuc WHERE MaGianHang=@gh2 AND TenDanhMuc=N'Nước uống');
INSERT INTO MonAn (MaDanhMuc, TenMon, Gia, MoTa) VALUES
(@gh2_chinh, N'Bún bò giò heo', 55000, N'Tô đầy đủ giò, chả, huyết'),
(@gh2_chinh, N'Bún bò đặc biệt', 65000, N'Thêm giò heo và chả cua'),
(@gh2_chinh, N'Bún chả cua', 50000, NULL),
(@gh2_chinh, N'Bún giò heo', 48000, NULL),
(@gh2_nuoc, N'Trà đá', 5000, NULL),
(@gh2_nuoc, N'Nước sâm', 10000, NULL);

-- Trà Sữa Mộc Trà
INSERT INTO DanhMuc (MaGianHang, TenDanhMuc) VALUES (@gh3, N'Trà sữa'), (@gh3, N'Trà trái cây');
DECLARE @gh3_ts INT = (SELECT TOP 1 MaDanhMuc FROM DanhMuc WHERE MaGianHang=@gh3 AND TenDanhMuc=N'Trà sữa');
DECLARE @gh3_tc INT = (SELECT TOP 1 MaDanhMuc FROM DanhMuc WHERE MaGianHang=@gh3 AND TenDanhMuc=N'Trà trái cây');
INSERT INTO MonAn (MaDanhMuc, TenMon, Gia, MoTa) VALUES
(@gh3_ts, N'Trà sữa truyền thống', 35000, NULL),
(@gh3_ts, N'Trà sữa trân châu đường đen', 42000, NULL),
(@gh3_ts, N'Trà sữa matcha', 40000, NULL),
(@gh3_tc, N'Trà đào cam sả', 39000, NULL),
(@gh3_tc, N'Trà vải', 39000, NULL);

-- Phở Hà Nội Xưa
INSERT INTO DanhMuc (MaGianHang, TenDanhMuc) VALUES (@gh4, N'Phở'), (@gh4, N'Nước uống');
DECLARE @gh4_pho INT = (SELECT TOP 1 MaDanhMuc FROM DanhMuc WHERE MaGianHang=@gh4 AND TenDanhMuc=N'Phở');
DECLARE @gh4_nuoc INT = (SELECT TOP 1 MaDanhMuc FROM DanhMuc WHERE MaGianHang=@gh4 AND TenDanhMuc=N'Nước uống');
INSERT INTO MonAn (MaDanhMuc, TenMon, Gia, MoTa) VALUES
(@gh4_pho, N'Phở bò tái', 50000, NULL),
(@gh4_pho, N'Phở bò chín', 50000, NULL),
(@gh4_pho, N'Phở gà', 45000, NULL),
(@gh4_pho, N'Phở đặc biệt', 65000, N'Tái, nạm, gân, bò viên'),
(@gh4_nuoc, N'Trà đá', 5000, NULL),
(@gh4_nuoc, N'Nước ngọt lon', 12000, NULL);

-- Bánh Mì Bà Huynh
INSERT INTO DanhMuc (MaGianHang, TenDanhMuc) VALUES (@gh5, N'Bánh mì'), (@gh5, N'Nước uống');
DECLARE @gh5_bm INT = (SELECT TOP 1 MaDanhMuc FROM DanhMuc WHERE MaGianHang=@gh5 AND TenDanhMuc=N'Bánh mì');
DECLARE @gh5_nuoc INT = (SELECT TOP 1 MaDanhMuc FROM DanhMuc WHERE MaGianHang=@gh5 AND TenDanhMuc=N'Nước uống');
INSERT INTO MonAn (MaDanhMuc, TenMon, Gia, MoTa) VALUES
(@gh5_bm, N'Bánh mì thịt nướng', 25000, NULL),
(@gh5_bm, N'Bánh mì pate chả', 20000, NULL),
(@gh5_bm, N'Bánh mì trứng ốp la', 18000, NULL),
(@gh5_bm, N'Bánh mì xíu mại', 22000, NULL),
(@gh5_nuoc, N'Sữa đậu nành', 10000, NULL);

-- Gà Rán Cô Út
INSERT INTO DanhMuc (MaGianHang, TenDanhMuc) VALUES (@gh6, N'Gà rán'), (@gh6, N'Món ăn kèm'), (@gh6, N'Nước ngọt');
DECLARE @gh6_ga INT = (SELECT TOP 1 MaDanhMuc FROM DanhMuc WHERE MaGianHang=@gh6 AND TenDanhMuc=N'Gà rán');
DECLARE @gh6_kem INT = (SELECT TOP 1 MaDanhMuc FROM DanhMuc WHERE MaGianHang=@gh6 AND TenDanhMuc=N'Món ăn kèm');
DECLARE @gh6_nuoc INT = (SELECT TOP 1 MaDanhMuc FROM DanhMuc WHERE MaGianHang=@gh6 AND TenDanhMuc=N'Nước ngọt');
INSERT INTO MonAn (MaDanhMuc, TenMon, Gia, MoTa) VALUES
(@gh6_ga, N'Gà rán miếng đùi', 35000, NULL),
(@gh6_ga, N'Combo 2 miếng gà + cơm', 55000, NULL),
(@gh6_kem, N'Khoai tây chiên', 20000, NULL),
(@gh6_kem, N'Salad trộn', 15000, NULL),
(@gh6_nuoc, N'Pepsi lon', 12000, NULL);

-- Hủ Tiếu Nam Vang
INSERT INTO DanhMuc (MaGianHang, TenDanhMuc) VALUES (@gh7, N'Hủ tiếu'), (@gh7, N'Nước uống');
DECLARE @gh7_ht INT = (SELECT TOP 1 MaDanhMuc FROM DanhMuc WHERE MaGianHang=@gh7 AND TenDanhMuc=N'Hủ tiếu');
DECLARE @gh7_nuoc INT = (SELECT TOP 1 MaDanhMuc FROM DanhMuc WHERE MaGianHang=@gh7 AND TenDanhMuc=N'Nước uống');
INSERT INTO MonAn (MaDanhMuc, TenMon, Gia, MoTa) VALUES
(@gh7_ht, N'Hủ tiếu Nam Vang', 45000, NULL),
(@gh7_ht, N'Hủ tiếu khô', 45000, NULL),
(@gh7_ht, N'Hủ tiếu đặc biệt', 55000, N'Tôm, thịt bằm, gan, trứng cút'),
(@gh7_nuoc, N'Trà đá', 5000, NULL);

-- Chè Ba Miền
INSERT INTO DanhMuc (MaGianHang, TenDanhMuc) VALUES (@gh8, N'Chè'), (@gh8, N'Sữa chua');
DECLARE @gh8_che INT = (SELECT TOP 1 MaDanhMuc FROM DanhMuc WHERE MaGianHang=@gh8 AND TenDanhMuc=N'Chè');
DECLARE @gh8_sc INT = (SELECT TOP 1 MaDanhMuc FROM DanhMuc WHERE MaGianHang=@gh8 AND TenDanhMuc=N'Sữa chua');
INSERT INTO MonAn (MaDanhMuc, TenMon, Gia, MoTa) VALUES
(@gh8_che, N'Chè đậu xanh', 18000, NULL),
(@gh8_che, N'Chè bà ba', 20000, NULL),
(@gh8_che, N'Chè thái', 25000, NULL),
(@gh8_che, N'Chè khúc bạch', 25000, NULL),
(@gh8_sc, N'Sữa chua nếp cẩm', 20000, NULL),
(@gh8_sc, N'Sữa chua trân châu', 20000, NULL);
GO

/* ============================================================
   PHẦN 4: KHUYẾN MÃI MẪU
   ============================================================ */
DECLARE @gh_bunbo INT = (SELECT MaGianHang FROM GianHang WHERE TenCuaHang = N'Bún Bò Huế Mệ Ba');
DECLARE @gh_traSua INT = (SELECT MaGianHang FROM GianHang WHERE TenCuaHang = N'Trà Sữa Mộc Trà');

INSERT INTO KhuyenMai (MaGianHang, MaCode, MoTa, PhanTramGiam, SoTienGiam, DieuKienApDung, NgayBatDau, NgayKetThuc) VALUES
(NULL, N'CHAODON10', N'Giảm 10% cho đơn hàng đầu tiên toàn hệ thống', 10, NULL, N'Áp dụng 1 lần/tài khoản', '2026-01-01', '2026-12-31'),
(NULL, N'FREESHIP', N'Miễn phí giao hàng cho đơn từ 150.000đ', NULL, 15000, N'Đơn tối thiểu 150.000đ', '2026-01-01', '2026-12-31'),
(@gh_bunbo, N'BUNBO20K', N'Giảm 20.000đ cho đơn từ 100.000đ tại Bún Bò Huế Mệ Ba', NULL, 20000, N'Đơn tối thiểu 100.000đ', '2026-01-01', '2026-12-31'),
(@gh_traSua, N'TRASUA15', N'Giảm 15% cho mọi đơn trà sữa', 15, NULL, N'Không giới hạn giá trị đơn', '2026-01-01', '2026-12-31');
GO

/* ============================================================
   PHẦN 5: ĐƠN HÀNG MẪU (kèm chi tiết, thanh toán, giao hàng, đánh giá)
   ============================================================ */
DECLARE @kh1 INT = (SELECT MaKH FROM KhachHang WHERE SDT = '0901000001');
DECLARE @kh2 INT = (SELECT MaKH FROM KhachHang WHERE SDT = '0907000007');
DECLARE @tx1 INT = (SELECT MaTaiXe FROM TaiXe WHERE SDT = '0903000003');
DECLARE @tx2 INT = (SELECT MaTaiXe FROM TaiXe WHERE SDT = '0906000006');
DECLARE @gh1_id INT = (SELECT MaGianHang FROM GianHang WHERE TenCuaHang = N'Quán Cơm Tấm Cô Ba');
DECLARE @gh2_id INT = (SELECT MaGianHang FROM GianHang WHERE TenCuaHang = N'Bún Bò Huế Mệ Ba');
DECLARE @mon_com1 INT = (SELECT MaMonAn FROM MonAn WHERE TenMon = N'Cơm sườn bì chả');
DECLARE @mon_bunbo1 INT = (SELECT MaMonAn FROM MonAn WHERE TenMon = N'Bún bò giò heo');

-- Đơn 1: đã giao thành công
INSERT INTO DonHang (MaKH, MaGianHang, MaTaiXe, TrangThai, TongTien, PhiGiaoHang, DiaChiGiaoHang)
VALUES (@kh1, @gh1_id, @tx1, N'DaGiao', 45000, 15000, N'123 Lê Lợi, Q.1, TP.HCM');
DECLARE @dh1 INT = SCOPE_IDENTITY();
INSERT INTO ChiTietDonHang (MaDonHang, MaMonAn, SoLuong, DonGia) VALUES (@dh1, @mon_com1, 1, 45000);
INSERT INTO ThanhToan (MaDonHang, HinhThuc, TrangThai, NgayThanhToan) VALUES (@dh1, N'Momo', N'DaThanhToan', GETDATE());
INSERT INTO GiaoHang (MaDonHang, MaTaiXe, TrangThai, ThoiGianNhan, ThoiGianGiao)
VALUES (@dh1, @tx1, N'GiaoThanhCong', DATEADD(MINUTE,-40,GETDATE()), DATEADD(MINUTE,-10,GETDATE()));
INSERT INTO DanhGia (MaKH, MaGianHang, MaDonHang, SoSao, NoiDung) VALUES (@kh1, @gh1_id, @dh1, 5, N'Cơm ngon, giao nhanh');

-- Đơn 2: đang giao
INSERT INTO DonHang (MaKH, MaGianHang, MaTaiXe, TrangThai, TongTien, PhiGiaoHang, DiaChiGiaoHang)
VALUES (@kh2, @gh2_id, @tx2, N'DangGiao', 55000, 20000, N'56 Lý Thường Kiệt, Q.10, TP.HCM');
DECLARE @dh2 INT = SCOPE_IDENTITY();
INSERT INTO ChiTietDonHang (MaDonHang, MaMonAn, SoLuong, DonGia) VALUES (@dh2, @mon_bunbo1, 1, 55000);
INSERT INTO ThanhToan (MaDonHang, HinhThuc, TrangThai) VALUES (@dh2, N'VNPay', N'DaThanhToan');
INSERT INTO GiaoHang (MaDonHang, MaTaiXe, TrangThai, ThoiGianNhan) VALUES (@dh2, @tx2, N'DangGiao', DATEADD(MINUTE,-10,GETDATE()));

-- Đơn 3: chờ xác nhận (chưa gán tài xế)
INSERT INTO DonHang (MaKH, MaGianHang, TrangThai, TongTien, DiaChiGiaoHang)
VALUES (@kh1, @gh2_id, N'ChoXacNhan', 65000, N'123 Lê Lợi, Q.1, TP.HCM');
DECLARE @dh3 INT = SCOPE_IDENTITY();
INSERT INTO ChiTietDonHang (MaDonHang, MaMonAn, SoLuong, DonGia)
SELECT @dh3, MaMonAn, 1, Gia FROM MonAn WHERE TenMon = N'Bún bò đặc biệt';
INSERT INTO ThanhToan (MaDonHang, HinhThuc, TrangThai) VALUES (@dh3, N'TienMat', N'ChoThanhToan');
GO

PRINT N'Hoàn tất: CSDL QuanLyDichVuAnUong với 17 bảng (đúng đề cương: KhachHang/GianHang/QuanTri + Shipper hỗ trợ giao hàng), 8 gian hàng, ~45 món ăn, 5 khách hàng, 2 quản trị viên, 4 tài xế, 4 khuyến mãi, 3 đơn hàng mẫu.';

/* ============================================================
   PHẦN 6: CẬP NHẬT MẬT KHẨU DEMO CHO TÀI KHOẢN MẪU BAN ĐẦU
   (8 gian hàng, 5 khách hàng, 4 tài xế, 2 quản trị viên ở PHẦN 2-4 ở trên)
   Mật khẩu plaintext để đăng nhập thử — xem ghi chú bảo mật trong README.
   ============================================================ */

-- Khách hàng: mật khẩu = 123456
UPDATE TaiKhoan SET MatKhau = '123456' WHERE VaiTro = N'KhachHang';

-- Gian hàng (quán ăn / đối tác): mật khẩu = 123456
UPDATE TaiKhoan SET MatKhau = '123456' WHERE VaiTro = N'GianHang';

-- Tài xế: mật khẩu = 123456
UPDATE TaiKhoan SET MatKhau = '123456' WHERE VaiTro = N'TaiXe';

-- Quản trị: mật khẩu = admin123 (đổi riêng cho dễ phân biệt khi demo)
UPDATE TaiKhoan SET MatKhau = 'admin123' WHERE VaiTro = N'QuanTri';
GO

-- Xem lại danh sách tài khoản vừa cập nhật để đối chiếu khi đăng nhập thử
SELECT
    t.MaTK,
    t.TenDangNhap,
    t.MatKhau,
    t.VaiTro,
    COALESCE(kh.HoTen, gh.TenCuaHang, tx.HoTen, qt.HoTen) AS TenHienThi
FROM TaiKhoan t
LEFT JOIN KhachHang kh ON kh.MaTK = t.MaTK
LEFT JOIN GianHang gh ON gh.MaTK = t.MaTK
LEFT JOIN TaiXe tx ON tx.MaTK = t.MaTK
LEFT JOIN QuanTri qt ON qt.MaTK = t.MaTK
ORDER BY t.VaiTro, t.MaTK;
GO

/* ============================================================
   PHẦN 7: SINH DỮ LIỆU LỚN — 100 KHÁCH HÀNG, 30 QUÁN ĂN, 40 TÀI XẾ
   Mật khẩu tất cả tài khoản sinh ra = 123456 (plaintext, chỉ để demo).
   ============================================================ */

-- ---------- Bảng tạm chứa các thành phần tên tiếng Việt để ghép ngẫu nhiên ----------
DECLARE @Ho TABLE (STT INT IDENTITY(0,1), Ten NVARCHAR(20));
INSERT INTO @Ho (Ten) VALUES
(N'Nguyễn'),(N'Trần'),(N'Lê'),(N'Phạm'),(N'Hoàng'),(N'Huỳnh'),(N'Phan'),(N'Vũ'),
(N'Võ'),(N'Đặng'),(N'Bùi'),(N'Đỗ'),(N'Hồ'),(N'Ngô'),(N'Dương'),(N'Lý');

DECLARE @TenDemNam TABLE (STT INT IDENTITY(0,1), Ten NVARCHAR(20));
INSERT INTO @TenDemNam (Ten) VALUES (N'Văn'),(N'Hữu'),(N'Đức'),(N'Công'),(N'Quang'),(N'Minh'),(N'Thành'),(N'Anh');

DECLARE @TenDemNu TABLE (STT INT IDENTITY(0,1), Ten NVARCHAR(20));
INSERT INTO @TenDemNu (Ten) VALUES (N'Thị'),(N'Ngọc'),(N'Thu'),(N'Kim'),(N'Hồng'),(N'Bích'),(N'Diễm'),(N'Ánh');

DECLARE @TenNam TABLE (STT INT IDENTITY(0,1), Ten NVARCHAR(20));
INSERT INTO @TenNam (Ten) VALUES
(N'An'),(N'Bình'),(N'Cường'),(N'Dũng'),(N'Đạt'),(N'Hải'),(N'Hùng'),(N'Khang'),
(N'Long'),(N'Minh'),(N'Nam'),(N'Phong'),(N'Quân'),(N'Sơn'),(N'Tài'),(N'Thắng'),
(N'Tuấn'),(N'Việt'),(N'Vinh'),(N'Đăng');

DECLARE @TenNu TABLE (STT INT IDENTITY(0,1), Ten NVARCHAR(20));
INSERT INTO @TenNu (Ten) VALUES
(N'Anh'),(N'Chi'),(N'Diệp'),(N'Giang'),(N'Hà'),(N'Hoa'),(N'Huyền'),(N'Lan'),
(N'Linh'),(N'Mai'),(N'My'),(N'Ngân'),(N'Nhi'),(N'Phương'),(N'Quyên'),(N'Thảo'),
(N'Thư'),(N'Trang'),(N'Vy'),(N'Yến');

DECLARE @QuanHuyen TABLE (STT INT IDENTITY(0,1), Ten NVARCHAR(50));
INSERT INTO @QuanHuyen (Ten) VALUES
(N'Quận 1'),(N'Quận 3'),(N'Quận 4'),(N'Quận 5'),(N'Quận 6'),(N'Quận 7'),
(N'Quận 8'),(N'Quận 10'),(N'Quận 11'),(N'Quận 12'),(N'Bình Thạnh'),(N'Tân Bình'),
(N'Tân Phú'),(N'Phú Nhuận'),(N'Gò Vấp'),(N'Bình Tân'),(N'Thủ Đức');

DECLARE @TenDuong TABLE (STT INT IDENTITY(0,1), Ten NVARCHAR(50));
INSERT INTO @TenDuong (Ten) VALUES
(N'Lê Lợi'),(N'Nguyễn Trãi'),(N'Trần Hưng Đạo'),(N'Cách Mạng Tháng 8'),
(N'Nguyễn Thị Minh Khai'),(N'Điện Biên Phủ'),(N'Hai Bà Trưng'),(N'Ba Tháng Hai'),
(N'Lý Thường Kiệt'),(N'Nguyễn Văn Cừ'),(N'Sư Vạn Hạnh'),(N'Cộng Hòa'),
(N'Phan Xích Long'),(N'Quang Trung'),(N'Nguyễn Oanh');

-- ---------- Loại hình quán ăn để đặt tên gian hàng ----------
DECLARE @LoaiQuan TABLE (STT INT IDENTITY(0,1), Ten NVARCHAR(50));
INSERT INTO @LoaiQuan (Ten) VALUES
(N'Cơm Tấm'),(N'Bún Bò Huế'),(N'Phở'),(N'Bánh Mì'),(N'Hủ Tiếu'),(N'Bún Riêu'),
(N'Mì Quảng'),(N'Cơm Gà'),(N'Trà Sữa'),(N'Chè'),(N'Gà Rán'),(N'Pizza'),
(N'Bún Chả'),(N'Bánh Xèo'),(N'Lẩu'),(N'Nướng BBQ'),(N'Sushi'),(N'Bún Đậu'),
(N'Xôi'),(N'Ốc'),(N'Trà Chanh'),(N'Bánh Canh'),(N'Cháo'),(N'Cơm Niêu'),
(N'Bò Kho'),(N'Nem Nướng'),(N'Bánh Cuốn'),(N'Cà Phê'),(N'Sinh Tố'),(N'Bún Thịt Nướng');

DECLARE @TenChuQuan TABLE (STT INT IDENTITY(0,1), Ten NVARCHAR(30));
INSERT INTO @TenChuQuan (Ten) VALUES
(N'Cô Ba'),(N'Cô Út'),(N'Cô Sáu'),(N'Cô Tư'),(N'Chú Hai'),(N'Chú Bảy'),
(N'Mẹ Ba'),(N'Bà Năm'),(N'Anh Tuấn'),(N'Chị Hoa'),(N'Sài Gòn'),(N'Hà Nội'),
(N'Miền Tây'),(N'Ba Miền'),(N'Xưa'),(N'136'),(N'168'),(N'Number One'),
(N'Gia Truyền'),(N'Truyền Thống'),(N'Ngon'),(N'Đệ Nhất'),(N'Phố'),(N'Quê'),
(N'Nhà Làm'),(N'Việt'),(N'Sài Thành'),(N'Ông Địa'),(N'Cây Bàng'),(N'Xóm Nhỏ');

-- ==============================================================
-- 100 TÀI KHOẢN KHÁCH HÀNG
-- ==============================================================
DECLARE @i INT = 1;
WHILE @i <= 100
BEGIN
    DECLARE @tenDangNhap NVARCHAR(50) = N'khachhang' + RIGHT('000' + CAST(@i AS VARCHAR), 3);
    DECLARE @laNam BIT = @i % 2;
    DECLARE @ho NVARCHAR(20) = (SELECT Ten FROM @Ho WHERE STT = @i % 16);
    DECLARE @tenDem NVARCHAR(20) = CASE WHEN @laNam = 1
        THEN (SELECT Ten FROM @TenDemNam WHERE STT = @i % 8)
        ELSE (SELECT Ten FROM @TenDemNu WHERE STT = @i % 8) END;
    DECLARE @ten NVARCHAR(20) = CASE WHEN @laNam = 1
        THEN (SELECT Ten FROM @TenNam WHERE STT = @i % 20)
        ELSE (SELECT Ten FROM @TenNu WHERE STT = @i % 20) END;
    DECLARE @hoTen NVARCHAR(100) = @ho + N' ' + @tenDem + N' ' + @ten;
    DECLARE @sdt VARCHAR(15) = '09' + RIGHT('00000000' + CAST(30000000 + @i AS VARCHAR), 8);
    DECLARE @diaChi NVARCHAR(255) =
        CAST(10 + (@i % 90) AS VARCHAR) + N' Đường ' +
        (SELECT Ten FROM @TenDuong WHERE STT = @i % 15) + N', ' +
        (SELECT Ten FROM @QuanHuyen WHERE STT = @i % 17) + N', TP.HCM';

    INSERT INTO TaiKhoan (TenDangNhap, MatKhau, VaiTro) VALUES (@tenDangNhap, '123456', N'KhachHang');
    DECLARE @maTK INT = SCOPE_IDENTITY();

    INSERT INTO KhachHang (MaTK, HoTen, SDT, Email, DiaChiGiaoHang)
    VALUES (@maTK, @hoTen, @sdt, LOWER(REPLACE(@tenDangNhap, N' ', '')) + '@gmail.com', @diaChi);

    SET @i += 1;
END
GO

-- ==============================================================
-- 30 TÀI KHOẢN QUÁN ĂN (GIAN HÀNG)
-- ==============================================================
DECLARE @Ho TABLE (STT INT IDENTITY(0,1), Ten NVARCHAR(20));
INSERT INTO @Ho (Ten) VALUES
(N'Nguyễn'),(N'Trần'),(N'Lê'),(N'Phạm'),(N'Hoàng'),(N'Huỳnh'),(N'Phan'),(N'Vũ'),
(N'Võ'),(N'Đặng'),(N'Bùi'),(N'Đỗ'),(N'Hồ'),(N'Ngô'),(N'Dương'),(N'Lý');

DECLARE @QuanHuyen TABLE (STT INT IDENTITY(0,1), Ten NVARCHAR(50));
INSERT INTO @QuanHuyen (Ten) VALUES
(N'Quận 1'),(N'Quận 3'),(N'Quận 4'),(N'Quận 5'),(N'Quận 6'),(N'Quận 7'),
(N'Quận 8'),(N'Quận 10'),(N'Quận 11'),(N'Quận 12'),(N'Bình Thạnh'),(N'Tân Bình'),
(N'Tân Phú'),(N'Phú Nhuận'),(N'Gò Vấp'),(N'Bình Tân'),(N'Thủ Đức');

DECLARE @TenDuong TABLE (STT INT IDENTITY(0,1), Ten NVARCHAR(50));
INSERT INTO @TenDuong (Ten) VALUES
(N'Lê Lợi'),(N'Nguyễn Trãi'),(N'Trần Hưng Đạo'),(N'Cách Mạng Tháng 8'),
(N'Nguyễn Thị Minh Khai'),(N'Điện Biên Phủ'),(N'Hai Bà Trưng'),(N'Ba Tháng Hai'),
(N'Lý Thường Kiệt'),(N'Nguyễn Văn Cừ'),(N'Sư Vạn Hạnh'),(N'Cộng Hòa'),
(N'Phan Xích Long'),(N'Quang Trung'),(N'Nguyễn Oanh');

DECLARE @LoaiQuan TABLE (STT INT IDENTITY(0,1), Ten NVARCHAR(50));
INSERT INTO @LoaiQuan (Ten) VALUES
(N'Cơm Tấm'),(N'Bún Bò Huế'),(N'Phở'),(N'Bánh Mì'),(N'Hủ Tiếu'),(N'Bún Riêu'),
(N'Mì Quảng'),(N'Cơm Gà'),(N'Trà Sữa'),(N'Chè'),(N'Gà Rán'),(N'Pizza'),
(N'Bún Chả'),(N'Bánh Xèo'),(N'Lẩu'),(N'Nướng BBQ'),(N'Sushi'),(N'Bún Đậu'),
(N'Xôi'),(N'Ốc'),(N'Trà Chanh'),(N'Bánh Canh'),(N'Cháo'),(N'Cơm Niêu'),
(N'Bò Kho'),(N'Nem Nướng'),(N'Bánh Cuốn'),(N'Cà Phê'),(N'Sinh Tố'),(N'Bún Thịt Nướng');

DECLARE @TenChuQuan TABLE (STT INT IDENTITY(0,1), Ten NVARCHAR(30));
INSERT INTO @TenChuQuan (Ten) VALUES
(N'Cô Ba'),(N'Cô Út'),(N'Cô Sáu'),(N'Cô Tư'),(N'Chú Hai'),(N'Chú Bảy'),
(N'Mẹ Ba'),(N'Bà Năm'),(N'Anh Tuấn'),(N'Chị Hoa'),(N'Sài Gòn'),(N'Hà Nội'),
(N'Miền Tây'),(N'Ba Miền'),(N'Xưa'),(N'136'),(N'168'),(N'Number One'),
(N'Gia Truyền'),(N'Truyền Thống'),(N'Ngon'),(N'Đệ Nhất'),(N'Phố'),(N'Quê'),
(N'Nhà Làm'),(N'Việt'),(N'Sài Thành'),(N'Ông Địa'),(N'Cây Bàng'),(N'Xóm Nhỏ');

DECLARE @j INT = 1;
WHILE @j <= 30
BEGIN
    DECLARE @tenDN NVARCHAR(50) = N'gianhang' + RIGHT('000' + CAST(@j AS VARCHAR), 3);
    DECLARE @tenCuaHang NVARCHAR(150) =
        (SELECT Ten FROM @LoaiQuan WHERE STT = (@j - 1) % 30) + N' ' +
        (SELECT Ten FROM @TenChuQuan WHERE STT = @j % 30);
    DECLARE @diaChiQ NVARCHAR(255) =
        CAST(5 + (@j % 200) AS VARCHAR) + N' ' +
        (SELECT Ten FROM @TenDuong WHERE STT = @j % 15) + N', ' +
        (SELECT Ten FROM @QuanHuyen WHERE STT = @j % 17) + N', TP.HCM';
    DECLARE @sdtQ VARCHAR(15) = '09' + RIGHT('00000000' + CAST(40000000 + @j AS VARCHAR), 8);

    INSERT INTO TaiKhoan (TenDangNhap, MatKhau, VaiTro) VALUES (@tenDN, '123456', N'GianHang');
    DECLARE @maTKQ INT = SCOPE_IDENTITY();

    INSERT INTO GianHang (MaTK, TenCuaHang, DiaChi, SDT, MoTa, TrangThaiKinhDoanh, TrangThaiDuyet)
    VALUES (@maTKQ, @tenCuaHang, @diaChiQ, @sdtQ, N'Quán ăn phục vụ tận tâm, nguyên liệu tươi mỗi ngày',
            CASE WHEN @j % 10 = 0 THEN N'TamNgung' ELSE N'DangMo' END, N'DaDuyet');

    -- Mỗi gian hàng có sẵn 1 danh mục "Món chính" + 3 món ăn cơ bản để không bị trống thực đơn
    DECLARE @maGH INT = SCOPE_IDENTITY();
    INSERT INTO DanhMuc (MaGianHang, TenDanhMuc) VALUES (@maGH, N'Món chính');
    DECLARE @maDM INT = SCOPE_IDENTITY();

    INSERT INTO MonAn (MaDanhMuc, TenMon, Gia, TinhTrang) VALUES
    (@maDM, (SELECT Ten FROM @LoaiQuan WHERE STT = (@j - 1) % 30) + N' đặc biệt', 45000 + (@j % 6) * 5000, N'ConHang'),
    (@maDM, (SELECT Ten FROM @LoaiQuan WHERE STT = (@j - 1) % 30) + N' thường', 35000 + (@j % 5) * 3000, N'ConHang'),
    (@maDM, N'Nước ngọt', 12000, N'ConHang');

    SET @j += 1;
END
GO

-- ==============================================================
-- 40 TÀI KHOẢN TÀI XẾ
-- ==============================================================
DECLARE @Ho2 TABLE (STT INT IDENTITY(0,1), Ten NVARCHAR(20));
INSERT INTO @Ho2 (Ten) VALUES
(N'Nguyễn'),(N'Trần'),(N'Lê'),(N'Phạm'),(N'Hoàng'),(N'Huỳnh'),(N'Phan'),(N'Vũ'),
(N'Võ'),(N'Đặng'),(N'Bùi'),(N'Đỗ'),(N'Hồ'),(N'Ngô'),(N'Dương'),(N'Lý');

DECLARE @TenDemNam2 TABLE (STT INT IDENTITY(0,1), Ten NVARCHAR(20));
INSERT INTO @TenDemNam2 (Ten) VALUES (N'Văn'),(N'Hữu'),(N'Đức'),(N'Công'),(N'Quang'),(N'Minh'),(N'Thành'),(N'Anh');

DECLARE @TenNam2 TABLE (STT INT IDENTITY(0,1), Ten NVARCHAR(20));
INSERT INTO @TenNam2 (Ten) VALUES
(N'An'),(N'Bình'),(N'Cường'),(N'Dũng'),(N'Đạt'),(N'Hải'),(N'Hùng'),(N'Khang'),
(N'Long'),(N'Minh'),(N'Nam'),(N'Phong'),(N'Quân'),(N'Sơn'),(N'Tài'),(N'Thắng'),
(N'Tuấn'),(N'Việt'),(N'Vinh'),(N'Đăng');

DECLARE @QuanHuyen2 TABLE (STT INT IDENTITY(0,1), Ten NVARCHAR(50));
INSERT INTO @QuanHuyen2 (Ten) VALUES
(N'Quận 1'),(N'Quận 3'),(N'Quận 4'),(N'Quận 5'),(N'Quận 6'),(N'Quận 7'),
(N'Quận 8'),(N'Quận 10'),(N'Quận 11'),(N'Quận 12'),(N'Bình Thạnh'),(N'Tân Bình'),
(N'Tân Phú'),(N'Phú Nhuận'),(N'Gò Vấp'),(N'Bình Tân'),(N'Thủ Đức');

DECLARE @k INT = 1;
WHILE @k <= 40
BEGIN
    DECLARE @tenDNTX NVARCHAR(50) = N'taixe' + RIGHT('000' + CAST(@k AS VARCHAR), 3);
    DECLARE @hoTX NVARCHAR(20) = (SELECT Ten FROM @Ho2 WHERE STT = @k % 16);
    DECLARE @tenDemTX NVARCHAR(20) = (SELECT Ten FROM @TenDemNam2 WHERE STT = @k % 8);
    DECLARE @tenTX NVARCHAR(20) = (SELECT Ten FROM @TenNam2 WHERE STT = @k % 20);
    DECLARE @hoTenTX NVARCHAR(100) = @hoTX + N' ' + @tenDemTX + N' ' + @tenTX;
    DECLARE @sdtTX VARCHAR(15) = '09' + RIGHT('00000000' + CAST(50000000 + @k AS VARCHAR), 8);
    DECLARE @khuVuc NVARCHAR(150) =
        (SELECT Ten FROM @QuanHuyen2 WHERE STT = @k % 17) + N', ' +
        (SELECT Ten FROM @QuanHuyen2 WHERE STT = (@k + 3) % 17);
    DECLARE @phuongTien NVARCHAR(50) = CASE WHEN @k % 5 = 0 THEN N'Xe đạp điện' ELSE N'Xe máy' END;
    DECLARE @bienSo VARCHAR(20) = N'59-' + CHAR(65 + (@k % 26)) + N'1-' +
        RIGHT('00000' + CAST(10000 + @k AS VARCHAR), 5);

    INSERT INTO TaiKhoan (TenDangNhap, MatKhau, VaiTro) VALUES (@tenDNTX, '123456', N'TaiXe');
    DECLARE @maTKTX INT = SCOPE_IDENTITY();

    INSERT INTO TaiXe (MaTK, HoTen, SDT, PhuongTien, BienSo, KhuVucHoatDong, TrangThai)
    VALUES (@maTKTX, @hoTenTX, @sdtTX, @phuongTien, @bienSo, @khuVuc,
            CASE WHEN @k % 7 = 0 THEN N'NgoaiTuyen' ELSE N'SanSang' END);

    SET @k += 1;
END
GO

PRINT N'Đã sinh xong: 100 khách hàng, 30 gian hàng (kèm 3 món/gian hàng), 40 tài xế. Mật khẩu tất cả = 123456.';

SELECT VaiTro, COUNT(*) AS SoLuong FROM TaiKhoan
WHERE TenDangNhap LIKE 'khachhang0%' OR TenDangNhap LIKE 'gianhang0%' OR TenDangNhap LIKE 'taixe0%'
GROUP BY VaiTro;
GO

/* ============================================================
   PHẦN 8: DỮ LIỆU MẪU CHO CÁC CHỨC NĂNG MỚI (sổ địa chỉ, đánh giá món...)
   ============================================================ */

-- Sổ địa chỉ: mỗi khách mẫu có 1 địa chỉ mặc định từ hồ sơ
INSERT INTO DiaChiKhachHang (MaKH, Nhan, DiaChi, MacDinh)
SELECT MaKH, N'Nhà', DiaChiGiaoHang, 1 FROM KhachHang WHERE DiaChiGiaoHang IS NOT NULL;
GO

-- Đánh giá món ăn mẫu (cho các đơn đã giao có chi tiết món)
INSERT INTO DanhGiaMon (MaKH, MaMonAn, MaDonHang, SoSao, NoiDung)
SELECT TOP 5 d.MaKH, c.MaMonAn, d.MaDonHang, 5, N'Món ngon, đúng vị'
FROM DonHang d JOIN ChiTietDonHang c ON c.MaDonHang = d.MaDonHang
WHERE d.TrangThai = N'DaGiao';
GO

-- Phản hồi mẫu
INSERT INTO PhanHoi (MaKH, Loai, TieuDe, NoiDung)
SELECT TOP 1 MaKH, N'GopY', N'Mong có thêm nhiều quán trong khu vực', N'Ứng dụng dùng rất tiện, mong sớm có thêm quán ở khu vực của mình.'
FROM KhachHang ORDER BY MaKH;
GO

/* ============================================================
   TỔNG KẾT TOÀN BỘ
   ============================================================ */
PRINT N'====== HOÀN TẤT TOÀN BỘ FILE ======';

SELECT VaiTro, COUNT(*) AS TongSoTaiKhoan FROM TaiKhoan GROUP BY VaiTro;
SELECT COUNT(*) AS TongSoGianHang FROM GianHang;
SELECT COUNT(*) AS TongSoMonAn FROM MonAn;
SELECT COUNT(*) AS TongSoDonHangMau FROM DonHang;
GO

/* ============================================================
   GHI CHÚ: nếu bạn ĐÃ có CSDL QuanLyDichVuAnUong chạy từ trước (tạo bằng
   bản cũ hơn của file này) và không muốn DROP/tạo lại từ đầu, KHÔNG chạy
   các lệnh ALTER thủ công ở đây — hãy chạy file NangCap_CSDL.sql đi kèm,
   đã gộp đầy đủ mọi cột/bảng còn thiếu và an toàn để chạy lại nhiều lần.
   ============================================================ */

/* ============================================================
   PHẦN 9: GÁN HÌNH ẢNH MINH HOẠ cho Gian hàng / Món ăn
   Ảnh SVG tự vẽ (không vi phạm bản quyền) đặt sẵn ở wwwroot/uploads/ —
   khớp theo TÊN (an toàn dù ID tự tăng khác nhau giữa các lần tạo DB).
   ============================================================ */

-- 1) Ảnh đại diện Gian hàng
UPDATE GianHang SET HinhAnh = N'/uploads/gh1_comtam_coba.svg'     WHERE TenCuaHang = N'Quán Cơm Tấm Cô Ba';
UPDATE GianHang SET HinhAnh = N'/uploads/gh2_bunbo_meba.svg'      WHERE TenCuaHang = N'Bún Bò Huế Mệ Ba';
UPDATE GianHang SET HinhAnh = N'/uploads/gh3_trasua_moctra.svg'   WHERE TenCuaHang = N'Trà Sữa Mộc Trà';
UPDATE GianHang SET HinhAnh = N'/uploads/gh4_pho_hanoi.svg'       WHERE TenCuaHang = N'Phở Hà Nội Xưa';
UPDATE GianHang SET HinhAnh = N'/uploads/gh5_banhmi_bahuynh.svg'  WHERE TenCuaHang = N'Bánh Mì Bà Huynh';
UPDATE GianHang SET HinhAnh = N'/uploads/gh6_garan_cout.svg'      WHERE TenCuaHang = N'Gà Rán Cô Út';
UPDATE GianHang SET HinhAnh = N'/uploads/gh7_hutieu_namvang.svg'  WHERE TenCuaHang = N'Hủ Tiếu Nam Vang Sáu Sài Gòn';
UPDATE GianHang SET HinhAnh = N'/uploads/gh8_che_bamien.svg'      WHERE TenCuaHang = N'Chè Ba Miền';
GO

-- 2) Ảnh món ăn tiêu biểu (1 món/gian hàng — có thể tự thêm ảnh cho các món còn lại theo mẫu này)
UPDATE MonAn SET HinhAnh = N'/uploads/mon1_com_suon_bi_cha.svg'              WHERE TenMon = N'Cơm sườn bì chả';
UPDATE MonAn SET HinhAnh = N'/uploads/mon2_bun_bo_gio_heo.svg'               WHERE TenMon = N'Bún bò giò heo';
UPDATE MonAn SET HinhAnh = N'/uploads/mon3_trasua_tranchau_duongden.svg'     WHERE TenMon = N'Trà sữa trân châu đường đen';
UPDATE MonAn SET HinhAnh = N'/uploads/mon4_pho_dac_biet.svg'                 WHERE TenMon = N'Phở đặc biệt';
UPDATE MonAn SET HinhAnh = N'/uploads/mon5_banhmi_thitnuong.svg'             WHERE TenMon = N'Bánh mì thịt nướng';
UPDATE MonAn SET HinhAnh = N'/uploads/mon6_combo_ga_com.svg'                 WHERE TenMon = N'Combo 2 miếng gà + cơm';
UPDATE MonAn SET HinhAnh = N'/uploads/mon7_hutieu_dac_biet.svg'              WHERE TenMon = N'Hủ tiếu đặc biệt';
UPDATE MonAn SET HinhAnh = N'/uploads/mon8_che_thai.svg'                     WHERE TenMon = N'Chè thái';
GO

-- Kiểm tra nhanh
SELECT TenCuaHang, HinhAnh FROM GianHang;
SELECT TenMon, HinhAnh FROM MonAn WHERE HinhAnh IS NOT NULL;

/* ============================================================
   PHẦN 10: BỔ SUNG CHO APP MAUI v8 (chạy được NHIỀU LẦN, an toàn)
   - Tài xế: CCCD, GPLX, ảnh GPLX, lý do từ chối (khóa tài khoản dùng TaiKhoan.TrangThai = 0)
   - Tin nhắn khách <-> tài xế theo đơn hàng (bảng TinNhan)
   - Khuyến mãi: loại giảm (kể cả GIẢM PHÍ GIAO), giảm tối đa, đơn tối thiểu, số lượt,
     mỗi khách 1 lần, giới hạn món / danh mục / gian hàng
   - Giao hàng: bước tài xế, phụ cấp, điểm & nhận xét của khách cho tài xế, lý do thất bại, ảnh giao
   - Đơn hàng: SanSangGiao (quán đã làm xong, chờ tài xế); đánh giá: thẻ nhận xét
   - Thanh toán: cho phép ChuyenKhoan, ViDienTu (app gửi 2 giá trị này)

   Ánh xạ trạng thái đơn app <-> DB:
     ChoXacNhan -> ChoXacNhan | DangLam -> DaXacNhan (SanSangGiao = 0: đang chuẩn bị; =1 & MaTaiXe NULL: chờ tài xế)
     DangGiao -> DangGiao | HoanThanh -> DaGiao | DaHuy -> DaHuy
   Ánh xạ khuyến mãi: GiaTriGiam -> PhanTramGiam (LoaiGiam = PhanTram) hoặc SoTienGiam (SoTien / GiamPhiVanChuyen;
     GiamPhiVanChuyen với SoTienGiam NULL hoặc 0 = miễn phí giao hàng)
   ============================================================ */
USE QuanLyDichVuAnUong;
GO

/* ---- 10.1 TaiXe: hồ sơ xét duyệt ---- */
IF COL_LENGTH('TaiXe', 'SoCCCD') IS NULL     ALTER TABLE TaiXe ADD SoCCCD   VARCHAR(20)   NULL;
IF COL_LENGTH('TaiXe', 'SoGPLX') IS NULL     ALTER TABLE TaiXe ADD SoGPLX   VARCHAR(20)   NULL;
IF COL_LENGTH('TaiXe', 'AnhGPLX') IS NULL    ALTER TABLE TaiXe ADD AnhGPLX  NVARCHAR(500) NULL;
IF COL_LENGTH('TaiXe', 'LyDoTuChoi') IS NULL ALTER TABLE TaiXe ADD LyDoTuChoi NVARCHAR(255) NULL;
GO

/* ---- 10.2 DonHang / GiaoHang / DanhGia ---- */
IF COL_LENGTH('DonHang', 'SanSangGiao') IS NULL
    ALTER TABLE DonHang ADD SanSangGiao BIT NOT NULL CONSTRAINT DF_DonHang_SanSangGiao DEFAULT 0;
IF COL_LENGTH('DanhGia', 'TheNhanXet') IS NULL
    ALTER TABLE DanhGia ADD TheNhanXet NVARCHAR(300) NULL;      -- các thẻ khách chọn, ngăn cách bằng |
IF COL_LENGTH('GiaoHang', 'BuocTaiXe') IS NULL
    ALTER TABLE GiaoHang ADD BuocTaiXe INT NOT NULL CONSTRAINT DF_GiaoHang_BuocTaiXe DEFAULT 0;  -- 0 đang đến quán, 1 đang lấy hàng
IF COL_LENGTH('GiaoHang', 'PhuCap') IS NULL
    ALTER TABLE GiaoHang ADD PhuCap DECIMAL(12,0) NOT NULL CONSTRAINT DF_GiaoHang_PhuCap DEFAULT 0;
IF COL_LENGTH('GiaoHang', 'DiemTaiXe') IS NULL
    ALTER TABLE GiaoHang ADD DiemTaiXe INT NULL;                 -- khách chấm 1-5, NULL = chưa chấm
IF COL_LENGTH('GiaoHang', 'NhanXetTaiXe') IS NULL
    ALTER TABLE GiaoHang ADD NhanXetTaiXe NVARCHAR(500) NULL;
IF COL_LENGTH('GiaoHang', 'LyDoThatBai') IS NULL
    ALTER TABLE GiaoHang ADD LyDoThatBai NVARCHAR(255) NULL;
IF COL_LENGTH('GiaoHang', 'AnhGiaoHang') IS NULL
    ALTER TABLE GiaoHang ADD AnhGiaoHang NVARCHAR(500) NULL;     -- ảnh bằng chứng giao hàng
GO

IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CK_GiaoHang_DiemTaiXe')
    ALTER TABLE GiaoHang ADD CONSTRAINT CK_GiaoHang_DiemTaiXe CHECK (DiemTaiXe IS NULL OR DiemTaiXe BETWEEN 1 AND 5);
GO

/* ---- 10.3 ThanhToan: thêm ChuyenKhoan, ViDienTu (xóa CHECK cũ không đặt tên rồi tạo lại) ---- */
DECLARE @ck sysname;
SELECT @ck = cc.name FROM sys.check_constraints cc
WHERE cc.parent_object_id = OBJECT_ID('ThanhToan') AND cc.definition LIKE '%HinhThuc%' AND cc.name <> 'CK_ThanhToan_HinhThuc';
IF @ck IS NOT NULL EXEC('ALTER TABLE ThanhToan DROP CONSTRAINT ' + @ck);
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CK_ThanhToan_HinhThuc')
    ALTER TABLE ThanhToan ADD CONSTRAINT CK_ThanhToan_HinhThuc
        CHECK (HinhThuc IN (N'TienMat', N'ChuyenKhoan', N'ViDienTu', N'VNPay', N'Momo'));
GO

/* ---- 10.4 TinNhan ---- */
IF OBJECT_ID('TinNhan', 'U') IS NULL
BEGIN
    CREATE TABLE TinNhan (
        MaTinNhan   INT IDENTITY(1,1) PRIMARY KEY,
        MaDonHang   INT NOT NULL REFERENCES DonHang(MaDonHang),
        NguoiGui    NVARCHAR(20) NOT NULL CHECK (NguoiGui IN (N'KhachHang', N'TaiXe')),
        TenNguoiGui NVARCHAR(100) NULL,
        NoiDung     NVARCHAR(1000) NOT NULL,
        ThoiGian    DATETIME NOT NULL DEFAULT GETDATE()
    );
    CREATE INDEX IX_TinNhan_DonHang ON TinNhan (MaDonHang, ThoiGian);
END
GO

/* ---- 10.5 KhuyenMai: các cột mới ---- */
IF COL_LENGTH('KhuyenMai', 'TenChuongTrinh') IS NULL
    ALTER TABLE KhuyenMai ADD TenChuongTrinh NVARCHAR(150) NULL;
IF COL_LENGTH('KhuyenMai', 'LoaiGiam') IS NULL
    ALTER TABLE KhuyenMai ADD LoaiGiam NVARCHAR(20) NOT NULL CONSTRAINT DF_KhuyenMai_LoaiGiam DEFAULT N'PhanTram';
IF COL_LENGTH('KhuyenMai', 'GiamToiDa') IS NULL
    ALTER TABLE KhuyenMai ADD GiamToiDa DECIMAL(12,0) NULL;
IF COL_LENGTH('KhuyenMai', 'GiaTriDonToiThieu') IS NULL
    ALTER TABLE KhuyenMai ADD GiaTriDonToiThieu DECIMAL(12,0) NOT NULL CONSTRAINT DF_KhuyenMai_DonToiThieu DEFAULT 0;
IF COL_LENGTH('KhuyenMai', 'SoLuongToiDa') IS NULL
    ALTER TABLE KhuyenMai ADD SoLuongToiDa INT NULL;             -- NULL = không giới hạn lượt
IF COL_LENGTH('KhuyenMai', 'SoLuongDaDung') IS NULL
    ALTER TABLE KhuyenMai ADD SoLuongDaDung INT NOT NULL CONSTRAINT DF_KhuyenMai_DaDung DEFAULT 0;
IF COL_LENGTH('KhuyenMai', 'MoiKhachMotLan') IS NULL
    ALTER TABLE KhuyenMai ADD MoiKhachMotLan BIT NOT NULL CONSTRAINT DF_KhuyenMai_MotLan DEFAULT 0;
GO

IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CK_KhuyenMai_LoaiGiam')
    ALTER TABLE KhuyenMai ADD CONSTRAINT CK_KhuyenMai_LoaiGiam
        CHECK (LoaiGiam IN (N'PhanTram', N'SoTien', N'GiamPhiVanChuyen'));
GO

/* ---- 10.6 Khuyến mãi: món / danh mục / gian hàng áp dụng, khách đã dùng (rỗng = không giới hạn) ---- */
IF OBJECT_ID('KhuyenMaiMonAn', 'U') IS NULL
    CREATE TABLE KhuyenMaiMonAn (
        MaKhuyenMai INT NOT NULL REFERENCES KhuyenMai(MaKhuyenMai) ON DELETE CASCADE,
        MaMonAn     INT NOT NULL REFERENCES MonAn(MaMonAn),
        PRIMARY KEY (MaKhuyenMai, MaMonAn)
    );
IF OBJECT_ID('KhuyenMaiDanhMuc', 'U') IS NULL
    CREATE TABLE KhuyenMaiDanhMuc (
        MaKhuyenMai INT NOT NULL REFERENCES KhuyenMai(MaKhuyenMai) ON DELETE CASCADE,
        MaDanhMuc   INT NOT NULL REFERENCES DanhMuc(MaDanhMuc),
        PRIMARY KEY (MaKhuyenMai, MaDanhMuc)
    );
IF OBJECT_ID('KhuyenMaiGianHang', 'U') IS NULL
    CREATE TABLE KhuyenMaiGianHang (
        MaKhuyenMai INT NOT NULL REFERENCES KhuyenMai(MaKhuyenMai) ON DELETE CASCADE,
        MaGianHang  INT NOT NULL REFERENCES GianHang(MaGianHang),
        PRIMARY KEY (MaKhuyenMai, MaGianHang)
    );
IF OBJECT_ID('KhuyenMaiKhachDung', 'U') IS NULL
    CREATE TABLE KhuyenMaiKhachDung (
        MaKhuyenMai INT NOT NULL REFERENCES KhuyenMai(MaKhuyenMai) ON DELETE CASCADE,
        MaKH        INT NOT NULL REFERENCES KhachHang(MaKH),
        MaDonHang   INT NOT NULL REFERENCES DonHang(MaDonHang),
        NgaySuDung  DATETIME NOT NULL DEFAULT GETDATE(),
        PRIMARY KEY (MaKhuyenMai, MaKH, MaDonHang)
    );
GO

/* ---- 10.7 Chuẩn hóa dữ liệu mẫu cũ cho khớp cột mới (chạy lại vẫn an toàn) ---- */
UPDATE KhuyenMai SET TenChuongTrinh = MaCode WHERE TenChuongTrinh IS NULL;
UPDATE KhuyenMai SET LoaiGiam = N'SoTien'
 WHERE LoaiGiam = N'PhanTram' AND PhanTramGiam IS NULL AND SoTienGiam > 0;
UPDATE KhuyenMai SET SoLuongDaDung = x.SoLuot
FROM KhuyenMai k
JOIN (SELECT MaKhuyenMaiApDung AS Ma, COUNT(*) AS SoLuot FROM DonHang
      WHERE MaKhuyenMaiApDung IS NOT NULL AND TrangThai <> N'DaHuy' GROUP BY MaKhuyenMaiApDung) x ON x.Ma = k.MaKhuyenMai
WHERE k.SoLuongDaDung = 0;

-- Số CCCD/GPLX demo cho tài xế mẫu chưa có (chỉ để dữ liệu mẫu đầy đủ, không phải số thật)
UPDATE TaiXe SET SoCCCD = '079' + RIGHT('000000000' + CAST(MaTaiXe AS VARCHAR(9)), 9)
 WHERE SoCCCD IS NULL;
UPDATE TaiXe SET SoGPLX = '79' + RIGHT('0000000000' + CAST(MaTaiXe AS VARCHAR(10)), 10)
 WHERE SoGPLX IS NULL;

-- Mã mẫu "miễn phí giao hàng" (toàn hệ thống) để thử loại GiamPhiVanChuyen
IF NOT EXISTS (SELECT 1 FROM KhuyenMai WHERE MaCode = 'FREESHIP')
    INSERT INTO KhuyenMai (MaGianHang, MaMonAn, MaCode, MoTa, PhanTramGiam, SoTienGiam, DieuKienApDung,
                           NgayBatDau, NgayKetThuc, TrangThai,
                           TenChuongTrinh, LoaiGiam, GiaTriDonToiThieu, SoLuongToiDa, MoiKhachMotLan)
    VALUES (NULL, NULL, 'FREESHIP', N'Miễn phí giao hàng cho đơn từ 100.000đ', NULL, 0, N'Đơn từ 100.000đ',
            GETDATE(), DATEADD(MONTH, 3, GETDATE()), 1,
            N'Miễn phí giao hàng', N'GiamPhiVanChuyen', 100000, 500, 1);
GO

/* ============================================================
   PHẦN 12: BỔ SUNG HẠ TẦNG BACKEND (chạy được NHIỀU LẦN, an toàn)
   12.1 TaiKhoan: khóa tạm khi sai mật khẩu nhiều lần, xác thực email/SĐT
   12.2 Cột còn thiếu: GianHang.Email, ảnh đại diện, DonHang.TienGiam/HenGiaoLuc, PhanHoi.DangXuLy
   12.3 Bảng mới: RefreshToken, OtpXacThuc, NhatKyHeThong, AuditLog, ThongBao, TepTin
   12.4 Chỉ mục + FULLTEXT (tìm kiếm nhanh)
   12.5 Trigger audit: ghi ai/khi nào/đổi gì ở các bảng quan trọng (ghi đè JSON cũ/mới, KHÔNG ghi mật khẩu)
   12.6 sp_DonDepDuLieu: dọn token/OTP/nhật ký quá hạn
   Việc sao lưu: xem SQL_NangCap/SaoLuu_PhucHoi.sql
   ============================================================ */
USE QuanLyDichVuAnUong;
GO

/* ---- 12.1 TaiKhoan ---- */
IF COL_LENGTH('TaiKhoan', 'SoLanDangNhapSai') IS NULL
    ALTER TABLE TaiKhoan ADD SoLanDangNhapSai INT NOT NULL CONSTRAINT DF_TaiKhoan_SaiLan DEFAULT 0;
IF COL_LENGTH('TaiKhoan', 'KhoaDen') IS NULL
    ALTER TABLE TaiKhoan ADD KhoaDen DATETIME NULL;                 -- khóa tạm tới thời điểm này
IF COL_LENGTH('TaiKhoan', 'LanDangNhapCuoi') IS NULL
    ALTER TABLE TaiKhoan ADD LanDangNhapCuoi DATETIME NULL;
IF COL_LENGTH('TaiKhoan', 'EmailDaXacThuc') IS NULL
    ALTER TABLE TaiKhoan ADD EmailDaXacThuc BIT NOT NULL CONSTRAINT DF_TaiKhoan_EmailXT DEFAULT 0;
IF COL_LENGTH('TaiKhoan', 'SdtDaXacThuc') IS NULL
    ALTER TABLE TaiKhoan ADD SdtDaXacThuc BIT NOT NULL CONSTRAINT DF_TaiKhoan_SdtXT DEFAULT 0;
GO

/* ---- 12.2 Cột còn thiếu ---- */
IF COL_LENGTH('GianHang', 'Email') IS NULL      ALTER TABLE GianHang ADD Email VARCHAR(100) NULL;
IF COL_LENGTH('KhachHang', 'AnhDaiDien') IS NULL ALTER TABLE KhachHang ADD AnhDaiDien NVARCHAR(500) NULL;
IF COL_LENGTH('TaiXe', 'AnhDaiDien') IS NULL     ALTER TABLE TaiXe ADD AnhDaiDien NVARCHAR(500) NULL;
IF COL_LENGTH('DonHang', 'TienGiam') IS NULL
    ALTER TABLE DonHang ADD TienGiam DECIMAL(12,0) NOT NULL CONSTRAINT DF_DonHang_TienGiam DEFAULT 0;  -- đã trừ khỏi TongTien
IF COL_LENGTH('DonHang', 'HenGiaoLuc') IS NULL   ALTER TABLE DonHang ADD HenGiaoLuc DATETIME NULL;      -- đơn hẹn giờ
GO

/* PhanHoi.TrangThai: thêm 'DangXuLy' (app có 3 trạng thái: mới / đang xử lý / đã giải quyết) */
DECLARE @ck sysname;
SELECT @ck = cc.name FROM sys.check_constraints cc
WHERE cc.parent_object_id = OBJECT_ID('PhanHoi') AND cc.definition LIKE '%ChuaXuLy%' AND cc.name <> 'CK_PhanHoi_TrangThai';
IF @ck IS NOT NULL EXEC('ALTER TABLE PhanHoi DROP CONSTRAINT ' + @ck);
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CK_PhanHoi_TrangThai')
    ALTER TABLE PhanHoi ADD CONSTRAINT CK_PhanHoi_TrangThai CHECK (TrangThai IN (N'ChuaXuLy', N'DangXuLy', N'DaXuLy'));
GO

/* Đồng bộ dữ liệu mẫu với app: quản trị mẫu đăng nhập bằng admin@fooddelivery.vn; gian hàng mẫu có email */
UPDATE QuanTri SET Email = 'admin@fooddelivery.vn'
WHERE MaTK = (SELECT MaTK FROM TaiKhoan WHERE TenDangNhap = N'quantri01')
  AND NOT EXISTS (SELECT 1 FROM QuanTri WHERE Email = 'admin@fooddelivery.vn');
UPDATE gh SET Email = LOWER(tk.TenDangNhap) + '@foodapp.vn'
FROM GianHang gh JOIN TaiKhoan tk ON tk.MaTK = gh.MaTK WHERE gh.Email IS NULL AND LEN(tk.TenDangNhap) <= 80;
GO

/* ---- 12.3 Bảng mới ---- */
IF OBJECT_ID('RefreshToken', 'U') IS NULL
BEGIN
    CREATE TABLE RefreshToken (
        MaRT        BIGINT IDENTITY(1,1) PRIMARY KEY,
        MaTK        INT NOT NULL REFERENCES TaiKhoan(MaTK) ON DELETE CASCADE,
        TokenHash   CHAR(64) NOT NULL UNIQUE,          -- SHA-256 của token; KHÔNG lưu token gốc
        NgayTao     DATETIME NOT NULL DEFAULT GETDATE(),
        HetHan      DATETIME NOT NULL,
        ThuHoiLuc   DATETIME NULL,
        ThayBangHash CHAR(64) NULL,                    -- token mới thay thế (xoay vòng)
        DiaChiIP    NVARCHAR(45) NULL,
        ThietBi     NVARCHAR(200) NULL
    );
    CREATE INDEX IX_RefreshToken_MaTK ON RefreshToken (MaTK, HetHan);
END
GO

IF OBJECT_ID('OtpXacThuc', 'U') IS NULL
BEGIN
    CREATE TABLE OtpXacThuc (
        MaOTP       BIGINT IDENTITY(1,1) PRIMARY KEY,
        MucDich     NVARCHAR(30) NOT NULL CHECK (MucDich IN (N'DangKy', N'QuenMatKhau', N'DoiLienHe', N'XacNhan')),
        Kenh        VARCHAR(10)  NOT NULL CHECK (Kenh IN ('email', 'sms')),
        NguoiNhan   NVARCHAR(150) NOT NULL,
        MaTK        INT NULL REFERENCES TaiKhoan(MaTK),
        OtpHash     CHAR(64) NOT NULL,                  -- HMAC-SHA256, không lưu mã gốc
        NgayTao     DATETIME NOT NULL DEFAULT GETDATE(),
        HetHan      DATETIME NOT NULL,
        SoLanSai    INT NOT NULL DEFAULT 0,
        DaDung      BIT NOT NULL DEFAULT 0
    );
    CREATE INDEX IX_Otp_NguoiNhan ON OtpXacThuc (NguoiNhan, MucDich, NgayTao DESC);
END
GO

IF OBJECT_ID('NhatKyHeThong', 'U') IS NULL
BEGIN
    CREATE TABLE NhatKyHeThong (
        MaLog       BIGINT IDENTITY(1,1) PRIMARY KEY,
        ThoiGian    DATETIME2(3) NOT NULL DEFAULT SYSDATETIME(),
        MucDo       VARCHAR(10)  NOT NULL CHECK (MucDo IN ('Info', 'Warn', 'Error')),
        Nguon       NVARCHAR(30) NOT NULL,              -- Http | Auth | Loi | Upload | Otp | Sao luu ...
        ThongDiep   NVARCHAR(500) NOT NULL,
        ChiTiet     NVARCHAR(MAX) NULL,                 -- stack trace (chỉ khi Error)
        MaTK        INT NULL,
        DiaChiIP    NVARCHAR(45) NULL,
        PhuongThuc  VARCHAR(10) NULL,
        DuongDan    NVARCHAR(300) NULL,
        MaTrangThai INT NULL,
        ThoiGianMs  INT NULL
    );
    CREATE INDEX IX_NhatKy_ThoiGian ON NhatKyHeThong (ThoiGian DESC);
    CREATE INDEX IX_NhatKy_MucDo ON NhatKyHeThong (MucDo, ThoiGian DESC);
END
GO

IF OBJECT_ID('AuditLog', 'U') IS NULL
BEGIN
    CREATE TABLE AuditLog (
        MaAudit     BIGINT IDENTITY(1,1) PRIMARY KEY,
        ThoiGian    DATETIME2(3) NOT NULL DEFAULT SYSDATETIME(),
        MaTK        INT NULL,                           -- người thao tác (từ SESSION_CONTEXT, NULL = thao tác trực tiếp trên DB)
        HanhDong    NVARCHAR(10) NOT NULL,              -- INSERT | UPDATE | DELETE
        Bang        NVARCHAR(60) NOT NULL,
        KhoaChinh   NVARCHAR(50) NOT NULL,
        GiaTriCu    NVARCHAR(MAX) NULL,                 -- JSON
        GiaTriMoi   NVARCHAR(MAX) NULL                  -- JSON
    );
    CREATE INDEX IX_Audit_Bang ON AuditLog (Bang, KhoaChinh, ThoiGian DESC);
    CREATE INDEX IX_Audit_MaTK ON AuditLog (MaTK, ThoiGian DESC);
END
GO

IF OBJECT_ID('ThongBao', 'U') IS NULL
BEGIN
    CREATE TABLE ThongBao (
        MaThongBao  BIGINT IDENTITY(1,1) PRIMARY KEY,
        MaTK        INT NOT NULL REFERENCES TaiKhoan(MaTK) ON DELETE CASCADE,
        TieuDe      NVARCHAR(150) NOT NULL,
        NoiDung     NVARCHAR(500) NOT NULL,
        Loai        NVARCHAR(20) NOT NULL DEFAULT N'HeThong' CHECK (Loai IN (N'HeThong', N'DonHang', N'KhuyenMai', N'TinNhan')),
        MaThamChieu NVARCHAR(50) NULL,                  -- ví dụ Id đơn hàng
        DaDoc       BIT NOT NULL DEFAULT 0,
        NgayTao     DATETIME NOT NULL DEFAULT GETDATE()
    );
    CREATE INDEX IX_ThongBao_MaTK ON ThongBao (MaTK, DaDoc, NgayTao DESC);
END
GO

IF OBJECT_ID('TepTin', 'U') IS NULL
BEGIN
    CREATE TABLE TepTin (
        MaTep       BIGINT IDENTITY(1,1) PRIMARY KEY,
        MaTK        INT NULL REFERENCES TaiKhoan(MaTK),
        Loai        NVARCHAR(20) NOT NULL CHECK (Loai IN (N'MonAn', N'GianHang', N'AvatarKhach', N'AvatarTaiXe', N'GPLX', N'GiaoHang')),
        TenGoc      NVARCHAR(200) NULL,
        DuongDan    NVARCHAR(400) NOT NULL,             -- đường dẫn tương đối trong thư mục lưu trữ
        MimeType    VARCHAR(50) NOT NULL,
        KichThuoc   BIGINT NOT NULL,
        Sha256      CHAR(64) NOT NULL,
        RiengTu     BIT NOT NULL DEFAULT 0,             -- 1 = chỉ chủ sở hữu/quản trị xem (GPLX)
        NgayTai     DATETIME NOT NULL DEFAULT GETDATE()
    );
    CREATE INDEX IX_TepTin_MaTK ON TepTin (MaTK, Loai);
END
GO

/* ---- 12.4 Chỉ mục ---- */
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_DonHang_KhachHang')
    CREATE INDEX IX_DonHang_KhachHang ON DonHang (MaKH, NgayDat DESC);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_DonHang_GianHang')
    CREATE INDEX IX_DonHang_GianHang ON DonHang (MaGianHang, TrangThai, NgayDat DESC);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_DonHang_TaiXe')
    CREATE INDEX IX_DonHang_TaiXe ON DonHang (MaTaiXe, TrangThai);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_DonHang_ChoTaiXe')
    CREATE INDEX IX_DonHang_ChoTaiXe ON DonHang (TrangThai, SanSangGiao, MaTaiXe) WHERE MaTaiXe IS NULL;
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_ChiTiet_DonHang')
    CREATE INDEX IX_ChiTiet_DonHang ON ChiTietDonHang (MaDonHang) INCLUDE (MaMonAn, SoLuong, DonGia);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_MonAn_DanhMuc')
    CREATE INDEX IX_MonAn_DanhMuc ON MonAn (MaDanhMuc, TinhTrang);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_DanhMuc_GianHang')
    CREATE INDEX IX_DanhMuc_GianHang ON DanhMuc (MaGianHang);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_GianHang_Duyet')
    CREATE INDEX IX_GianHang_Duyet ON GianHang (TrangThaiDuyet, TrangThaiKinhDoanh);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_GianHang_Email')
    CREATE INDEX IX_GianHang_Email ON GianHang (Email) WHERE Email IS NOT NULL;
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_GianHang_SDT')
    CREATE INDEX IX_GianHang_SDT ON GianHang (SDT);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_KhachHang_Email')
    CREATE INDEX IX_KhachHang_Email ON KhachHang (Email) WHERE Email IS NOT NULL;
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_KhachHang_SDT')
    CREATE INDEX IX_KhachHang_SDT ON KhachHang (SDT);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_TaiXe_SDT')
    CREATE INDEX IX_TaiXe_SDT ON TaiXe (SDT);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_GiaoHang_TaiXe')
    CREATE INDEX IX_GiaoHang_TaiXe ON GiaoHang (MaTaiXe, TrangThai);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_DanhGia_GianHang')
    CREATE INDEX IX_DanhGia_GianHang ON DanhGia (MaGianHang) INCLUDE (SoSao);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_KhuyenMai_Hieu')
    CREATE INDEX IX_KhuyenMai_Hieu ON KhuyenMai (TrangThai, NgayKetThuc);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_PhanHoi_TrangThai')
    CREATE INDEX IX_PhanHoi_TrangThai ON PhanHoi (TrangThai, NgayGui DESC);
GO

/* FULLTEXT cho tìm kiếm tên quán / món (cần cài tính năng Full-Text; không có thì bỏ qua, server tự dùng LIKE) */
IF FULLTEXTSERVICEPROPERTY('IsFullTextInstalled') = 1
BEGIN
    BEGIN TRY
        IF NOT EXISTS (SELECT 1 FROM sys.fulltext_catalogs WHERE name = 'ftc_FoodService')
            EXEC('CREATE FULLTEXT CATALOG ftc_FoodService');

        DECLARE @pk sysname, @s NVARCHAR(500);
        IF NOT EXISTS (SELECT 1 FROM sys.fulltext_indexes WHERE object_id = OBJECT_ID('MonAn'))
        BEGIN
            SELECT @pk = name FROM sys.indexes WHERE object_id = OBJECT_ID('MonAn') AND is_primary_key = 1;
            SET @s = N'CREATE FULLTEXT INDEX ON MonAn (TenMon LANGUAGE 0, MoTa LANGUAGE 0) KEY INDEX ' + QUOTENAME(@pk) + N' ON ftc_FoodService WITH CHANGE_TRACKING AUTO';
            EXEC(@s);
        END
        IF NOT EXISTS (SELECT 1 FROM sys.fulltext_indexes WHERE object_id = OBJECT_ID('GianHang'))
        BEGIN
            SELECT @pk = name FROM sys.indexes WHERE object_id = OBJECT_ID('GianHang') AND is_primary_key = 1;
            SET @s = N'CREATE FULLTEXT INDEX ON GianHang (TenCuaHang LANGUAGE 0, MoTa LANGUAGE 0) KEY INDEX ' + QUOTENAME(@pk) + N' ON ftc_FoodService WITH CHANGE_TRACKING AUTO';
            EXEC(@s);
        END
    END TRY
    BEGIN CATCH
        PRINT N'Bỏ qua FULLTEXT: ' + ERROR_MESSAGE();
    END CATCH
END
ELSE PRINT N'Full-Text chưa được cài: server sẽ tìm kiếm bằng LIKE.';
GO

/* ---- 12.5 Trigger audit ---- */
-- Bảng thường: ghi JSON giá trị cũ/mới của cả dòng. Người thao tác lấy từ SESSION_CONTEXT('MaTK') do server đặt.
DECLARE @bang TABLE (Ten SYSNAME, Pk SYSNAME);
INSERT INTO @bang VALUES (N'GianHang', N'MaGianHang'), (N'TaiXe', N'MaTaiXe'), (N'KhuyenMai', N'MaKhuyenMai'),
                         (N'MonAn', N'MaMonAn'), (N'DonHang', N'MaDonHang'), (N'ThanhToan', N'MaThanhToan'),
                         (N'PhanHoi', N'MaPhanHoi'), (N'KhachHang', N'MaKH');
DECLARE @t SYSNAME, @key SYSNAME, @ci NVARCHAR(MAX), @cd NVARCHAR(MAX), @sql NVARCHAR(MAX);
DECLARE cur CURSOR LOCAL FAST_FORWARD FOR SELECT Ten, Pk FROM @bang;
OPEN cur;
FETCH NEXT FROM cur INTO @t, @key;
WHILE @@FETCH_STATUS = 0
BEGIN
    SELECT @ci = STRING_AGG(CAST(N'i.' + QUOTENAME(COLUMN_NAME) AS NVARCHAR(MAX)), N',') WITHIN GROUP (ORDER BY ORDINAL_POSITION),
           @cd = STRING_AGG(CAST(N'd.' + QUOTENAME(COLUMN_NAME) AS NVARCHAR(MAX)), N',') WITHIN GROUP (ORDER BY ORDINAL_POSITION)
    FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = @t;

    SET @sql = N'CREATE OR ALTER TRIGGER ' + QUOTENAME(N'trg_Audit_' + @t) + N' ON ' + QUOTENAME(@t) + N' AFTER INSERT, UPDATE, DELETE AS
BEGIN
    SET NOCOUNT ON;
    IF NOT EXISTS (SELECT 1 FROM inserted) AND NOT EXISTS (SELECT 1 FROM deleted) RETURN;
    DECLARE @tk INT = TRY_CAST(SESSION_CONTEXT(N''MaTK'') AS INT);
    INSERT INTO AuditLog (MaTK, HanhDong, Bang, KhoaChinh, GiaTriCu, GiaTriMoi)
    SELECT @tk,
           CASE WHEN d.' + QUOTENAME(@key) + N' IS NULL THEN N''INSERT'' WHEN i.' + QUOTENAME(@key) + N' IS NULL THEN N''DELETE'' ELSE N''UPDATE'' END,
           N''' + @t + N''',
           CAST(COALESCE(i.' + QUOTENAME(@key) + N', d.' + QUOTENAME(@key) + N') AS NVARCHAR(50)),
           CASE WHEN d.' + QUOTENAME(@key) + N' IS NULL THEN NULL ELSE (SELECT ' + @cd + N' FOR JSON PATH, WITHOUT_ARRAY_WRAPPER, INCLUDE_NULL_VALUES) END,
           CASE WHEN i.' + QUOTENAME(@key) + N' IS NULL THEN NULL ELSE (SELECT ' + @ci + N' FOR JSON PATH, WITHOUT_ARRAY_WRAPPER, INCLUDE_NULL_VALUES) END
    FROM inserted i FULL OUTER JOIN deleted d ON i.' + QUOTENAME(@key) + N' = d.' + QUOTENAME(@key) + N';
END';
    EXEC(@sql);
    FETCH NEXT FROM cur INTO @t, @key;
END
CLOSE cur; DEALLOCATE cur;
GO

-- TaiKhoan: không ghi mật khẩu; chỉ ghi khi đổi trạng thái / vai trò / tên đăng nhập / mật khẩu (chỉ cờ DoiMatKhau)
CREATE OR ALTER TRIGGER trg_Audit_TaiKhoan ON TaiKhoan AFTER INSERT, UPDATE, DELETE AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @tk INT = TRY_CAST(SESSION_CONTEXT(N'MaTK') AS INT);
    INSERT INTO AuditLog (MaTK, HanhDong, Bang, KhoaChinh, GiaTriCu, GiaTriMoi)
    SELECT @tk,
           CASE WHEN d.MaTK IS NULL THEN N'INSERT' WHEN i.MaTK IS NULL THEN N'DELETE' ELSE N'UPDATE' END,
           N'TaiKhoan', CAST(COALESCE(i.MaTK, d.MaTK) AS NVARCHAR(50)),
           CASE WHEN d.MaTK IS NULL THEN NULL
                ELSE (SELECT d.TenDangNhap, d.VaiTro, d.TrangThai FOR JSON PATH, WITHOUT_ARRAY_WRAPPER, INCLUDE_NULL_VALUES) END,
           CASE WHEN i.MaTK IS NULL THEN NULL
                ELSE (SELECT i.TenDangNhap, i.VaiTro, i.TrangThai,
                             CAST(CASE WHEN d.MaTK IS NOT NULL AND i.MatKhau <> d.MatKhau THEN 1 ELSE 0 END AS BIT) AS DoiMatKhau
                      FOR JSON PATH, WITHOUT_ARRAY_WRAPPER, INCLUDE_NULL_VALUES) END
    FROM inserted i FULL OUTER JOIN deleted d ON i.MaTK = d.MaTK
    WHERE d.MaTK IS NULL OR i.MaTK IS NULL
       OR i.TrangThai <> d.TrangThai OR i.VaiTro <> d.VaiTro OR i.TenDangNhap <> d.TenDangNhap OR i.MatKhau <> d.MatKhau;
END
GO

/* ---- 12.6 Dọn dữ liệu quá hạn ---- */
CREATE OR ALTER PROCEDURE sp_DonDepDuLieu
    @GiuNhatKyNgay INT = 90,
    @GiuAuditNgay  INT = 365
AS
BEGIN
    SET NOCOUNT ON;
    DELETE FROM RefreshToken WHERE HetHan < DATEADD(DAY, -7, GETDATE()) OR (ThuHoiLuc IS NOT NULL AND ThuHoiLuc < DATEADD(DAY, -7, GETDATE()));
    DELETE FROM OtpXacThuc   WHERE NgayTao < DATEADD(DAY, -1, GETDATE());
    DELETE FROM NhatKyHeThong WHERE ThoiGian < DATEADD(DAY, -@GiuNhatKyNgay, GETDATE());
    DELETE FROM AuditLog     WHERE ThoiGian < DATEADD(DAY, -@GiuAuditNgay, GETDATE());
    DELETE FROM ThongBao     WHERE DaDoc = 1 AND NgayTao < DATEADD(DAY, -60, GETDATE());
END
GO

/* ============================================================
   PHẦN 13: TRUY VẤN KIỂM TRA SAU KHI TẠO (gộp từ 03_BoSung.sql, 04_table.sql)
   ============================================================ */
USE QuanLyDichVuAnUong;
GO

-- Đếm nhanh số gian hàng / món ăn đã sinh
SELECT COUNT(*) FROM GianHang;
SELECT COUNT(*) FROM MonAn;
GO

-- Xem cấu trúc cột của toàn bộ bảng
SELECT TABLE_NAME, COLUMN_NAME, DATA_TYPE
FROM INFORMATION_SCHEMA.COLUMNS
ORDER BY TABLE_NAME, ORDINAL_POSITION;
GO

GO

/* ============================================================
   PHẦN 14: BỔ SUNG ĐỐI TÁC / GIAN HÀNG (cùng nội dung 04_BoSung_DoiTac.sql — chạy lại an toàn)
   ============================================================ */
USE QuanLyDichVuAnUong;
GO
/* ---- 1. GianHang: giờ hoạt động, nhận đơn, chiết khấu ---- */
IF COL_LENGTH('GianHang', 'GioMoCua') IS NULL
    ALTER TABLE GianHang ADD GioMoCua TIME(0) NOT NULL CONSTRAINT DF_GianHang_GioMoCua DEFAULT '00:00';
GO
IF COL_LENGTH('GianHang', 'GioDongCua') IS NULL
    ALTER TABLE GianHang ADD GioDongCua TIME(0) NOT NULL CONSTRAINT DF_GianHang_GioDongCua DEFAULT '23:59';   -- 00:00–23:59 = hoạt động cả ngày
GO
IF COL_LENGTH('GianHang', 'DangNhanDon') IS NULL
    ALTER TABLE GianHang ADD DangNhanDon BIT NOT NULL CONSTRAINT DF_GianHang_DangNhanDon DEFAULT 1;           -- tạm dừng nhận đơn nhưng vẫn mở cửa
GO
IF COL_LENGTH('GianHang', 'TyLeChietKhau') IS NULL
    ALTER TABLE GianHang ADD TyLeChietKhau DECIMAL(5,4) NOT NULL CONSTRAINT DF_GianHang_TyLeChietKhau DEFAULT 0.15;   -- 0.15 = 15% (NV09: thỏa thuận lúc đăng ký)
GO

/* ---- 2. DanhMuc: thứ tự hiển thị ---- */
IF COL_LENGTH('DanhMuc', 'ThuTu') IS NULL
    ALTER TABLE DanhMuc ADD ThuTu INT NOT NULL CONSTRAINT DF_DanhMuc_ThuTu DEFAULT 0;
GO
-- Đánh số thứ tự ban đầu theo mã danh mục cho mỗi gian hàng (chỉ khi chưa có thứ tự nào)
;WITH x AS (
    SELECT MaDanhMuc, ROW_NUMBER() OVER (PARTITION BY MaGianHang ORDER BY MaDanhMuc) AS stt
    FROM DanhMuc
)
UPDATE d SET d.ThuTu = x.stt
FROM DanhMuc d JOIN x ON x.MaDanhMuc = d.MaDanhMuc
WHERE NOT EXISTS (SELECT 1 FROM DanhMuc t WHERE t.MaGianHang = d.MaGianHang AND t.ThuTu <> 0);
GO

/* ---- 3. MonAn: tạm ngừng bán, giá vốn, cho ghi chú ---- */
IF COL_LENGTH('MonAn', 'GiaVon') IS NULL
    ALTER TABLE MonAn ADD GiaVon DECIMAL(12,0) NOT NULL CONSTRAINT DF_MonAn_GiaVon DEFAULT 0
        CONSTRAINT CK_MonAn_GiaVon CHECK (GiaVon >= 0);
GO
IF COL_LENGTH('MonAn', 'ChoGhiChu') IS NULL
    ALTER TABLE MonAn ADD ChoGhiChu BIT NOT NULL CONSTRAINT DF_MonAn_ChoGhiChu DEFAULT 1;
GO
-- Mở rộng CHECK của TinhTrang: thêm 'TamNgung' (tạm ngừng bán)
DECLARE @ck sysname;
SELECT @ck = cc.name
FROM sys.check_constraints cc
JOIN sys.columns c ON c.object_id = cc.parent_object_id AND c.column_id = cc.parent_column_id
WHERE cc.parent_object_id = OBJECT_ID('MonAn') AND c.name = 'TinhTrang';
IF @ck IS NOT NULL EXEC('ALTER TABLE MonAn DROP CONSTRAINT ' + @ck);
ALTER TABLE MonAn ADD CONSTRAINT CK_MonAn_TinhTrang
    CHECK (TinhTrang IN (N'ConHang', N'HetHang', N'TamNgung'));
GO

/* ---- 4. TuyChonMon: size / topping / mức cay của từng món ---- */
IF OBJECT_ID('TuyChonMon', 'U') IS NULL
CREATE TABLE TuyChonMon (
    MaTuyChon   INT IDENTITY(1,1) PRIMARY KEY,
    MaMonAn     INT NOT NULL REFERENCES MonAn(MaMonAn) ON DELETE CASCADE,
    Loai        NVARCHAR(10)  NOT NULL CHECK (Loai IN (N'Size', N'Topping', N'MucCay')),
    TenTuyChon  NVARCHAR(100) NOT NULL,
    GiaThem     DECIMAL(12,0) NOT NULL DEFAULT 0 CHECK (GiaThem >= 0),
    CONSTRAINT UQ_TuyChonMon UNIQUE (MaMonAn, Loai, TenTuyChon)
);
GO

/* ---- 5. DonHang: lý do từ chối / hủy (NV04: từ chối bắt buộc có lý do) ---- */
IF COL_LENGTH('DonHang', 'LyDoTuChoi') IS NULL
    ALTER TABLE DonHang ADD LyDoTuChoi NVARCHAR(255) NULL;
GO

/* ---- 6. DanhGia: đánh giá giao hàng, thẻ nhanh, phản hồi của gian hàng ---- */
IF COL_LENGTH('DanhGia', 'SoSaoGiaoHang') IS NULL
    ALTER TABLE DanhGia ADD SoSaoGiaoHang INT NULL CONSTRAINT CK_DanhGia_SaoGiao CHECK (SoSaoGiaoHang BETWEEN 1 AND 5);
GO
IF COL_LENGTH('DanhGia', 'TheNhanXet') IS NULL
    ALTER TABLE DanhGia ADD TheNhanXet NVARCHAR(200) NULL;          -- "Món ngon;Đóng gói đẹp"
GO
IF COL_LENGTH('DanhGia', 'PhanHoi') IS NULL
    ALTER TABLE DanhGia ADD PhanHoi NVARCHAR(500) NULL;
GO
IF COL_LENGTH('DanhGia', 'NgayPhanHoi') IS NULL
    ALTER TABLE DanhGia ADD NgayPhanHoi DATETIME NULL;
GO

/* ---- 7. Thông báo cho gian hàng: dùng bảng ThongBao (theo MaTK) của NangCap_v9_Backend.sql — không tạo bảng riêng ---- */

/* ---- Kiểm tra nhanh ---- */
SELECT N'GianHang' AS Bang, name FROM sys.columns WHERE object_id = OBJECT_ID('GianHang') AND name IN ('GioMoCua','GioDongCua','DangNhanDon','TyLeChietKhau')
UNION ALL SELECT N'MonAn', name FROM sys.columns WHERE object_id = OBJECT_ID('MonAn') AND name IN ('GiaVon','ChoGhiChu')
UNION ALL SELECT N'DanhGia', name FROM sys.columns WHERE object_id = OBJECT_ID('DanhGia') AND name IN ('SoSaoGiaoHang','TheNhanXet','PhanHoi','NgayPhanHoi')
UNION ALL SELECT N'Bang moi', name FROM sys.tables WHERE name IN ('TuyChonMon');
GO
GO

/* ============================================================
   PHẦN 15: GIẤY TỜ GIAN HÀNG + GIỎ HÀNG TRÊN SERVER (cùng nội dung NangCap_v10_TaiKhoan_GioHang.sql)
   ============================================================ */
USE QuanLyDichVuAnUong;
GO

IF COL_LENGTH('GianHang', 'GiayPhepKinhDoanh') IS NULL ALTER TABLE GianHang ADD GiayPhepKinhDoanh NVARCHAR(500) NULL;
IF COL_LENGTH('GianHang', 'GiayChungNhanATTP') IS NULL ALTER TABLE GianHang ADD GiayChungNhanATTP NVARCHAR(500) NULL;
GO

IF OBJECT_ID('GioHang', 'U') IS NULL
BEGIN
    CREATE TABLE GioHang (
        MaGioHang   INT IDENTITY(1,1) PRIMARY KEY,
        MaKH        INT NOT NULL UNIQUE REFERENCES KhachHang(MaKH),       -- UNIQUE: mỗi khách đúng một giỏ
        MaGianHang  INT NOT NULL REFERENCES GianHang(MaGianHang),
        NgayCapNhat DATETIME NOT NULL DEFAULT GETDATE()
    );
END
GO

IF OBJECT_ID('GioHangChiTiet', 'U') IS NULL
BEGIN
    CREATE TABLE GioHangChiTiet (
        MaChiTiet   INT IDENTITY(1,1) PRIMARY KEY,
        MaGioHang   INT NOT NULL REFERENCES GioHang(MaGioHang) ON DELETE CASCADE,
        MaMonAn     INT NOT NULL REFERENCES MonAn(MaMonAn),
        SoLuong     INT NOT NULL CHECK (SoLuong BETWEEN 1 AND 99),
        GhiChu      NVARCHAR(255) NULL,
        TuyChon     NVARCHAR(MAX) NULL                                    -- JSON: ["Lớn","Trân châu"]
    );
    CREATE INDEX IX_GioHangChiTiet_GioHang ON GioHangChiTiet(MaGioHang);
END
-- Giá tiền KHÔNG lưu trong giỏ: luôn lấy MonAn.Gia hiện tại. API kiểm tra mọi món thuộc đúng gian hàng khi PUT giỏ.
GO
GO

/* ============================================================
   PHẦN 16: CHỨC NĂNG KHÁCH HÀNG v11 (cùng nội dung NangCap_v11_KhachHang.sql)
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

/* ========================================================================
   PHẦN 17: CHỨC NĂNG QUẢN TRỊ v12 (cùng nội dung NangCap_v12_QuanTri.sql)
   ======================================================================== */

IF OBJECT_ID('LichSuTrangThaiDon', 'U') IS NULL
    CREATE TABLE LichSuTrangThaiDon (
        MaLichSu   BIGINT IDENTITY(1,1) PRIMARY KEY,
        MaDonHang  INT NOT NULL REFERENCES DonHang(MaDonHang) ON DELETE CASCADE,
        TuTrangThai NVARCHAR(30) NULL,
        DenTrangThai NVARCHAR(30) NOT NULL,
        LyDo       NVARCHAR(300) NULL,
        MaTK       INT NULL,
        ThoiGian   DATETIME NOT NULL DEFAULT GETDATE()
    );
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_LichSuTrangThaiDon' AND object_id = OBJECT_ID('LichSuTrangThaiDon'))
    CREATE INDEX IX_LichSuTrangThaiDon ON LichSuTrangThaiDon(MaDonHang, ThoiGian);
GO

IF OBJECT_ID('YeuCauHoanTien', 'U') IS NULL
    CREATE TABLE YeuCauHoanTien (
        MaYeuCau   INT IDENTITY(1,1) PRIMARY KEY,
        MaDonHang  INT NOT NULL REFERENCES DonHang(MaDonHang) ON DELETE CASCADE,
        MaKH       INT NULL,
        SoTien     DECIMAL(12,0) NOT NULL CHECK (SoTien >= 0),
        LyDo       NVARCHAR(400) NOT NULL,
        Nguon      NVARCHAR(20) NOT NULL DEFAULT N'KhachHang',     -- KhachHang | QuanTri | KhieuNai
        TrangThai  NVARCHAR(15) NOT NULL DEFAULT N'ChoDuyet' CHECK (TrangThai IN (N'ChoDuyet', N'DaDuyet', N'TuChoi')),
        MaTKXuLy   INT NULL,
        KetQua     NVARCHAR(400) NULL,
        NgayTao    DATETIME NOT NULL DEFAULT GETDATE(),
        NgayXuLy   DATETIME NULL
    );
GO

IF OBJECT_ID('CauHinhHeThong', 'U') IS NULL
    CREATE TABLE CauHinhHeThong (
        Khoa      NVARCHAR(60) NOT NULL PRIMARY KEY,
        GiaTri    NVARCHAR(300) NOT NULL,
        MoTa      NVARCHAR(300) NULL,
        MaTKSua   INT NULL,
        NgaySua   DATETIME NOT NULL DEFAULT GETDATE()
    );
GO
IF OBJECT_ID('CauHinhHeThongLichSu', 'U') IS NULL
    CREATE TABLE CauHinhHeThongLichSu (
        MaLichSu  BIGINT IDENTITY(1,1) PRIMARY KEY,
        Khoa      NVARCHAR(60) NOT NULL,
        GiaTriCu  NVARCHAR(300) NULL,
        GiaTriMoi NVARCHAR(300) NOT NULL,
        MaTK      INT NULL,
        ThoiGian  DATETIME NOT NULL DEFAULT GETDATE()
    );
GO
MERGE CauHinhHeThong AS t
USING (VALUES
  (N'Giao.PhiMoiKm',        N'5000',   N'Phí giao hàng cho mỗi km (đ)'),
  (N'Giao.PhiMacDinh',      N'15000',  N'Phí giao mặc định khi không xác định được khoảng cách (đ)'),
  (N'Giao.BanKinhKm',       N'20',     N'Bán kính phục vụ tối đa (km)'),
  (N'HoaHong.MacDinh',      N'0.15',   N'Phí nền tảng/hoa hồng mặc định cho gian hàng mới (0.15 = 15%)'),
  (N'Don.TuHuyPhut',        N'15',     N'Khách được tự hủy khi đơn còn Chờ xác nhận (không giới hạn phút nếu để 0)'),
  (N'Don.DuKienPhut',       N'45',     N'Thời gian dự kiến xử lý + giao đơn (phút)'),
  (N'Don.TreSauPhut',       N'30',     N'Đơn bị coi là trễ khi quá thời gian dự kiến thêm bao nhiêu phút'),
  (N'Don.KetTrangThaiPhut', N'60',     N'Đơn đứng yên một trạng thái quá bao nhiêu phút thì coi là bị kẹt'),
  (N'HoanTien.NguongQuanLy',N'200000', N'Quản lý được duyệt hoàn tiền tới mức này (đ); trên mức cần Admin tổng'),
  (N'HoanTien.ChinhSach',   N'Hoàn tiền khi đơn đã thanh toán online bị hủy hoặc giao thất bại do lỗi gian hàng/tài xế; xét theo từng trường hợp với khiếu nại.', N'Nội dung chính sách hoàn tiền hiển thị cho khách'),
  (N'Voucher.ToiDaMoiKhach',N'5',      N'Giới hạn số lần một khách dùng cùng một mã trong 30 ngày (cảnh báo lạm dụng)'),
  (N'QuyDinh.KhachHang',    N'Khách hàng cung cấp thông tin đúng, không đặt đơn ảo, không lạm dụng khuyến mãi.', N'Quy định áp dụng cho khách hàng'),
  (N'QuyDinh.GianHang',     N'Gian hàng đăng thông tin món/giá đúng thực tế, không đăng nội dung vi phạm, xử lý đơn đúng thời hạn.', N'Quy định áp dụng cho gian hàng'),
  (N'QuyDinh.TaiXe',        N'Tài xế giao đúng đơn, giữ thái độ phục vụ, không từ chối đơn đã nhận vô cớ.', N'Quy định áp dụng cho tài xế')
) AS s(Khoa, GiaTri, MoTa) ON t.Khoa = s.Khoa
WHEN NOT MATCHED THEN INSERT (Khoa, GiaTri, MoTa) VALUES (s.Khoa, s.GiaTri, s.MoTa);
GO

IF OBJECT_ID('DieuPhoiLog', 'U') IS NULL
    CREATE TABLE DieuPhoiLog (
        MaLog       BIGINT IDENTITY(1,1) PRIMARY KEY,
        MaDonHang   INT NOT NULL REFERENCES DonHang(MaDonHang) ON DELETE CASCADE,
        HanhDong    NVARCHAR(30) NOT NULL,          -- PhanCong | DoiTaiXe | GoTaiXe | HuyDon
        MaTaiXeCu   INT NULL,
        MaTaiXeMoi  INT NULL,
        LyDo        NVARCHAR(300) NOT NULL,
        MaTK        INT NULL,
        ThoiGian    DATETIME NOT NULL DEFAULT GETDATE()
    );
GO

/* ---- Cột bổ sung ---- */
IF COL_LENGTH('DanhGia', 'DaAn') IS NULL ALTER TABLE DanhGia ADD DaAn BIT NOT NULL CONSTRAINT DF_DanhGia_DaAn DEFAULT 0;
IF COL_LENGTH('DanhGia', 'LyDoAn') IS NULL ALTER TABLE DanhGia ADD LyDoAn NVARCHAR(255) NULL;
IF COL_LENGTH('BaoCaoDanhGia', 'KetQua') IS NULL ALTER TABLE BaoCaoDanhGia ADD KetQua NVARCHAR(255) NULL;
IF COL_LENGTH('BaoCaoDanhGia', 'NgayXuLy') IS NULL ALTER TABLE BaoCaoDanhGia ADD NgayXuLy DATETIME NULL;
IF COL_LENGTH('PhanHoi', 'MaTKPhuTrach') IS NULL ALTER TABLE PhanHoi ADD MaTKPhuTrach INT NULL;
IF COL_LENGTH('PhanHoi', 'YeuCauBoSung') IS NULL ALTER TABLE PhanHoi ADD YeuCauBoSung NVARCHAR(400) NULL;
IF COL_LENGTH('PhanHoi', 'PhanLoai') IS NULL ALTER TABLE PhanHoi ADD PhanLoai NVARCHAR(30) NULL;   -- DonHang | MonAn | TaiXe | GianHang | ThanhToan | Khac
IF COL_LENGTH('GianHang', 'YeuCauBoSung') IS NULL ALTER TABLE GianHang ADD YeuCauBoSung NVARCHAR(400) NULL;
IF COL_LENGTH('GianHang', 'LyDoTuChoi') IS NULL ALTER TABLE GianHang ADD LyDoTuChoi NVARCHAR(255) NULL;
IF COL_LENGTH('TaiXe', 'YeuCauBoSung') IS NULL ALTER TABLE TaiXe ADD YeuCauBoSung NVARCHAR(400) NULL;
IF COL_LENGTH('MonAn', 'LyDoAn') IS NULL ALTER TABLE MonAn ADD LyDoAn NVARCHAR(255) NULL;
IF COL_LENGTH('DonHang', 'TienGiam') IS NULL ALTER TABLE DonHang ADD TienGiam DECIMAL(12,0) NOT NULL CONSTRAINT DF_DonHang_TienGiam DEFAULT 0;
IF COL_LENGTH('ThanhToan', 'DaDoiSoat') IS NULL ALTER TABLE ThanhToan ADD DaDoiSoat BIT NOT NULL CONSTRAINT DF_ThanhToan_DaDoiSoat DEFAULT 0;
IF COL_LENGTH('ThanhToan', 'NgayDoiSoat') IS NULL ALTER TABLE ThanhToan ADD NgayDoiSoat DATETIME NULL;
GO

/* ===== v13: hoàn tiền qua cổng + phân quyền chi tiết ===== */
IF COL_LENGTH('ThanhToan', 'NgayGiaoDichCong') IS NULL ALTER TABLE ThanhToan ADD NgayGiaoDichCong VARCHAR(14) NULL;   -- vnp_PayDate
IF COL_LENGTH('ThanhToan', 'MaGiaoDichHoan')   IS NULL ALTER TABLE ThanhToan ADD MaGiaoDichHoan NVARCHAR(100) NULL;
IF COL_LENGTH('ThanhToan', 'NgayHoan')         IS NULL ALTER TABLE ThanhToan ADD NgayHoan DATETIME NULL;
IF COL_LENGTH('ThanhToan', 'KetQuaHoan')       IS NULL ALTER TABLE ThanhToan ADD KetQuaHoan NVARCHAR(400) NULL;
IF COL_LENGTH('QuanTri', 'QuyenChiTiet')       IS NULL ALTER TABLE QuanTri ADD QuyenChiTiet NVARCHAR(300) NULL;      -- NULL = mặc định theo CapQuyenHan
GO

/* ===== v14: mã xác nhận giao hàng an toàn, bằng chứng giao hàng, vị trí tài xế lưu bền ===== */
IF COL_LENGTH('DonHang', 'MaXacNhanGiao') IS NULL ALTER TABLE DonHang ADD MaXacNhanGiao VARCHAR(8) NULL;
IF COL_LENGTH('DonHang', 'SoLanSaiMa')    IS NULL ALTER TABLE DonHang ADD SoLanSaiMa INT NOT NULL CONSTRAINT DF_DonHang_SoLanSaiMa DEFAULT 0;
IF COL_LENGTH('GiaoHang', 'AnhBangChung')   IS NULL ALTER TABLE GiaoHang ADD AnhBangChung NVARCHAR(300) NULL;
IF COL_LENGTH('GiaoHang', 'ViDoGiao')       IS NULL ALTER TABLE GiaoHang ADD ViDoGiao FLOAT NULL;
IF COL_LENGTH('GiaoHang', 'KinhDoGiao')     IS NULL ALTER TABLE GiaoHang ADD KinhDoGiao FLOAT NULL;
IF COL_LENGTH('GiaoHang', 'LyDoThatBai')    IS NULL ALTER TABLE GiaoHang ADD LyDoThatBai NVARCHAR(300) NULL;
IF COL_LENGTH('GiaoHang', 'TinhPhiThatBai') IS NULL ALTER TABLE GiaoHang ADD TinhPhiThatBai BIT NOT NULL CONSTRAINT DF_GiaoHang_TinhPhiTB DEFAULT 0;
IF COL_LENGTH('GiaoHang', 'NguoiGayLoi')    IS NULL ALTER TABLE GiaoHang ADD NguoiGayLoi NVARCHAR(20) NULL;
IF COL_LENGTH('GiaoHang', 'TienThuCOD')     IS NULL ALTER TABLE GiaoHang ADD TienThuCOD DECIMAL(12,0) NULL;
GO
IF OBJECT_ID('TaiXeViTri', 'U') IS NULL
    CREATE TABLE TaiXeViTri (
        MaDonHang  INT NOT NULL PRIMARY KEY REFERENCES DonHang(MaDonHang) ON DELETE CASCADE,
        MaTaiXe    INT NOT NULL,
        ViDo       FLOAT NOT NULL,
        KinhDo     FLOAT NOT NULL,
        CapNhatLuc DATETIME NOT NULL DEFAULT GETDATE()
    );
GO
/* Chuyển mã cũ "[MA_GIAO:1234]" trong GhiChu sang cột riêng (trước đây tài xế đọc được ghi chú) rồi xóa khỏi ghi chú */
UPDATE DonHang
SET MaXacNhanGiao = SUBSTRING(GhiChu, CHARINDEX('[MA_GIAO:', GhiChu) + 9, CHARINDEX(']', GhiChu, CHARINDEX('[MA_GIAO:', GhiChu)) - CHARINDEX('[MA_GIAO:', GhiChu) - 9),
    GhiChu = NULLIF(LTRIM(RTRIM(STUFF(GhiChu, CHARINDEX('[MA_GIAO:', GhiChu), CHARINDEX(']', GhiChu, CHARINDEX('[MA_GIAO:', GhiChu)) - CHARINDEX('[MA_GIAO:', GhiChu) + 1, ''))), '')
WHERE MaXacNhanGiao IS NULL AND GhiChu LIKE '%[[]MA_GIAO:%]%';
GO

/* ===== v15: sổ giao dịch thanh toán + lịch sử giao hàng ===== */
IF OBJECT_ID('GiaoDichThanhToan', 'U') IS NULL
    CREATE TABLE GiaoDichThanhToan (
        MaGiaoDichTT   BIGINT IDENTITY(1,1) PRIMARY KEY,
        MaThanhToan    INT NOT NULL,
        MaDonHang      INT NOT NULL REFERENCES DonHang(MaDonHang) ON DELETE CASCADE,
        HanhDong       NVARCHAR(20) NOT NULL,      -- TaoMoi | Thu | ThatBai | Hoan | DoiSoat | CapNhat
        HinhThuc       NVARCHAR(20) NOT NULL,
        TuTrangThai    NVARCHAR(30) NULL,
        DenTrangThai   NVARCHAR(30) NOT NULL,
        SoTien         DECIMAL(12,0) NOT NULL DEFAULT 0,
        MaGiaoDichCong NVARCHAR(100) NULL,
        GhiChu         NVARCHAR(500) NULL,
        ThoiGian       DATETIME NOT NULL DEFAULT GETDATE()
    );
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_GiaoDichThanhToan_Don' AND object_id = OBJECT_ID('GiaoDichThanhToan'))
    CREATE INDEX IX_GiaoDichThanhToan_Don ON GiaoDichThanhToan(MaDonHang, ThoiGian);
GO
IF OBJECT_ID('LichSuGiaoHang', 'U') IS NULL
    CREATE TABLE LichSuGiaoHang (
        MaLichSuGH   BIGINT IDENTITY(1,1) PRIMARY KEY,
        MaGiaoHang   INT NOT NULL,
        MaDonHang    INT NOT NULL REFERENCES DonHang(MaDonHang) ON DELETE CASCADE,
        MaTaiXe      INT NOT NULL,
        TuTrangThai  NVARCHAR(30) NULL,
        DenTrangThai NVARCHAR(30) NOT NULL,
        ViDo         FLOAT NULL,
        KinhDo       FLOAT NULL,
        GhiChu       NVARCHAR(500) NULL,
        ThoiGian     DATETIME NOT NULL DEFAULT GETDATE()
    );
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_LichSuGiaoHang_Don' AND object_id = OBJECT_ID('LichSuGiaoHang'))
    CREATE INDEX IX_LichSuGiaoHang_Don ON LichSuGiaoHang(MaDonHang, ThoiGian);
GO
-- Điền lịch sử ban đầu từ dữ liệu hiện có (chạy một lần)
INSERT INTO GiaoDichThanhToan (MaThanhToan, MaDonHang, HanhDong, HinhThuc, DenTrangThai, SoTien, MaGiaoDichCong, ThoiGian)
SELECT t.MaThanhToan, t.MaDonHang, N'CapNhat', t.HinhThuc, t.TrangThai, d.TongTien, t.MaGiaoDich, ISNULL(t.NgayThanhToan, d.NgayDat)
FROM ThanhToan t JOIN DonHang d ON d.MaDonHang = t.MaDonHang
WHERE NOT EXISTS (SELECT 1 FROM GiaoDichThanhToan g WHERE g.MaThanhToan = t.MaThanhToan);
GO
INSERT INTO LichSuGiaoHang (MaGiaoHang, MaDonHang, MaTaiXe, DenTrangThai, ThoiGian)
SELECT g.MaGiaoHang, g.MaDonHang, g.MaTaiXe, g.TrangThai, ISNULL(g.ThoiGianGiao, ISNULL(g.ThoiGianNhan, GETDATE()))
FROM GiaoHang g
WHERE NOT EXISTS (SELECT 1 FROM LichSuGiaoHang l WHERE l.MaGiaoHang = g.MaGiaoHang);
GO
