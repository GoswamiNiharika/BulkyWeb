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
        private readonly ApplicationDbContext  _dbContext;
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
            return Json(new { data = users });
        }

        [HttpDelete]
        public IActionResult Delete(int? id)
        {            
            return Json(new { success = true, message = "Delete Successful" });
        }
        #endregion
    }
}
