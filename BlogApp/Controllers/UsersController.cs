using System.Security.Claims;
using BlogApp.Data.Abstract;
using BlogApp.Data.Concrete.EfCore;
using BlogApp.Entity;
using BlogApp.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BlogApp.Controllers
{

    public class UsersController : Controller
    {
        private readonly BlogContext _context;
        private readonly UserManager<User> _userManager;
        private readonly SignInManager<User> _signInManager;
        private readonly IEmailSender _emailSender;
        private readonly INotificationService _notificationService;
        public UsersController(UserManager<User> userManager, SignInManager<User> signInManager, IEmailSender emailSender, BlogContext context, INotificationService notificationService)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _emailSender = emailSender;
            _context = context;
            _notificationService = notificationService;
        }

        public IActionResult Login()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Index", "Post");
            }
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await _userManager.FindByEmailAsync(model.Email!);

                if (user == null)
                {
                    ModelState.AddModelError("", "Invalid email or password");
                    return View(model);
                }

                var result = await _signInManager.PasswordSignInAsync(user, model.Password!, isPersistent: true, lockoutOnFailure: false);

                if (result.Succeeded)
                {
                    return RedirectToAction("Index", "Post");
                }

                ModelState.AddModelError("", "Invalid email or password");
            }
            return View(model);
        }

        public IActionResult Register()
        {
            return View();
        }
        [HttpPost]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = new User
                {
                    UserName = model.UserName,
                    Email = model.Email,
                    Name = model.Name,
                    Image = "2.jpg"
                };

                var result = await _userManager.CreateAsync(user, model.Password!);
                if (result.Succeeded)
                {
                    var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                    var confirmationLink = Url.Action("ConfirmEmail", "Users", new { userId = user.Id, token }, Request.Scheme);

                    if (model.Email != null && confirmationLink != null)
                    {
                        await _emailSender.SendEmailAsync(
                            model.Email,
                            "Verify Your Email Address",
                           $"Please <a href='{confirmationLink}'>click here</a> to verify your email address.");
                    }

                    await _signInManager.SignInAsync(user, isPersistent: false);
                    return RedirectToAction("Index", "Post");
                }
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError("", error.Description);
                }
            }
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Login");
        }
        [HttpGet]
        public IActionResult ConfirmEmail(string userId, string token)
        {
            if (userId == null || token == null)
            {
                return RedirectToAction("Index", "Home");
            }

            var user = _userManager.FindByIdAsync(userId).Result;
            if (user == null)
            {
                ViewBag.ErrorMessage = "User not found.";
                return View("Error");
            }

            var result = _userManager.ConfirmEmailAsync(user, token).Result;
            if (result.Succeeded)
            {
                return View("ConfirmEmail");
            }

            ViewBag.ErrorMessage = "Email verification failed.";
            return View("Error");
        }
        [HttpGet]
        public IActionResult Profile(string username)
        {
            if (string.IsNullOrEmpty(username))
            {
                return NotFound();
            }
            var user = _userManager.Users
           .Include(u => u.Posts)
           .Include(u => u.Comments)
           .ThenInclude(c => c.Post)
           .Include(u => u.Followers)
           .Include(u => u.Following)
           .FirstOrDefault(x => x.UserName == username);
            if (user == null)
            {
                return NotFound();
            }
            return View(user);
        }
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> EditProfile()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return NotFound();
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound();
            }

            var model = new ProfileEditViewModel
            {
                UserId = user.Id,
                UserName = user.UserName,
                Name = user.Name,
                Email = user.Email,
                Image = user.Image
            };

            return View(model);
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> EditProfile(ProfileEditViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = await _userManager.FindByIdAsync(userId!);

            if (user == null)
            {
                return NotFound();
            }

            if (user.UserName != model.UserName)
            {
                var existingUser = await _userManager.FindByNameAsync(model.UserName!);
                if (existingUser != null)
                {
                    ModelState.AddModelError("UserName", "This username is already taken.");
                    return View(model);
                }
                await _userManager.SetUserNameAsync(user, model.UserName);
            }

            user.Name = model.Name;
            user.Email = model.Email;

            if (model.ImageFile != null)
            {
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png" };
                var extension = Path.GetExtension(model.ImageFile.FileName).ToLowerInvariant();
                if (!allowedExtensions.Contains(extension))
                {
                    ModelState.AddModelError("ImageFile", "Invalid image format.");
                    return View(model);
                }

                var randomFileName = $"{Guid.NewGuid()}{extension}";
                var path = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/img", randomFileName);

                using (var stream = new FileStream(path, FileMode.Create))
                {
                    await model.ImageFile.CopyToAsync(stream);
                }
                user.Image = randomFileName;
            }

            var result = await _userManager.UpdateAsync(user);

            if (result.Succeeded)
            {
                await _signInManager.RefreshSignInAsync(user);
                return RedirectToAction("Profile", new { username = user.UserName });
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError("", error.Description);
            }

            return View(model);
        }
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Follow(string username)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var userToFollow = await _userManager.FindByNameAsync(username);
            var currentUser = await _userManager.FindByIdAsync(currentUserId!);

            if (userToFollow == null || currentUser == null || currentUserId == userToFollow.Id)
            {
                return Json(new { success = false, message = "Invalid request." });
            }

            var alreadyFollowing = await _context.Follows
                .AnyAsync(f => f.FollowerId == currentUserId && f.FollowingId == userToFollow.Id);

            if (alreadyFollowing)
            {
                return Json(new { success = false, message = "You are already following this user." });
            }

            var follow = new Follow { FollowerId = currentUserId, FollowingId = userToFollow.Id };
            _context.Follows.Add(follow);
            await _context.SaveChangesAsync();
            var message = $"{currentUser.Name ?? currentUser.UserName} started following you.";
            var link = $"/profile/{currentUser.UserName}";
            await _notificationService.CreateNotificationAsync(userToFollow.Id, message, link);

            var followerCount = await _context.Follows.CountAsync(f => f.FollowingId == userToFollow.Id);
            return Json(new { success = true, followerCount });
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Unfollow(string username)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var userToUnfollow = await _userManager.FindByNameAsync(username);

            if (userToUnfollow == null)
            {
                return Json(new { success = false, message = "User not found." });
            }

            var follow = await _context.Follows
                .FirstOrDefaultAsync(f => f.FollowerId == currentUserId && f.FollowingId == userToUnfollow.Id);

            if (follow == null)
            {
                return Json(new { success = false, message = "You are not following this user." });
            }

            _context.Follows.Remove(follow);
            await _context.SaveChangesAsync();

            var followerCount = await _context.Follows.CountAsync(f => f.FollowingId == userToUnfollow.Id);
            return Json(new { success = true, followerCount });
        }

    }

}