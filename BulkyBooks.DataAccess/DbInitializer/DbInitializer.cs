using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BulkyBooks.Utility;
using BulkyBooks.Models;

namespace BulkyBooks.DataAccess.DbInitializer
{
    public class DbInitializer : IDbInitializer
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ApplicationDbContext _db;

        public DbInitializer(
           UserManager<IdentityUser> userManager,
           RoleManager<IdentityRole> roleManager,
           ApplicationDbContext db)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _db = db;

        }

        public void Initialize()
        {
            //apply migrations if they are not applied
            try
            {
                if (_db.Database.GetPendingMigrations().Count() > 0)
                {
                    _db.Database.Migrate();
                }
            }
            catch (Exception)
            {

                throw;
            }

            //create roles if they are not created
            if (!_roleManager.RoleExistsAsync(SD.Role_Admin).GetAwaiter().GetResult())
            {
                _roleManager.CreateAsync(new IdentityRole(SD.Role_Admin)).GetAwaiter().GetResult();
                _roleManager.CreateAsync(new IdentityRole(SD.Role_User_Indi)).GetAwaiter().GetResult();
                _roleManager.CreateAsync(new IdentityRole(SD.Role_Emoloyee)).GetAwaiter().GetResult();
                _roleManager.CreateAsync(new IdentityRole(SD.Role_User_Comp)).GetAwaiter().GetResult();


                //if roles are not created THEN we WILL CREATE ADMIN USER AS WELL
                _userManager.CreateAsync(new ApplicationUser
                {
                    UserName = "Admin@mail.ru",
                    Email = "Admin@mail.ru",
                    Name = "Bhrugen Patel",
                    PhoneNumber = "1112223333",
                    StreetAdress = "test 123 Ave",
                    State = "IL",
                    PostalCode = "23422",
                    City = "Chicago"
                }, "Adbvcnh21354*").GetAwaiter().GetResult();

                //getting user from db
                ApplicationUser user = _db.ApplicationUsers.FirstOrDefault(x => x.Email == "Admin@mail.ru");

                //GIVING THIS USER ROLE ADMIN
                _userManager.AddToRoleAsync(user,SD.Role_Admin).GetAwaiter().GetResult();
            }
            return;
        }
    }
}
