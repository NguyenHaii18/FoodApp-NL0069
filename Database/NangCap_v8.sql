/* NÂNG CẤP CSDL ĐÃ CÓ -> khớp app MAUI v8. Chạy SAU NangCap_CSDL_DBCu.sql (nếu DB còn cũ). KHÔNG xóa dữ liệu. */

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

