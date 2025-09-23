using BlogApp.Data.Abstract;
using BlogApp.Data.Concrete.EfCore;
using BlogApp.Entity;
using BlogApp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace BlogApp.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly UserManager<User> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly BlogContext _context;
        private readonly INotificationService _notificationService;
        private readonly ILogger<AdminController> _logger;

        public AdminController(
            UserManager<User> userManager,
            RoleManager<IdentityRole> roleManager,
            BlogContext context,
            INotificationService notificationService,
            ILogger<AdminController> logger)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _context = context;
            _notificationService = notificationService;
            _logger = logger;
        }

        public async Task<IActionResult> UserList()
        {
            _logger.LogInformation("UserList eylemi çağrıldı.");
            var users = await _userManager.Users.ToListAsync();
            if (users == null || !users.Any())
            {
                _logger.LogWarning("Hiç kullanıcı bulunamadı.");
            }
            return View(users);
        }

        [HttpGet]
        public async Task<IActionResult> EditRoles(string id)
        {
            _logger.LogInformation($"EditRoles çağrıldı. Kullanıcı ID: {id}");
            if (string.IsNullOrEmpty(id))
            {
                _logger.LogWarning("Kullanıcı ID boş veya null.");
                return NotFound();
            }

            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                _logger.LogWarning($"Kullanıcı bulunamadı. ID: {id}");
                return NotFound();
            }

            var userRoles = await _userManager.GetRolesAsync(user);
            var allRoles = await _roleManager.Roles.Select(r => r.Name).Where(r => r != null).ToListAsync();
            var model = new RoleManagementViewModel
            {
                UserId = user.Id,
                UserName = user.UserName ?? "",
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
            _logger.LogInformation($"EditRoles POST çağrıldı. Kullanıcı ID: {model.UserId}");
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("ModelState geçersiz.");
                return View(model);
            }

            var user = await _userManager.FindByIdAsync(model.UserId);
            if (user == null)
            {
                _logger.LogWarning($"Kullanıcı bulunamadı. ID: {model.UserId}");
                return NotFound();
            }

            var userRoles = await _userManager.GetRolesAsync(user);
            foreach (var role in model.Roles)
            {
                if (string.IsNullOrEmpty(role.RoleName))
                {
                    _logger.LogWarning($"Rol adı boş. Atlanıyor.");
                    continue;
                }
                if (role.IsSelected && !userRoles.Contains(role.RoleName))
                {
                    await _userManager.AddToRoleAsync(user, role.RoleName);
                    _logger.LogInformation($"Kullanıcıya rol eklendi: {role.RoleName}");
                }
                else if (!role.IsSelected && userRoles.Contains(role.RoleName))
                {
                    await _userManager.RemoveFromRoleAsync(user, role.RoleName);
                    _logger.LogInformation($"Kullanıcıdan rol kaldırıldı: {role.RoleName}");
                }
            }

            return RedirectToAction("UserList");
        }

        [HttpGet]
        public async Task<IActionResult> EditPostStatus(string id)
        {
            _logger.LogInformation($"EditPostStatus çağrıldı. Tam URL: {Request.Path + Request.QueryString}, Parametre URL: {id}");
            if (string.IsNullOrEmpty(id))
            {
                _logger.LogWarning("URL boş veya null.");
                return NotFound();
            }

            id = id.Trim().ToLower();
            var post = await _context.Posts
                .Include(p => p.User)
                .FirstOrDefaultAsync(p => p.Url == id);

            if (post == null)
            {
                _logger.LogWarning($"Gönderi bulunamadı. URL: {id}");
                var allPosts = await _context.Posts.Select(p => new { p.PostId, p.Title, p.Url }).ToListAsync();
                _logger.LogInformation($"Mevcut Post'lar: {string.Join(", ", allPosts.Select(p => p.Url))}");
                return NotFound();
            }

            _logger.LogInformation($"Gönderi bulundu: {post.Title}, ID: {post.PostId}, UserId: {post.UserId}");
            return View(post);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditPostStatus(int PostId, bool IsActive)
        {
            _logger.LogInformation($"EditPostStatus POST çağrıldı. PostId: {PostId}, IsActive: {IsActive}");
            var post = await _context.Posts
                .Include(p => p.User)
                .FirstOrDefaultAsync(p => p.PostId == PostId);

            if (post == null)
            {
                _logger.LogWarning($"Gönderi bulunamadı. PostId: {PostId}");
                return NotFound();
            }

            post.IsActive = IsActive;
            _context.Update(post);
            await _context.SaveChangesAsync();
            _logger.LogInformation($"Gönderi durumu güncellendi: {post.Title}, IsActive: {IsActive}");

            if (IsActive)
            {
                var adminUser = await _userManager.GetUserAsync(User);
                var message = $"Your post \"{post.Title}\" has been approved and published by an admin.";
                var link = $"/posts/details/{post.Url}";
                await _notificationService.CreateNotificationAsync(post.UserId, message, link);
                _logger.LogInformation($"Bildirim gönderildi. Kullanıcı: {post.UserId}, Gönderi: {post.Title}");
            }

            return RedirectToAction("List", "Post");
        }

        [HttpGet]
        public async Task<IActionResult> AddTestPost()
        {
            _logger.LogInformation("AddTestPost çağrıldı.");
            var currentUserName = User.Identity?.Name ?? "";
            var adminUser = await _userManager.Users.FirstOrDefaultAsync(u => u.UserName != currentUserName);
            if (adminUser == null)
            {
                _logger.LogWarning("Test gönderisi için uygun bir kullanıcı bulunamadı.");
                return BadRequest("Test gönderisi eklemek için başka bir kullanıcıya ihtiyaç var.");
            }

            var newPost = new Post
            {
                Title = "Test Gönderisi",
                Url = "test-post",
                Content = "Bu bir test gönderisidir.",
                IsActive = true,
                PublishedOn = DateTime.Now,
                UserId = adminUser.Id
            };
            _context.Posts.Add(newPost);
            await _context.SaveChangesAsync();
            _logger.LogInformation($"Test gönderisi eklendi: {newPost.Title}, URL: {newPost.Url}");
            return Ok("Test gönderisi eklendi. URL: /Admin/EditPostStatus/test-post");
        }
    }
}