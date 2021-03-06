using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using WhatsOnTap.Models;
using WhatsOnTap.ViewModels;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;
using System.Collections.Generic;

namespace WhatsOnTap.Controllers
{
    public class UserController : Controller
    {
        private readonly WhatsOnTapContext _db;
        private readonly UserManager<ApplicationUser> _userManager;

        public UserController(UserManager<ApplicationUser> userManager, WhatsOnTapContext db)
        {
            _userManager = userManager;
            _db = db;
        }

        [Authorize(Roles="user, owner")]
        [HttpGet("/user/beers")]
        public IActionResult Beers()
        {
            var userId = this.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var matches = _db.UsersBeers.Where(entry => entry.UserId == userId).ToList();
            List<Beer> favorites = new List<Beer>();
            foreach (var entry in matches)
            {
                var favoriteBeer = _db.Beers.FirstOrDefault(e => e.BeerId == entry.BeerId);
                favorites.Add(favoriteBeer);
            }
            return View(favorites);
        }

        [HttpPost("/user/beers")]
        public async Task<IActionResult> AddBeer(int beerId)
        {
            var userId = this.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var currentUser = await _userManager.FindByIdAsync(userId);
            UserBeer userBeer = new UserBeer(userId.ToString(), beerId);
            userBeer.User = currentUser;
            _db.UsersBeers.Add(userBeer);
            _db.SaveChanges();
            return View("Details");
        }

        [HttpPost("/user/beers/{id}/delete")]
        public IActionResult DeleteFromJoinTable(int id)
        {
            var userId = this.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            UserBeer foundBeer = _db.UsersBeers.Where(entry => entry.BeerId == id).Where(entry => entry.UserId == userId).ToList()[0];
            _db.UsersBeers.Remove(foundBeer);
            _db.SaveChanges();
            return RedirectToAction("Beers");
        }

        [Authorize(Roles="user, admin, owner")]
        [HttpGet("/user/profile")]
        public async Task<IActionResult> Details()
        {
            var userId = this.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var currentUser = await _userManager.FindByIdAsync(userId);
            ProfileViewModel newViewModel = new ProfileViewModel(currentUser.Email);
            return View(newViewModel);
        }

        [HttpPost("/user/profile")]
        public async Task<IActionResult> Details(ProfileViewModel model)
        {
            var userId = this.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var currentUser = await _userManager.FindByIdAsync(userId);
            if (model.CurrentPassword != null)
            {
                var changePassword = await _userManager.ChangePasswordAsync(currentUser, model.CurrentPassword, model.NewPassword);
            }
            currentUser.Email = model.Email;
            currentUser.UserName = model.Email;
            await _userManager.UpdateAsync(currentUser);
            return View("Details");
        }
    }
}