/* ============================================================
   SAO LƯU / PHỤC HỒI CSDL QuanLyDichVuAnUong
   Chiến lược: Full mỗi đêm 02:00 + Differential mỗi 6 giờ + Log mỗi 15 phút (mất tối đa ~15 phút dữ liệu)
   + dọn dữ liệu quá hạn lúc 03:00. Chạy trên SQL Server có SQL Server Agent (không dùng được trên Express: xem mục B).
   ĐỔI @Dir sang thư mục có quyền ghi của tài khoản chạy SQL Server, và nên là ổ đĩa/NAS khác máy DB.
   ============================================================ */

/* ---------- A. Thiết lập ---------- */
USE master;
GO
ALTER DATABASE QuanLyDichVuAnUong SET RECOVERY FULL;     -- bắt buộc để sao lưu Log
GO

/* ---------- B. Sao lưu thủ công (chạy được trên mọi phiên bản, kể cả Express) ---------- */
DECLARE @Dir NVARCHAR(300) = N'C:\Backup\FoodService\';
DECLARE @f NVARCHAR(400) = @Dir + N'Full_' + FORMAT(GETDATE(), 'yyyyMMdd_HHmmss') + N'.bak';
BACKUP DATABASE QuanLyDichVuAnUong TO DISK = @f
    WITH COMPRESSION, CHECKSUM, INIT, STATS = 10, NAME = N'FoodService Full';
RESTORE VERIFYONLY FROM DISK = @f WITH CHECKSUM;         -- kiểm tra file sao lưu đọc được
GO

/* Differential:
DECLARE @f NVARCHAR(400) = N'C:\Backup\FoodService\Diff_' + FORMAT(GETDATE(), 'yyyyMMdd_HHmmss') + N'.bak';
BACKUP DATABASE QuanLyDichVuAnUong TO DISK = @f WITH DIFFERENTIAL, COMPRESSION, CHECKSUM, INIT;
   Log:
DECLARE @f NVARCHAR(400) = N'C:\Backup\FoodService\Log_' + FORMAT(GETDATE(), 'yyyyMMdd_HHmmss') + N'.trn';
BACKUP LOG QuanLyDichVuAnUong TO DISK = @f WITH COMPRESSION, CHECKSUM, INIT;
*/

/* ---------- C. Tạo lịch tự động bằng SQL Server Agent ---------- */
USE msdb;
GO
DECLARE @Dir NVARCHAR(300) = N'C:\Backup\FoodService\';     -- phải trùng thư mục ở trên
DECLARE @DB  sysname = N'QuanLyDichVuAnUong';

DECLARE @jobs TABLE (Ten sysname, Lenh NVARCHAR(MAX), FreqType INT, FreqInterval INT, SubType INT, SubInterval INT, StartTime INT);
INSERT INTO @jobs VALUES
 (N'FoodService - Full backup 02:00',
  N'DECLARE @f NVARCHAR(400)=N''' + @Dir + N'Full_''+FORMAT(GETDATE(),''yyyyMMdd_HHmmss'')+N''.bak''; '
  + N'BACKUP DATABASE ' + QUOTENAME(@DB) + N' TO DISK=@f WITH COMPRESSION, CHECKSUM, INIT; RESTORE VERIFYONLY FROM DISK=@f WITH CHECKSUM;',
  4, 1, 1, 0, 020000),
 (N'FoodService - Differential 6 gio',
  N'DECLARE @f NVARCHAR(400)=N''' + @Dir + N'Diff_''+FORMAT(GETDATE(),''yyyyMMdd_HHmmss'')+N''.bak''; '
  + N'BACKUP DATABASE ' + QUOTENAME(@DB) + N' TO DISK=@f WITH DIFFERENTIAL, COMPRESSION, CHECKSUM, INIT;',
  4, 1, 8, 6, 080000),
 (N'FoodService - Log backup 15 phut',
  N'DECLARE @f NVARCHAR(400)=N''' + @Dir + N'Log_''+FORMAT(GETDATE(),''yyyyMMdd_HHmmss'')+N''.trn''; '
  + N'BACKUP LOG ' + QUOTENAME(@DB) + N' TO DISK=@f WITH COMPRESSION, CHECKSUM, INIT;',
  4, 1, 4, 15, 000500),
 (N'FoodService - Don dep du lieu 03:00',
  N'EXEC ' + QUOTENAME(@DB) + N'.dbo.sp_DonDepDuLieu;',
  4, 1, 1, 0, 030000);

DECLARE @n sysname, @c NVARCHAR(MAX), @ft INT, @fi INT, @st INT, @si INT, @t INT;
DECLARE jc CURSOR LOCAL FAST_FORWARD FOR SELECT Ten, Lenh, FreqType, FreqInterval, SubType, SubInterval, StartTime FROM @jobs;
OPEN jc; FETCH NEXT FROM jc INTO @n, @c, @ft, @fi, @st, @si, @t;
WHILE @@FETCH_STATUS = 0
BEGIN
    IF NOT EXISTS (SELECT 1 FROM dbo.sysjobs WHERE name = @n)
    BEGIN
        EXEC dbo.sp_add_job         @job_name = @n, @enabled = 1;
        EXEC dbo.sp_add_jobstep     @job_name = @n, @step_name = N'Chay', @subsystem = N'TSQL', @command = @c, @database_name = N'master';
        EXEC dbo.sp_add_jobschedule @job_name = @n, @name = N'Lich', @freq_type = @ft, @freq_interval = @fi,
                                    @freq_subday_type = @st, @freq_subday_interval = @si, @active_start_time = @t;
        EXEC dbo.sp_add_jobserver   @job_name = @n;
    END
    FETCH NEXT FROM jc INTO @n, @c, @ft, @fi, @st, @si, @t;
END
CLOSE jc; DEALLOCATE jc;
GO

/* ---------- D. Phục hồi (ví dụ) — CHẠY TỪNG BƯỚC, thay tên file cho đúng ----------
USE master;
ALTER DATABASE QuanLyDichVuAnUong SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
RESTORE DATABASE QuanLyDichVuAnUong FROM DISK = N'C:\Backup\FoodService\Full_20261007_020000.bak' WITH NORECOVERY, REPLACE;
RESTORE DATABASE QuanLyDichVuAnUong FROM DISK = N'C:\Backup\FoodService\Diff_20261007_080000.bak' WITH NORECOVERY;
RESTORE LOG      QuanLyDichVuAnUong FROM DISK = N'C:\Backup\FoodService\Log_20261007_081500.trn'  WITH NORECOVERY;
-- ... các file Log kế tiếp theo thứ tự; muốn dừng ở thời điểm cụ thể: WITH STOPAT = '2026-10-07 09:10:00', RECOVERY
RESTORE DATABASE QuanLyDichVuAnUong WITH RECOVERY;
ALTER DATABASE QuanLyDichVuAnUong SET MULTI_USER;
-- Nên thử phục hồi sang một DB tên khác (WITH MOVE ...) định kỳ để chắc chắn bản sao lưu dùng được.
-- Xóa file sao lưu cũ hơn 14 ngày: dùng Windows Task Scheduler/PowerShell (Get-ChildItem | Where LastWriteTime -lt (Get-Date).AddDays(-14) | Remove-Item).
*/
