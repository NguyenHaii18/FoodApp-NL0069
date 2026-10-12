/* ============================================================
   BỔ SUNG CSDL CHO GIAO HÀNG THEO KHOẢNG CÁCH + BẢN ĐỒ (chạy được NHIỀU LẦN, an toàn)
   Khớp app: phí giao tính theo km (15.000đ/2 km đầu, +5.000đ/km, +10.000đ ngoại thành, tối đa 70.000đ),
   chia sẻ vị trí tài xế, mã xác nhận giao 4 số, giao thất bại (lỗi khách vẫn nhận phí giao).
   Chạy SAU 03_NangCap_v8.sql.
   ============================================================ */
USE QuanLyDichVuAnUong;
GO

/* ---- DonHang: tuyến đường, phí, mã xác nhận ---- */
IF COL_LENGTH('DonHang','DiaChiLayHang')      IS NULL ALTER TABLE DonHang ADD DiaChiLayHang NVARCHAR(300) NULL;
IF COL_LENGTH('DonHang','KhoangCachKm')       IS NULL ALTER TABLE DonHang ADD KhoangCachKm DECIMAL(6,1) NOT NULL CONSTRAINT DF_DonHang_KhoangCachKm DEFAULT 0;      -- 0 = chưa xác định
IF COL_LENGTH('DonHang','KhoangCachThucTeKm') IS NULL ALTER TABLE DonHang ADD KhoangCachThucTeKm DECIMAL(6,1) NOT NULL CONSTRAINT DF_DonHang_KmThucTe DEFAULT 0;      -- tài xế báo lại
IF COL_LENGTH('DonHang','ThoiGianDuKienPhut') IS NULL ALTER TABLE DonHang ADD ThoiGianDuKienPhut INT NOT NULL CONSTRAINT DF_DonHang_ThoiGianDuKien DEFAULT 0;
IF COL_LENGTH('DonHang','KhuVucLay')          IS NULL ALTER TABLE DonHang ADD KhuVucLay NVARCHAR(60) NULL;
IF COL_LENGTH('DonHang','KhuVucGiao')         IS NULL ALTER TABLE DonHang ADD KhuVucGiao NVARCHAR(60) NULL;
IF COL_LENGTH('DonHang','NgoaiThanh')         IS NULL ALTER TABLE DonHang ADD NgoaiThanh BIT NOT NULL CONSTRAINT DF_DonHang_NgoaiThanh DEFAULT 0;
IF COL_LENGTH('DonHang','PhiKhoangCach')      IS NULL ALTER TABLE DonHang ADD PhiKhoangCach DECIMAL(12,0) NOT NULL CONSTRAINT DF_DonHang_PhiKm DEFAULT 0;
IF COL_LENGTH('DonHang','PhuPhiKhuVuc')       IS NULL ALTER TABLE DonHang ADD PhuPhiKhuVuc DECIMAL(12,0) NOT NULL CONSTRAINT DF_DonHang_PhuPhi DEFAULT 0;
IF COL_LENGTH('DonHang','PhiDaDieuChinh')     IS NULL ALTER TABLE DonHang ADD PhiDaDieuChinh BIT NOT NULL CONSTRAINT DF_DonHang_PhiDaDieuChinh DEFAULT 0;
IF COL_LENGTH('DonHang','MaXacNhanGiao')      IS NULL ALTER TABLE DonHang ADD MaXacNhanGiao VARCHAR(4) NULL;   -- khách đọc cho tài xế khi nhận hàng
GO

/* ---- GiaoHang: lấy hàng, vị trí tài xế, liên lạc, giao thất bại ---- */
IF COL_LENGTH('GiaoHang','LayHangLuc')        IS NULL ALTER TABLE GiaoHang ADD LayHangLuc DATETIME NULL;
IF COL_LENGTH('GiaoHang','TaiXeLat')          IS NULL ALTER TABLE GiaoHang ADD TaiXeLat DECIMAL(9,6) NULL;
IF COL_LENGTH('GiaoHang','TaiXeLng')          IS NULL ALTER TABLE GiaoHang ADD TaiXeLng DECIMAL(9,6) NULL;
IF COL_LENGTH('GiaoHang','ViTriCapNhatLuc')   IS NULL ALTER TABLE GiaoHang ADD ViTriCapNhatLuc DATETIME NULL;
IF COL_LENGTH('GiaoHang','SoLanGoiKhach')     IS NULL ALTER TABLE GiaoHang ADD SoLanGoiKhach INT NOT NULL CONSTRAINT DF_GiaoHang_SoLanGoi DEFAULT 0;
IF COL_LENGTH('GiaoHang','GiaoThatBaiLuc')    IS NULL ALTER TABLE GiaoHang ADD GiaoThatBaiLuc DATETIME NULL;
IF COL_LENGTH('GiaoHang','TinhPhiKhiThatBai') IS NULL ALTER TABLE GiaoHang ADD TinhPhiKhiThatBai BIT NOT NULL CONSTRAINT DF_GiaoHang_TinhPhiThatBai DEFAULT 0;  -- lỗi do khách: tài xế vẫn nhận phí giao
GO

SELECT N'DonHang' AS Bang, name FROM sys.columns WHERE object_id = OBJECT_ID('DonHang') AND name IN ('KhoangCachKm','PhiKhoangCach','PhuPhiKhuVuc','MaXacNhanGiao')
UNION ALL SELECT N'GiaoHang', name FROM sys.columns WHERE object_id = OBJECT_ID('GiaoHang') AND name IN ('TaiXeLat','TaiXeLng','SoLanGoiKhach','TinhPhiKhiThatBai');
GO
