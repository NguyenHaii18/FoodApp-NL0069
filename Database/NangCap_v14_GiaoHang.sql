/* NangCap_v14: chạy 1 lần (idempotent). Đã gộp sẵn ở cuối 01_TaoMoi_Full.sql. */
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
