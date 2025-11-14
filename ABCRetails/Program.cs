using System.Globalization;
using ABCRetails.Data;
using ABCRetails.Models;
using ABCRetails.Services;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace ABCRetails
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Global authentication policy (Require login by default)
            builder.Services.AddControllersWithViews(options =>
            {
                var policy = new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder()
                    .RequireAuthenticatedUser()
                    .Build();

                options.Filters.Add(new Microsoft.AspNetCore.Mvc.Authorization.AuthorizeFilter(policy));
            });

            // App services
            builder.Services.AddScoped<IFunctionApi, FunctionApiClient>();
            builder.Services.AddScoped<IProductService, ProductService>();
            builder.Services.AddScoped<IOrderService, OrderService>();
            builder.Services.AddScoped<IUserService, UserService>();

            // Database
            builder.Services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

            // Identity
            builder.Services.AddIdentity<User, IdentityRole>(options =>
            {
                options.Password.RequiredLength = 8;
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireNonAlphanumeric = true;
                options.User.RequireUniqueEmail = true;
            })
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddSignInManager()
            .AddDefaultTokenProviders();

            builder.Services.ConfigureApplicationCookie(options =>
            {
                options.LoginPath = "/Account/Login";
                options.LogoutPath = "/Account/Logout";
                options.AccessDeniedPath = "/Account/Login";
            });

            // Http Client for API
            builder.Services.AddHttpClient("Functions", (sp, client) =>
            {
                var config = sp.GetRequiredService<IConfiguration>();
                var baseUrl = config["Functions:BaseUrl"] ?? throw new InvalidOperationException();
                client.BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/api/");
            });

            // Session + Memory cache
            builder.Services.AddDistributedMemoryCache();
            builder.Services.AddSession(options =>
            {
                options.IdleTimeout = TimeSpan.FromHours(1);
                options.Cookie.HttpOnly = true;
                options.Cookie.IsEssential = true;
            });

            builder.Services.Configure<FormOptions>(o =>
            {
                o.MultipartBodyLengthLimit = 50 * 1024 * 1024;
            });

            var app = builder.Build();

            // Decimal culture fix
            CultureInfo.DefaultThreadCurrentCulture = new CultureInfo("en-US");
            CultureInfo.DefaultThreadCurrentUICulture = new CultureInfo("en-US");

            // Error handling
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }

            // ?RUN MIGRATIONS & SEED USERS
            using (var scope = app.Services.CreateScope())
            {
                try
                {
                    var services = scope.ServiceProvider;
                    var context = services.GetRequiredService<ApplicationDbContext>();

                    // Ensures tables exist
                    context.Database.Migrate();

                    // Run your user seeder
                    await UserSeeder.SeedAsync(services);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("ERROR DURING SEEDING: ");
                    Console.WriteLine(ex.Message);
                    Console.WriteLine(ex.StackTrace);
                }
            }

            // Middleware
            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseRouting();
            app.UseSession();
            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");

            app.Run();
        }
    }
}
