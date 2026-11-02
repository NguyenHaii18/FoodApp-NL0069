using FoodApp.Maui.Models;

namespace FoodApp.Maui.Views;

[QueryProperty(nameof(Mon), "Mon")]
[QueryProperty(nameof(GioHang), "GioHang")]
public partial class MonAnTuyChonPage : ContentPage
{
    public MonAn Mon { get; set; } = null!;
    public List<MucGioHang> GioHang { get; set; } = null!;

    private readonly HashSet<TuyChonMon> _toppings = new();
    private TuyChonMon? _size;
    private TuyChonMon? _mucCay;
    private int _soLuong = 1;
    private bool _daNap;

    public MonAnTuyChonPage() => InitializeComponent();

    protected override void OnAppearing()
    {
        base.OnAppearing();
        if (_daNap || Mon is null) return;
        _daNap = true;
        LblTenMon.Text = Mon.TenMon;
        LblMoTa.Text = Mon.MoTa;
        LblMoTa.IsVisible = !string.IsNullOrWhiteSpace(Mon.MoTa);
        LblGiaGoc.Text = $"Giá gốc: {Mon.Gia:N0}đ";
        LblAnhMon.Text = string.IsNullOrWhiteSpace(Mon.HinhAnh) ? "🍲" : Mon.HinhAnh;
        if (Uri.TryCreate(Mon.HinhAnh, UriKind.Absolute, out var uri) && uri.Scheme is "http" or "https")
        {
            ImgMon.Source = ImageSource.FromUri(uri);
            ImgMon.IsVisible = true;
            LblAnhMon.IsVisible = false;
        }

        NapLuaChonDon(DanhSachSize, Mon.TuyChon.Where(x => x.Loai == TuyChonMon.Size), "size", x => _size = x);
        NapTopping(Mon.TuyChon.Where(x => x.Loai == TuyChonMon.Topping));
        NapLuaChonDon(DanhSachMucCay, Mon.TuyChon.Where(x => x.Loai == TuyChonMon.MucCay), "muc-cay", x => _mucCay = x);
        NhomSize.IsVisible = DanhSachSize.Children.Count > 0;
        NhomTopping.IsVisible = DanhSachTopping.Children.Count > 0;
        NhomMucCay.IsVisible = DanhSachMucCay.Children.Count > 0;
        NhomGhiChu.IsVisible = Mon.ChoGhiChu;
        CapNhatTong();
    }

    private void NapLuaChonDon(Layout container, IEnumerable<TuyChonMon> source, string group, Action<TuyChonMon?> select)
    {
        var items = source.ToList();
        if (items.Count == 0) return;
        var none = new RadioButton { Content = "Không chọn", GroupName = group, IsChecked = true };
        none.CheckedChanged += (_, e) => { if (e.Value) { select(null); CapNhatTong(); } };
        container.Children.Add(none);
        foreach (var option in items)
        {
            var radio = new RadioButton { Content = option.HienThi, GroupName = group };
            radio.CheckedChanged += (_, e) => { if (e.Value) { select(option); CapNhatTong(); } };
            container.Children.Add(radio);
        }
    }

    private void NapTopping(IEnumerable<TuyChonMon> source)
    {
        foreach (var option in source)
        {
            var check = new CheckBox();
            check.CheckedChanged += (_, e) =>
            {
                if (e.Value) _toppings.Add(option); else _toppings.Remove(option);
                CapNhatTong();
            };
            var row = new HorizontalStackLayout { Spacing = 8 };
            row.Children.Add(check);
            row.Children.Add(new Label { Text = option.HienThi, VerticalTextAlignment = TextAlignment.Center });
            DanhSachTopping.Children.Add(row);
        }
    }

    private List<TuyChonMon> LuaChonHienTai()
    {
        var selected = new List<TuyChonMon>();
        if (_size is not null) selected.Add(_size);
        selected.AddRange(_toppings.OrderBy(x => x.Id).ThenBy(x => x.Ten));
        if (_mucCay is not null) selected.Add(_mucCay);
        return selected;
    }

    private void CapNhatTong()
    {
        if (Mon is null) return;
        var unitPrice = Mon.Gia + LuaChonHienTai().Sum(x => x.GiaThem);
        BtnThemVaoGio.Text = $"Thêm vào giỏ hàng • {unitPrice * _soLuong:N0}đ";
        LblSoLuong.Text = _soLuong.ToString();
    }

    private void OnTangSoLuong(object sender, EventArgs e) { _soLuong = Math.Min(50, _soLuong + 1); CapNhatTong(); }
    private void OnGiamSoLuong(object sender, EventArgs e) { _soLuong = Math.Max(1, _soLuong - 1); CapNhatTong(); }
    private void OnLuaChonThayDoi(object sender, TextChangedEventArgs e) => CapNhatTong();

    private async void OnThemVaoGio(object sender, EventArgs e)
    {
        var note = Mon.ChoGhiChu ? EditorGhiChu.Text?.Trim() : null;
        var selected = LuaChonHienTai();
        var key = MucGioHang.TaoKhoa(Mon.Id, selected, note);
        var line = GioHang.FirstOrDefault(x => x.KhoaCauHinh == key);
        if (line is null)
            GioHang.Add(new MucGioHang { MonAn = Mon, SoLuong = _soLuong, TuyChon = selected, GhiChu = note });
        else
            line.SoLuong = Math.Min(50, line.SoLuong + _soLuong);
        await Shell.Current.GoToAsync("..");
    }
}
