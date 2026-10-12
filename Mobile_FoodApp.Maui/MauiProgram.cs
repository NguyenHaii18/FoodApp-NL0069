using CommunityToolkit.Maui;
using Microsoft.Extensions.Logging;

namespace FoodApp.Maui;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .UseMauiCommunityToolkit()
            .UseMauiCommunityToolkitMediaElement();

        // Ghi chú: bản gốc có cấu hình font OpenSans-Regular.ttf / OpenSans-Semibold.ttf,
        // nhưng project không có sẵn 2 file .ttf đó (không tải được từ môi trường tạo file
        // này), nên tạm bỏ để tránh lỗi "font not found" khi chạy. App vẫn chạy bình thường
        // bằng font hệ thống mặc định. Muốn dùng lại font riêng: tải OpenSans-Regular.ttf và
        // OpenSans-Semibold.ttf (ví dụ từ fonts.google.com/specimen/Open+Sans), bỏ vào
        // Resources/Fonts/, rồi thêm lại đoạn dưới đây:
        //
        // builder.ConfigureFonts(fonts =>
        // {
        //     fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
        //     fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
        // });

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
