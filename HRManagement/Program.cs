using BLL.Interfaces;
using BLL.Repositories;
using DAL.Context;
using DAL.Models;
using Hangfire;
using HRManagement.Helper;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace HRManagement
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddControllersWithViews();
            builder.Services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
            builder.Services.AddAutoMapper(typeof(MappingProfile));
            builder.Services.AddScoped<IMonthlyData, MonthlyData>();
            
            //Connection to the database
            builder.Services.AddDbContext<HRDbContext>(options =>
            { options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")); });
            //Identity configuration
            builder.Services.AddIdentity<User, IdentityRole>(Options => 
            {
                Options.Password.RequiredLength = 1;
                Options.Password.RequireDigit = false;
                Options.Password.RequireNonAlphanumeric = false; 
                Options.Password.RequireUppercase = false; 
                Options.Password.RequireLowercase = false;
            }).AddEntityFrameworkStores<HRDbContext>().AddDefaultTokenProviders();
            builder.Services.ConfigureApplicationCookie(options =>
            {
                options.LoginPath = "/Auth/Login";
                options.ExpireTimeSpan = TimeSpan.FromMinutes(60);
                options.SlidingExpiration = true;
            });
            builder.Services.AddAuthentication();
            //hangfire configuration
            builder.Services.AddHangfire(configuration =>
            {
                configuration.UseSqlServerStorage(
                    builder.Configuration.GetConnectionString("DefaultConnection"));
            });
            builder.Services.AddHangfireServer();
            
            var app = builder.Build();
            using (var scope = app.Services.CreateScope())
            {
                var services = scope.ServiceProvider;

                var context = services.GetRequiredService<HRDbContext>();
                await context.Database.MigrateAsync();
         
                await DataSeeder.SeedDataAsync(services);
            }
            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }
            app.UseHangfireDashboard();
            RecurringJob.AddOrUpdate<IMonthlyData>(
                "start-new-month-job",
                    service => service.NewMonthlyData(),
                    "0 0 1 * *" // CRON expression: ??? 1 ?? ?? ???
            );
            app.UseHttpsRedirection();
            

            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Auth}/{action=Index}/{id?}");
            app.Run();
        }
    }
}