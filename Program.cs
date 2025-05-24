using BirileriWebSitesi.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using BirileriWebSitesi.Interfaces;
using BirileriWebSitesi.Services;
using BirileriWebSitesi.Areas;
using BirileriWebSitesi.Models;
using Microsoft.AspNetCore.HttpOverrides;
using System.Net;
using Microsoft.AspNetCore.StaticFiles;
using System.Diagnostics;

var counter = Stopwatch.StartNew();

var builder = WebApplication.CreateBuilder(args);


Console.WriteLine($"[Startup] Builder initialization: {counter.ElapsedMilliseconds} ms");


builder.Logging.AddConsole();

Console.WriteLine($"[Startup] Adding Console: {counter.ElapsedMilliseconds} ms");

var config = builder.Configuration;
string? connectionString = string.Empty;
// Load configuration based on environment
if (builder.Environment.IsDevelopment())
{
    builder.Configuration.AddJsonFile("Secrets.json", optional: true, reloadOnChange: true);
}
else
{
    builder.Configuration.AddEnvironmentVariables();
    builder.WebHost.ConfigureKestrel(options =>
    {
        options.ListenAnyIP(5000);
    });
    builder.Services.Configure<ForwardedHeadersOptions>(options =>
    {
        options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
        options.KnownProxies.Add(IPAddress.Parse("127.0.0.1"));
    });
}

Console.WriteLine($"[Startup] Env: {counter.ElapsedMilliseconds} ms");


connectionString = builder.Configuration["BirileriConnectionString"] ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseMySql(connectionString,
            ServerVersion.AutoDetect(connectionString),
            mysqlOptions => mysqlOptions.CommandTimeout(10))
        );
Console.WriteLine($"[Startup] Db: {counter.ElapsedMilliseconds} ms");

builder.Services.Configure<IyzipayOptions>(options =>
{
    options.BaseUrl = builder.Configuration["IyzipayOptions:BaseUrl"];
    options.ApiKey = builder.Configuration["IyzipayOptions:ApiKey"];
    options.SecretKey = builder.Configuration["IyzipayOptions:SecretKey"];
});

Console.WriteLine($"[Startup] Iyzi: {counter.ElapsedMilliseconds} ms");

builder.Services.Configure<IpInfoSettings>(options =>
{
    options.Token = builder.Configuration["IpInfo:Token"];
});
builder.Services.AddAuthentication()
       .AddGoogle(options =>
       {
           IConfigurationSection googleAuthNSection =
           config.GetSection("Authentication:Google"); 
           options.ClientId = googleAuthNSection["ClientId"];
           options.ClientSecret = googleAuthNSection["ClientSecret"];
       })
       .AddFacebook(options =>
       {
           IConfigurationSection FBAuthNSection =
           config.GetSection("Authentication:FB");
           options.ClientId = FBAuthNSection["ClientId"];
           options.ClientSecret = FBAuthNSection["ClientSecret"];
       })
       .AddMicrosoftAccount(microsoftOptions =>
       {
           microsoftOptions.ClientId = config["Authentication:Microsoft:ClientId"];
           microsoftOptions.ClientSecret = config["Authentication:Microsoft:ClientSecret"];
       })
       .AddTwitter(twitterOptions =>
       {
           twitterOptions.ConsumerKey = config["Authentication:Twitter:ConsumerAPIKey"];
           twitterOptions.ConsumerSecret = config["Authentication:Twitter:ConsumerSecret"];
           twitterOptions.RetrieveUserDetails = true;
       });

Console.WriteLine($"[Startup] Iyzi: {counter.ElapsedMilliseconds} ms");
builder.Services.AddRazorPages(options =>
{
    options.Conventions.AuthorizeAreaFolder("Identity", "/Account/Manage"); // Secure areas
    options.Conventions.AllowAnonymousToAreaPage("Identity", "/Account/Login"); // Allow anonymous login
});

Console.WriteLine($"[Startup] Razor: {counter.ElapsedMilliseconds} ms");
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(20);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

Console.WriteLine($"[Startup] Session: {counter.ElapsedMilliseconds} ms");
builder.Services.AddIdentity<IdentityUser, IdentityRole>(options => options.SignIn.RequireConfirmedAccount = true)
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders()
    .AddDefaultUI()
    .AddErrorDescriber<TurkishIdentityErrorDescriber>();

Console.WriteLine($"[Startup] Identity: {counter.ElapsedMilliseconds} ms");
builder.Services.AddControllersWithViews();

Console.WriteLine($"[Startup] MVC: {counter.ElapsedMilliseconds} ms");
builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
});

Console.WriteLine($"[Startup] Json: {counter.ElapsedMilliseconds} ms");

builder.Services.AddScoped<IBasketService, BasketService>(); 
builder.Services.AddScoped<IProductService, ProductService>(); 
builder.Services.AddScoped<IOrderService, OrderService>(); 
builder.Services.AddScoped<IUserService, UserService>(); 
builder.Services.AddScoped<IUserAuditService, UserAuditService>(); 
builder.Services.AddScoped<IEmailSender, EmailSender>();
builder.Services.AddScoped<IIyzipayPaymentService, IyziPayPaymentService>();

Console.WriteLine($"[Startup] Local Services: {counter.ElapsedMilliseconds} ms");

builder.Services.Configure<IdentityOptions>(options =>
{
    // Password settings.
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequireUppercase = true;
    options.Password.RequiredLength = 6;
    options.Password.RequiredUniqueChars = 1;

    // Lockout settings.
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.AllowedForNewUsers = true;

    // User settings.
    options.User.AllowedUserNameCharacters =
    "abc�defg�hijklmno�pqrstu�vwxyzABC�DEFGHI�JKLMNO�PQRSTU�VWXYZ0123456789-._@+";
    options.User.RequireUniqueEmail = true;
});

Console.WriteLine($"[Startup] Identity Options: {counter.ElapsedMilliseconds} ms");
builder.Services.ConfigureApplicationCookie(options =>
{
    // Cookie settings
    options.Cookie.HttpOnly = true;
     options.ExpireTimeSpan = TimeSpan.FromMinutes(30); 
     options.SlidingExpiration = true; // Renew the cookie when active
   

    options.LoginPath = "/Identity/Account/Login";
    options.AccessDeniedPath = "/Identity/Account/AccessDenied";
    options.Cookie.IsEssential = true;
});

Console.WriteLine($"[Startup] Cookies: {counter.ElapsedMilliseconds} ms");
var supportedCultures = new[] { "tr-TR" };
var localizationOptions = new RequestLocalizationOptions
{
    DefaultRequestCulture = new Microsoft.AspNetCore.Localization.RequestCulture("tr-TR"),
    SupportedCultures = supportedCultures.Select(c => new System.Globalization.CultureInfo(c)).ToList(),
    SupportedUICultures = supportedCultures.Select(c => new System.Globalization.CultureInfo(c)).ToList()
};

Console.WriteLine($"[Startup] Localization: {counter.ElapsedMilliseconds} ms");
var app = builder.Build();

Console.WriteLine($"[Startup] Build: {counter.ElapsedMilliseconds} ms");
app.UseForwardedHeaders();

Console.WriteLine($"[Startup] ForwardedHeadears: {counter.ElapsedMilliseconds} ms");
app.UseRequestLocalization(localizationOptions);

Console.WriteLine($"[Startup] Localization: {counter.ElapsedMilliseconds} ms");
app.UseSession();

Console.WriteLine($"[Startup] Session: {counter.ElapsedMilliseconds} ms");
// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
  app.UseExceptionHandler("/home/notfound");
  // the default hsts value is 30 days. you may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
  app.UseHsts();
}

Console.WriteLine($"[Startup] Exc Handl: {counter.ElapsedMilliseconds} ms");
// Set up custom content types - associating file extension to MIME type
var provider = new FileExtensionContentTypeProvider();
// Add new mappings
provider.Mappings[".avif"] = "image/avif";

Console.WriteLine($"[Startup] Aviif: {counter.ElapsedMilliseconds} ms");
app.UseStaticFiles(new StaticFileOptions
{
    ContentTypeProvider = provider
});

Console.WriteLine($"[Startup] StaticFiles: {counter.ElapsedMilliseconds} ms");
app.UseRouting();

Console.WriteLine($"[Startup] Routing: {counter.ElapsedMilliseconds} ms");
app.UseAuthentication();

Console.WriteLine($"[Startup] Authorize: {counter.ElapsedMilliseconds} ms");
app.UseAuthorization();

Console.WriteLine($"[Startup] Authenticate: {counter.ElapsedMilliseconds} ms");
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

Console.WriteLine($"[Startup] Map Controller: {counter.ElapsedMilliseconds} ms");
app.MapRazorPages();

Console.WriteLine($"[Startup] Map Razor: {counter.ElapsedMilliseconds} ms");

app.Run();

Console.WriteLine($"[Startup] Run: {counter.ElapsedMilliseconds} ms");


counter.Stop();