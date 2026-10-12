/* NÂNG CẤP CSDL ĐANG CÓ -> thêm hạ tầng backend (token, OTP, nhật ký, audit, thông báo, tệp, chỉ mục).
   Chạy SAU NangCap_v8.sql. KHÔNG xóa dữ liệu, chạy lại nhiều lần an toàn. */

/* ============================================================
   PHẦN 12: BỔ SUNG HẠ TẦNG BACKEND (chạy được NHIỀU LẦN, an toàn)
   12.1 TaiKhoan: khóa tạm khi sai mật khẩu nhiều lần, xác thực email/SĐT
   12.2 Cột còn thiếu: GianHang.Email, ảnh đại diện, DonHang.TienGiam/HenGiaoLuc, PhanHoi.DangXuLy
   12.3 Bảng mới: RefreshToken, OtpXacThuc, NhatKyHeThong, AuditLog, ThongBao, TepTin
   12.4 Chỉ mục + FULLTEXT (tìm kiếm nhanh)
   12.5 Trigger audit: ghi ai/khi nào/đổi gì ở các bảng quan trọng (ghi đè JSON cũ/mới, KHÔNG ghi mật khẩu)
   12.6 sp_DonDepDuLieu: dọn token/OTP/nhật ký quá hạn
   Việc sao lưu: xem SQL_NangCap/SaoLuu_PhucHoi.sql
   ============================================================ */
USE QuanLyDichVuAnUong;
GO

/* ---- 12.1 TaiKhoan ---- */
IF COL_LENGTH('TaiKhoan', 'SoLanDangNhapSai') IS NULL
    ALTER TABLE TaiKhoan ADD SoLanDangNhapSai INT NOT NULL CONSTRAINT DF_TaiKhoan_SaiLan DEFAULT 0;
IF COL_LENGTH('TaiKhoan', 'KhoaDen') IS NULL
    ALTER TABLE TaiKhoan ADD KhoaDen DATETIME NULL;                 -- khóa tạm tới thời điểm này
IF COL_LENGTH('TaiKhoan', 'LanDangNhapCuoi') IS NULL
    ALTER TABLE TaiKhoan ADD LanDangNhapCuoi DATETIME NULL;
IF COL_LENGTH('TaiKhoan', 'EmailDaXacThuc') IS NULL
    ALTER TABLE TaiKhoan ADD EmailDaXacThuc BIT NOT NULL CONSTRAINT DF_TaiKhoan_EmailXT DEFAULT 0;
IF COL_LENGTH('TaiKhoan', 'SdtDaXacThuc') IS NULL
    ALTER TABLE TaiKhoan ADD SdtDaXacThuc BIT NOT NULL CONSTRAINT DF_TaiKhoan_SdtXT DEFAULT 0;
GO

/* ---- 12.2 Cột còn thiếu ---- */
IF COL_LENGTH('GianHang', 'Email') IS NULL      ALTER TABLE GianHang ADD Email VARCHAR(100) NULL;
IF COL_LENGTH('KhachHang', 'AnhDaiDien') IS NULL ALTER TABLE KhachHang ADD AnhDaiDien NVARCHAR(500) NULL;
IF COL_LENGTH('TaiXe', 'AnhDaiDien') IS NULL     ALTER TABLE TaiXe ADD AnhDaiDien NVARCHAR(500) NULL;
IF COL_LENGTH('DonHang', 'TienGiam') IS NULL
    ALTER TABLE DonHang ADD TienGiam DECIMAL(12,0) NOT NULL CONSTRAINT DF_DonHang_TienGiam DEFAULT 0;  -- đã trừ khỏi TongTien
IF COL_LENGTH('DonHang', 'HenGiaoLuc') IS NULL   ALTER TABLE DonHang ADD HenGiaoLuc DATETIME NULL;      -- đơn hẹn giờ
GO

/* PhanHoi.TrangThai: thêm 'DangXuLy' (app có 3 trạng thái: mới / đang xử lý / đã giải quyết) */
DECLARE @ck sysname;
SELECT @ck = cc.name FROM sys.check_constraints cc
WHERE cc.parent_object_id = OBJECT_ID('PhanHoi') AND cc.definition LIKE '%ChuaXuLy%' AND cc.name <> 'CK_PhanHoi_TrangThai';
IF @ck IS NOT NULL EXEC('ALTER TABLE PhanHoi DROP CONSTRAINT ' + @ck);
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CK_PhanHoi_TrangThai')
    ALTER TABLE PhanHoi ADD CONSTRAINT CK_PhanHoi_TrangThai CHECK (TrangThai IN (N'ChuaXuLy', N'DangXuLy', N'DaXuLy'));
GO

/* Đồng bộ dữ liệu mẫu với app: quản trị mẫu đăng nhập bằng admin@fooddelivery.vn; gian hàng mẫu có email */
UPDATE QuanTri SET Email = 'admin@fooddelivery.vn'
WHERE MaTK = (SELECT MaTK FROM TaiKhoan WHERE TenDangNhap = N'quantri01')
  AND NOT EXISTS (SELECT 1 FROM QuanTri WHERE Email = 'admin@fooddelivery.vn');
UPDATE gh SET Email = LOWER(tk.TenDangNhap) + '@foodapp.vn'
FROM GianHang gh JOIN TaiKhoan tk ON tk.MaTK = gh.MaTK WHERE gh.Email IS NULL AND LEN(tk.TenDangNhap) <= 80;
GO

/* ---- 12.3 Bảng mới ---- */
IF OBJECT_ID('RefreshToken', 'U') IS NULL
BEGIN
    CREATE TABLE RefreshToken (
        MaRT        BIGINT IDENTITY(1,1) PRIMARY KEY,
        MaTK        INT NOT NULL REFERENCES TaiKhoan(MaTK) ON DELETE CASCADE,
        TokenHash   CHAR(64) NOT NULL UNIQUE,          -- SHA-256 của token; KHÔNG lưu token gốc
        NgayTao     DATETIME NOT NULL DEFAULT GETDATE(),
        HetHan      DATETIME NOT NULL,
        ThuHoiLuc   DATETIME NULL,
        ThayBangHash CHAR(64) NULL,                    -- token mới thay thế (xoay vòng)
        DiaChiIP    NVARCHAR(45) NULL,
        ThietBi     NVARCHAR(200) NULL
    );
    CREATE INDEX IX_RefreshToken_MaTK ON RefreshToken (MaTK, HetHan);
END
GO

IF OBJECT_ID('OtpXacThuc', 'U') IS NULL
BEGIN
    CREATE TABLE OtpXacThuc (
        MaOTP       BIGINT IDENTITY(1,1) PRIMARY KEY,
        MucDich     NVARCHAR(30) NOT NULL CHECK (MucDich IN (N'DangKy', N'QuenMatKhau', N'DoiLienHe', N'XacNhan')),
        Kenh        VARCHAR(10)  NOT NULL CHECK (Kenh IN ('email', 'sms')),
        NguoiNhan   NVARCHAR(150) NOT NULL,
        MaTK        INT NULL REFERENCES TaiKhoan(MaTK),
        OtpHash     CHAR(64) NOT NULL,                  -- HMAC-SHA256, không lưu mã gốc
        NgayTao     DATETIME NOT NULL DEFAULT GETDATE(),
        HetHan      DATETIME NOT NULL,
        SoLanSai    INT NOT NULL DEFAULT 0,
        DaDung      BIT NOT NULL DEFAULT 0
    );
    CREATE INDEX IX_Otp_NguoiNhan ON OtpXacThuc (NguoiNhan, MucDich, NgayTao DESC);
END
GO

IF OBJECT_ID('NhatKyHeThong', 'U') IS NULL
BEGIN
    CREATE TABLE NhatKyHeThong (
        MaLog       BIGINT IDENTITY(1,1) PRIMARY KEY,
        ThoiGian    DATETIME2(3) NOT NULL DEFAULT SYSDATETIME(),
        MucDo       VARCHAR(10)  NOT NULL CHECK (MucDo IN ('Info', 'Warn', 'Error')),
        Nguon       NVARCHAR(30) NOT NULL,              -- Http | Auth | Loi | Upload | Otp | Sao luu ...
        ThongDiep   NVARCHAR(500) NOT NULL,
        ChiTiet     NVARCHAR(MAX) NULL,                 -- stack trace (chỉ khi Error)
        MaTK        INT NULL,
        DiaChiIP    NVARCHAR(45) NULL,
        PhuongThuc  VARCHAR(10) NULL,
        DuongDan    NVARCHAR(300) NULL,
        MaTrangThai INT NULL,
        ThoiGianMs  INT NULL
    );
    CREATE INDEX IX_NhatKy_ThoiGian ON NhatKyHeThong (ThoiGian DESC);
    CREATE INDEX IX_NhatKy_MucDo ON NhatKyHeThong (MucDo, ThoiGian DESC);
END
GO

IF OBJECT_ID('AuditLog', 'U') IS NULL
BEGIN
    CREATE TABLE AuditLog (
        MaAudit     BIGINT IDENTITY(1,1) PRIMARY KEY,
        ThoiGian    DATETIME2(3) NOT NULL DEFAULT SYSDATETIME(),
        MaTK        INT NULL,                           -- người thao tác (từ SESSION_CONTEXT, NULL = thao tác trực tiếp trên DB)
        HanhDong    NVARCHAR(10) NOT NULL,              -- INSERT | UPDATE | DELETE
        Bang        NVARCHAR(60) NOT NULL,
        KhoaChinh   NVARCHAR(50) NOT NULL,
        GiaTriCu    NVARCHAR(MAX) NULL,                 -- JSON
        GiaTriMoi   NVARCHAR(MAX) NULL                  -- JSON
    );
    CREATE INDEX IX_Audit_Bang ON AuditLog (Bang, KhoaChinh, ThoiGian DESC);
    CREATE INDEX IX_Audit_MaTK ON AuditLog (MaTK, ThoiGian DESC);
END
GO

IF OBJECT_ID('ThongBao', 'U') IS NULL
BEGIN
    CREATE TABLE ThongBao (
        MaThongBao  BIGINT IDENTITY(1,1) PRIMARY KEY,
        MaTK        INT NOT NULL REFERENCES TaiKhoan(MaTK) ON DELETE CASCADE,
        TieuDe      NVARCHAR(150) NOT NULL,
        NoiDung     NVARCHAR(500) NOT NULL,
        Loai        NVARCHAR(20) NOT NULL DEFAULT N'HeThong' CHECK (Loai IN (N'HeThong', N'DonHang', N'KhuyenMai', N'TinNhan')),
        MaThamChieu NVARCHAR(50) NULL,                  -- ví dụ Id đơn hàng
        DaDoc       BIT NOT NULL DEFAULT 0,
        NgayTao     DATETIME NOT NULL DEFAULT GETDATE()
    );
    CREATE INDEX IX_ThongBao_MaTK ON ThongBao (MaTK, DaDoc, NgayTao DESC);
END
GO

IF OBJECT_ID('TepTin', 'U') IS NULL
BEGIN
    CREATE TABLE TepTin (
        MaTep       BIGINT IDENTITY(1,1) PRIMARY KEY,
        MaTK        INT NULL REFERENCES TaiKhoan(MaTK),
        Loai        NVARCHAR(20) NOT NULL CHECK (Loai IN (N'MonAn', N'GianHang', N'AvatarKhach', N'AvatarTaiXe', N'GPLX', N'GiaoHang')),
        TenGoc      NVARCHAR(200) NULL,
        DuongDan    NVARCHAR(400) NOT NULL,             -- đường dẫn tương đối trong thư mục lưu trữ
        MimeType    VARCHAR(50) NOT NULL,
        KichThuoc   BIGINT NOT NULL,
        Sha256      CHAR(64) NOT NULL,
        RiengTu     BIT NOT NULL DEFAULT 0,             -- 1 = chỉ chủ sở hữu/quản trị xem (GPLX)
        NgayTai     DATETIME NOT NULL DEFAULT GETDATE()
    );
    CREATE INDEX IX_TepTin_MaTK ON TepTin (MaTK, Loai);
END
GO

/* ---- 12.4 Chỉ mục ---- */
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_DonHang_KhachHang')
    CREATE INDEX IX_DonHang_KhachHang ON DonHang (MaKH, NgayDat DESC);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_DonHang_GianHang')
    CREATE INDEX IX_DonHang_GianHang ON DonHang (MaGianHang, TrangThai, NgayDat DESC);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_DonHang_TaiXe')
    CREATE INDEX IX_DonHang_TaiXe ON DonHang (MaTaiXe, TrangThai);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_DonHang_ChoTaiXe')
    CREATE INDEX IX_DonHang_ChoTaiXe ON DonHang (TrangThai, SanSangGiao, MaTaiXe) WHERE MaTaiXe IS NULL;
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_ChiTiet_DonHang')
    CREATE INDEX IX_ChiTiet_DonHang ON ChiTietDonHang (MaDonHang) INCLUDE (MaMonAn, SoLuong, DonGia);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_MonAn_DanhMuc')
    CREATE INDEX IX_MonAn_DanhMuc ON MonAn (MaDanhMuc, TinhTrang);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_DanhMuc_GianHang')
    CREATE INDEX IX_DanhMuc_GianHang ON DanhMuc (MaGianHang);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_GianHang_Duyet')
    CREATE INDEX IX_GianHang_Duyet ON GianHang (TrangThaiDuyet, TrangThaiKinhDoanh);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_GianHang_Email')
    CREATE INDEX IX_GianHang_Email ON GianHang (Email) WHERE Email IS NOT NULL;
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_GianHang_SDT')
    CREATE INDEX IX_GianHang_SDT ON GianHang (SDT);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_KhachHang_Email')
    CREATE INDEX IX_KhachHang_Email ON KhachHang (Email) WHERE Email IS NOT NULL;
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_KhachHang_SDT')
    CREATE INDEX IX_KhachHang_SDT ON KhachHang (SDT);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_TaiXe_SDT')
    CREATE INDEX IX_TaiXe_SDT ON TaiXe (SDT);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_GiaoHang_TaiXe')
    CREATE INDEX IX_GiaoHang_TaiXe ON GiaoHang (MaTaiXe, TrangThai);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_DanhGia_GianHang')
    CREATE INDEX IX_DanhGia_GianHang ON DanhGia (MaGianHang) INCLUDE (SoSao);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_KhuyenMai_Hieu')
    CREATE INDEX IX_KhuyenMai_Hieu ON KhuyenMai (TrangThai, NgayKetThuc);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_PhanHoi_TrangThai')
    CREATE INDEX IX_PhanHoi_TrangThai ON PhanHoi (TrangThai, NgayGui DESC);
GO

/* FULLTEXT cho tìm kiếm tên quán / món (cần cài tính năng Full-Text; không có thì bỏ qua, server tự dùng LIKE) */
IF FULLTEXTSERVICEPROPERTY('IsFullTextInstalled') = 1
BEGIN
    BEGIN TRY
        IF NOT EXISTS (SELECT 1 FROM sys.fulltext_catalogs WHERE name = 'ftc_FoodService')
            EXEC('CREATE FULLTEXT CATALOG ftc_FoodService');

        DECLARE @pk sysname, @s NVARCHAR(500);
        IF NOT EXISTS (SELECT 1 FROM sys.fulltext_indexes WHERE object_id = OBJECT_ID('MonAn'))
        BEGIN
            SELECT @pk = name FROM sys.indexes WHERE object_id = OBJECT_ID('MonAn') AND is_primary_key = 1;
            SET @s = N'CREATE FULLTEXT INDEX ON MonAn (TenMon LANGUAGE 0, MoTa LANGUAGE 0) KEY INDEX ' + QUOTENAME(@pk) + N' ON ftc_FoodService WITH CHANGE_TRACKING AUTO';
            EXEC(@s);
        END
        IF NOT EXISTS (SELECT 1 FROM sys.fulltext_indexes WHERE object_id = OBJECT_ID('GianHang'))
        BEGIN
            SELECT @pk = name FROM sys.indexes WHERE object_id = OBJECT_ID('GianHang') AND is_primary_key = 1;
            SET @s = N'CREATE FULLTEXT INDEX ON GianHang (TenCuaHang LANGUAGE 0, MoTa LANGUAGE 0) KEY INDEX ' + QUOTENAME(@pk) + N' ON ftc_FoodService WITH CHANGE_TRACKING AUTO';
            EXEC(@s);
        END
    END TRY
    BEGIN CATCH
        PRINT N'Bỏ qua FULLTEXT: ' + ERROR_MESSAGE();
    END CATCH
END
ELSE PRINT N'Full-Text chưa được cài: server sẽ tìm kiếm bằng LIKE.';
GO

/* ---- 12.5 Trigger audit ---- */
-- Bảng thường: ghi JSON giá trị cũ/mới của cả dòng. Người thao tác lấy từ SESSION_CONTEXT('MaTK') do server đặt.
DECLARE @bang TABLE (Ten SYSNAME, Pk SYSNAME);
INSERT INTO @bang VALUES (N'GianHang', N'MaGianHang'), (N'TaiXe', N'MaTaiXe'), (N'KhuyenMai', N'MaKhuyenMai'),
                         (N'MonAn', N'MaMonAn'), (N'DonHang', N'MaDonHang'), (N'ThanhToan', N'MaThanhToan'),
                         (N'PhanHoi', N'MaPhanHoi'), (N'KhachHang', N'MaKH');
DECLARE @t SYSNAME, @key SYSNAME, @ci NVARCHAR(MAX), @cd NVARCHAR(MAX), @sql NVARCHAR(MAX);
DECLARE cur CURSOR LOCAL FAST_FORWARD FOR SELECT Ten, Pk FROM @bang;
OPEN cur;
FETCH NEXT FROM cur INTO @t, @key;
WHILE @@FETCH_STATUS = 0
BEGIN
    SELECT @ci = STRING_AGG(CAST(N'i.' + QUOTENAME(COLUMN_NAME) AS NVARCHAR(MAX)), N',') WITHIN GROUP (ORDER BY ORDINAL_POSITION),
           @cd = STRING_AGG(CAST(N'd.' + QUOTENAME(COLUMN_NAME) AS NVARCHAR(MAX)), N',') WITHIN GROUP (ORDER BY ORDINAL_POSITION)
    FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = @t;

    SET @sql = N'CREATE OR ALTER TRIGGER ' + QUOTENAME(N'trg_Audit_' + @t) + N' ON ' + QUOTENAME(@t) + N' AFTER INSERT, UPDATE, DELETE AS
BEGIN
    SET NOCOUNT ON;
    IF NOT EXISTS (SELECT 1 FROM inserted) AND NOT EXISTS (SELECT 1 FROM deleted) RETURN;
    DECLARE @tk INT = TRY_CAST(SESSION_CONTEXT(N''MaTK'') AS INT);
    INSERT INTO AuditLog (MaTK, HanhDong, Bang, KhoaChinh, GiaTriCu, GiaTriMoi)
    SELECT @tk,
           CASE WHEN d.' + QUOTENAME(@key) + N' IS NULL THEN N''INSERT'' WHEN i.' + QUOTENAME(@key) + N' IS NULL THEN N''DELETE'' ELSE N''UPDATE'' END,
           N''' + @t + N''',
           CAST(COALESCE(i.' + QUOTENAME(@key) + N', d.' + QUOTENAME(@key) + N') AS NVARCHAR(50)),
           CASE WHEN d.' + QUOTENAME(@key) + N' IS NULL THEN NULL ELSE (SELECT ' + @cd + N' FOR JSON PATH, WITHOUT_ARRAY_WRAPPER, INCLUDE_NULL_VALUES) END,
           CASE WHEN i.' + QUOTENAME(@key) + N' IS NULL THEN NULL ELSE (SELECT ' + @ci + N' FOR JSON PATH, WITHOUT_ARRAY_WRAPPER, INCLUDE_NULL_VALUES) END
    FROM inserted i FULL OUTER JOIN deleted d ON i.' + QUOTENAME(@key) + N' = d.' + QUOTENAME(@key) + N';
END';
    EXEC(@sql);
    FETCH NEXT FROM cur INTO @t, @key;
END
CLOSE cur; DEALLOCATE cur;
GO

-- TaiKhoan: không ghi mật khẩu; chỉ ghi khi đổi trạng thái / vai trò / tên đăng nhập / mật khẩu (chỉ cờ DoiMatKhau)
CREATE OR ALTER TRIGGER trg_Audit_TaiKhoan ON TaiKhoan AFTER INSERT, UPDATE, DELETE AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @tk INT = TRY_CAST(SESSION_CONTEXT(N'MaTK') AS INT);
    INSERT INTO AuditLog (MaTK, HanhDong, Bang, KhoaChinh, GiaTriCu, GiaTriMoi)
    SELECT @tk,
           CASE WHEN d.MaTK IS NULL THEN N'INSERT' WHEN i.MaTK IS NULL THEN N'DELETE' ELSE N'UPDATE' END,
           N'TaiKhoan', CAST(COALESCE(i.MaTK, d.MaTK) AS NVARCHAR(50)),
           CASE WHEN d.MaTK IS NULL THEN NULL
                ELSE (SELECT d.TenDangNhap, d.VaiTro, d.TrangThai FOR JSON PATH, WITHOUT_ARRAY_WRAPPER, INCLUDE_NULL_VALUES) END,
           CASE WHEN i.MaTK IS NULL THEN NULL
                ELSE (SELECT i.TenDangNhap, i.VaiTro, i.TrangThai,
                             CAST(CASE WHEN d.MaTK IS NOT NULL AND i.MatKhau <> d.MatKhau THEN 1 ELSE 0 END AS BIT) AS DoiMatKhau
                      FOR JSON PATH, WITHOUT_ARRAY_WRAPPER, INCLUDE_NULL_VALUES) END
    FROM inserted i FULL OUTER JOIN deleted d ON i.MaTK = d.MaTK
    WHERE d.MaTK IS NULL OR i.MaTK IS NULL
       OR i.TrangThai <> d.TrangThai OR i.VaiTro <> d.VaiTro OR i.TenDangNhap <> d.TenDangNhap OR i.MatKhau <> d.MatKhau;
END
GO

/* ---- 12.6 Dọn dữ liệu quá hạn ---- */
CREATE OR ALTER PROCEDURE sp_DonDepDuLieu
    @GiuNhatKyNgay INT = 90,
    @GiuAuditNgay  INT = 365
AS
BEGIN
    SET NOCOUNT ON;
    DELETE FROM RefreshToken WHERE HetHan < DATEADD(DAY, -7, GETDATE()) OR (ThuHoiLuc IS NOT NULL AND ThuHoiLuc < DATEADD(DAY, -7, GETDATE()));
    DELETE FROM OtpXacThuc   WHERE NgayTao < DATEADD(DAY, -1, GETDATE());
    DELETE FROM NhatKyHeThong WHERE ThoiGian < DATEADD(DAY, -@GiuNhatKyNgay, GETDATE());
    DELETE FROM AuditLog     WHERE ThoiGian < DATEADD(DAY, -@GiuAuditNgay, GETDATE());
    DELETE FROM ThongBao     WHERE DaDoc = 1 AND NgayTao < DATEADD(DAY, -60, GETDATE());
END
GO

