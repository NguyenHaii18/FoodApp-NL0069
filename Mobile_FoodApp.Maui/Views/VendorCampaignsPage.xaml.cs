using FoodApp.Maui.Models;
using FoodApp.Maui.Services;

namespace FoodApp.Maui.Views;

public partial class VendorCampaignsPage : ContentPage
{
    public VendorCampaignsPage()
    {
        InitializeComponent();
        ViTri.SelectedIndex = 1;
        BatDau.Date = DateTime.Today;
        KetThuc.Date = DateTime.Today.AddDays(7);
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await Reload();
    }

    private async Task Reload()
    {
        var storeId = MockData.GianHangHienTai.Id;
        var result = await ApiClient.GetAsync<List<ChienDichQuangCao>>($"api/mobile/gian-hang/{storeId}/chien-dich");
        if (!result.ThanhCong)
        {
            Rong.Text = result.ThongDiep;
            Rong.IsVisible = true;
            DanhSach.ItemsSource = null;
            return;
        }
        var data = result.DuLieu ?? [];
        DanhSach.ItemsSource = data;
        Rong.Text = "Chưa có chiến dịch.";
        Rong.IsVisible = data.Count == 0;
    }

    private async void OnCreate(object sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(Ten.Text) || string.IsNullOrWhiteSpace(TieuDe.Text) ||
            !decimal.TryParse(NganSach.Text, out var budget) || budget < 0 || KetThuc.Date <= BatDau.Date)
        {
            await DisplayAlert("Dữ liệu chưa đúng", "Nhập tên, tiêu đề, ngân sách hợp lệ và ngày kết thúc sau ngày bắt đầu.", "OK");
            return;
        }

        var storeId = MockData.GianHangHienTai.Id;
        var result = await ApiClient.PostAsync<object>($"api/mobile/gian-hang/{storeId}/chien-dich", new
        {
            TenChienDich = Ten.Text,
            LoaiViTri = ViTri.SelectedItem?.ToString() ?? "NoiBat",
            TieuDe = TieuDe.Text,
            NoiDung = NoiDung.Text,
            HinhAnh = HinhAnh.Text,
            NgayBatDau = BatDau.Date,
            NgayKetThuc = KetThuc.Date.AddDays(1).AddSeconds(-1),
            NganSach = budget,
            KichHoat = KichHoat.IsToggled
        });
        if (!result.ThanhCong) { await DisplayAlert("Không tạo được", result.ThongDiep, "OK"); return; }
        Ten.Text = TieuDe.Text = NoiDung.Text = HinhAnh.Text = NganSach.Text = "";
        await Reload();
    }

    private async void OnDelete(object sender, EventArgs e)
    {
        if (sender is not Button { CommandParameter: ChienDichQuangCao campaign } ||
            !await DisplayAlert("Xóa chiến dịch", $"Xóa {campaign.TenChienDich}?", "Xóa", "Hủy")) return;
        var storeId = MockData.GianHangHienTai.Id;
        var result = await ApiClient.DeleteAsync<object>($"api/mobile/gian-hang/{storeId}/chien-dich/{campaign.MaChienDich}");
        if (!result.ThanhCong) { await DisplayAlert("Không xóa được", result.ThongDiep, "OK"); return; }
        await Reload();
    }
}
