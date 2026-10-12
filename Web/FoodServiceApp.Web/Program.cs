using Microsoft.AspNetCore.Authentication;
using System.Text;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using FoodServiceApp.Web.Data;
using FoodServiceApp.Web.Services;
using FoodServiceApp.Web.Services.Configs;
using FoodServiceApp.Web.Services.Maps;
using FoodServiceApp.Web.Services.Payment;

var builder = WebApplication.CreateBuilder(args);

// MVC (Website: Gian hàng/Quản trị/Tài xế) + API Controllers (Controllers/Api/*, cho FoodApp mobile)
builder.Services.AddControllersWithViews(o => o.Filters.AddService<FoodServiceApp.Web.Filters.NhatKyThaoTacFilter>());
builder.Services.AddScoped<FoodServiceApp.Web.Filters.NhatKyThaoTacFilter>();
builder.Services.AddScoped<FoodServiceApp.Web.Services.OtpService>();
builder.Services.AddScoped<FoodServiceApp.Web.Services.TaiKhoanService>();
builder.Services.AddScoped<FoodServiceApp.Web.Services.CauHinhService>();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c => c.SwaggerDoc("v1", new() { Title = "FoodServiceApp API", Version = "v1" }));

// ---------- SQL Server ----------
// Tự tìm SQL Server instance đang chạy trên máy Windows. Ưu tiên instance đã có
// database QuanLyDichVuAnUong; nếu chưa có database thì dùng instance đầu tiên
// đang truy cập được để EF Core có thể tự tạo database. Có thể ghi đè bằng
// ConnectionStrings:DefaultConnection hoặc biến môi trường FOOD_DB_CONNECTION.
var configuredConnection = Environment.GetEnvironmentVariable("FOOD_DB_CONNECTION")
    ?? builder.Configuration.GetConnectionString("DefaultConnection");

var databaseName = "QuanLyDichVuAnUong";
var serverCandidates = builder.Configuration.GetSection("DatabaseServers").GetChildren()
    .Select(x => x.Value)
    .Where(x => !string.IsNullOrWhiteSpace(x))
    .Cast<string>()
    .ToArray();
if (serverCandidates.Length == 0)
{
    serverCandidates = new[] { "(localdb)\\MSSQLLocalDB", ".\\SQLEXPRESS", "localhost" };
}

string? selectedConnection = null;
var reachableServer = new List<string>();

if (!string.IsNullOrWhiteSpace(configuredConnection))
{
    try
    {
        var csb = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(configuredConnection)
        { ConnectTimeout = 3 };
        using var test = new Microsoft.Data.SqlClient.SqlConnection(csb.ConnectionString);
        test.Open();
        selectedConnection = configuredConnection;
    }
    catch
    {
        // Nếu connection string cũ (ví dụ localhost) không chạy, thử các instance
        // phổ biến bên dưới thay vì làm ứng dụng crash ngay lúc khởi động.
    }
}

if (selectedConnection is null)
{
    foreach (var server in serverCandidates)
    {
        try
        {
            var masterCs = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder
            {
                DataSource = server,
                InitialCatalog = "master",
                IntegratedSecurity = true,
                TrustServerCertificate = true,
                ConnectTimeout = 3
            }.ConnectionString;

            using var test = new Microsoft.Data.SqlClient.SqlConnection(masterCs);
            test.Open();
            reachableServer.Add(server);

            using var cmd = test.CreateCommand();
            cmd.CommandText = "SELECT DB_ID(@db)";
            cmd.Parameters.AddWithValue("@db", databaseName);
            var scalar = cmd.ExecuteScalar();
            var exists = scalar is not null && scalar != DBNull.Value;
            if (exists)
            {
                selectedConnection = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder
                {
                    DataSource = server,
                    InitialCatalog = databaseName,
                    IntegratedSecurity = true,
                    TrustServerCertificate = true,
                    MultipleActiveResultSets = true
                }.ConnectionString;
                break;
            }
        }
        catch
        {
            // Instance không tồn tại/chưa chạy -> thử instance kế tiếp.
        }
    }
}

if (selectedConnection is null && reachableServer.Count > 0)
{
    var server = reachableServer[0];
    selectedConnection = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder
    {
        DataSource = server,
        InitialCatalog = databaseName,
        IntegratedSecurity = true,
        TrustServerCertificate = true,
        MultipleActiveResultSets = true
    }.ConnectionString;
}

selectedConnection ??= configuredConnection
    ?? "Server=(localdb)\\MSSQLLocalDB;Database=QuanLyDichVuAnUong;Trusted_Connection=True;TrustServerCertificate=True;";

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(selectedConnection));

// ---------- Storage / Maps / Payment ----------
builder.Services.AddSingleton<IFileStorageService, LocalFileStorageService>();

builder.Services.AddHttpContextAccessor();
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(2);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});
builder.Services.AddScoped<FoodServiceApp.Web.Services.CartService>();

builder.Services.Configure<GoongMapsSettings>(builder.Configuration.GetSection("GoongMaps"));
builder.Services.AddHttpClient<IGoongMapsService, GoongMapsService>();

builder.Services.Configure<VnPaySettings>(builder.Configuration.GetSection("VnPay"));
builder.Services.AddHttpClient<IVnPayService, VnPayService>(c => c.Timeout = TimeSpan.FromSeconds(30));

// ---------- Xác thực: Cookie (Website, mặc định) + JWT (API cho mobile) ----------
var jwtSection = builder.Configuration.GetSection("Jwt");

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Auth/Login";
        options.LogoutPath = "/Auth/Logout";
        options.AccessDeniedPath = "/Auth/Login";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.Cookie.Name = "FoodServiceApp.Auth";

        // Route /QuanTri/* chưa đăng nhập -> đưa về trang đăng nhập Quản trị,
        // /TaiXe/* -> đưa về trang đăng nhập Tài xế, còn lại -> Gian hàng.
        // Lưu ý: các route /api/* dùng JWT (AuthenticationSchemes chỉ định rõ ở
        // từng controller trong Controllers/Api/), nên KHÔNG rơi vào các sự kiện
        // redirect này — API trả 401 JSON như bình thường khi thiếu/sai token.
        options.Events.OnRedirectToLogin = context =>
        {
            var path = context.Request.Path;
            var target = path.StartsWithSegments("/QuanTri") ? "/AdminAuth/Login"
                       : path.StartsWithSegments("/TaiXe") ? "/DriverAuth/Login"
                       : path.StartsWithSegments("/MuaHang") ? "/MuaHang/Auth/Login"
                       : options.LoginPath;
            context.Response.Redirect($"{target}?ReturnUrl={Uri.EscapeDataString(context.Request.Path)}");
            return Task.CompletedTask;
        };
        options.Events.OnRedirectToAccessDenied = context =>
        {
            var path = context.Request.Path;
            var target = path.StartsWithSegments("/QuanTri") ? "/AdminAuth/Login"
                       : path.StartsWithSegments("/TaiXe") ? "/DriverAuth/Login"
                       : path.StartsWithSegments("/MuaHang") ? "/MuaHang/Auth/Login"
                       : options.LoginPath;
            context.Response.Redirect(target);
            return Task.CompletedTask;
        };

        // Theo dõi phiên đăng nhập: mỗi lần đăng nhập web tạo 1 dòng PhienDangNhap + claim "MaPhien";
        // phiên bị thu hồi ("đăng xuất thiết bị khác") sẽ bị từ chối ở request kế tiếp.
        options.Events.OnSigningIn = async context =>
        {
            try
            {
                if (context.Principal?.Identity is System.Security.Claims.ClaimsIdentity id
                    && int.TryParse(id.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value, out var maTK))
                {
                    var db = context.HttpContext.RequestServices.GetRequiredService<AppDbContext>();
                    var ua = context.HttpContext.Request.Headers.UserAgent.ToString();
                    var phien = new FoodServiceApp.Web.Models.Entities.PhienDangNhap
                    {
                        MaPhien = Guid.NewGuid(),
                        MaTK = maTK,
                        Kenh = "web",
                        ThietBi = ua.Length > 190 ? ua[..190] : ua,
                        DiaChiIP = context.HttpContext.Connection.RemoteIpAddress?.ToString(),
                    };
                    db.PhienDangNhaps.Add(phien);
                    await db.SaveChangesAsync();
                    id.AddClaim(new System.Security.Claims.Claim("MaPhien", phien.MaPhien.ToString()));
                }
            }
            catch { /* không để lỗi ghi phiên chặn đăng nhập */ }
        };
        options.Events.OnValidatePrincipal = async context =>
        {
            var v = context.Principal?.FindFirst("MaPhien")?.Value;
            if (!Guid.TryParse(v, out var maPhien)) return;
            try
            {
                var db = context.HttpContext.RequestServices.GetRequiredService<AppDbContext>();
                var phien = await db.PhienDangNhaps.FirstOrDefaultAsync(p => p.MaPhien == maPhien);
                if (phien != null && phien.ThuHoiLuc != null)
                {
                    context.RejectPrincipal();
                    await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                    return;
                }
                if (phien != null && (DateTime.Now - phien.LanCuoi).TotalMinutes > 5)
                {
                    phien.LanCuoi = DateTime.Now;
                    await db.SaveChangesAsync();
                }
            }
            catch { }
        };
    })
    .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSection["Issuer"],
            ValidAudience = jwtSection["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSection["Key"]!)),
        };
    });

builder.Services.AddAuthorization();

// FoodApp (MAUI) gọi từ thiết bị thật/emulator -> cần CORS mở cho dev.
// Khi triển khai thật, nên giới hạn origin cụ thể thay vì AllowAnyOrigin.
builder.Services.AddCors(options =>
{
    options.AddPolicy("MobileApp", policy =>
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
});

var app = builder.Build();

// Tạo database/schema tự động nếu instance đã chạy nhưng database chưa tồn tại.
// Nếu bạn có file QuanLyDichVuAnUong_Full.sql, vẫn nên chạy file SQL đó để có dữ liệu mẫu.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    try
    {
        db.Database.EnsureCreated();
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[Database] Không thể khởi tạo SQL Server: {ex.Message}");
    }
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(); // /swagger — chỉ để test các route /api/*, không ảnh hưởng Website
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// Ghi log lỗi hệ thống (exception chưa được xử lý) vào NhatKyHeThong rồi ném lại cho trình xử lý lỗi chung
app.Use(async (ctx, next) =>
{
    try { await next(); }
    catch (Exception ex)
    {
        try
        {
            using var scope = ctx.RequestServices.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await FoodServiceApp.Web.Services.NhatKy.GhiAsync(db, ctx, "LoiHeThong", ex.GetType().Name + ": " + ex.Message, null, "Error", 500);
        }
        catch { }
        throw;
    }
});

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.UseSession();

app.UseCors("MobileApp");

app.UseAuthentication();
app.UseAuthorization();

// Giám sát: /health trả 200 nếu ứng dụng + SQL Server còn kết nối được, 503 nếu không
app.MapGet("/health", async (AppDbContext db) =>
{
    try { return await db.Database.CanConnectAsync() ? Results.Ok(new { trangThai = "OK", sql = "OK", thoiGian = DateTime.Now }) : Results.Json(new { trangThai = "LOI", sql = "KhongKetNoi" }, statusCode: 503); }
    catch (Exception ex) { return Results.Json(new { trangThai = "LOI", sql = ex.Message }, statusCode: 503); }
});

app.MapControllers(); // API attribute-routed trong Controllers/Api/*
// Không đặt default controller/action nữa — nếu không, "/" sẽ vừa khớp route quy
// ước (Dashboard/Index) vừa khớp [HttpGet("/")] của CustomerHomeController, gây
// xung đột AmbiguousMatch. "/" giờ chỉ do CustomerHomeController xử lý (trang chủ
// công khai cho khách hàng); các link nội bộ (asp-controller/asp-action) của
// Gian hàng/Quản trị/Tài xế vẫn hoạt động bình thường vì luôn sinh URL đầy đủ.
app.MapControllerRoute(
    name: "default",
    pattern: "{controller}/{action}/{id?}");

app.Run();
