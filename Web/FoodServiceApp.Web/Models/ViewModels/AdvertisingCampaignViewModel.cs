using System.ComponentModel.DataAnnotations;

namespace FoodServiceApp.Web.Models.ViewModels;

public class AdvertisingCampaignViewModel : IValidatableObject
{
    [Required, StringLength(150)] public string TenChienDich { get; set; } = "";
    [Required, RegularExpression("Banner|NoiBat|GoiY")] public string LoaiViTri { get; set; } = "NoiBat";
    [Required, StringLength(150)] public string TieuDe { get; set; } = "";
    [StringLength(500)] public string? NoiDung { get; set; }
    [StringLength(500)] public string? HinhAnh { get; set; }
    public DateTime NgayBatDau { get; set; } = DateTime.Today;
    public DateTime NgayKetThuc { get; set; } = DateTime.Today.AddDays(7);
    [Range(typeof(decimal), "0", "1000000000")] public decimal NganSach { get; set; }
    public bool KichHoat { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (NgayKetThuc <= NgayBatDau)
            yield return new ValidationResult("Ngày kết thúc phải sau ngày bắt đầu.", [nameof(NgayKetThuc)]);
    }
}
