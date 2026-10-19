using System.Globalization;
using FoodApp.Maui.Services;

namespace FoodApp.Maui.Views;

/// <summary>Online voucher editor. Option IDs come from the authorized backend catalogue.</summary>
public sealed class PromotionEditorPage : ContentPage
{
    public sealed class Form
    {
        public string MaCode { get; set; } = "";
        public string? TenChuongTrinh { get; set; }
        public string? MoTa { get; set; }
        public string LoaiGiam { get; set; } = "PhanTram";
        public decimal GiaTriGiam { get; set; }
        public decimal? GiamToiDa { get; set; }
        public decimal GiaTriDonToiThieu { get; set; }
        public int? SoLuongToiDa { get; set; }
        public bool MoiKhachMotLan { get; set; }
        public DateTime NgayBatDau { get; set; } = DateTime.Today;
        public DateTime NgayKetThuc { get; set; } = DateTime.Today.AddMonths(1);
        public bool TrangThai { get; set; } = true;
        public string? DsMonAn { get; set; }
        public string? DsDanhMuc { get; set; }
        public string? DsGianHang { get; set; }
    }
    public sealed class Option { public string Id { get; set; } = ""; public string Name { get; set; } = ""; }
    public sealed class Options
    {
        public List<Option> Foods { get; set; } = new();
        public List<Option> Categories { get; set; } = new();
        public List<Option> Stores { get; set; } = new();
    }
    private readonly string _url;
    private readonly string? _id;
    private bool _loaded;
    private readonly VerticalStackLayout _body = new() { Padding = 20, Spacing = 10 };
    public PromotionEditorPage(bool admin, string? id = null)
    {
        _url = "api/mobile/promotions/" + (admin ? "admin" : "vendor"); _id = id;
        Title = id == null ? "Tạo voucher" : "Sửa voucher";
        Content = new ScrollView { Content = _body };
    }
    protected override async void OnAppearing()
    {
        base.OnAppearing(); if (_loaded) return; _loaded = true;
        var options = await ApiClient.GetAsync<Options>(_url + "/options");
        if (!options.ThanhCong || options.DuLieu == null) { await DisplayAlert("Không tải được phạm vi", options.ThongDiep, "OK"); await Navigation.PopAsync(); return; }
        var form = new Form();
        if (_id != null)
        {
            var result = await ApiClient.GetAsync<Form>(_url + "/" + _id);
            if (!result.ThanhCong || result.DuLieu == null) { await DisplayAlert("Không tải được voucher", result.ThongDiep, "OK"); await Navigation.PopAsync(); return; }
            form = result.DuLieu;
        }
        Entry Field(string label, string? value, bool numeric = false)
        {
            _body.Add(new Label { Text = label }); var entry = new Entry { Text = value, Keyboard = numeric ? Keyboard.Numeric : Keyboard.Default }; _body.Add(entry); return entry;
        }
        string Number(decimal? value) => value?.ToString(CultureInfo.CurrentCulture) ?? "";
        var code = Field("Mã voucher", form.MaCode);
        var name = Field("Tên chương trình", form.TenChuongTrinh);
        var description = Field("Mô tả", form.MoTa);
        var type = new Picker { Title = "Loại giảm", ItemsSource = new[] { "PhanTram", "SoTien", "GiamPhiVanChuyen" }, SelectedItem = form.LoaiGiam }; _body.Add(type);
        var amount = Field("Giá trị giảm (phí giao: 0 = miễn phí)", Number(form.GiaTriGiam), true);
        var capLabel = new Label { Text = "Giảm tối đa cho phần trăm (để trống = không giới hạn)" }; _body.Add(capLabel);
        var cap = new Entry { Text = Number(form.GiamToiDa), Keyboard = Keyboard.Numeric }; _body.Add(cap);
        void ShowCap() { cap.IsVisible = capLabel.IsVisible = (string?)type.SelectedItem == "PhanTram"; }
        type.SelectedIndexChanged += (_, _) => ShowCap(); ShowCap();
        var minimum = Field("Đơn tối thiểu (VNĐ)", Number(form.GiaTriDonToiThieu), true);
        var limit = Field("Tổng lượt (để trống = không giới hạn)", form.SoLuongToiDa?.ToString(), true);
        var once = new CheckBox { IsChecked = form.MoiKhachMotLan }; _body.Add(new Label { Text = "Mỗi khách một lần" }); _body.Add(once);
        var active = new Switch { IsToggled = form.TrangThai }; _body.Add(new Label { Text = "Kích hoạt" }); _body.Add(active);
        _body.Add(new Label { Text = "Bắt đầu (giờ Việt Nam)" });
        var start = new DatePicker { Date = form.NgayBatDau.Date }; var startTime = new TimePicker { Time = form.NgayBatDau.TimeOfDay }; _body.Add(start); _body.Add(startTime);
        _body.Add(new Label { Text = "Kết thúc (giờ Việt Nam)" });
        var end = new DatePicker { Date = form.NgayKetThuc.Date }; var endTime = new TimePicker { Time = form.NgayKetThuc.TimeOfDay }; _body.Add(end); _body.Add(endTime);
        HashSet<string> Scope(string title, List<Option> rows, string? selected)
        {
            var values = (selected ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries).ToHashSet();
            _body.Add(new Label { Text = title + " (không chọn = không giới hạn)", FontAttributes = FontAttributes.Bold });
            foreach (var row in rows)
            {
                var check = new CheckBox { IsChecked = values.Contains(row.Id) };
                check.CheckedChanged += (_, args) => { if (args.Value) values.Add(row.Id); else values.Remove(row.Id); };
                _body.Add(new HorizontalStackLayout { Children = { check, new Label { Text = row.Name, VerticalOptions = LayoutOptions.Center } } });
            }
            return values;
        }
        var foods = Scope("Món ăn", options.DuLieu.Foods, form.DsMonAn);
        var categories = Scope("Danh mục", options.DuLieu.Categories, form.DsDanhMuc);
        var stores = _url.EndsWith("admin") ? Scope("Gian hàng", options.DuLieu.Stores, form.DsGianHang) : new HashSet<string>();
        var save = new Button { Text = "Lưu voucher" }; _body.Add(save);
        save.Clicked += async (_, _) =>
        {
            if (!decimal.TryParse(amount.Text, out var discount) || !decimal.TryParse(minimum.Text, out var min) ||
                (!string.IsNullOrWhiteSpace(cap.Text) && !decimal.TryParse(cap.Text, out _)) ||
                (!string.IsNullOrWhiteSpace(limit.Text) && !int.TryParse(limit.Text, out _)))
            { await DisplayAlert("Dữ liệu không hợp lệ", "Vui lòng kiểm tra các ô số.", "OK"); return; }
            form.MaCode = code.Text ?? ""; form.TenChuongTrinh = name.Text; form.MoTa = description.Text;
            form.LoaiGiam = (string?)type.SelectedItem ?? ""; form.GiaTriGiam = discount;
            form.GiamToiDa = form.LoaiGiam == "PhanTram" && decimal.TryParse(cap.Text, out var max) ? max : null;
            form.GiaTriDonToiThieu = min; form.SoLuongToiDa = int.TryParse(limit.Text, out var count) ? count : null;
            form.MoiKhachMotLan = once.IsChecked; form.TrangThai = active.IsToggled;
            form.NgayBatDau = start.Date + startTime.Time; form.NgayKetThuc = end.Date + endTime.Time;
            form.DsMonAn = string.Join(",", foods); form.DsDanhMuc = string.Join(",", categories); form.DsGianHang = string.Join(",", stores);
            save.IsEnabled = false;
            try
            {
                var result = _id == null ? await ApiClient.PostAsync<object>(_url, form) : await ApiClient.PutAsync<object>(_url + "/" + _id, form);
                if (!result.ThanhCong) { await DisplayAlert("Chưa lưu voucher", result.ThongDiep, "OK"); return; }
                await Navigation.PopAsync();
            }
            finally { save.IsEnabled = true; }
        };
    }
}
