namespace FoodApp.Maui.Models;

/// <summary>Một tùy chọn của món: size, topping hoặc mức cay (kèm phụ phí nếu có).</summary>
public class TuyChonMon
{
    public const string Size = "Size";
    public const string Topping = "Topping";
    public const string MucCay = "MucCay";

    public string Loai { get; set; } = Size;
    public string Ten { get; set; } = "";
    public decimal GiaThem { get; set; }

    public static string NhanLoai(string loai) => loai switch
    {
        Size => "Size",
        Topping => "Topping",
        MucCay => "Mức cay",
        _ => loai
    };

    [System.Text.Json.Serialization.JsonIgnore]
    public string HienThi => GiaThem > 0 ? $"{Ten} (+{GiaThem:N0}đ)" : Ten;
}

/// <summary>Đánh giá của khách cho một đơn đã giao, gian hàng có thể phản hồi công khai (NV theo mục đánh giá trong báo cáo).</summary>
public class DanhGiaGianHang
{
    public string Id { get; set; } = "";
    public string GianHangId { get; set; } = "";
    public string DonHangId { get; set; } = "";
    public string KhachTen { get; set; } = "";
    public int SoSaoMonAn { get; set; }
    public int SoSaoGiaoHang { get; set; }
    public List<string> The { get; set; } = new();       // thẻ nhận xét nhanh: "Món ngon", "Đóng gói đẹp"…
    public string NoiDung { get; set; } = "";
    public DateTime Ngay { get; set; } = DateTime.Now;
    public string PhanHoi { get; set; } = "";            // phản hồi của gian hàng (rỗng = chưa phản hồi)
    public DateTime? PhanHoiLuc { get; set; }

    [System.Text.Json.Serialization.JsonIgnore] public string SaoText => new string('★', Math.Clamp(SoSaoMonAn, 0, 5)) + new string('☆', 5 - Math.Clamp(SoSaoMonAn, 0, 5));
    [System.Text.Json.Serialization.JsonIgnore] public string SaoGiaoHangText => SoSaoGiaoHang > 0 ? $"Giao hàng: {SoSaoGiaoHang}★" : "";
    [System.Text.Json.Serialization.JsonIgnore] public bool CoNoiDung => !string.IsNullOrWhiteSpace(NoiDung);
    [System.Text.Json.Serialization.JsonIgnore] public bool CoThe => The.Count > 0;
    [System.Text.Json.Serialization.JsonIgnore] public string TheText => string.Join(" · ", The);
    [System.Text.Json.Serialization.JsonIgnore] public bool CoPhanHoi => !string.IsNullOrWhiteSpace(PhanHoi);
    [System.Text.Json.Serialization.JsonIgnore] public string PhanHoiText => $"💬 Quán phản hồi: {PhanHoi}";
    [System.Text.Json.Serialization.JsonIgnore] public string NutPhanHoiText => CoPhanHoi ? "Sửa phản hồi" : "Phản hồi";
    [System.Text.Json.Serialization.JsonIgnore] public string TieuDeText => $"{KhachTen}  ·  Đơn #{DonHangId}";
    [System.Text.Json.Serialization.JsonIgnore] public string NgayText => Ngay.ToString("HH:mm dd/MM/yyyy");
}

/// <summary>Thông báo gửi tới gian hàng.</summary>
public class ThongBaoGianHang
{
    public const string DonMoi = "DonMoi";
    public const string HuyDon = "HuyDon";
    public const string DanhGiaMoi = "DanhGiaMoi";
    public const string HeThong = "HeThong";

    public string Id { get; set; } = "";
    public string GianHangId { get; set; } = "";
    public string Loai { get; set; } = HeThong;
    public string TieuDe { get; set; } = "";
    public string NoiDung { get; set; } = "";
    public string DonHangId { get; set; } = "";
    public DateTime Ngay { get; set; } = DateTime.Now;
    public bool DaDoc { get; set; }

    [System.Text.Json.Serialization.JsonIgnore]
    public string Icon => Loai switch { DonMoi => "🛎️", HuyDon => "❌", DanhGiaMoi => "⭐", _ => "📢" };
    [System.Text.Json.Serialization.JsonIgnore] public string ChamChuaDoc => DaDoc ? "" : "●";
    [System.Text.Json.Serialization.JsonIgnore] public string NgayText => Ngay.ToString("HH:mm dd/MM");
}

/// <summary>Kết quả đối soát tài chính của gian hàng trong một kỳ.</summary>
public class KetQuaTaiChinh
{
    public int SoDon { get; set; }
    /// <summary>Tiền món khách đặt (chưa trừ giảm giá, không gồm phí giao — phí giao thuộc về tài xế).</summary>
    public decimal DoanhThuMon { get; set; }
    /// <summary>Giảm giá do chính gian hàng tài trợ (mã khuyến mãi quán tự tạo).</summary>
    public decimal GiamGiaQuanChiu { get; set; }
    public decimal DoanhThuRong => DoanhThuMon - GiamGiaQuanChiu;
    public decimal PhiNenTang { get; set; }
    public decimal ThucNhan => DoanhThuRong - PhiNenTang;
    public decimal TongGiaVon { get; set; }
    /// <summary>Số đơn có món chưa nhập giá vốn — lợi nhuận chỉ là ước tính tối thiểu.</summary>
    public int SoDonThieuGiaVon { get; set; }
    public decimal LoiNhuan => ThucNhan - TongGiaVon;
    public decimal GiaTriTrungBinh => SoDon == 0 ? 0 : DoanhThuMon / SoDon;
    public List<DongDonTaiChinh> ChiTiet { get; set; } = new();
}

public class DongDonTaiChinh
{
    public string DonHangId { get; set; } = "";
    public DateTime Ngay { get; set; }
    public decimal DoanhThuMon { get; set; }
    public decimal GiamGiaQuanChiu { get; set; }
    public decimal PhiNenTang { get; set; }
    public decimal ThucNhan => DoanhThuMon - GiamGiaQuanChiu - PhiNenTang;

    public string TieuDe => $"Đơn #{DonHangId}";
    public string NgayText => Ngay.ToString("HH:mm dd/MM/yyyy");
    public string ThucNhanText => $"+{ThucNhan:N0}đ";
    public string ChiTietText => $"Món {DoanhThuMon:N0}đ" +
        (GiamGiaQuanChiu > 0 ? $" − giảm giá {GiamGiaQuanChiu:N0}đ" : "") + $" − phí {PhiNenTang:N0}đ";
}
