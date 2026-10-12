using FoodServiceApp.Web.Models.Entities;

namespace FoodServiceApp.Web.Services
{
    public static class GianHangHelper
    {
        /// <summary>Trong giờ hoạt động? Hỗ trợ ca qua đêm (18:00 → 02:00). 00:00–23:59 hoặc mở = đóng = cả ngày.</summary>
        public static bool TrongGio(GianHang g)
        {
            var now = DateTime.Now.TimeOfDay;
            if (g.GioMoCua == g.GioDongCua) return true;
            if (g.GioMoCua == TimeSpan.Zero && g.GioDongCua >= new TimeSpan(23, 59, 0)) return true;
            return g.GioMoCua < g.GioDongCua ? now >= g.GioMoCua && now <= g.GioDongCua : now >= g.GioMoCua || now <= g.GioDongCua;
        }

        public static bool DangMo(GianHang g) => g.TrangThaiKinhDoanh == "DangMo" && g.TrangThaiDuyet == "DaDuyet" && TrongGio(g);

        /// <summary>Có nhận đơn mới lúc này không (mở cửa + trong giờ + không tạm dừng nhận đơn).</summary>
        public static bool NhanDon(GianHang g) => DangMo(g) && g.DangNhanDon;

        public static string GioKinhDoanh(GianHang g) =>
            g.GioMoCua == g.GioDongCua || (g.GioMoCua == TimeSpan.Zero && g.GioDongCua >= new TimeSpan(23, 59, 0))
                ? "Cả ngày" : $"{g.GioMoCua:hh\\:mm} – {g.GioDongCua:hh\\:mm}";

        public const string PhiGiaoMoTa = "Phí giao tính theo khoảng cách (từ 15.000đ) · dự kiến 25–40 phút";
    }
}
