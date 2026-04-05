using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Websitebanhang.Data;
using Websitebanhang.Models;
using Websitebanhang.Repositores;

var builder = WebApplication.CreateBuilder(args);


// ================= MVC =================

builder.Services.AddControllersWithViews();
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
})
.AddRoles<IdentityRole>()
.AddEntityFrameworkStores<AppDbContext>();


// ================= SESSION =================

builder.Services.AddDistributedMemoryCache();

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});


// ================= REPOSITORY =================

builder.Services.AddScoped<IProductRepository, MockProductRepository>();
builder.Services.AddScoped<ICategoryRepository, MockCategoryRepository>();


var app = builder.Build();


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


    // ===== CREATE ADMIN ACCOUNT =====

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


    // ===== CATEGORY SEED =====

    Category category;

    if (!context.Categories.Any())
    {
        category = new Category { Name = "Coffee" };
        context.Categories.Add(category);
        context.SaveChanges();
    }
    else
    {
        category = context.Categories.First();
    }


    // ===== PRODUCT SEED =====

    if (!context.Products.Any())
    {
        var countries = new[]
        {
            "Vietnam",
            "Brazil",
            "Colombia",
            "Ethiopia",
            "Indonesia"
        };

        for (int i = 1; i <= 70; i++)
        {
            context.Products.Add(new Product
            {
                Name = "Coffee " + i,
                Price = 50000 + (i * 10000),
                Description = "Coffee product " + i,
                Country = countries[i % countries.Length],
                CategoryId = category.Id,
                ImageUrl = "/images/coffee.jpg"
            });
        }

        context.SaveChanges();
    }
}

app.Run();