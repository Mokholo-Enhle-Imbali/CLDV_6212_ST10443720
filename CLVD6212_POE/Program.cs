// Program.cs
using CLVD6212_POE.Data;
using CLVD6212_POE.Service;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Runtime;

var builder = WebApplication.CreateBuilder(args);

// MVC
builder.Services.AddControllersWithViews();

//register ef core db conext for sql login
builder.Services.AddDbContext<abcretailersDbContext>(options =>
{
    var connStr = builder.Configuration.GetConnectionString("abcretailers");
    options.UseSqlServer(connStr);
});

// Typed HttpClient for your Azure Functions
builder.Services.AddHttpClient("Functions", (sp, client) =>
{
    var cfg = sp.GetRequiredService<IConfiguration>();
    var baseUrl = cfg["Functions:BaseUrl"] ?? throw new InvalidOperationException("Functions:BaseUrl missing");
    client.BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/"); // adjust if your Functions don't use /api
    client.Timeout = TimeSpan.FromSeconds(100);
});

//Cookie Authentication (For login page)
builder.Services.AddAuthentication("MyCookieAuth").AddCookie("MyCookieAuth", options =>
{
    options.LoginPath = "/Login/Index";
    options.AccessDeniedPath = "/Login/AccessDenied";
    options.Cookie.Name = "ABCAuthCookie";
    options.Cookie.HttpOnly= true;
    options.Cookie.SameSite=SameSiteMode.Strict;
    options.Cookie.SecurePolicy= CookieSecurePolicy.Always;
    options.ExpireTimeSpan=TimeSpan.FromMinutes(30);
    options.SlidingExpiration = true;
});

//Session Setup
builder.Services.AddSession(options =>
{
    options.Cookie.Name = "ABCSession";
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SameSite = SameSiteMode.Strict;
});

// Use the typed client (replaces IAzureStorageService everywhere)
builder.Services.AddScoped<IFunctionsApi, FunctionsApiClient>();

// Optional: allow larger multipart uploads (images, proofs, etc.)
builder.Services.Configure<FormOptions>(o =>
{
    o.MultipartBodyLengthLimit = 50 * 1024 * 1024; // 50 MB
});

// Optional: logging is added by default, keeping this is harmless
builder.Services.AddLogging();

var app = builder.Build();

// Culture (your original fix for decimal handling)
var culture = new CultureInfo("en-US");
CultureInfo.DefaultThreadCurrentCulture = culture;
CultureInfo.DefaultThreadCurrentUICulture = culture;

// Pipeline
if (!app.Environment.IsDevelopment())
{
    //app.UseExceptionHandler("/Home/Error");
    app.UseDeveloperExceptionPage(); // show full errors
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Login}/{action=Index}/{id?}");

app.Run();
