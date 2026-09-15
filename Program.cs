using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using FarmFlow.Web.Data;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllersWithViews();

// Add Database Context
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Add Authentication
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Auth/Login";
        options.LogoutPath = "/Auth/Logout";
        options.AccessDeniedPath = "/Auth/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
        options.Cookie.Name = "FarmFlow.Auth";
    });

builder.Services.AddAuthorization();

var app = builder.Build();
Console.WriteLine(BCrypt.Net.BCrypt.HashPassword("Admin123!"));
// Configure the HTTP request pipeline
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

// ==============================================
// ADD DASHBOARD ROUTE
// ==============================================
app.MapControllerRoute(
    name: "dashboard",
    pattern: "Dashboard/{action=Index}/{id?}",
    defaults: new { controller = "Dashboard", action = "Index" });

// Route for UserManagement
app.MapControllerRoute(
    name: "usermanagement",
    pattern: "UserManagement/{action=Index}/{id?}",
    defaults: new { controller = "UserManagement", action = "Index" });

// Supplier
app.MapControllerRoute(
    name: "supplier",
    pattern: "Supplier/{action=Index}/{id?}",
    defaults: new { controller = "Supplier", action = "Index" });

// PurchaseOrder
app.MapControllerRoute(
    name: "purchaseorder",
    pattern: "PurchaseOrder/{action=Index}/{id?}",
    defaults: new { controller = "PurchaseOrder", action = "Index" });

// Default route - goes to Login page
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Auth}/{action=Login}/{id?}");

// Route for Farm
app.MapControllerRoute(
    name: "farm",
    pattern: "Farm/{action=Index}/{id?}",
    defaults: new { controller = "Farm", action = "Index" });

    // Crop route
app.MapControllerRoute(
    name: "crop",
    pattern: "Crop/{action=Index}/{id?}",
    defaults: new { controller = "Crop", action = "Index" });

    // ProductionBatch route
app.MapControllerRoute(
    name: "productionbatch",
    pattern: "ProductionBatch/{action=Index}/{id?}",
    defaults: new { controller = "ProductionBatch", action = "Index" });
    
    // ✅ NEW: Inventory
    app.MapControllerRoute(
        name: "inventory",
        pattern: "Inventory/{action=Index}/{id?}",
        defaults: new { controller = "Inventory", action = "Index" });

    // ✅ NEW: Category
    app.MapControllerRoute(
        name: "category",
        pattern: "Category/{action=Index}/{id?}",
        defaults: new { controller = "Category", action = "Index" });


       
app.Run();