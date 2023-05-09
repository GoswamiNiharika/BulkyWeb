using BulkyWeb.DataAccess.Data;
using BulkyWeb.DataAccess.Repository.IRepository;
using BulkyWeb.Models;
using BulkyWeb.Models.ViewModels;
using BulkyWeb.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
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
        private readonly UserManager<IdentityUser> _userManager;
        public UserController(ApplicationDbContext dbContext, UserManager<IdentityUser> userManager)
        {
            _dbContext = dbContext;
            _userManager = userManager;
        }
        public IActionResult Index()
        { 
            return View();
        }        

        public IActionResult RoleManagement(string userId)
        {
            string roleId = _dbContext.UserRoles.FirstOrDefault(u => u.UserId == userId).RoleId;

            RoleManagementViewModel roleManagementViewModel = new RoleManagementViewModel()
            {
                ApplicationUser = _dbContext.ApplicationUsers.Include(c => c.Company).FirstOrDefault(u => u.Id == userId),
                RoleList = _dbContext.Roles.Select(i => new SelectListItem
                {
                    Text = i.Name,
                    Value = i.Name
                }),
                CompanyList = _dbContext.Companies.Select(i => new SelectListItem
                {
                    Text = i.Name,
                    Value = i.Id.ToString()
                })
            };
            roleManagementViewModel.ApplicationUser.Role = _dbContext.Roles.FirstOrDefault(u => u.Id == roleId).Name;
            return View(roleManagementViewModel);
        }

        [HttpPost]
        public IActionResult RoleManagement(RoleManagementViewModel roleManagementViewModel)
        {
            string roleId = _dbContext.UserRoles.FirstOrDefault(u => u.UserId == roleManagementViewModel.ApplicationUser.Id).RoleId;
            var oldRole = _dbContext.Roles.FirstOrDefault(u => u.Id == roleId).Name;

            if (!(roleManagementViewModel.ApplicationUser.Role == oldRole))
            {
                ApplicationUser applicationUser = _dbContext.ApplicationUsers.FirstOrDefault(u => u.Id == roleManagementViewModel.ApplicationUser.Id);
                if (roleManagementViewModel.ApplicationUser.Role == SD.Role_Company)
                {
                    applicationUser.CompanyId = roleManagementViewModel.ApplicationUser.CompanyId;
                }
                if (oldRole == SD.Role_Company)
                {
                    applicationUser.CompanyId = null;
                }
                _dbContext.SaveChanges();

                _userManager.RemoveFromRoleAsync(applicationUser, oldRole).GetAwaiter().GetResult();
                _userManager.AddToRoleAsync(applicationUser, roleManagementViewModel.ApplicationUser.Role).GetAwaiter().GetResult();
            }
            return RedirectToAction("Index");
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
            return Json(new { success = true, message = "Operation Successful" });
        }
        #endregion
    }
}
