using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Services.Extensions.Logger.Service;
using RealTimeAttendanceTracker.lib.Entity;
using RealTimeAttendanceTracker.lib.Service;

namespace RealTimeAttendanceTracker.Web
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddControllersWithViews();
            builder.Services.AddControllersWithViews().AddRazorRuntimeCompilation();
            builder.Services.AddAuthentication();
            builder.Services.AddAuthorization();
            builder.Services.AddSingleton<AttendanceContext>();
            builder.Services.AddSingleton<AttendanceService>();
            builder.Services.AddDataProtection();
            builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
     .AddCookie(options =>
     {
         options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
         options.LoginPath = "/Login/Index";
         options.LogoutPath = "/Login/Logout";
         options.SlidingExpiration = true;
     });
            builder.Services.AddDistributedMemoryCache(); // Required for session
            builder.Services.AddSession(options =>
            {
                options.IdleTimeout = TimeSpan.FromMinutes(30); // Set session timeout
                options.Cookie.HttpOnly = true;
                options.Cookie.IsEssential = true;
            });
            ConfigHelper._configuration = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json")
    .AddJsonFile("appsettings.Development.json")
    .Build();
            builder.Services.AddSession();
            builder.Services.AddMemoryCache();
            builder.Services.ConfigureApplicationCookie(options =>
            {
                options.Cookie.Name = ".AspNetCore.Identity.Application";
                options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
                options.Cookie.MaxAge = TimeSpan.FromMinutes(30);
                options.SlidingExpiration = true;
            });
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowAll",
                    builder => builder
                        .AllowAnyOrigin()
                        .AllowAnyMethod()
                        .AllowAnyHeader());
            });
            builder.Services.AddControllers();
            //builder.WebHost.UseUrls("http://192.168.1.10:5248");
            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseSession();
            app.UseRouting();
            app.UseCors("AllowAll");
            app.UseAuthorization();

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Login}/{action=Index}/{id?}");

            app.Run();
        }
    }
}
