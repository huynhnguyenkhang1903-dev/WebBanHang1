using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Websitebanhang.Data;
using Websitebanhang.Models;
using Websitebanhang.Repositores;
using Websitebanhang.Services;

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

    options.Password.RequireDigit = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireLowercase = false;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequiredLength = 6;

    options.User.AllowedUserNameCharacters =
        "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+ áàảãạăắằẳẵặâấầẩẫậéèẻẽẹêếềểễệíìỉĩịóòỏõọôốồổỗộơớờởỡợúùủũụưứừửữựýỳỷỹỵđÁÀẢÃẠĂẮẰẲẴẶÂẤẦẨẪẬÉÈẺẼẸÊẾỀỂỄỆÍÌỈĨỊÓÒỎÕỌÔỐỒỔỖỘƠỚỜỞỠỢÚÙỦŨỤƯỨỪỬỮỰÝỲỶỸỴĐ";
})
.AddRoles<IdentityRole>()
.AddEntityFrameworkStores<AppDbContext>();

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

// ================= REPOSITORY =================
builder.Services.AddScoped<IProductRepository, EFProductRepository>();
builder.Services.AddScoped<ICategoryRepository, EFCategoryRepository>();
builder.Services.AddScoped<IEmailService, EmailService>();

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

app.UseRequestLocalization(new RequestLocalizationOptions
{
    DefaultRequestCulture = new RequestCulture("vi"),
    SupportedCultures = supportedCultures,
    SupportedUICultures = supportedCultures
});

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

        await userManager.CreateAsync(admin, "123456");
        await userManager.AddToRoleAsync(admin, "Admin");
    }

    // ===== SEED DATA =====
    DbInitializer.Seed(context);
}

app.Run();