using System.Globalization;
using ABCRetails.Data;
using ABCRetails.Services;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PROG6212POE.Models;

namespace ABCRetails
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddControllersWithViews();

            // Configure MySQL DbContext
            builder.Services.AddDbContext<ApplicationDbContext>(options =>
                options.UseMySql(
                    builder.Configuration.GetConnectionString("DefaultConnection"),
                    new MySqlServerVersion(new Version(8, 0, 33))
                )
            );

            // Configure Identity for .NET 9
            builder.Services.AddIdentity<User, IdentityRole>(options =>
            {
                options.SignIn.RequireConfirmedAccount = false;
                // Configure password options if needed
                options.Password.RequireDigit = false;
                options.Password.RequiredLength = 6;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequireUppercase = false;
                options.Password.RequireLowercase = false;
            })
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders();

            // Configure HttpClient
            builder.Services.AddHttpClient("Functions", (sp, client) =>
            {
                var service = sp.GetRequiredService<IConfiguration>();
                var baseUrl = service["Functions:BaseUrl"] ?? throw new InvalidOperationException();
                client.BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/api/");
                client.Timeout = TimeSpan.FromSeconds(100);
            });

            builder.Services.AddScoped<IFunctionApi, FunctionApiClient>();
            builder.Services.AddScoped<IProductService, ProductService>();
            builder.Services.AddScoped<IOrderService, OrderService>();
            builder.Services.AddScoped<IUserService, UserService>();

            // Allow larger multipart uploads
            builder.Services.Configure<FormOptions>(o =>
            {
                o.MultipartBodyLengthLimit = 50 * 1024 * 1024; // 50MB
            });

            // Add logging
            builder.Services.AddLogging();

            var app = builder.Build();

            // Set culture for decimal handling (fixes price issue)
            var culture = new CultureInfo("en-US");
            CultureInfo.DefaultThreadCurrentCulture = culture;
            CultureInfo.DefaultThreadCurrentUICulture = culture;

            // Configure the HTTP request pipeline
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseRouting();

            app.UseAuthentication(); // <--- important for Identity
            app.UseAuthorization();

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");

            app.Run();
        }
    }
}
