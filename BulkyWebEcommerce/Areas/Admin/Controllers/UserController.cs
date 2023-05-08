using BulkyWeb.DataAccess.Data;
using BulkyWeb.DataAccess.Repository.IRepository;
using BulkyWeb.Models;
using BulkyWeb.Models.ViewModels;
using BulkyWeb.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace BulkyWeb.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = SD.Role_Admin)]
    public class UserController : Controller
    {
        private readonly ApplicationDbContext _dbContext;
        public UserController(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }
        public IActionResult Index()
        { 
            return View();
        }        

        #region API Calls
        [HttpGet]
        public IActionResult GetAll()
        {
            List<ApplicationUser> users = _dbContext.ApplicationUsers.Include(u => u.Company).ToList();

            var userRoles = _dbContext.UserRoles.ToList();
            var roles = _dbContext.Roles.ToList();
            foreach (var user in users)
            {
                var roleId = userRoles.FirstOrDefault(u => u.UserId == user.Id).RoleId;
                user.Role = roles.FirstOrDefault(r => r.Id == roleId).Name;
                if(user.Company == null)
                {
                    user.Company = new()
                    {
                        Name = ""
                    };
                }
            }
            return Json(new { data = users });
        }

        [HttpPost]
        public IActionResult LockUnlock([FromBody]string id)
        {
            var users = _dbContext.ApplicationUsers.FirstOrDefault(u => u.Id == id);
            if(users == null)
            {
                return Json(new { success = false, message = "Error while Locking/Unlocking" });
            }

            if(users.LockoutEnd != null && users.LockoutEnd > DateTime.Now)
            {
                users.LockoutEnd = DateTime.Now;
            }
            else
            {
                users.LockoutEnd = DateTime.Now.AddYears(1000);
            }
            _dbContext.SaveChanges();
            return Json(new { success = true, message = "Delete Successful" });
        }
        #endregion
    }
}
