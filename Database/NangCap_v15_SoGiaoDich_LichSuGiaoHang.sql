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
