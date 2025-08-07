using BlogApp.Data.Abstract;
using BlogApp.Data.Concrete.EfCore;
using BlogApp.Entity;
using BlogApp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BlogApp.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly UserManager<User> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        private readonly BlogContext _context;
        private readonly INotificationService _notificationService;
        public AdminController(UserManager<User> userManager, RoleManager<IdentityRole> roleManager, BlogContext context, INotificationService notificationService)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _context = context;
            _notificationService = notificationService;
        }

        public async Task<IActionResult> UserList()
        {
            var users = await _userManager.Users.ToListAsync();
            return View(users);
        }
        [HttpGet]
        public async Task<IActionResult> EditRoles(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }
            var userRoles = await _userManager.GetRolesAsync(user);
            var allRoles = await _roleManager.Roles.Select(r => r.Name).ToListAsync();
            var model = new RoleManagementViewModel
            {
                UserId = user.Id,
                UserName = user.UserName,
                Roles = allRoles.Select(role => new SelectableRole
                {
                    RoleName = role,
                    IsSelected = userRoles.Contains(role)
                }).ToList()
            };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditRoles(RoleManagementViewModel model)
        {
            var user = await _userManager.FindByIdAsync(model.UserId);
            if (user == null)
            {
                return NotFound();
            }

            var userRoles = await _userManager.GetRolesAsync(user);

            foreach (var role in model.Roles)
            {
                if (role.IsSelected && !userRoles.Contains(role.RoleName))
                {
                    await _userManager.AddToRoleAsync(user, role.RoleName);
                }
                else if (!role.IsSelected && userRoles.Contains(role.RoleName))
                {
                    await _userManager.RemoveFromRoleAsync(user, role.RoleName);
                }
            }

            return RedirectToAction("UserList");
        }
        [HttpGet]
        public async Task<IActionResult> EditPostStatus(string url)
        {
            if (string.IsNullOrEmpty(url))
            {
                return NotFound();
            }

            var post = await _context.Posts.Include(p => p.User).FirstOrDefaultAsync(p => p.Url == url);
            if (post == null)
            {
                return NotFound();
            }
            return View(post);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditPostStatus(int PostId, bool IsActive)
        {
            var post = await _context.Posts.Include(p => p.User).FirstOrDefaultAsync(p => p.PostId == PostId);
            if (post == null)
            {
                return NotFound();
            }

            post.IsActive = IsActive;
            _context.Update(post);
            await _context.SaveChangesAsync();

            if (IsActive)
            {
                var adminUser = await _userManager.GetUserAsync(User);
                var message = $"Your post \"{post.Title}\" has been approved and published by an admin.";
                var link = $"/posts/details/{post.Url}";
                await _notificationService.CreateNotificationAsync(post.UserId, message, link);
            }

            return RedirectToAction("List", "Post");
        }
    }
}