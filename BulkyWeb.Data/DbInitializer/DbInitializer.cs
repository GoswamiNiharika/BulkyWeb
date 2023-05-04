using BulkyWeb.DataAccess.Data;
using BulkyWeb.Models;
using BulkyWeb.Utility;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BulkyWeb.DataAccess.DbInitializer
{
    public class DbInitializer : IDbInitializer
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ApplicationDbContext _applicationDbContext;
        public DbInitializer(UserManager<IdentityUser> userManager,
                            RoleManager<IdentityRole> roleManager,
                            ApplicationDbContext applicationDbContext) 
        {
            _roleManager = roleManager;
            _userManager = userManager;
            _applicationDbContext = applicationDbContext;
        }
        public void Initialize()
        {
            //migrations if they are not applied
            try
            {
                if(_applicationDbContext.Database.GetPendingMigrations().Count() > 0)
                {
                    _applicationDbContext.Database.Migrate();
                }
            }
            catch(Exception ex)
            {
            }

            //create roles if they are not created
            if (!_roleManager.RoleExistsAsync(SD.Role_Customer).GetAwaiter().GetResult())
            {
                _roleManager.CreateAsync(new IdentityRole(SD.Role_Customer)).GetAwaiter().GetResult();
                _roleManager.CreateAsync(new IdentityRole(SD.Role_Employee)).GetAwaiter().GetResult();
                _roleManager.CreateAsync(new IdentityRole(SD.Role_Admin)).GetAwaiter().GetResult();
                _roleManager.CreateAsync(new IdentityRole(SD.Role_Company)).GetAwaiter().GetResult();

                //if roles are not created, then we will create admin user as well
                _userManager.CreateAsync(new ApplicationUser
                {
                    UserName = "admin@gmail.com",
                    Email = "goswami.niharika@0103@gmail.com",
                    Name = "Niharika Goswami",
                    PhoneNumber = "1234567890",
                    StreetAddress = "Shanti-nagar",
                    City = "Valsad",
                    State = "Gujarat",
                    PostalCode = "12345"
                }, "Admin@1234").GetAwaiter().GetResult();

                ApplicationUser user = _applicationDbContext.ApplicationUsers.FirstOrDefault(u => u.Email == "goswami.niharika@0103@gmail.com");
                _userManager.AddToRoleAsync(user, SD.Role_Admin).GetAwaiter().GetResult();
            }
            return;
        }
    }
}
