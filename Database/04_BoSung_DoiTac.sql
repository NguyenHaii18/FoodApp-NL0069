/* ============================================================
   BỔ SUNG CSDL CHO CHỨC NĂNG ĐỐI TÁC / GIAN HÀNG (chạy được NHIỀU LẦN, an toàn)
   Khớp với phần code MAUI bổ sung: giờ hoạt động, trạng thái nhận đơn, thứ tự danh mục,
   món "tạm ngừng bán", giá vốn, tùy chọn món (size/topping/mức cay), lý do từ chối đơn,
   phản hồi đánh giá, tỉ lệ chiết khấu nền tảng (thông báo gian hàng dùng bảng ThongBao của v9).

   Cách dùng: chạy SAU 01_TaoMoi_Full.sql + 03_NangCap_v8.sql (hoặc trên CSDL cũ đã nâng cấp bằng 02 + 03).
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
