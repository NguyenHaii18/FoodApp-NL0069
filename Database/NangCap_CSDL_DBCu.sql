/* ============================================================
   NÂNG CẤP CSDL CŨ -> khớp bản code tích hợp (chạy được NHIỀU LẦN, an toàn)
   Gộp: BoSungSchema.sql (GianHang.TrangThaiDuyet, DonHang.MaKhuyenMaiApDung)
        + phí giao hàng + duyệt tài xế + ảnh gian hàng/món ăn + 3 bảng mới.
   Dùng khi bạn ĐÃ có CSDL QuanLyDichVuAnUong từ file cũ và không muốn tạo lại.
   Nếu tạo mới từ đầu, chỉ cần chạy MotFileDuyNhat.sql (đã gồm tất cả).
   ============================================================ */

USE QuanLyDichVuAnUong;
GO

/* ---- 1. GianHang.TrangThaiDuyet (từ BoSungSchema.sql) ---- */
IF COL_LENGTH('GianHang', 'TrangThaiDuyet') IS NULL
BEGIN
    ALTER TABLE GianHang ADD TrangThaiDuyet NVARCHAR(20) NOT NULL
        CONSTRAINT DF_GianHang_TrangThaiDuyet DEFAULT N'DaDuyet';   -- gian hàng cũ coi như đã duyệt
    EXEC('ALTER TABLE GianHang ADD CONSTRAINT CK_GianHang_TrangThaiDuyet
          CHECK (TrangThaiDuyet IN (N''ChoDuyet'', N''DaDuyet'', N''TuChoi''))');
END
GO

/* ---- 2. DonHang.MaKhuyenMaiApDung (từ BoSungSchema.sql) ---- */
IF COL_LENGTH('DonHang', 'MaKhuyenMaiApDung') IS NULL
BEGIN
    ALTER TABLE DonHang ADD MaKhuyenMaiApDung INT NULL;
    EXEC('ALTER TABLE DonHang ADD CONSTRAINT FK_DonHang_KhuyenMai
          FOREIGN KEY (MaKhuyenMaiApDung) REFERENCES KhuyenMai(MaKhuyenMai)');
END
GO

/* ---- 3. DonHang.PhiGiaoHang (thu nhập tài xế = phí giao hàng) ---- */
IF COL_LENGTH('DonHang', 'PhiGiaoHang') IS NULL
    ALTER TABLE DonHang ADD PhiGiaoHang DECIMAL(12,0) NOT NULL
        CONSTRAINT DF_DonHang_PhiGiaoHang DEFAULT 0;
GO

/* Đồng bộ phí từ chuyến giao cũ (nếu bảng GiaoHang đang có cột PhiGiaoHang) */
IF COL_LENGTH('GiaoHang', 'PhiGiaoHang') IS NOT NULL
    EXEC('UPDATE d SET d.PhiGiaoHang = ISNULL(g.PhiGiaoHang, 0)
          FROM DonHang d JOIN GiaoHang g ON g.MaDonHang = d.MaDonHang
          WHERE d.PhiGiaoHang = 0');
GO

/* ---- 4. TaiXe: duyệt hồ sơ tài xế tự đăng ký ---- */
IF COL_LENGTH('TaiXe', 'TrangThaiDuyet') IS NULL
BEGIN
    ALTER TABLE TaiXe ADD TrangThaiDuyet NVARCHAR(20) NOT NULL
        CONSTRAINT DF_TaiXe_TrangThaiDuyet DEFAULT N'DaDuyet';       -- tài xế cũ coi như đã duyệt
    EXEC('ALTER TABLE TaiXe ADD CONSTRAINT CK_TaiXe_TrangThaiDuyet
          CHECK (TrangThaiDuyet IN (N''ChoDuyet'', N''DaDuyet'', N''TuChoi''))');
END
GO
IF COL_LENGTH('TaiXe', 'NgayDangKy') IS NULL
    ALTER TABLE TaiXe ADD NgayDangKy DATETIME NOT NULL CONSTRAINT DF_TaiXe_NgayDangKy DEFAULT GETDATE();
GO

/* ---- 5. GianHang.HinhAnh ---- */
IF COL_LENGTH('GianHang', 'HinhAnh') IS NULL
    ALTER TABLE GianHang ADD HinhAnh NVARCHAR(500) NULL;
GO

/* ---- 6. Ba bảng mới ---- */
IF OBJECT_ID('DanhGiaMon', 'U') IS NULL
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

IF OBJECT_ID('DiaChiKhachHang', 'U') IS NULL
CREATE TABLE DiaChiKhachHang (
    MaDiaChi        INT IDENTITY(1,1) PRIMARY KEY,
    MaKH            INT NOT NULL REFERENCES KhachHang(MaKH),
    Nhan            NVARCHAR(50)  NOT NULL,
    DiaChi          NVARCHAR(255) NOT NULL,
    MacDinh         BIT NOT NULL DEFAULT 0
);
GO

IF OBJECT_ID('PhanHoi', 'U') IS NULL
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

/* ---- 7. Sổ địa chỉ: tạo địa chỉ mặc định từ hồ sơ khách (chỉ cho khách chưa có) ---- */
INSERT INTO DiaChiKhachHang (MaKH, Nhan, DiaChi, MacDinh)
SELECT k.MaKH, N'Nhà', k.DiaChiGiaoHang, 1
FROM KhachHang k
WHERE k.DiaChiGiaoHang IS NOT NULL
  AND NOT EXISTS (SELECT 1 FROM DiaChiKhachHang d WHERE d.MaKH = k.MaKH);
GO

/* ---- 8. Ảnh minh hoạ cho 8 gian hàng mẫu / món tiêu biểu (chỉ gán nếu đang trống) ---- */
UPDATE GianHang SET HinhAnh = N'/uploads/gh1_comtam_coba.svg'     WHERE TenCuaHang = N'Quán Cơm Tấm Cô Ba'              AND HinhAnh IS NULL;
UPDATE GianHang SET HinhAnh = N'/uploads/gh2_bunbo_meba.svg'      WHERE TenCuaHang = N'Bún Bò Huế Mệ Ba'                 AND HinhAnh IS NULL;
UPDATE GianHang SET HinhAnh = N'/uploads/gh3_trasua_moctra.svg'   WHERE TenCuaHang = N'Trà Sữa Mộc Trà'                  AND HinhAnh IS NULL;
UPDATE GianHang SET HinhAnh = N'/uploads/gh4_pho_hanoi.svg'       WHERE TenCuaHang = N'Phở Hà Nội Xưa'                   AND HinhAnh IS NULL;
UPDATE GianHang SET HinhAnh = N'/uploads/gh5_banhmi_bahuynh.svg'  WHERE TenCuaHang = N'Bánh Mì Bà Huynh'                 AND HinhAnh IS NULL;
UPDATE GianHang SET HinhAnh = N'/uploads/gh6_garan_cout.svg'      WHERE TenCuaHang = N'Gà Rán Cô Út'                     AND HinhAnh IS NULL;
UPDATE GianHang SET HinhAnh = N'/uploads/gh7_hutieu_namvang.svg'  WHERE TenCuaHang = N'Hủ Tiếu Nam Vang Sáu Sài Gòn'     AND HinhAnh IS NULL;
UPDATE GianHang SET HinhAnh = N'/uploads/gh8_che_bamien.svg'      WHERE TenCuaHang = N'Chè Ba Miền'                      AND HinhAnh IS NULL;
UPDATE MonAn SET HinhAnh = N'/uploads/mon1_com_suon_bi_cha.svg'              WHERE TenMon = N'Cơm sườn bì chả'                       AND HinhAnh IS NULL;
UPDATE MonAn SET HinhAnh = N'/uploads/mon2_bun_bo_gio_heo.svg'               WHERE TenMon = N'Bún bò giò heo'                        AND HinhAnh IS NULL;
UPDATE MonAn SET HinhAnh = N'/uploads/mon3_trasua_tranchau_duongden.svg'     WHERE TenMon = N'Trà sữa trân châu đường đen'           AND HinhAnh IS NULL;
UPDATE MonAn SET HinhAnh = N'/uploads/mon4_pho_dac_biet.svg'                 WHERE TenMon = N'Phở đặc biệt'                          AND HinhAnh IS NULL;
UPDATE MonAn SET HinhAnh = N'/uploads/mon5_banhmi_thitnuong.svg'             WHERE TenMon = N'Bánh mì thịt nướng'                    AND HinhAnh IS NULL;
UPDATE MonAn SET HinhAnh = N'/uploads/mon6_combo_ga_com.svg'                 WHERE TenMon = N'Combo 2 miếng gà + cơm'                AND HinhAnh IS NULL;
UPDATE MonAn SET HinhAnh = N'/uploads/mon7_hutieu_dac_biet.svg'              WHERE TenMon = N'Hủ tiếu đặc biệt'                      AND HinhAnh IS NULL;
UPDATE MonAn SET HinhAnh = N'/uploads/mon8_che_thai.svg'                     WHERE TenMon = N'Chè thái'                              AND HinhAnh IS NULL;
GO

/* ---- 9. Khuyến mãi theo từng món + tài xế từ chối đơn ---- */
IF COL_LENGTH('KhuyenMai', 'MaMonAn') IS NULL
BEGIN
    ALTER TABLE KhuyenMai ADD MaMonAn INT NULL;
    EXEC('ALTER TABLE KhuyenMai ADD CONSTRAINT FK_KhuyenMai_MonAn FOREIGN KEY (MaMonAn) REFERENCES MonAn(MaMonAn)');
END
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

PRINT N'Đã nâng cấp CSDL khớp bản code tích hợp (17 bảng).';
GO
