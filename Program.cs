using BirileriWebSitesi.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using BirileriWebSitesi.Interfaces;
using BirileriWebSitesi.Services;
using BirileriWebSitesi.Areas;
using BirileriWebSitesi.Models;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);
builder.Logging.ClearProviders();
builder.Logging.AddConsole();

var config = builder.Configuration;
string? connectionString = string.Empty;
builder.Configuration.AddEnvironmentVariables();
// Load configuration based on environment
if (builder.Environment.IsDevelopment())
{
    // Add secret.json file for development environment
    builder.Configuration.AddJsonFile("Secrets.json", optional: true, reloadOnChange: true);
}
else
{
    // In production, use environment variables for sensitive data
    builder.Configuration.AddEnvironmentVariables();
}

//if (builder.Environment.IsProduction())
//{
//builder.WebHost.UseUrls("https://birilerigt.com");
//builder.Services.Configure<ForwardedHeadersOptions>(options =>
//{
//    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
//    options.KnownNetworks.Clear(); // Clear known networks to allow all
//    options.KnownProxies.Clear();  // Clear known proxies to allow all
//});
builder.WebHost.ConfigureKestrel(options =>
    {
        options.ListenAnyIP(5000); 

    });

    connectionString = builder.Configuration["BirileriConnectionString"] ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseMySql(connectionString,
            new MySqlServerVersion(new Version(8, 0, 42)),
            mysqlOptions => mysqlOptions.CommandTimeout(10));
        

builder.Services.Configure<IyzipayOptions>(options =>
{
    options.BaseUrl = builder.Configuration["IyzipayOptions:BaseUrl"];
    options.ApiKey = builder.Configuration["IyzipayOptions:ApiKey"];
    options.SecretKey = builder.Configuration["IyzipayOptions:SecretKey"];
});


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
//builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddRazorPages(options =>
{
    options.Conventions.AuthorizeAreaFolder("Identity", "/Account/Manage"); // Secure areas
    options.Conventions.AllowAnonymousToAreaPage("Identity", "/Account/Login"); // Allow anonymous login
});
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromDays(365 * 5); // 5 years
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});
builder.Services.AddIdentity<IdentityUser, IdentityRole>(options => options.SignIn.RequireConfirmedAccount = true)
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders()
    .AddDefaultUI()
    .AddErrorDescriber<TurkishIdentityErrorDescriber>();
builder.Services.AddControllersWithViews();
builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
});

builder.Services.AddScoped<IBasketService, BasketService>(); 
builder.Services.AddScoped<IProductService, ProductService>(); 
builder.Services.AddScoped<IOrderService, OrderService>(); 
builder.Services.AddScoped<IUserService, UserService>(); 
builder.Services.AddScoped<IUserAuditService, UserAuditService>(); 
builder.Services.AddSingleton<IEmailSender, SmtpEmailSender>();


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
Console.WriteLine("Using identity options: ");
builder.Services.ConfigureApplicationCookie(options =>
{
    // Cookie settings
    options.Cookie.HttpOnly = true;
     options.ExpireTimeSpan = TimeSpan.FromDays(365 * 5); // 5 years
     options.SlidingExpiration = true; // Renew the cookie when active
   

    options.LoginPath = "/Identity/Account/Login";
    options.AccessDeniedPath = "/Identity/Account/AccessDenied";
    options.Cookie.IsEssential = true;
});

var supportedCultures = new[] { "tr-TR" };
var localizationOptions = new RequestLocalizationOptions
{
    DefaultRequestCulture = new Microsoft.AspNetCore.Localization.RequestCulture("tr-TR"),
    SupportedCultures = supportedCultures.Select(c => new System.Globalization.CultureInfo(c)).ToList(),
    SupportedUICultures = supportedCultures.Select(c => new System.Globalization.CultureInfo(c)).ToList()
};
var sw = Stopwatch.StartNew();
var app = builder.Build();
Console.WriteLine("App built in " + sw.Elapsed);
app.UseRequestLocalization(localizationOptions);

app.UseSession();
// Configure the HTTP request pipeline.
//if (app.Environment.IsDevelopment())
//{
    //app.UseMigrationsEndPoint();
//}
//else
//{
    //app.UseExceptionHandler("/Home/NotFound");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    //app.UseHsts();
//}

//app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseForwardedHeaders();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapRazorPages();
Console.WriteLine("App starting at: " + DateTime.Now);
app.Run();
