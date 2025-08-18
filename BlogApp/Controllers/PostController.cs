using System;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using BlogApp.Data.Abstract;
using BlogApp.Data.Concrete.EfCore;
using BlogApp.Entity;
using BlogApp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BlogApp.Controllers
{
    [Route("posts")]
    public class PostController : Controller
    {
        private readonly IPostRepository _postRepository;
        private readonly ICommentRepository _commentRepository;
        private readonly ITagRepository _tagRepository;
        private readonly ILogger<PostController> _logger;
        private readonly UserManager<User> _userManager;
        private readonly INotificationService _notificationService;
        private readonly BlogContext _context;

        public PostController(
            IPostRepository postRepository,
            ICommentRepository commentRepository,
            ITagRepository tagRepository,
            ILogger<PostController> logger,
            UserManager<User> userManager,
            INotificationService notificationService,
            BlogContext context)
        {
            _postRepository = postRepository;
            _commentRepository = commentRepository;
            _tagRepository = tagRepository;
            _logger = logger;
            _userManager = userManager;
            _notificationService = notificationService;
            _context = context;
        }

        // GET: /posts veya /posts/tag/{url}
        [HttpGet("")]
        [HttpGet("tag/{url}")]
        public async Task<IActionResult> Index(string? url, int page = 1)
        {
            const int postsPerPage = 4;
            var postsQuery = _postRepository.Posts
                .Include(p => p.Tags)
                .Include(p => p.User)
                .Where(p => p.IsActive)
                .AsQueryable();

            if (!string.IsNullOrEmpty(url))
            {
                postsQuery = postsQuery
                        .Where(p => p.Tags.Any(t => t.Url == url));
            }

            int totalPosts = await postsQuery.CountAsync();
            var posts = await postsQuery
                .OrderByDescending(p => p.PublishedOn)
                .Skip((page - 1) * postsPerPage)
                .Take(postsPerPage)
                .ToListAsync();

            var model = new PostsViewModel
            {
                Posts = posts,
                Tag = url,
                CurrentPage = page,
                TotalPages = (int)Math.Ceiling((double)totalPosts / postsPerPage)
            };

            ViewBag.MetaDescription = string.IsNullOrEmpty(url)
                ? "Teknoloji, yaşam tarzı ve daha fazlası hakkında en son yazıları MyBlog'da keşfedin."
                : $"'{url}' etiketli yazıları MyBlog'da keşfedin.";
            ViewBag.CanonicalUrl = string.IsNullOrEmpty(url)
                ? $"/posts?page={page}"
                : $"/posts/tag/{url}?page={page}";

            _logger.LogInformation($"Index action yüklendi. Toplam yazı: {totalPosts}, Sayfa: {page}, Etiket: {url}");
            return View(model);
        }

        // GET: /posts/details/{url}
        [HttpGet("details/{url}")]
        public async Task<IActionResult> Details(string? url)
        {
            if (string.IsNullOrEmpty(url))
            {
                return NotFound();
            }

            var post = await _postRepository.Posts
                .Include(p => p.User)
                .Include(p => p.Tags)
                .Include(p => p.Likes)
                .Include(p => p.Comments)
                .ThenInclude(c => c.User)
                .Include(p => p.Comments)
                .ThenInclude(c => c.Replies)
                .ThenInclude(r => r.User)
                .FirstOrDefaultAsync(p => p.IsActive && p.Url == url);

            if (post == null)
            {
                return NotFound();
            }

            var sessionKey = $"viewed_post_{post.PostId}";
            if (HttpContext.Session.GetString(sessionKey) == null)
            {
                post.ViewCount++;
                await _postRepository.UpdatePost(post);
                HttpContext.Session.SetString(sessionKey, "true");
            }

            if (post.User != null)
            {
                post.User.Image ??= "default-avatar.jpg";
            }
            if (post.Comments != null)
            {
                foreach (var comment in post.Comments)
                {
                    if (comment.User != null)
                    {
                        comment.User.Image ??= "default-avatar.jpg";
                    }
                }
            }

            return View(post);
        }

        // POST: /posts/addreply
        [HttpPost("addreply")]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<JsonResult> AddReply(int PostId, int ParentCommentId, string Text)
        {
            if (string.IsNullOrWhiteSpace(Text))
            {
                return Json(new { success = false, message = "Yanıt metni boş olamaz." });
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Json(new { success = false, message = "Kullanıcı doğrulanamadı." });
            }

            var currentUser = await _userManager.FindByIdAsync(userId);
            if (currentUser == null)
            {
                return Json(new { success = false, message = "Kullanıcı bulunamadı." });
            }

            var post = await _postRepository.Posts.FirstOrDefaultAsync(p => p.PostId == PostId && p.IsActive);
            if (post == null)
            {
                return Json(new { success = false, message = "Yazı bulunamadı." });
            }

            var parentComment = await _commentRepository.GetCommentById(ParentCommentId);
            if (parentComment == null)
            {
                return Json(new { success = false, message = "Ana yorum bulunamadı." });
            }

            var reply = new Comment
            {
                PostId = PostId,
                Text = Text,
                PublishedOn = DateTime.Now,
                UserId = userId,
                ParentCommentId = ParentCommentId
            };

            await _commentRepository.CreateComment(reply);

            if (parentComment.UserId != userId)
            {
                var message = $"{currentUser.Name ?? currentUser.UserName}, \"{post.Title}\" yazınızdaki yorumunuza yanıt verdi.";
                var link = $"/posts/details/{post.Url}#comment-{ParentCommentId}";
                await _notificationService.CreateNotificationAsync(parentComment.UserId, message, link);
            }

            return Json(new
            {
                success = true,
                replyId = reply.CommentId,
                text = reply.Text,
                publishedOn = reply.PublishedOn.ToString("o"),
                name = currentUser.Name ?? currentUser.UserName,
                username = currentUser.UserName,
                avatar = currentUser.Image ?? "default-avatar.jpg"
            });
        }

        // POST: /posts/addcomment
        [HttpPost("addcomment")]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<JsonResult> AddComment(int PostId, string? Text)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId) || string.IsNullOrWhiteSpace(Text))
            {
                return Json(new { success = false, message = "Yorum metni boş olamaz veya kullanıcı doğrulanamadı." });
            }

            var currentUser = await _userManager.FindByIdAsync(userId);
            if (currentUser == null)
            {
                return Json(new { success = false, message = "Kullanıcı bulunamadı." });
            }

            var post = await _postRepository.Posts.FirstOrDefaultAsync(p => p.PostId == PostId);
            if (post == null)
            {
                return Json(new { success = false, message = "Yazı bulunamadı." });
            }

            var comment = new Comment
            {
                PostId = PostId,
                Text = Text,
                PublishedOn = DateTime.Now,
                UserId = userId
            };
            await _commentRepository.CreateComment(comment);

            if (post.UserId != userId)
            {
                var message = $"{currentUser.Name ?? currentUser.UserName}, \"{post.Title}\" yazınıza yorum yaptı.";
                var link = $"/posts/details/{post.Url}";
                await _notificationService.CreateNotificationAsync(post.UserId, message, link);
            }

            return Json(new
            {
                success = true,
                username = currentUser.UserName,
                name = currentUser.Name,
                text = comment.Text,
                publishedOn = comment.PublishedOn.ToString("MMMM dd, yyyy HH:mm"),
                avatar = currentUser.Image ?? "default-avatar.jpg"
            });
        }

        // GET: /posts/create
        [HttpGet("create")]
        [Authorize]
        public async Task<IActionResult> Create()
        {
            var tags = await _tagRepository.Tags.ToListAsync();
            ViewBag.Tags = tags ?? new List<Tag>();
            _logger.LogInformation($"Create görünümü yüklendi. Etiket sayısı: {(tags?.Count ?? 0)}");
            return View(new PostCreateViewModel());
        }

        // POST: /posts/create
        [HttpPost("create")]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(PostCreateViewModel model, IFormFile? imageFile)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Tags = await _tagRepository.Tags.ToListAsync();
                return View(model);
            }

            var newPost = new Post
            {
                Title = model.Title,
                Description = model.Description,
                Content = model.Content,
                Url = model.Url,
                PublishedOn = DateTime.Now,
                IsActive = model.IsActive,
                UserId = User.FindFirstValue(ClaimTypes.NameIdentifier)
            };

            if (imageFile != null)
            {
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png" };
                var extension = Path.GetExtension(imageFile.FileName).ToLowerInvariant();
                if (!allowedExtensions.Contains(extension))
                {
                    ModelState.AddModelError("", "Geçersiz resim formatı. Yalnızca JPG, JPEG ve PNG desteklenir.");
                    ViewBag.Tags = await _tagRepository.Tags.ToListAsync();
                    return View(model);
                }

                var randomFileName = $"{Guid.NewGuid()}{extension}";
                var path = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/img", randomFileName);

                using (var stream = new FileStream(path, FileMode.Create))
                {
                    await imageFile.CopyToAsync(stream);
                }
                newPost.Image = randomFileName;
            }

            var selectedTagIds = model.SelectedTagIds?.Select(int.Parse).ToArray() ?? Array.Empty<int>();
            await _postRepository.CreatePost(newPost, selectedTagIds);

            var admins = await _userManager.GetUsersInRoleAsync("Admin");
            var postCreator = await _userManager.FindByIdAsync(newPost.UserId);
            foreach (var admin in admins)
            {
                if (admin.Id != newPost.UserId)
                {
                    var message = $"{postCreator?.Name ?? "Bir kullanıcı"}, \"{newPost.Title}\" başlıklı yeni bir yazı oluşturdu.";
                    var link = "/posts/list";
                    await _notificationService.CreateNotificationAsync(admin.Id, message, link);
                }
            }

            TempData["message"] = "Yazı başarıyla oluşturuldu.";
            return RedirectToAction("List");
        }

        // GET: /posts/list
        [HttpGet("list")]
        [Authorize]
        public async Task<IActionResult> List(string? search, string? status, int page = 1)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("List action başarısız: Kullanıcı ID'si null veya geçersiz.");
                return Unauthorized();
            }

            const int postsPerPage = 6;
            var postsQuery = _postRepository.Posts
                .Include(p => p.Tags)
                .Include(p => p.User)
                .AsQueryable();

            if (!User.IsInRole("Admin"))
            {
                postsQuery = postsQuery.Where(p => p.UserId == userId);
            }

            if (!string.IsNullOrEmpty(search))
            {
                search = search.ToLower();
                postsQuery = postsQuery.Where(p => (p.Title != null && p.Title.ToLower().Contains(search)) ||
                                                  (p.Description != null && p.Description.ToLower().Contains(search)));
            }

            if (!string.IsNullOrEmpty(status))
            {
                bool isActive = status.ToLower() == "active";
                postsQuery = postsQuery.Where(p => p.IsActive == isActive);
            }

            int totalPosts = await postsQuery.CountAsync();
            var posts = await postsQuery
                .OrderByDescending(p => p.PublishedOn)
                .Skip((page - 1) * postsPerPage)
                .Take(postsPerPage)
                .ToListAsync();

            foreach (var post in posts)
            {
                if (post.User != null)
                {
                    post.User.Image ??= "default-avatar.jpg";
                }
            }

            var model = new PostsViewModel
            {
                Posts = posts,
                CurrentPage = page,
                TotalPages = (int)Math.Ceiling((double)totalPosts / postsPerPage)
            };

            ViewBag.Search = search;
            ViewBag.Status = status;

            _logger.LogInformation($"List action yüklendi. Kullanıcı ID: {userId}, Arama: {search}, Durum: {status}, Toplam yazı: {totalPosts}");
            return View(model);
        }

        // GET: /posts/edit/{url}
        [HttpGet("edit/{url}")]
        [Authorize]
        public async Task<IActionResult> Edit(string? url)
        {
            if (string.IsNullOrEmpty(url))
            {
                _logger.LogWarning("Edit GET action başarısız: URL null veya boş.");
                return NotFound();
            }

            var post = await _postRepository.Posts
                .Include(p => p.Tags)
                .Include(p => p.User)
                .FirstOrDefaultAsync(p => p.Url == url);

            if (post == null)
            {
                _logger.LogWarning($"Yazı bulunamadı. URL: {url}");
                return NotFound();
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId) || (post.UserId != userId && !User.IsInRole("Admin")))
            {
                _logger.LogWarning($"Yetkisiz erişim: URL: {url}, Kullanıcı ID: {userId}");
                return Unauthorized();
            }

            if (post.User != null)
            {
                post.User.Image ??= "default-avatar-icon.jpg";
            }

            var tags = await _tagRepository.Tags.ToListAsync();
            ViewBag.Tags = tags ?? new List<Tag>();
            var model = new PostCreateViewModel
            {
                PostId = post.PostId,
                Title = post.Title,
                Description = post.Description,
                Content = post.Content,
                Url = post.Url,
                Image = post.Image,
                IsActive = post.IsActive,
                SelectedTagIds = post.Tags?.Select(t => t.TagId.ToString()).ToArray() ?? Array.Empty<string>()
            };

            _logger.LogInformation($"Edit görünümü yüklendi. Yazı URL: {url}, Yazı ID: {post.PostId}, Kullanıcı ID: {userId}, Seçili Etiketler: {string.Join(",", model.SelectedTagIds)}, Etiket sayısı: {(tags?.Count ?? 0)}");
            return View(model);
        }

        // POST: /posts/edit
        [HttpPost("edit")]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(PostCreateViewModel model, IFormFile? imageFile)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Tags = await _tagRepository.Tags.ToListAsync();
                return View(model);
            }

            var entityToUpdate = await _postRepository.Posts.Include(p => p.Tags).FirstOrDefaultAsync(p => p.PostId == model.PostId);

            if (entityToUpdate == null)
            {
                return NotFound();
            }

            entityToUpdate.Title = model.Title;
            entityToUpdate.Description = model.Description;
            entityToUpdate.Content = model.Content;
            entityToUpdate.Url = model.Url;
            entityToUpdate.IsActive = model.IsActive;

            if (imageFile != null)
            {
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png" };
                var extension = Path.GetExtension(imageFile.FileName).ToLowerInvariant();
                if (!allowedExtensions.Contains(extension))
                {
                    ModelState.AddModelError("", "Geçersiz resim formatı. Yalnızca JPG, JPEG ve PNG desteklenir.");
                    ViewBag.Tags = await _tagRepository.Tags.ToListAsync();
                    return View(model);
                }

                var randomFileName = $"{Guid.NewGuid()}{extension}";
                var path = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/img", randomFileName);

                using (var stream = new FileStream(path, FileMode.Create))
                {
                    await imageFile.CopyToAsync(stream);
                }
                entityToUpdate.Image = randomFileName;
            }

            var selectedTagIds = model.SelectedTagIds?.Select(int.Parse).ToArray() ?? Array.Empty<int>();
            await _postRepository.EditPost(entityToUpdate, selectedTagIds);

            return RedirectToAction("List");
        }

        // POST: /posts/uploadimage
        [HttpPost("uploadimage")]
        [Authorize]
        public async Task<IActionResult> UploadImage(IFormFile? file)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("UploadImage action başarısız: Kullanıcı doğrulanamadı.");
                return Json(new { success = false, message = "Kullanıcı doğrulanamadı." });
            }

            if (file == null || file.Length == 0)
            {
                _logger.LogWarning("UploadImage action başarısız: Dosya yüklenmedi.");
                return Json(new { success = false, message = "Dosya yüklenmedi." });
            }

            if (!IsValidImage(file))
            {
                _logger.LogWarning("UploadImage action başarısız: Geçersiz resim formatı.");
                return Json(new { success = false, message = "Geçersiz resim formatı. Yalnızca PNG, JPG, JPEG ve GIF desteklenir." });
            }

            try
            {
                var imageName = await SaveImageAsync(file, userId);
                if (imageName == null)
                {
                    _logger.LogError("UploadImage action başarısız: Resim kaydedilemedi.");
                    return Json(new { success = false, message = "Resim yüklenirken hata oluştu." });
                }
                _logger.LogInformation($"Resim başarıyla yüklendi. Kullanıcı ID: {userId}, Resim: {imageName}");
                return Json(new { success = true, location = $"/img/{imageName}" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Resim yükleme başarısız. Kullanıcı ID: {userId}");
                return Json(new { success = false, message = $"Resim yüklenirken hata oluştu: {ex.Message}" });
            }
        }

        // POST: /posts/delete
        [HttpPost("delete")]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete([FromBody] DeletePostRequest? request)
        {
            if (request == null || request.Id <= 0)
            {
                _logger.LogWarning("Delete action başarısız: Geçersiz yazı ID'si.");
                return Json(new { success = false, message = "Geçersiz yazı ID'si." });
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("Delete action başarısız: Kullanıcı ID'si null veya geçersiz.");
                return Json(new { success = false, message = "Kullanıcı doğrulanamadı." });
            }

            var post = await _postRepository.Posts
                .Where(p => p.PostId == request.Id && (p.UserId == userId || User.IsInRole("Admin")))
                .FirstOrDefaultAsync();

            if (post == null)
            {
                _logger.LogWarning($"Delete action başarısız: Yazı bulunamadı veya yetkisiz. Yazı ID: {request.Id}, Kullanıcı ID: {userId}");
                return Json(new { success = false, message = "Yazı bulunamadı veya silme izniniz yok." });
            }

            try
            {
                if (!string.IsNullOrEmpty(post.Image) && post.Image != "default.jpg")
                {
                    await DeleteImageAsync(post.Image, userId);
                }

                await _postRepository.DeletePost(request.Id);
                _logger.LogInformation($"Yazı {request.Id} silindi. Kullanıcı ID: {userId}");
                return Json(new { success = true, message = "Yazı başarıyla silindi." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Yazı silme başarısız. Yazı ID: {request.Id}, Kullanıcı ID: {userId}");
                return Json(new { success = false, message = "Yazı silinirken hata oluştu." });
            }
        }

        // POST: /posts/togglelike
        [HttpPost("togglelike")]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleLike(int postId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Json(new { success = false, message = "Kullanıcı bulunamadı." });
            }

            var existingLike = await _context.Likes
                .FirstOrDefaultAsync(l => l.PostId == postId && l.UserId == userId);

            bool isLiked;

            if (existingLike != null)
            {
                _context.Likes.Remove(existingLike);
                isLiked = false;
            }
            else
            {
                var newLike = new Like { PostId = postId, UserId = userId };
                _context.Likes.Add(newLike);
                isLiked = true;

                var post = await _context.Posts.FindAsync(postId);
                if (post != null && post.UserId != userId)
                {
                    var currentUser = await _userManager.FindByIdAsync(userId);
                    var message = $"{currentUser?.Name ?? "Biri"}, \"{post.Title}\" yazınızı beğendi.";
                    var link = $"/posts/details/{post.Url}";
                    await _notificationService.CreateNotificationAsync(post.UserId, message, link);
                }
            }

            await _context.SaveChangesAsync();
            var likeCount = await _context.Likes.CountAsync(l => l.PostId == postId);

            return Json(new { success = true, isLiked, likeCount });
        }

        private bool IsValidImage(IFormFile file)
        {
            var allowedExtensions = new[] { ".png", ".jpg", ".jpeg", ".gif" };
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            return allowedExtensions.Contains(extension);
        }

        private async Task<string?> SaveImageAsync(IFormFile file, string userId)
        {
            try
            {
                var fileName = Path.GetFileNameWithoutExtension(file.FileName);
                var extension = Path.GetExtension(file.FileName);
                var newFileName = $"{fileName}_{Guid.NewGuid()}{extension}";
                var path = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/img", newFileName);

                var directory = Path.GetDirectoryName(path);
                if (directory != null && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                using (var stream = new FileStream(path, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }
                _logger.LogInformation($"Resim kaydedildi: {newFileName}, Kullanıcı ID: {userId}");
                return newFileName;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Resim kaydetme başarısız. Kullanıcı ID: {userId}");
                return null;
            }
        }

        private async Task DeleteImageAsync(string imageName, string userId)
        {
            try
            {
                var imagePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/img", imageName);
                if (System.IO.File.Exists(imagePath))
                {
                    await Task.Run(() => System.IO.File.Delete(imagePath));
                    _logger.LogInformation($"Resim silindi: {imageName}, Kullanıcı ID: {userId}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, $"Resim silme başarısız: {imageName}, Kullanıcı ID: {userId}");
            }
        }

        public class DeletePostRequest
        {
            public int Id { get; set; }
        }
    }
}