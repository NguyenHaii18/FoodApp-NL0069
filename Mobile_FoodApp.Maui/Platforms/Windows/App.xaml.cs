using Microsoft.UI.Xaml;

namespace FoodApp.Maui.WinUI;

public partial class App : MauiWinUIApplication
{
    public App()
    {
        UnhandledException += (_, args) =>
            GhiLoiKhoiDong("WinUI.UnhandledException", args.Exception);
        AppDomain.CurrentDomain.UnhandledException += (_, args) =>
            GhiLoiKhoiDong(
                "AppDomain.UnhandledException",
                args.ExceptionObject as Exception ?? new Exception(args.ExceptionObject?.ToString()));

        try
        {
            InitializeComponent();
        }
        catch (Exception ex)
        {
            GhiLoiKhoiDong("InitializeComponent", ex);
            throw;
        }
    }

    protected override MauiApp CreateMauiApp()
    {
        try
        {
            return MauiProgram.CreateMauiApp();
        }
        catch (Exception ex)
        {
            GhiLoiKhoiDong("CreateMauiApp", ex);
            throw;
        }
    }

    private static void GhiLoiKhoiDong(string buoc, Exception ex)
    {
        try
        {
            File.WriteAllText(
                Path.Combine(AppContext.BaseDirectory, "maui-startup-error.log"),
                $"{buoc}{Environment.NewLine}{ex}");
        }
        catch
        {
            // Không che mất lỗi khởi động ban đầu nếu thư mục ứng dụng không ghi được.
        }
    }
}
