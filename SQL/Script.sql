SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_PADDING ON;
SET ANSI_WARNINGS ON;
SET ARITHABORT ON;
SET CONCAT_NULL_YIELDS_NULL ON;
SET NUMERIC_ROUNDABORT OFF;
GO

USE QuanLyDichVuAnUong;
GO

IF COL_LENGTH(N'dbo.DanhMuc', N'DaAn') IS NULL
    ALTER TABLE dbo.DanhMuc ADD DaAn BIT NOT NULL CONSTRAINT DF_DanhMuc_DaAn DEFAULT 0;
GO

/* Nâng cấp v16: chiến dịch quảng cáo của gian hàng. Có thể chạy lặp lại an toàn. */
IF OBJECT_ID(N'dbo.ChienDichQuangCao', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ChienDichQuangCao (
        MaChienDich   INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_ChienDichQuangCao PRIMARY KEY,
        MaGianHang    INT NOT NULL,
        TenChienDich  NVARCHAR(150) NOT NULL,
        LoaiViTri     NVARCHAR(20) NOT NULL CONSTRAINT DF_ChienDichQuangCao_LoaiViTri DEFAULT N'NoiBat',
        TieuDe        NVARCHAR(150) NOT NULL,
        NoiDung       NVARCHAR(500) NULL,
        HinhAnh       NVARCHAR(500) NULL,
        NgayBatDau    DATETIME2 NOT NULL,
        NgayKetThuc   DATETIME2 NOT NULL,
        NganSach      DECIMAL(12,0) NOT NULL CONSTRAINT DF_ChienDichQuangCao_NganSach DEFAULT 0,
        TrangThai     NVARCHAR(20) NOT NULL CONSTRAINT DF_ChienDichQuangCao_TrangThai DEFAULT N'BanNhap',
        LuotHienThi   INT NOT NULL CONSTRAINT DF_ChienDichQuangCao_LuotHienThi DEFAULT 0,
        LuotTuongTac  INT NOT NULL CONSTRAINT DF_ChienDichQuangCao_LuotTuongTac DEFAULT 0,
        NgayTao       DATETIME2 NOT NULL CONSTRAINT DF_ChienDichQuangCao_NgayTao DEFAULT GETDATE(),
        CONSTRAINT FK_ChienDichQuangCao_GianHang FOREIGN KEY (MaGianHang)
            REFERENCES dbo.GianHang(MaGianHang) ON DELETE CASCADE,
        CONSTRAINT CK_ChienDichQuangCao_LoaiViTri CHECK (LoaiViTri IN (N'Banner', N'NoiBat', N'GoiY')),
        CONSTRAINT CK_ChienDichQuangCao_TrangThai CHECK (TrangThai IN (N'BanNhap', N'DangChay', N'TamDung')),
        CONSTRAINT CK_ChienDichQuangCao_ThoiGian CHECK (NgayKetThuc > NgayBatDau),
        CONSTRAINT CK_ChienDichQuangCao_NganSach CHECK (NganSach >= 0)
    );
END;
GO

-- End of the idempotent database upgrade script.
IF OBJECT_ID(N'dbo.ChienDichQuangCao', N'U') IS NOT NULL
   AND NOT EXISTS (
       SELECT 1 FROM sys.indexes
       WHERE object_id = OBJECT_ID(N'dbo.ChienDichQuangCao')
         AND name = N'IX_ChienDichQuangCao_MaGianHang_TrangThai_ThoiGian'
   )
BEGIN
    CREATE INDEX IX_ChienDichQuangCao_MaGianHang_TrangThai_ThoiGian
        ON dbo.ChienDichQuangCao(MaGianHang, TrangThai, NgayBatDau, NgayKetThuc);
END;
GO
-- End of script.
