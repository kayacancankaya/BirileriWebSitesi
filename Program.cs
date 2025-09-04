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
using BirileriWebSitesi.Areas.Identity;
using BirileriWebSitesi.Apis.Interfaces;
using BirileriWebSitesi.Apis.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.AddConsole();

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



connectionString = builder.Configuration["BirileriConnectionString"] ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseMySql(connectionString,
            ServerVersion.AutoDetect(connectionString),
            mysqlOptions => mysqlOptions.CommandTimeout(10))
        );

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

builder.Services.AddRazorPages(options =>
{
    options.Conventions.AuthorizeAreaFolder("Identity", "/Account/Manage"); // Secure areas
    options.Conventions.AllowAnonymousToAreaPage("Identity", "/Account/Login"); // Allow anonymous login
});


builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(20);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

builder.Services.AddIdentity<IdentityUser, IdentityRole>(options =>
{ 
    options.SignIn.RequireConfirmedAccount = true;
    options.Tokens.ProviderMap.Add("CustomEmailConfirmation",
        new TokenProviderDescriptor(typeof(CustomEmailConfirmationTokenProvider<IdentityUser>)));
    options.Tokens.EmailConfirmationTokenProvider = "CustomEmailConfirmation";

})
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultUI()
    .AddErrorDescriber<TurkishIdentityErrorDescriber>();


builder.Services.AddTransient<CustomEmailConfirmationTokenProvider<IdentityUser>>();
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
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IIyzipayPaymentService, IyziPayPaymentService>();
builder.Services.AddScoped<IInventoryService, InventoryService>();
builder.Services.AddScoped<IBlogInterface, BlogService>();
builder.Services.AddHttpClient<IEasyCargoService, EasyCargoService>();

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

var supportedCultures = new[] { "tr-TR" };
var localizationOptions = new RequestLocalizationOptions
{
    DefaultRequestCulture = new Microsoft.AspNetCore.Localization.RequestCulture("tr-TR"),
    SupportedCultures = supportedCultures.Select(c => new System.Globalization.CultureInfo(c)).ToList(),
    SupportedUICultures = supportedCultures.Select(c => new System.Globalization.CultureInfo(c)).ToList()
};
builder.Services.Configure<DataProtectionTokenProviderOptions>(o =>
       o.TokenLifespan = TimeSpan.FromHours(3));
var app = builder.Build();

app.UseForwardedHeaders();

app.UseRequestLocalization(localizationOptions);

app.UseSession();

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

// Set up custom content types - associating file extension to MIME type
var provider = new FileExtensionContentTypeProvider();
// Add new mappings
provider.Mappings[".avif"] = "image/avif";

app.UseStaticFiles(new StaticFileOptions
{
    ContentTypeProvider = provider
});

app.UseRouting();

app.UseAuthentication();

app.UseAuthorization();

//app.MapGet("/robots.txt", async context =>
//{
//    context.Response.ContentType = "text/plain";

//    // Allow all crawlers
//    var robotsContent = @"User-agent: *
//Disallow:

//Sitemap: https://www.birilerigt.com/sitemap.xml";

//    await context.Response.WriteAsync(robotsContent);
//});
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");


app.MapRazorPages();


app.Run();

