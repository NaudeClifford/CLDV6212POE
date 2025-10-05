using System.Globalization;
using ABCRetails.Services;
using Microsoft.AspNetCore.Http.Features;

namespace ABCRetails
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddControllersWithViews();

            builder.Services.AddHttpClient("Functions", (sp, client) =>
            {
                var service = sp.GetRequiredService<IConfiguration>();
                var baseUrl = service["Functions:BaseUrl"] ?? throw new InvalidOperationException();
                client.BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/api/");
                client.Timeout = TimeSpan.FromSeconds(100);
            });

            //Register the typed client
            builder.Services.AddScoped<IFunctionApi, FunctionApiClient>();

            //allow larger multipart uploads
            builder.Services.Configure<FormOptions>(o =>
            {
                o.MultipartBodyLengthLimit = 50 * 1024 * 1024; //50MB
            });

            //Add logging
            builder.Services.AddLogging();

            var app = builder.Build();

            //Set culture for decimal handling(fixes price issue)
            var culture = new CultureInfo("en-US");
            CultureInfo.DefaultThreadCurrentCulture = culture;
            CultureInfo.DefaultThreadCurrentUICulture = culture;

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseRouting();

            app.UseAuthorization();

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");

            app.Run();
        }
    }
}
