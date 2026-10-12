-- Giỏ hàng lưu trên server (mỗi khách một giỏ, mọi món cùng một gian hàng)
-- Chạy sau 01_MotFileDuyNhat_TaoMoi.sql
CREATE TABLE GioHang (
    MaGioHang   INT IDENTITY(1,1) PRIMARY KEY,
    MaKH        INT NOT NULL UNIQUE REFERENCES KhachHang(MaKH),       -- UNIQUE: mỗi khách đúng một giỏ
    MaGianHang  INT NOT NULL REFERENCES GianHang(MaGianHang),
    NgayCapNhat DATETIME NOT NULL DEFAULT GETDATE()
);
GO

CREATE TABLE GioHangChiTiet (
    MaChiTiet   INT IDENTITY(1,1) PRIMARY KEY,
    MaGioHang   INT NOT NULL REFERENCES GioHang(MaGioHang) ON DELETE CASCADE,
    MaMonAn     INT NOT NULL REFERENCES MonAn(MaMonAn),
    SoLuong     INT NOT NULL CHECK (SoLuong BETWEEN 1 AND 99),
    GhiChu      NVARCHAR(255) NULL,
    TuyChon     NVARCHAR(MAX) NULL                                    -- JSON: ["Lớn","Trân châu"]
);
GO

-- Ràng buộc "không gom món sai gian hàng" nên kiểm tra trong API khi PUT giỏ:
--   mọi MaMonAn trong chiTiet phải có MonAn.MaGianHang = GioHang.MaGianHang.
-- Giá tiền KHÔNG lưu trong giỏ: luôn lấy MonAn.Gia hiện tại khi hiển thị và khi tạo đơn.
CREATE INDEX IX_GioHangChiTiet_GioHang ON GioHangChiTiet(MaGioHang);
GO
