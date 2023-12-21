using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TheBlogProject.Data;
using TheBlogProject.Enums;
using TheBlogProject.Models;

namespace TheBlogProject.Services
{
    public class DataService
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly UserManager<BlogUser> _userManager;

        public DataService(ApplicationDbContext dbContext, RoleManager<IdentityRole> roleManager, UserManager<BlogUser> userManager)
        {
            _dbContext = dbContext;
            _roleManager = roleManager;
            _userManager = userManager;
        }

        public async Task ManageDataAsync()
        {
            await _dbContext.Database.MigrateAsync();
            //Goal 1: Seed a few roles into the system
            await SeedRolesAsync();

            //Goal 2: Seed a few users into the system
            await SeedUsersAsync();

        }

        private async Task SeedRolesAsync()
        {
            //If there are already roles in the system, do nothing
            if (_dbContext.Roles.Any()) 
            {
                return;
            }

            //Otherwise create a few roles
            foreach(var role in Enum.GetNames(typeof(BlogRole)))
            {
               await _roleManager.CreateAsync(new IdentityRole(role));
            }

        }

        private async Task SeedUsersAsync()
        {
            //If there are already users in the system, do nothing
            if (_dbContext.Users.Any())
            {
                return;
            }

            //Create a new instance of BlogUser
            var adminUser = new BlogUser()
            {
                Email = "AdminTestAccount@test.com",
                UserName = "AdminTestAccount@test.com",
                FirstName = "Demo",
                LastName = "Admin",
                DisplayName="Demo Admin",
                PhoneNumber = "(800) 555-1212",
                EmailConfirmed = true
            };

            //Use UserManager to create a new user that is defined by adminUser
            await _userManager.CreateAsync(adminUser, "Abc&123!");

            //Add this new user to the Administrator role
            await _userManager.AddToRoleAsync(adminUser, BlogRole.Administrator.ToString());


            var modUser = new BlogUser()
            {
                Email = "ModTestAccount@test.com",
                UserName = "ModTestAccount@test.com",
                FirstName = "Demo",
                LastName = "Mod",
                DisplayName= "Demo Mod",
                PhoneNumber = "(800) 555-1213",
                EmailConfirmed = true
            };

            await _userManager.CreateAsync(modUser, "Abc&123!");

            await _userManager.AddToRoleAsync(modUser, BlogRole.Moderator.ToString());

            var demoUser = new BlogUser()
            {
                Email = "UserTestAccount@test.com",
                UserName = "UserTestAccount@test.com",
                FirstName = "Demo",
                LastName = "User",
                DisplayName = "Demo User",
                PhoneNumber = "(800) 555-1213",
                EmailConfirmed = true
            };

            await _userManager.CreateAsync(demoUser, "Abc&123!");

        }




    }
}
