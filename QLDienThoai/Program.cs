using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Encodings.Web;
using System.Text.Unicode;
using QLDienThoai.Data;
using QLDienThoai.Models;
using QLDienThoai.Services;

var builder = WebApplication.CreateBuilder(new WebApplicationOptions
{
    Args = args,
    ContentRootPath = TimThuMucGocWeb()
});

// MVC + chống CSRF tự động cho mọi POST/PUT/DELETE
builder.Services.AddControllersWithViews(o =>
{
    o.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());
    // Chỉ bắt buộc các trường có [Required] rõ ràng (không tự suy ra từ kiểu string không-null)
    o.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true;
});

// Không mã hóa chữ tiếng Việt thành &#x...; khi render (vẫn mã hóa < > & " để chống XSS)
builder.Services.AddSingleton(HtmlEncoder.Create(UnicodeRanges.All));

// EF Core + SQL Server
builder.Services.AddDbContext<AppDbContext>(o =>
    o.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Session (giỏ hàng, so sánh)
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(o =>
{
    o.IdleTimeout = TimeSpan.FromHours(2);
    o.Cookie.Name = ".QLDienThoai.Session";
    o.Cookie.HttpOnly = true;
    o.Cookie.IsEssential = true;
    o.Cookie.SameSite = SameSiteMode.Lax;
});

// Xác thực bằng cookie + phân quyền theo vai trò
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(o =>
    {
        o.LoginPath = "/TaiKhoan/DangNhap";
        o.LogoutPath = "/TaiKhoan/DangXuat";
        o.AccessDeniedPath = "/TaiKhoan/TuChoi";
        o.Cookie.Name = ".QLDienThoai.Auth";
        o.Cookie.HttpOnly = true;
        o.Cookie.SameSite = SameSiteMode.Lax;
        o.ExpireTimeSpan = TimeSpan.FromDays(7);
        o.SlidingExpiration = true;
    });
builder.Services.AddAuthorization();

builder.Services.AddHttpContextAccessor();
builder.Services.AddSingleton<IPasswordHasher<TaiKhoan>, PasswordHasher<TaiKhoan>>();
builder.Services.AddSingleton<HtmlCleaner>();
builder.Services.AddScoped<GioHangService>();
builder.Services.AddScoped<SoSanhService>();
builder.Services.AddScoped<DaXemService>();
builder.Services.AddScoped<UploadService>();
builder.Services.AddScoped<EmailService>();
builder.Services.AddScoped<VnPayService>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();   // phục vụ cả ảnh upload lúc chạy (wwwroot/uploads)
app.UseRouting();
app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Dashboard}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

await DbSeeder.SeedAsync(app.Services);

app.Run();

// Khi chạy bằng nút Run/F5 của Visual Studio (chế độ Open Folder), thư mục làm việc có thể là bin\Debug\net10.0
// (không có wwwroot) => WebRootPath = null, không lưu được ảnh upload. Tìm thư mục project có wwwroot để làm gốc.
static string TimThuMucGocWeb()
{
    var cwd = Directory.GetCurrentDirectory();
    if (Directory.Exists(Path.Combine(cwd, "wwwroot"))) return cwd;
    var dir = new DirectoryInfo(AppContext.BaseDirectory);
    for (var i = 0; i < 6 && dir != null; i++, dir = dir.Parent)
        if (Directory.Exists(Path.Combine(dir.FullName, "wwwroot")) && dir.GetFiles("*.csproj").Length > 0)
            return dir.FullName;
    return cwd;
}
