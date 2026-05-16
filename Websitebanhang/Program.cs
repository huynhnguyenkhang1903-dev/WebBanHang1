using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Websitebanhang.Data;
using Websitebanhang.Models;
using Websitebanhang.Repositores;
using Websitebanhang.Services;
using Websitebanhang.Hubs;

using Microsoft.AspNetCore.Localization;
using System.Globalization;

var builder = WebApplication.CreateBuilder(args);

// ================= LOCALIZATION =================
builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");

// ================= MVC =================
builder.Services.AddControllersWithViews()
    .AddViewLocalization() // 🔥 THÊM
    .AddDataAnnotationsLocalization() // 🔥 THÊM
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler =
            System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
    });

builder.Services.AddRazorPages();
builder.Services.AddSignalR(); // 🔥 THÊM SIGNALR

// ================= DATABASE =================
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        sqlOptions => sqlOptions.EnableRetryOnFailure()
    ));

// ================= IDENTITY =================
builder.Services.AddDefaultIdentity<ApplicationUser>(options =>
{
    options.SignIn.RequireConfirmedAccount = false;

    options.Password.RequireDigit = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequiredLength = 8;

    options.User.AllowedUserNameCharacters =
        "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+ áàảãạăắằẳẵặâấầẩẫậéèẻẽẹêếềểễệíìỉĩịóòỏõọôốồổỗộơớờởỡợúùủũụưứừửữựýỳỷỹỵđÁÀẢÃẠĂẮẰẲẴẶÂẤẦẨẪẬÉÈẺẼẸÊẾỀỂỄỆÍÌỈĨỊÓÒỎÕỌÔỐỒỔỖỘƠỚỜỞỠỢÚÙỦŨỤƯỨỪỬỮỰÝỲỶỸỴĐ";

    // Cấu hình Lockout
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.AllowedForNewUsers = true;
})
.AddRoles<IdentityRole>()
.AddEntityFrameworkStores<AppDbContext>();

builder.Services.AddAuthentication()
    .AddGoogle(options =>
    {
        var googleAuthNSection = builder.Configuration.GetSection("Authentication:Google");
        options.ClientId = googleAuthNSection["ClientId"]!;
        options.ClientSecret = googleAuthNSection["ClientSecret"]!;
    })
    .AddFacebook(options =>
    {
        var facebookAuthNSection = builder.Configuration.GetSection("Authentication:Facebook");
        options.AppId = facebookAuthNSection["AppId"]!;
        options.AppSecret = facebookAuthNSection["AppSecret"]!;
    });

// ================= COOKIE =================
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/Login";
});

// ================= SESSION =================
builder.Services.AddDistributedMemoryCache();

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// ================= REPOSITORY & SERVICES =================
builder.Services.AddScoped<IProductRepository, EFProductRepository>();
builder.Services.AddScoped<ICategoryRepository, EFCategoryRepository>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IActivityLogService, ActivityLogService>();
builder.Services.AddScoped<IWebsiteSettingService, WebsiteSettingService>();
builder.Services.AddHttpContextAccessor();

var app = builder.Build();

// ================= LOCALIZATION MIDDLEWARE =================
var supportedCultures = new[]
{
    new CultureInfo("vi"),     // 🇻🇳 Vietnamese
    new CultureInfo("en"),     // 🇺🇸 English
    new CultureInfo("fr"),     // 🇫🇷 French
    new CultureInfo("de"),     // 🇩🇪 German
    new CultureInfo("es"),     // 🇪🇸 Spanish
    new CultureInfo("it"),     // 🇮🇹 Italian
    new CultureInfo("pt"),     // 🇵🇹 Portuguese
    new CultureInfo("ru"),     // 🇷🇺 Russian
    new CultureInfo("ja"),     // 🇯🇵 Japanese
    new CultureInfo("ko"),     // 🇰🇷 Korean
    new CultureInfo("zh-CN"),  // 🇨🇳 Chinese Simplified
    new CultureInfo("zh-TW"),  // 🇹🇼 Chinese Traditional
    new CultureInfo("th"),     // 🇹🇭 Thai
    new CultureInfo("id"),     // 🇮🇩 Indonesian
    new CultureInfo("ms")      // 🇲🇾 Malay
};

var localizationOptions = new RequestLocalizationOptions
{
    DefaultRequestCulture = new RequestCulture("vi"),
    SupportedCultures = supportedCultures,
    SupportedUICultures = supportedCultures
};

// Ưu tiên đọc ngôn ngữ từ Cookie
localizationOptions.RequestCultureProviders.Clear();
localizationOptions.RequestCultureProviders.Add(new CookieRequestCultureProvider());

app.UseRequestLocalization(localizationOptions);

// ================= MIDDLEWARE =================
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.UseSession();

// ================= ROUTE =================
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapRazorPages();
app.MapHub<NotificationHub>("/notificationHub"); // 🔥 THÊM SIGNALR HUB ROUTE

// ================= SEED DATA =================
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;

    var context = services.GetRequiredService<AppDbContext>();
    var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
    var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();

    // APPLY MIGRATION
    context.Database.Migrate();

    // ===== CREATE ROLES =====
    if (!await roleManager.RoleExistsAsync("Admin"))
        await roleManager.CreateAsync(new IdentityRole("Admin"));

    if (!await roleManager.RoleExistsAsync("User"))
        await roleManager.CreateAsync(new IdentityRole("User"));

    // ===== CREATE ADMIN =====
    var adminEmail = "admin@gmail.com";

    if (await userManager.FindByEmailAsync(adminEmail) == null)
    {
        var admin = new ApplicationUser
        {
            UserName = adminEmail,
            Email = adminEmail,
            EmailConfirmed = true,
            FullName = "Administrator",
            Address = "System",
            DateOfBirth = new DateTime(2000, 1, 1)
        };

        await userManager.CreateAsync(admin, "Admin@123");
        await userManager.AddToRoleAsync(admin, "Admin");
    }

    // ===== SEED DATA =====
    DbInitializer.Seed(context);
}

app.Run();