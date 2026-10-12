/* ============================================================
   NÂNG CẤP v10: giấy tờ gian hàng + giỏ hàng lưu trên server (chạy NHIỀU LẦN an toàn, không xóa dữ liệu)
   Yêu cầu đã chạy: NangCap_v8 -> NangCap_v9_Backend -> 04_BoSung_DoiTac (hoặc tạo mới bằng 01_TaoMoi_Full.sql đã gồm sẵn)
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
