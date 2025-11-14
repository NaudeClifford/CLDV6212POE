using Microsoft.AspNetCore.Identity;
using ABCRetails.Models;

namespace ABCRetails.Data
{
    public static class UserSeeder
    {
        public static async Task SeedAsync(IServiceProvider services)
        {
            using var scope = services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();

            string[] roles = { "Admin", "Customer" };

            //Seed roles
            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                    await roleManager.CreateAsync(new IdentityRole(role));
            }

            // ADMIN
            var adminEmail = "manager@system.local";
            var adminUser = await userManager.FindByEmailAsync(adminEmail);

            if (adminUser == null)
            {
                adminUser = new User
                {
                    UserName = "manager",          
                    Email = adminEmail,             
                    EmailConfirmed = true,
                    FirstName = "Manager",
                    LastName = "One"
                };

                var result = await userManager.CreateAsync(adminUser, "Manager#12345");

                if (!result.Succeeded)
                    throw new Exception(string.Join(", ", result.Errors.Select(e => e.Description)));

                await userManager.AddToRoleAsync(adminUser, "Admin");

                context.Admins.Add(new Admin
                {
                    UserId = adminUser.Id,
                    Username = adminUser.UserName,
                    Email = adminUser.Email,
                    Name = adminUser.FirstName,
                    Surname = adminUser.LastName,
                    Role = "Admin"
                });
            }

            //CUSTOMER
            var customerEmail = "customer@system.local";
            var customerUser = await userManager.FindByEmailAsync(customerEmail);

            if (customerUser == null)
            {
                customerUser = new User
                {
                    UserName = "customer",      
                    Email = customerEmail,          
                    EmailConfirmed = true,
                    FirstName = "Customer",
                    LastName = "One"
                };

                var result = await userManager.CreateAsync(customerUser, "Customer#12345");

                if (!result.Succeeded)
                    throw new Exception(string.Join(", ", result.Errors.Select(e => e.Description)));

                await userManager.AddToRoleAsync(customerUser, "Customer");

                context.Customers.Add(new Customer
                {
                    UserId = customerUser.Id,
                    Username = customerUser.UserName,
                    Email = customerUser.Email,
                    Name = customerUser.FirstName,
                    Surname = customerUser.LastName,
                    ShippingAddress = "14 Pik Street, Cape Town"
                });
            }

            await context.SaveChangesAsync();
        }
    }
}
