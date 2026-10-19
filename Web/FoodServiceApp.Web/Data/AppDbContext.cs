using Microsoft.EntityFrameworkCore;
using FoodServiceApp.Web.Models.Entities;

namespace FoodServiceApp.Web.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<TaiKhoan> TaiKhoans => Set<TaiKhoan>();
        public DbSet<GianHang> GianHangs => Set<GianHang>();
        public DbSet<QuanTri> QuanTris => Set<QuanTri>();
        public DbSet<KhachHang> KhachHangs => Set<KhachHang>();
        public DbSet<TaiXe> TaiXes => Set<TaiXe>();
        public DbSet<DanhMuc> DanhMucs => Set<DanhMuc>();
        public DbSet<MonAn> MonAns => Set<MonAn>();
        public DbSet<DonHang> DonHangs => Set<DonHang>();
        public DbSet<ChiTietDonHang> ChiTietDonHangs => Set<ChiTietDonHang>();
        public DbSet<ThanhToan> ThanhToans => Set<ThanhToan>();
        public DbSet<KhuyenMai> KhuyenMais => Set<KhuyenMai>();
        public DbSet<DanhGia> DanhGias => Set<DanhGia>();
        public DbSet<GiaoHang> GiaoHangs => Set<GiaoHang>();
        public DbSet<DanhGiaMon> DanhGiaMons => Set<DanhGiaMon>();
        public DbSet<DiaChiKhachHang> DiaChiKhachHangs => Set<DiaChiKhachHang>();
        public DbSet<PhanHoi> PhanHois => Set<PhanHoi>();
        public DbSet<ThongBao> ThongBaos => Set<ThongBao>();
        public DbSet<YeuThich> YeuThichs => Set<YeuThich>();
        public DbSet<PhienDangNhap> PhienDangNhaps => Set<PhienDangNhap>();
        public DbSet<BaoCaoDanhGia> BaoCaoDanhGias => Set<BaoCaoDanhGia>();
        public DbSet<TuyChonMon> TuyChonMons => Set<TuyChonMon>();
        public DbSet<GioHang> GioHangs => Set<GioHang>();
        public DbSet<LichSuTrangThaiDon> LichSuTrangThaiDons => Set<LichSuTrangThaiDon>();
        public DbSet<YeuCauHoanTien> YeuCauHoanTiens => Set<YeuCauHoanTien>();
        public DbSet<TaiXeViTri> TaiXeViTris => Set<TaiXeViTri>();
        public DbSet<CauHinhHeThong> CauHinhHeThongs => Set<CauHinhHeThong>();
        public DbSet<CauHinhHeThongLichSu> CauHinhHeThongLichSus => Set<CauHinhHeThongLichSu>();
        public DbSet<DieuPhoiLog> DieuPhoiLogs => Set<DieuPhoiLog>();
        public DbSet<KhuyenMaiMonAn> KhuyenMaiMonAns => Set<KhuyenMaiMonAn>();
        public DbSet<KhuyenMaiDanhMuc> KhuyenMaiDanhMucs => Set<KhuyenMaiDanhMuc>();
        public DbSet<KhuyenMaiGianHang> KhuyenMaiGianHangs => Set<KhuyenMaiGianHang>();
        public DbSet<KhuyenMaiKhachDung> KhuyenMaiKhachDungs => Set<KhuyenMaiKhachDung>();
        public DbSet<GioHangChiTiet> GioHangChiTiets => Set<GioHangChiTiet>();
        public DbSet<OtpXacThuc> OtpXacThucs => Set<OtpXacThuc>();
        public DbSet<GiaoDichThanhToan> GiaoDichThanhToans => Set<GiaoDichThanhToan>();
        public DbSet<LichSuGiaoHang> LichSuGiaoHangs => Set<LichSuGiaoHang>();
        public DbSet<NhatKyHeThong> NhatKyHeThongs => Set<NhatKyHeThong>();

        // ---- Thông báo tự động cho khách khi đơn hàng được tạo / đổi trạng thái / thanh toán đổi trạng thái ----
        private bool _dangGhiThongBao;

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            var cho = new List<(int? maKH, int? maDon, string loai, string tieuDe, string noiDung, int? maTK)>();
            var kmMoi = new List<KhuyenMai>();
            var lichSu = new List<(DonHang don, string? tu, string den, string? lyDo)>();
            var ttLog = new List<(ThanhToan tt, string? tu, bool moi)>();
            var ghLog = new List<(GiaoHang gh, string? tu)>();
            if (!_dangGhiThongBao)
            {
                kmMoi = ChangeTracker.Entries<KhuyenMai>().Where(e => e.State == EntityState.Added && e.Entity.TrangThai).Select(e => e.Entity).ToList();
                foreach (var e in ChangeTracker.Entries<DonHang>().ToList())
                {
                    if (e.State == EntityState.Added)
                        lichSu.Add((e.Entity, null, e.Entity.TrangThai, null));
                    else if (e.State == EntityState.Modified && e.Property(x => x.TrangThai).IsModified
                             && !Equals(e.OriginalValues[nameof(DonHang.TrangThai)], e.Entity.TrangThai))
                        lichSu.Add((e.Entity, e.OriginalValues[nameof(DonHang.TrangThai)]?.ToString(), e.Entity.TrangThai, e.Entity.LyDoTuChoi));

                    if (e.State == EntityState.Added)
                        cho.Add((e.Entity.MaKH, null, "DonHang", "Đặt hàng thành công", "Đơn hàng của bạn đã được gửi tới gian hàng, vui lòng chờ xác nhận.", null));
                    else if (e.State == EntityState.Modified && e.Property(x => x.TrangThai).IsModified
                             && !Equals(e.OriginalValues[nameof(DonHang.TrangThai)], e.Entity.TrangThai))
                    {
                        var (tieuDe, noiDung) = e.Entity.TrangThai switch
                        {
                            "DaXacNhan" => ("Gian hàng đã xác nhận", "Gian hàng đã nhận đơn và đang chuẩn bị món."),
                            "DangGiao" => ("Đang giao hàng", "Tài xế đã nhận đơn và đang trên đường giao."),
                            "DaGiao" => ("Giao hàng thành công", "Đơn hàng đã được giao. Bạn có thể đánh giá đơn này."),
                            "DaHuy" => ("Đơn hàng đã hủy", string.IsNullOrWhiteSpace(e.Entity.LyDoTuChoi) ? "Đơn hàng đã bị hủy." : "Đơn hàng đã hủy. Lý do: " + e.Entity.LyDoTuChoi),
                            _ => ("Cập nhật đơn hàng", "Trạng thái đơn hàng đã thay đổi."),
                        };
                        cho.Add((e.Entity.MaKH, e.Entity.MaDonHang, "DonHang", tieuDe, noiDung, null));
                    }
                }
                foreach (var e in ChangeTracker.Entries<ThanhToan>().ToList())
                {
                    if (e.State == EntityState.Added) ttLog.Add((e.Entity, null, true));
                    else if (e.State == EntityState.Modified && (e.Property(x => x.TrangThai).IsModified || e.Property(x => x.DaDoiSoat).IsModified)
                             && (!Equals(e.OriginalValues[nameof(ThanhToan.TrangThai)], e.Entity.TrangThai) || !Equals(e.OriginalValues[nameof(ThanhToan.DaDoiSoat)], e.Entity.DaDoiSoat)))
                        ttLog.Add((e.Entity, e.OriginalValues[nameof(ThanhToan.TrangThai)]?.ToString(), false));
                    if (e.State == EntityState.Modified && e.Property(x => x.TrangThai).IsModified
                        && !Equals(e.OriginalValues[nameof(ThanhToan.TrangThai)], e.Entity.TrangThai))
                    {
                        var (tieuDe, noiDung) = e.Entity.TrangThai switch
                        {
                            "DaThanhToan" => ("Thanh toán thành công", "Hệ thống đã ghi nhận thanh toán cho đơn hàng của bạn."),
                            "DaHoanTien" => ("Đã hoàn tiền", "Khoản thanh toán của đơn hàng đã được hoàn lại."),
                            "ThatBai" => ("Thanh toán thất bại", "Thanh toán chưa thành công, vui lòng thử lại."),
                            _ => ("", ""),
                        };
                        if (tieuDe != "") cho.Add((null, e.Entity.MaDonHang, "ThanhToan", tieuDe, noiDung, null));
                    }
                }
            }

            if (!_dangGhiThongBao)
                foreach (var e in ChangeTracker.Entries<GiaoHang>().ToList())
                {
                    if (e.State == EntityState.Added) ghLog.Add((e.Entity, null));
                    else if (e.State == EntityState.Modified && !Equals(e.OriginalValues[nameof(GiaoHang.TrangThai)], e.Entity.TrangThai))
                        ghLog.Add((e.Entity, e.OriginalValues[nameof(GiaoHang.TrangThai)]?.ToString()));
                    else if (e.State == EntityState.Modified && !Equals(e.OriginalValues[nameof(GiaoHang.MaTaiXe)], e.Entity.MaTaiXe))
                        ghLog.Add((e.Entity, e.Entity.TrangThai));
                }

            var kq = await base.SaveChangesAsync(cancellationToken);

            if (ttLog.Count > 0 || ghLog.Count > 0)
            {
                try
                {
                    _dangGhiThongBao = true;
                    foreach (var (tt, tu, moi) in ttLog)
                    {
                        var tien = await DonHangs.AsNoTracking().Where(d => d.MaDonHang == tt.MaDonHang).Select(d => d.TongTien).FirstOrDefaultAsync(cancellationToken);
                        var hanhDong = moi ? "TaoMoi" : tt.TrangThai switch { "DaThanhToan" => "Thu", "ThatBai" => "ThatBai", "DaHoanTien" => "Hoan", _ => tt.DaDoiSoat ? "DoiSoat" : "CapNhat" };
                        GiaoDichThanhToans.Add(new GiaoDichThanhToan
                        {
                            MaThanhToan = tt.MaThanhToan, MaDonHang = tt.MaDonHang, HanhDong = hanhDong, HinhThuc = tt.HinhThuc,
                            TuTrangThai = tu, DenTrangThai = tt.TrangThai, SoTien = tien,
                            MaGiaoDichCong = hanhDong == "Hoan" ? tt.MaGiaoDichHoan : tt.MaGiaoDich,
                            GhiChu = hanhDong == "Hoan" ? tt.KetQuaHoan : null
                        });
                    }
                    foreach (var (gh, tu) in ghLog)
                        LichSuGiaoHangs.Add(new LichSuGiaoHang
                        {
                            MaGiaoHang = gh.MaGiaoHang, MaDonHang = gh.MaDonHang, MaTaiXe = gh.MaTaiXe, TuTrangThai = tu, DenTrangThai = gh.TrangThai,
                            ViDo = gh.ViDoGiao, KinhDo = gh.KinhDoGiao, GhiChu = gh.LyDoThatBai
                        });
                    await base.SaveChangesAsync(cancellationToken);
                }
                catch
                {
                    foreach (var t in ChangeTracker.Entries<GiaoDichThanhToan>().Where(x => x.State == EntityState.Added).ToList()) t.State = EntityState.Detached;
                    foreach (var t in ChangeTracker.Entries<LichSuGiaoHang>().Where(x => x.State == EntityState.Added).ToList()) t.State = EntityState.Detached;
                }
                finally { _dangGhiThongBao = false; }
            }

            if (lichSu.Count > 0)
            {
                try
                {
                    _dangGhiThongBao = true;
                    foreach (var l in lichSu)
                        LichSuTrangThaiDons.Add(new LichSuTrangThaiDon { MaDonHang = l.don.MaDonHang, TuTrangThai = l.tu, DenTrangThai = l.den, LyDo = l.lyDo });
                    await base.SaveChangesAsync(cancellationToken);
                }
                catch { foreach (var t in ChangeTracker.Entries<LichSuTrangThaiDon>().Where(x => x.State == EntityState.Added).ToList()) t.State = EntityState.Detached; }
                finally { _dangGhiThongBao = false; }
            }

            if (cho.Count > 0 || kmMoi.Count > 0)
            {
                try
                {
                    _dangGhiThongBao = true;
                    // Khuyến mãi mới -> thông báo cho khách bật nhận thông báo khuyến mãi
                    foreach (var km in kmMoi)
                    {
                        var giam = km.PhanTramGiam != null ? $"giảm {km.PhanTramGiam:0.#}%" : $"giảm {km.SoTienGiam:N0}đ";
                        var tenQuan = km.MaGianHang == null ? "Hệ thống" : (await GianHangs.AsNoTracking().Where(g => g.MaGianHang == km.MaGianHang).Select(g => g.TenCuaHang).FirstOrDefaultAsync(cancellationToken) ?? "Gian hàng");
                        var dsTK = await KhachHangs.AsNoTracking().Where(k => k.NhanTbKhuyenMai).Select(k => k.MaTK).Take(5000).ToListAsync(cancellationToken);
                        foreach (var maTK in dsTK)
                            ThongBaos.Add(new ThongBao { MaTK = maTK, Loai = "KhuyenMai", TieuDe = $"Khuyến mãi mới từ {tenQuan}", NoiDung = $"Mã {km.MaCode} {giam}, hiệu lực đến {km.NgayKetThuc:dd/MM/yyyy}.", MaThamChieu = km.MaGianHang?.ToString() });
                    }
                    foreach (var c in cho)
                    {
                        // maTK + cờ cài đặt thông báo của khách
                        var maKH = c.maKH ?? await DonHangs.Where(d => d.MaDonHang == c.maDon).Select(d => (int?)d.MaKH).FirstOrDefaultAsync(cancellationToken);
                        if (maKH == null) continue;
                        var kh = await KhachHangs.AsNoTracking().FirstOrDefaultAsync(k => k.MaKH == maKH, cancellationToken);
                        if (kh == null) continue;
                        if (c.loai == "DonHang" && !kh.NhanTbDonHang) continue;
                        if (c.loai == "ThanhToan" && !kh.NhanTbThanhToan) continue;
                        // đơn mới: chưa có MaDonHang trong danh sách chờ -> lấy đơn mới nhất của khách
                        var maDon = c.maDon ?? await DonHangs.Where(d => d.MaKH == maKH).OrderByDescending(d => d.MaDonHang).Select(d => (int?)d.MaDonHang).FirstOrDefaultAsync(cancellationToken);
                        ThongBaos.Add(new ThongBao
                        {
                            MaTK = kh.MaTK, TieuDe = c.tieuDe, NoiDung = c.noiDung,
                            Loai = c.loai == "ThanhToan" ? "HeThong" : c.loai, MaThamChieu = maDon?.ToString(),
                        });
                    }
                    await base.SaveChangesAsync(cancellationToken);
                }
                catch { /* chưa chạy NangCap_v11/v9: bỏ qua, không làm hỏng nghiệp vụ */ foreach (var t in ChangeTracker.Entries<ThongBao>().Where(x => x.State == EntityState.Added).ToList()) t.State = EntityState.Detached; }
                finally { _dangGhiThongBao = false; }
            }
            return kq;
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<KhuyenMaiMonAn>().HasKey(x => new { x.MaKhuyenMai, x.MaMonAn });
            modelBuilder.Entity<KhuyenMaiDanhMuc>().HasKey(x => new { x.MaKhuyenMai, x.MaDanhMuc });
            modelBuilder.Entity<KhuyenMaiGianHang>().HasKey(x => new { x.MaKhuyenMai, x.MaGianHang });
            modelBuilder.Entity<KhuyenMaiKhachDung>().HasKey(x => new { x.MaKhuyenMai, x.MaKH, x.MaDonHang });
            modelBuilder.Entity<KhuyenMai>().HasIndex(x => x.MaCode).IsUnique();
            modelBuilder.Entity<KhuyenMai>().Property(x => x.MaCode).HasMaxLength(50);
            modelBuilder.Entity<KhuyenMaiKhachDung>().HasIndex(x => x.MaDonHang).IsUnique();
            modelBuilder.Entity<DonHang>().Property(x => x.MaYeuCauDat).HasMaxLength(40);
            modelBuilder.Entity<DonHang>().HasIndex(x => x.MaYeuCauDat).IsUnique().HasFilter("[MaYeuCauDat] IS NOT NULL");
            modelBuilder.Entity<GioHang>().HasMany(g => g.ChiTiet).WithOne().HasForeignKey(c => c.MaGioHang).OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<GianHang>()
                .HasOne(g => g.TaiKhoan)
                .WithMany()
                .HasForeignKey(g => g.MaTK);

            modelBuilder.Entity<KhachHang>()
                .HasOne(k => k.TaiKhoan)
                .WithMany()
                .HasForeignKey(k => k.MaTK);

            modelBuilder.Entity<DanhMuc>()
                .HasOne(d => d.GianHang)
                .WithMany(g => g.DanhMucs)
                .HasForeignKey(d => d.MaGianHang);

            modelBuilder.Entity<MonAn>()
                .HasOne(m => m.DanhMuc)
                .WithMany(d => d.MonAns)
                .HasForeignKey(m => m.MaDanhMuc);

            modelBuilder.Entity<DonHang>()
                .HasOne(d => d.KhachHang)
                .WithMany()
                .HasForeignKey(d => d.MaKH);

            modelBuilder.Entity<DonHang>()
                .HasOne(d => d.GianHang)
                .WithMany(g => g.DonHangs)
                .HasForeignKey(d => d.MaGianHang);

            modelBuilder.Entity<ChiTietDonHang>()
                .HasOne(c => c.MonAn)
                .WithMany()
                .HasForeignKey(c => c.MaMonAn);

            modelBuilder.Entity<ThanhToan>()
                .HasOne<DonHang>()
                .WithOne(d => d.ThanhToan)
                .HasForeignKey<ThanhToan>(t => t.MaDonHang);

            modelBuilder.Entity<KhuyenMai>()
                .HasOne<GianHang>()
                .WithMany(g => g.KhuyenMais)
                .HasForeignKey(k => k.MaGianHang);

            modelBuilder.Entity<GiaoHang>()
                .HasOne(g => g.DonHang)
                .WithOne(d => d.GiaoHang)
                .HasForeignKey<GiaoHang>(g => g.MaDonHang);

            modelBuilder.Entity<GiaoHang>()
                .HasOne(g => g.TaiXe)
                .WithMany(t => t.GiaoHangs)
                .HasForeignKey(g => g.MaTaiXe);

            modelBuilder.Entity<DanhGiaMon>()
                .HasOne(d => d.MonAn)
                .WithMany()
                .HasForeignKey(d => d.MaMonAn);

            modelBuilder.Entity<PhanHoi>()
                .HasOne(p => p.KhachHang)
                .WithMany()
                .HasForeignKey(p => p.MaKH);

            modelBuilder.Entity<DonHang>()
                .HasOne(d => d.KhuyenMaiApDung)
                .WithMany()
                .HasForeignKey(d => d.MaKhuyenMaiApDung)
                .OnDelete(DeleteBehavior.NoAction);
        }
    }
}
