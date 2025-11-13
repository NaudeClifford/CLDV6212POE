using Microsoft.AspNetCore.Identity;
using ABCRetails.Models;
using ABCRetails.Data;

namespace PROG6212POE.Data
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

            //Check if roles exist
            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    var result = await roleManager.CreateAsync(new IdentityRole(role));
                    if (!result.Succeeded)
                    {
                        var error = string.Join("; ", result.Errors.Select(e => $"{e.Code}: {e.Description}"));
                        throw new Exception($"Failed to create role '{role}': {error}");
                    }
                }
            }

            //Seed Admin
            var managerEmail = "manager@system.local";
            var managerUser = await userManager.FindByEmailAsync(managerEmail);
            
            if (managerUser == null)
            {
                managerUser = new User
                {
                    UserName = "manager",
                    Email = managerEmail,
                    EmailConfirmed = true,
                    FirstName = "Manager",
                    LastName = "One"
                };
                await userManager.CreateAsync(managerUser, "Manager#12345");
                await userManager.AddToRoleAsync(managerUser, "AcademicManager");

                context.Admins.Add(new Admin
                {
                    UserId = managerUser.Id,
                    User = managerUser,
                    Role = "Admin",
                    Username = managerUser.UserName,
                    Email = managerUser.Email,
                    Name = managerUser.FirstName,
                    Surname = managerUser.LastName
                });
            }
         
            //Seed Customer
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
                await userManager.CreateAsync(customerUser, "Customer#12345");
                await userManager.AddToRoleAsync(customerUser, "Customer");

                context.Customers.Add(new Customer
                {
                    Id = customerUser.Id,
                    User = customerUser,
                    Name = customerUser.FirstName,
                    Surname = customerUser.LastName,
                    Username = customerUser.UserName,
                    Email = customerUser.Email,
                    ShippingAddress = "14 Pik Street, Cape Town"
                });
            }

            await context.SaveChangesAsync();
        }
    }
}
