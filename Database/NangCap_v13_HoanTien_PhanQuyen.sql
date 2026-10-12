/* NangCap_v13: chạy 1 lần trên CSDL đang dùng (idempotent). Đã gộp sẵn ở cuối 01_TaoMoi_Full.sql. */
/* ===== v13: hoàn tiền qua cổng + phân quyền chi tiết ===== */
IF COL_LENGTH('ThanhToan', 'NgayGiaoDichCong') IS NULL ALTER TABLE ThanhToan ADD NgayGiaoDichCong VARCHAR(14) NULL;   -- vnp_PayDate
IF COL_LENGTH('ThanhToan', 'MaGiaoDichHoan')   IS NULL ALTER TABLE ThanhToan ADD MaGiaoDichHoan NVARCHAR(100) NULL;
IF COL_LENGTH('ThanhToan', 'NgayHoan')         IS NULL ALTER TABLE ThanhToan ADD NgayHoan DATETIME NULL;
IF COL_LENGTH('ThanhToan', 'KetQuaHoan')       IS NULL ALTER TABLE ThanhToan ADD KetQuaHoan NVARCHAR(400) NULL;
IF COL_LENGTH('QuanTri', 'QuyenChiTiet')       IS NULL ALTER TABLE QuanTri ADD QuyenChiTiet NVARCHAR(300) NULL;      -- NULL = mặc định theo CapQuyenHan
GO
