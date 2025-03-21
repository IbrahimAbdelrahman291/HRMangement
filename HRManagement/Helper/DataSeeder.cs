using DAL.Context;
using DAL.Models;
using Microsoft.AspNetCore.Identity;

namespace HRManagement.Helper
{
    public static class DataSeeder
    {
        public static async Task SeedDataAsync(IServiceProvider serviceProvider)
        {
            var context = serviceProvider.GetRequiredService<HRDbContext>();
            var userManager = serviceProvider.GetRequiredService<UserManager<User>>();
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();

            string[] roles = { "Admin", "HR", "Employee", "Accountant", "Manager" };
            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole(role));
                }
            }

            var users = new[]
            {
            new { UserName = "Admin1", Password = "P@$$w0rd", Role = "Admin" },
        };

            foreach (var userData in users)
            {
                if (await userManager.FindByNameAsync(userData.UserName) == null)
                {
                    var user = new User { UserName = userData.UserName };
                    var result = await userManager.CreateAsync(user, userData.Password);
                    if (result.Succeeded)
                    {
                        await userManager.AddToRoleAsync(user, userData.Role);
                    }
                }
            }

            await context.SaveChangesAsync();
        }
    }
}

