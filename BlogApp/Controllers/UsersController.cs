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
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await _userManager.FindByEmailAsync(model.Email!);
                if (user != null)
                {
                    if (!await _userManager.IsEmailConfirmedAsync(user))
                    {
                        ModelState.AddModelError("", "Please confirm your email before you can log in.");
                        return View(model);
                    }

                    var result = await _signInManager.PasswordSignInAsync(user, model.Password!, model.RememberMe, lockoutOnFailure: false);
                    if (result.Succeeded)
                    {
                        return RedirectToAction("Index", "Post");
                    }
                }
                ModelState.AddModelError("", "Invalid email or password.");
            }
            return View(model);
        }
        public IActionResult Register()
        {
            return View();
        }


        [HttpGet]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Login");
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = new User
                {
                    UserName = model.UserName,
                    Email = model.Email,
                    Name = model.Name,
                    Image = "default-avatar.jpg"
                };

                var result = await _userManager.CreateAsync(user, model.Password!);
                if (result.Succeeded)
                {
                    var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                    var callbackUrl = Url.Action("ConfirmEmail", "Users", new { userId = user.Id, token = token }, protocol: Request.Scheme);
                    var emailBody = $@"
    <div style='font-family: Arial, sans-serif; font-size: 16px; color: #333;'>
        <h2 style='color: #7f143f;'>Welcome to BlogApp!</h2>
        <p>Thank you for registering. Please confirm your account to get started.</p>
        <p>Click the button below to verify your email address:</p>
        <a href='{callbackUrl}' style='display: inline-block; padding: 12px 24px; font-size: 16px; color: #fff; background-color: #7f143f; text-decoration: none; border-radius: 5px;'>Confirm Account</a>
        <p style='margin-top: 20px; font-size: 12px; color: #888;'>
            If you did not create this account, you can safely ignore this email.
        </p>
    </div>";
                    await _emailSender.SendEmailAsync(
      model.Email!,
      "Confirm Your Email",
      emailBody);

                    return RedirectToAction("RegisterConfirmation");
                }
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError("", error.Description);
                }
            }
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> ConfirmEmail(string? userId, string? token)
        {
            if (userId == null || token == null)
            {
                TempData["message"] = "Invalid token or user ID.";
                return View("Error");
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                TempData["message"] = "User not found.";
                return View("Error");
            }

            var result = await _userManager.ConfirmEmailAsync(user, token);
            if (result.Succeeded)
            {
                TempData["message"] = "Your account has been successfully confirmed. You can now log in.";
                return RedirectToAction("Login");
            }

            TempData["message"] = "There was an error confirming your email.";
            return View("Error");
        }
        [HttpGet]
        public IActionResult RegisterConfirmation()
        {
            return View();
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

        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View();
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null || !(await _userManager.IsEmailConfirmedAsync(user)))
            {
                return RedirectToAction("ForgotPasswordConfirmation");
            }

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var callbackUrl = Url.Action("ResetPassword", "Users", new { userId = user.Id, token = token }, protocol: Request.Scheme);

            var emailBody = $@"
    <div style='font-family: Arial, sans-serif; font-size: 16px; color: #333;'>
        <h2 style='color: #7f143f;'>Reset Your Password</h2>
        <p>We received a request to reset the password for your account.</p>
        <p>Click the button below to choose a new password:</p>
        <a href='{callbackUrl}' style='display: inline-block; padding: 12px 24px; font-size: 16px; color: #fff; background-color: #7f143f; text-decoration: none; border-radius: 5px;'>Reset Password</a>
        <p style='margin-top: 20px; font-size: 12px; color: #888;'>
            If you did not request a password reset, you can safely ignore this email. This link is valid for a limited time.
        </p>
    </div>";

            await _emailSender.SendEmailAsync(
                model.Email,
                "Reset Your Password",
                emailBody);

            return RedirectToAction("ForgotPasswordConfirmation");
        }
        [HttpGet]
        public IActionResult ForgotPasswordConfirmation()
        {
            return View();
        }


        [HttpGet]
        public async Task<IActionResult> ResetPassword(string? userId = null, string? token = null)
        {
            if (userId == null || token == null)
            {
                ModelState.AddModelError("", "Invalid password reset token.");
                return View("Error");
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound();
            }

            var model = new ResetPasswordViewModel
            {
                Token = token,
                Email = user.Email ?? ""
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                return RedirectToAction("ResetPasswordConfirmation");
            }

            var result = await _userManager.ResetPasswordAsync(user, model.Token, model.Password);
            if (result.Succeeded)
            {
                return RedirectToAction("ResetPasswordConfirmation");
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError("", error.Description);
            }
            return View(model);
        }

        [HttpGet]
        public IActionResult ResetPasswordConfirmation()
        {
            return View();
        }
    }

}