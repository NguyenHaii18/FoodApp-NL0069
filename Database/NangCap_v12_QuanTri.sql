/* =========================================================================
   NÂNG CẤP v12 — CHỨC NĂNG QUẢN TRỊ (chạy được NHIỀU LẦN, an toàn)
   Lịch sử trạng thái đơn, yêu cầu hoàn tiền, cấu hình hệ thống, nhật ký điều phối,
   kiểm duyệt đánh giá/món, phân công khiếu nại, đối soát COD.
   ========================================================================= */
USE QuanLyDichVuAnUong;
GO

IF OBJECT_ID('LichSuTrangThaiDon', 'U') IS NULL
    CREATE TABLE LichSuTrangThaiDon (
        MaLichSu   BIGINT IDENTITY(1,1) PRIMARY KEY,
        MaDonHang  INT NOT NULL REFERENCES DonHang(MaDonHang) ON DELETE CASCADE,
        TuTrangThai NVARCHAR(30) NULL,
        DenTrangThai NVARCHAR(30) NOT NULL,
        LyDo       NVARCHAR(300) NULL,
        MaTK       INT NULL,
        ThoiGian   DATETIME NOT NULL DEFAULT GETDATE()
    );
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_LichSuTrangThaiDon' AND object_id = OBJECT_ID('LichSuTrangThaiDon'))
    CREATE INDEX IX_LichSuTrangThaiDon ON LichSuTrangThaiDon(MaDonHang, ThoiGian);
GO

IF OBJECT_ID('YeuCauHoanTien', 'U') IS NULL
    CREATE TABLE YeuCauHoanTien (
        MaYeuCau   INT IDENTITY(1,1) PRIMARY KEY,
        MaDonHang  INT NOT NULL REFERENCES DonHang(MaDonHang) ON DELETE CASCADE,
        MaKH       INT NULL,
        SoTien     DECIMAL(12,0) NOT NULL CHECK (SoTien >= 0),
        LyDo       NVARCHAR(400) NOT NULL,
        Nguon      NVARCHAR(20) NOT NULL DEFAULT N'KhachHang',     -- KhachHang | QuanTri | KhieuNai
        TrangThai  NVARCHAR(15) NOT NULL DEFAULT N'ChoDuyet' CHECK (TrangThai IN (N'ChoDuyet', N'DaDuyet', N'TuChoi')),
        MaTKXuLy   INT NULL,
        KetQua     NVARCHAR(400) NULL,
        NgayTao    DATETIME NOT NULL DEFAULT GETDATE(),
        NgayXuLy   DATETIME NULL
    );
GO

IF OBJECT_ID('CauHinhHeThong', 'U') IS NULL
    CREATE TABLE CauHinhHeThong (
        Khoa      NVARCHAR(60) NOT NULL PRIMARY KEY,
        GiaTri    NVARCHAR(300) NOT NULL,
        MoTa      NVARCHAR(300) NULL,
        MaTKSua   INT NULL,
        NgaySua   DATETIME NOT NULL DEFAULT GETDATE()
    );
GO
IF OBJECT_ID('CauHinhHeThongLichSu', 'U') IS NULL
    CREATE TABLE CauHinhHeThongLichSu (
        MaLichSu  BIGINT IDENTITY(1,1) PRIMARY KEY,
        Khoa      NVARCHAR(60) NOT NULL,
        GiaTriCu  NVARCHAR(300) NULL,
        GiaTriMoi NVARCHAR(300) NOT NULL,
        MaTK      INT NULL,
        ThoiGian  DATETIME NOT NULL DEFAULT GETDATE()
    );
GO
MERGE CauHinhHeThong AS t
USING (VALUES
  (N'Giao.PhiMoiKm',        N'5000',   N'Phí giao hàng cho mỗi km (đ)'),
  (N'Giao.PhiMacDinh',      N'15000',  N'Phí giao mặc định khi không xác định được khoảng cách (đ)'),
  (N'Giao.BanKinhKm',       N'20',     N'Bán kính phục vụ tối đa (km)'),
  (N'HoaHong.MacDinh',      N'0.15',   N'Phí nền tảng/hoa hồng mặc định cho gian hàng mới (0.15 = 15%)'),
  (N'Don.TuHuyPhut',        N'15',     N'Khách được tự hủy khi đơn còn Chờ xác nhận (không giới hạn phút nếu để 0)'),
  (N'Don.DuKienPhut',       N'45',     N'Thời gian dự kiến xử lý + giao đơn (phút)'),
  (N'Don.TreSauPhut',       N'30',     N'Đơn bị coi là trễ khi quá thời gian dự kiến thêm bao nhiêu phút'),
  (N'Don.KetTrangThaiPhut', N'60',     N'Đơn đứng yên một trạng thái quá bao nhiêu phút thì coi là bị kẹt'),
  (N'HoanTien.NguongQuanLy',N'200000', N'Quản lý được duyệt hoàn tiền tới mức này (đ); trên mức cần Admin tổng'),
  (N'HoanTien.ChinhSach',   N'Hoàn tiền khi đơn đã thanh toán online bị hủy hoặc giao thất bại do lỗi gian hàng/tài xế; xét theo từng trường hợp với khiếu nại.', N'Nội dung chính sách hoàn tiền hiển thị cho khách'),
  (N'Voucher.ToiDaMoiKhach',N'5',      N'Giới hạn số lần một khách dùng cùng một mã trong 30 ngày (cảnh báo lạm dụng)'),
  (N'QuyDinh.KhachHang',    N'Khách hàng cung cấp thông tin đúng, không đặt đơn ảo, không lạm dụng khuyến mãi.', N'Quy định áp dụng cho khách hàng'),
  (N'QuyDinh.GianHang',     N'Gian hàng đăng thông tin món/giá đúng thực tế, không đăng nội dung vi phạm, xử lý đơn đúng thời hạn.', N'Quy định áp dụng cho gian hàng'),
  (N'QuyDinh.TaiXe',        N'Tài xế giao đúng đơn, giữ thái độ phục vụ, không từ chối đơn đã nhận vô cớ.', N'Quy định áp dụng cho tài xế')
) AS s(Khoa, GiaTri, MoTa) ON t.Khoa = s.Khoa
WHEN NOT MATCHED THEN INSERT (Khoa, GiaTri, MoTa) VALUES (s.Khoa, s.GiaTri, s.MoTa);
GO

IF OBJECT_ID('DieuPhoiLog', 'U') IS NULL
    CREATE TABLE DieuPhoiLog (
        MaLog       BIGINT IDENTITY(1,1) PRIMARY KEY,
        MaDonHang   INT NOT NULL REFERENCES DonHang(MaDonHang) ON DELETE CASCADE,
        HanhDong    NVARCHAR(30) NOT NULL,          -- PhanCong | DoiTaiXe | GoTaiXe | HuyDon
        MaTaiXeCu   INT NULL,
        MaTaiXeMoi  INT NULL,
        LyDo        NVARCHAR(300) NOT NULL,
        MaTK        INT NULL,
        ThoiGian    DATETIME NOT NULL DEFAULT GETDATE()
    );
GO

/* ---- Cột bổ sung ---- */
IF COL_LENGTH('DanhGia', 'DaAn') IS NULL ALTER TABLE DanhGia ADD DaAn BIT NOT NULL CONSTRAINT DF_DanhGia_DaAn DEFAULT 0;
IF COL_LENGTH('DanhGia', 'LyDoAn') IS NULL ALTER TABLE DanhGia ADD LyDoAn NVARCHAR(255) NULL;
IF COL_LENGTH('BaoCaoDanhGia', 'KetQua') IS NULL ALTER TABLE BaoCaoDanhGia ADD KetQua NVARCHAR(255) NULL;
IF COL_LENGTH('BaoCaoDanhGia', 'NgayXuLy') IS NULL ALTER TABLE BaoCaoDanhGia ADD NgayXuLy DATETIME NULL;
IF COL_LENGTH('PhanHoi', 'MaTKPhuTrach') IS NULL ALTER TABLE PhanHoi ADD MaTKPhuTrach INT NULL;
IF COL_LENGTH('PhanHoi', 'YeuCauBoSung') IS NULL ALTER TABLE PhanHoi ADD YeuCauBoSung NVARCHAR(400) NULL;
IF COL_LENGTH('PhanHoi', 'PhanLoai') IS NULL ALTER TABLE PhanHoi ADD PhanLoai NVARCHAR(30) NULL;   -- DonHang | MonAn | TaiXe | GianHang | ThanhToan | Khac
IF COL_LENGTH('GianHang', 'YeuCauBoSung') IS NULL ALTER TABLE GianHang ADD YeuCauBoSung NVARCHAR(400) NULL;
IF COL_LENGTH('GianHang', 'LyDoTuChoi') IS NULL ALTER TABLE GianHang ADD LyDoTuChoi NVARCHAR(255) NULL;
IF COL_LENGTH('TaiXe', 'YeuCauBoSung') IS NULL ALTER TABLE TaiXe ADD YeuCauBoSung NVARCHAR(400) NULL;
IF COL_LENGTH('MonAn', 'LyDoAn') IS NULL ALTER TABLE MonAn ADD LyDoAn NVARCHAR(255) NULL;
IF COL_LENGTH('DonHang', 'TienGiam') IS NULL ALTER TABLE DonHang ADD TienGiam DECIMAL(12,0) NOT NULL CONSTRAINT DF_DonHang_TienGiam DEFAULT 0;
IF COL_LENGTH('ThanhToan', 'DaDoiSoat') IS NULL ALTER TABLE ThanhToan ADD DaDoiSoat BIT NOT NULL CONSTRAINT DF_ThanhToan_DaDoiSoat DEFAULT 0;
IF COL_LENGTH('ThanhToan', 'NgayDoiSoat') IS NULL ALTER TABLE ThanhToan ADD NgayDoiSoat DATETIME NULL;
GO
