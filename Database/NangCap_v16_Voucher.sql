-- Voucher upgrade: execute explicitly on the restored test database only.
-- sqlcmd -S "(localdb)\MSSQLLocalDB" -d QuanLyDichVuAnUong_MigrationTest -E -b -i Database/NangCap_v16_Voucher.sql
-- No USE statement: wrong database aborts before any mutation.
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
SET ANSI_PADDING ON;
SET ANSI_WARNINGS ON;
SET CONCAT_NULL_YIELDS_NULL ON;
SET ARITHABORT ON;
SET NUMERIC_ROUNDABORT OFF;
SET NOCOUNT ON;
SET XACT_ABORT ON;
IF DB_NAME() <> N'QuanLyDichVuAnUong_MigrationTest'
    THROW 51001, N'Voucher migration only permits QuanLyDichVuAnUong_MigrationTest.', 1;
IF OBJECT_ID(N'dbo.DonHang', N'U') IS NULL OR OBJECT_ID(N'dbo.KhachHang', N'U') IS NULL
    THROW 51002, N'Restore the existing database before applying this upgrade.', 1;
BEGIN TRY
BEGIN TRANSACTION;
DECLARE @LegacyVoucher bit = CASE WHEN COL_LENGTH('dbo.KhuyenMai','LoaiGiam') IS NULL THEN 1 ELSE 0 END;
IF OBJECT_ID(N'dbo.KhuyenMai', N'U') IS NULL
    CREATE TABLE dbo.KhuyenMai (
      MaKhuyenMai int IDENTITY PRIMARY KEY, MaGianHang int NULL REFERENCES dbo.GianHang(MaGianHang),
      MaCode nvarchar(50) NOT NULL, MoTa nvarchar(1000) NULL,
      PhanTramGiam decimal(5,2) NULL, SoTienGiam decimal(18,2) NULL, DieuKienApDung nvarchar(500) NULL,
      NgayBatDau datetime2 NOT NULL, NgayKetThuc datetime2 NOT NULL, TrangThai bit NOT NULL DEFAULT 1
    );
IF COL_LENGTH('dbo.KhuyenMai','TenChuongTrinh') IS NULL EXEC(N'ALTER TABLE dbo.KhuyenMai ADD TenChuongTrinh nvarchar(150) NULL');
IF COL_LENGTH('dbo.KhuyenMai','LoaiGiam') IS NULL EXEC(N'ALTER TABLE dbo.KhuyenMai ADD LoaiGiam nvarchar(20) NOT NULL CONSTRAINT DF_V16_Loai DEFAULT N''PhanTram''');
IF COL_LENGTH('dbo.KhuyenMai','GiamToiDa') IS NULL EXEC(N'ALTER TABLE dbo.KhuyenMai ADD GiamToiDa decimal(18,2) NULL');
IF COL_LENGTH('dbo.KhuyenMai','GiaTriDonToiThieu') IS NULL EXEC(N'ALTER TABLE dbo.KhuyenMai ADD GiaTriDonToiThieu decimal(18,2) NOT NULL CONSTRAINT DF_V16_Min DEFAULT 0');
IF COL_LENGTH('dbo.KhuyenMai','SoLuongToiDa') IS NULL EXEC(N'ALTER TABLE dbo.KhuyenMai ADD SoLuongToiDa int NULL');
IF COL_LENGTH('dbo.KhuyenMai','SoLuongDaDung') IS NULL EXEC(N'ALTER TABLE dbo.KhuyenMai ADD SoLuongDaDung int NOT NULL CONSTRAINT DF_V16_Used DEFAULT 0');
IF COL_LENGTH('dbo.KhuyenMai','MoiKhachMotLan') IS NULL EXEC(N'ALTER TABLE dbo.KhuyenMai ADD MoiKhachMotLan bit NOT NULL CONSTRAINT DF_V16_Once DEFAULT 0');
IF COL_LENGTH('dbo.DonHang','MaKhuyenMaiApDung') IS NULL EXEC(N'ALTER TABLE dbo.DonHang ADD MaKhuyenMaiApDung int NULL');
IF COL_LENGTH('dbo.DonHang','TienGiam') IS NULL EXEC(N'ALTER TABLE dbo.DonHang ADD TienGiam decimal(18,2) NOT NULL CONSTRAINT DF_V16_Discount DEFAULT 0');
IF COL_LENGTH('dbo.DonHang','MaYeuCauDat') IS NULL EXEC(N'ALTER TABLE dbo.DonHang ADD MaYeuCauDat varchar(40) NULL');
IF OBJECT_ID(N'dbo.KhuyenMaiMonAn', N'U') IS NULL
 CREATE TABLE dbo.KhuyenMaiMonAn (MaKhuyenMai int NOT NULL, MaMonAn int NOT NULL, PRIMARY KEY(MaKhuyenMai,MaMonAn));
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE parent_object_id=OBJECT_ID('dbo.KhuyenMaiMonAn') AND referenced_object_id=OBJECT_ID('dbo.KhuyenMai'))
 ALTER TABLE dbo.KhuyenMaiMonAn WITH CHECK ADD CONSTRAINT FK_V16_MonAn_Voucher FOREIGN KEY(MaKhuyenMai) REFERENCES dbo.KhuyenMai(MaKhuyenMai);
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE parent_object_id=OBJECT_ID('dbo.KhuyenMaiMonAn') AND referenced_object_id=OBJECT_ID('dbo.MonAn'))
 ALTER TABLE dbo.KhuyenMaiMonAn WITH CHECK ADD CONSTRAINT FK_V16_MonAn_Target FOREIGN KEY(MaMonAn) REFERENCES dbo.MonAn(MaMonAn);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID('dbo.KhuyenMaiMonAn') AND name='UX_V16_MonAn')
 CREATE UNIQUE INDEX UX_V16_MonAn ON dbo.KhuyenMaiMonAn(MaKhuyenMai,MaMonAn);
IF OBJECT_ID(N'dbo.KhuyenMaiDanhMuc', N'U') IS NULL
 CREATE TABLE dbo.KhuyenMaiDanhMuc (MaKhuyenMai int NOT NULL, MaDanhMuc int NOT NULL, PRIMARY KEY(MaKhuyenMai,MaDanhMuc));
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE parent_object_id=OBJECT_ID('dbo.KhuyenMaiDanhMuc') AND referenced_object_id=OBJECT_ID('dbo.KhuyenMai'))
 ALTER TABLE dbo.KhuyenMaiDanhMuc WITH CHECK ADD CONSTRAINT FK_V16_DanhMuc_Voucher FOREIGN KEY(MaKhuyenMai) REFERENCES dbo.KhuyenMai(MaKhuyenMai);
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE parent_object_id=OBJECT_ID('dbo.KhuyenMaiDanhMuc') AND referenced_object_id=OBJECT_ID('dbo.DanhMuc'))
 ALTER TABLE dbo.KhuyenMaiDanhMuc WITH CHECK ADD CONSTRAINT FK_V16_DanhMuc_Target FOREIGN KEY(MaDanhMuc) REFERENCES dbo.DanhMuc(MaDanhMuc);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID('dbo.KhuyenMaiDanhMuc') AND name='UX_V16_DanhMuc')
 CREATE UNIQUE INDEX UX_V16_DanhMuc ON dbo.KhuyenMaiDanhMuc(MaKhuyenMai,MaDanhMuc);
IF OBJECT_ID(N'dbo.KhuyenMaiGianHang', N'U') IS NULL
 CREATE TABLE dbo.KhuyenMaiGianHang (MaKhuyenMai int NOT NULL, MaGianHang int NOT NULL, PRIMARY KEY(MaKhuyenMai,MaGianHang));
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE parent_object_id=OBJECT_ID('dbo.KhuyenMaiGianHang') AND referenced_object_id=OBJECT_ID('dbo.KhuyenMai'))
 ALTER TABLE dbo.KhuyenMaiGianHang WITH CHECK ADD CONSTRAINT FK_V16_GianHang_Voucher FOREIGN KEY(MaKhuyenMai) REFERENCES dbo.KhuyenMai(MaKhuyenMai);
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE parent_object_id=OBJECT_ID('dbo.KhuyenMaiGianHang') AND referenced_object_id=OBJECT_ID('dbo.GianHang'))
 ALTER TABLE dbo.KhuyenMaiGianHang WITH CHECK ADD CONSTRAINT FK_V16_GianHang_Target FOREIGN KEY(MaGianHang) REFERENCES dbo.GianHang(MaGianHang);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID('dbo.KhuyenMaiGianHang') AND name='UX_V16_GianHang')
 CREATE UNIQUE INDEX UX_V16_GianHang ON dbo.KhuyenMaiGianHang(MaKhuyenMai,MaGianHang);
IF OBJECT_ID(N'dbo.KhuyenMaiKhachDung', N'U') IS NULL
 CREATE TABLE dbo.KhuyenMaiKhachDung (
  MaKhuyenMai int NOT NULL, MaKH int NOT NULL, MaDonHang int NOT NULL,
  NgaySuDung datetime2 NOT NULL DEFAULT SYSDATETIME(), PRIMARY KEY(MaKhuyenMai,MaKH,MaDonHang));
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE parent_object_id=OBJECT_ID('dbo.KhuyenMaiKhachDung') AND referenced_object_id=OBJECT_ID('dbo.KhuyenMai'))
 ALTER TABLE dbo.KhuyenMaiKhachDung WITH CHECK ADD CONSTRAINT FK_V16_Used_KhuyenMai FOREIGN KEY(MaKhuyenMai) REFERENCES dbo.KhuyenMai(MaKhuyenMai);
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE parent_object_id=OBJECT_ID('dbo.KhuyenMaiKhachDung') AND referenced_object_id=OBJECT_ID('dbo.KhachHang'))
 ALTER TABLE dbo.KhuyenMaiKhachDung WITH CHECK ADD CONSTRAINT FK_V16_Used_KhachHang FOREIGN KEY(MaKH) REFERENCES dbo.KhachHang(MaKH);
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE parent_object_id=OBJECT_ID('dbo.KhuyenMaiKhachDung') AND referenced_object_id=OBJECT_ID('dbo.DonHang'))
 ALTER TABLE dbo.KhuyenMaiKhachDung WITH CHECK ADD CONSTRAINT FK_V16_Used_DonHang FOREIGN KEY(MaDonHang) REFERENCES dbo.DonHang(MaDonHang);
-- Migrate legacy single-target columns once; leave those columns intact.
IF @LegacyVoucher=1
BEGIN
IF COL_LENGTH('dbo.KhuyenMai','MaMonAn') IS NOT NULL EXEC(N'INSERT dbo.KhuyenMaiMonAn(MaKhuyenMai,MaMonAn) SELECT k.MaKhuyenMai,k.MaMonAn FROM dbo.KhuyenMai k WHERE k.MaMonAn IS NOT NULL AND NOT EXISTS (SELECT 1 FROM dbo.KhuyenMaiMonAn l WHERE l.MaKhuyenMai=k.MaKhuyenMai AND l.MaMonAn=k.MaMonAn)');
IF COL_LENGTH('dbo.KhuyenMai','MaDanhMuc') IS NOT NULL EXEC(N'INSERT dbo.KhuyenMaiDanhMuc(MaKhuyenMai,MaDanhMuc) SELECT k.MaKhuyenMai,k.MaDanhMuc FROM dbo.KhuyenMai k WHERE k.MaDanhMuc IS NOT NULL AND NOT EXISTS (SELECT 1 FROM dbo.KhuyenMaiDanhMuc l WHERE l.MaKhuyenMai=k.MaKhuyenMai AND l.MaDanhMuc=k.MaDanhMuc)');
IF COL_LENGTH('dbo.KhuyenMai','MaGianHangApDung') IS NOT NULL EXEC(N'INSERT dbo.KhuyenMaiGianHang(MaKhuyenMai,MaGianHang) SELECT k.MaKhuyenMai,k.MaGianHangApDung FROM dbo.KhuyenMai k WHERE k.MaGianHangApDung IS NOT NULL AND NOT EXISTS (SELECT 1 FROM dbo.KhuyenMaiGianHang l WHERE l.MaKhuyenMai=k.MaKhuyenMai AND l.MaGianHang=k.MaGianHangApDung)');
IF COL_LENGTH('dbo.KhuyenMai','GiamPhiVanChuyen') IS NOT NULL AND COL_LENGTH('dbo.KhuyenMai','GiamPhiVanChuyenToiDa') IS NOT NULL
 EXEC(N'UPDATE dbo.KhuyenMai SET LoaiGiam=N''GiamPhiVanChuyen'', PhanTramGiam=NULL, SoTienGiam=COALESCE(GiamPhiVanChuyenToiDa,0) WHERE GiamPhiVanChuyen=1');
END;
-- A legacy nvarchar(max) code cannot be indexed. Fail instead of truncating any code.
IF EXISTS (SELECT 1 FROM dbo.KhuyenMai WHERE LEN(MaCode)>50) THROW 51004,N'Voucher code longer than 50 characters. Review manually.',1;
IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id=OBJECT_ID('dbo.KhuyenMai') AND name='MaCode' AND (max_length=-1 OR max_length>100))
 ALTER TABLE dbo.KhuyenMai ALTER COLUMN MaCode nvarchar(50) NOT NULL;
EXEC(N'IF EXISTS (SELECT 1 FROM dbo.KhuyenMai GROUP BY UPPER(LTRIM(RTRIM(MaCode))) HAVING COUNT(*) > 1)
 THROW 51003, N''Duplicate normalized voucher codes: resolve manually; no data was deleted.'', 1;
UPDATE dbo.KhuyenMai SET MaCode=UPPER(LTRIM(RTRIM(MaCode))) WHERE MaCode <> UPPER(LTRIM(RTRIM(MaCode))) OR DATALENGTH(MaCode) <> DATALENGTH(LTRIM(RTRIM(MaCode)));
UPDATE dbo.KhuyenMai SET LoaiGiam=N''SoTien'' WHERE LoaiGiam=N''PhanTram'' AND PhanTramGiam IS NULL AND SoTienGiam IS NOT NULL;
-- Preserve history, including cancelled orders, under the documented consume-on-placement policy.
INSERT dbo.KhuyenMaiKhachDung(MaKhuyenMai,MaKH,MaDonHang,NgaySuDung)
 SELECT d.MaKhuyenMaiApDung,d.MaKH,d.MaDonHang,d.NgayDat FROM dbo.DonHang d
 WHERE d.MaKhuyenMaiApDung IS NOT NULL AND NOT EXISTS (SELECT 1 FROM dbo.KhuyenMaiKhachDung u WHERE u.MaDonHang=d.MaDonHang);
-- Never reduce an existing counter; old records may have historical consumption without an order link.
UPDATE k SET SoLuongDaDung=h.Total FROM dbo.KhuyenMai k
 JOIN (SELECT MaKhuyenMai,COUNT(*) Total FROM dbo.KhuyenMaiKhachDung GROUP BY MaKhuyenMai) h ON h.MaKhuyenMai=k.MaKhuyenMai
 WHERE k.SoLuongDaDung<h.Total;
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(''dbo.KhuyenMai'') AND name=''UX_V16_Code'')
 CREATE UNIQUE INDEX UX_V16_Code ON dbo.KhuyenMai(MaCode);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(''dbo.KhuyenMaiKhachDung'') AND name=''UX_V16_OrderUsage'')
 CREATE UNIQUE INDEX UX_V16_OrderUsage ON dbo.KhuyenMaiKhachDung(MaDonHang);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(''dbo.KhuyenMaiKhachDung'') AND name=''IX_V16_CustomerUsage'')
 CREATE INDEX IX_V16_CustomerUsage ON dbo.KhuyenMaiKhachDung(MaKhuyenMai,MaKH);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(''dbo.DonHang'') AND name=''UX_DonHang_MaYeuCauDat'')
 CREATE UNIQUE INDEX UX_DonHang_MaYeuCauDat ON dbo.DonHang(MaYeuCauDat) WHERE MaYeuCauDat IS NOT NULL;
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name=''CK_V16_Voucher'')
 ALTER TABLE dbo.KhuyenMai WITH CHECK ADD CONSTRAINT CK_V16_Voucher CHECK (
  LoaiGiam IN (N''PhanTram'',N''SoTien'',N''GiamPhiVanChuyen'') AND GiaTriDonToiThieu>=0 AND SoLuongDaDung>=0
  AND (SoLuongToiDa IS NULL OR SoLuongToiDa>0) AND (GiamToiDa IS NULL OR GiamToiDa>=0)
  AND (PhanTramGiam IS NULL OR PhanTramGiam BETWEEN 1 AND 100) AND (SoTienGiam IS NULL OR SoTienGiam>=0));
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE parent_object_id=OBJECT_ID(''dbo.DonHang'') AND referenced_object_id=OBJECT_ID(''dbo.KhuyenMai''))
 ALTER TABLE dbo.DonHang WITH CHECK ADD CONSTRAINT FK_V16_OrderVoucher FOREIGN KEY(MaKhuyenMaiApDung) REFERENCES dbo.KhuyenMai(MaKhuyenMai);
');
COMMIT;
END TRY
BEGIN CATCH
 IF @@TRANCOUNT > 0 ROLLBACK;
 THROW;
END CATCH;
