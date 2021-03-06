using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Pubstars2.Models;
using PubstarsDtos;

namespace Pubstars2.Controllers
{
    public class UserdataController : Controller
    {
        private UserManager<ApplicationUser> _userManager;

        public UserdataController(UserManager<ApplicationUser> usermanager)
        {
            _userManager = usermanager;
            
        }

        public IActionResult Index()
        {
            return View();
        }

        //for client
        [HttpGet]
        public IActionResult GetUserData()
        {
            Dictionary<string, UserData> userData = new Dictionary<string, UserData>();
           
            foreach (ApplicationUser user in _userManager.Users)
            {
                userData[user.UserName] = new UserData()
                {
                    Name = user.UserName,
                    Password = user.PubstarsPassword,
                    Rating = user.PlayerStats.RatingMean
                };                    
            }
            return Json(userData);
        }
    }
}