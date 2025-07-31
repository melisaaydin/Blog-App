using System;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using BlogApp.Data.Abstract;
using BlogApp.Entity;
using BlogApp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BlogApp.Controllers
{
    public class PostController : Controller
    {
        private readonly IPostRepository _postRepository;
        private readonly ICommentRepository _commentRepository;
        private readonly ITagRepository _tagRepository;
        private readonly ILogger<PostController> _logger;
        private readonly UserManager<User> _userManager;
        private readonly INotificationService _notificationService;
        public PostController(
            IPostRepository postRepository,
            ICommentRepository commentRepository,
            ITagRepository tagRepository,
            ILogger<PostController> logger,
            UserManager<User> userManager, INotificationService notificationService)
        {
            _postRepository = postRepository;
            _commentRepository = commentRepository;
            _tagRepository = tagRepository;
            _logger = logger;
            _userManager = userManager;
            _notificationService = notificationService;

        }

        // GET: /post or /posts/tag/{tag}
        public async Task<IActionResult> Index(string? tag, int page = 1)
        {
            const int postsPerPage = 4;
            var postsQuery = _postRepository.Posts
                .Include(p => p.Tags)
                .Include(p => p.User)
                .Where(p => p.IsActive)
                .AsQueryable();

            if (!string.IsNullOrEmpty(tag))
            {
                postsQuery = postsQuery.Where(p => p.Tags != null && p.Tags.Any(t => t.Url == tag));
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
                Tag = tag,
                CurrentPage = page,
                TotalPages = (int)Math.Ceiling((double)totalPosts / postsPerPage)
            };

            ViewBag.MetaDescription = string.IsNullOrEmpty(tag)
                ? "Discover the latest posts on technology, lifestyle, and more at MyBlog."
                : $"Explore posts tagged '{tag}' at MyBlog.";
            ViewBag.CanonicalUrl = string.IsNullOrEmpty(tag)
                ? $"/post?page={page}"
                : $"/posts/tag/{tag}?page={page}";

            _logger.LogInformation($"Index action loaded. Total posts: {totalPosts}, Page: {page}, Tag: {tag}");
            return View(model);
        }

        // GET: /posts/details/{url}
        public async Task<IActionResult> Details(string? url)
        {
            if (string.IsNullOrEmpty(url))
            {
                return NotFound();
            }

            var post = await _postRepository.Posts
                .Include(p => p.User)
                .Include(p => p.Tags)
                .Include(p => p.Comments)
                .ThenInclude(c => c.User)
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
            await _postRepository.UpdatePost(post);
            // Set default avatar for users without images
            if (post.User != null)
            {
                post.User.Image ??= "default-avatar-icon.png";
            }
            if (post.Comments != null)
            {
                foreach (var comment in post.Comments)
                {
                    if (comment.User != null)
                    {
                        comment.User.Image ??= "default-avatar-icon.png";
                    }
                }
            }

            return View(post);
        }

        // POST: /Post/AddComment
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<JsonResult> AddComment(int PostId, string? Text)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId) || string.IsNullOrWhiteSpace(Text))
            {
                return Json(new { success = false, message = "Comment text cannot be empty or user not authenticated." });
            }

            var currentUser = await _userManager.FindByIdAsync(userId);
            if (currentUser == null)
            {
                return Json(new { success = false, message = "User not found." });
            }
            var post = await _postRepository.Posts.FirstOrDefaultAsync(p => p.PostId == PostId);
            if (post == null)
            {
                return Json(new { success = false, message = "Post not found." });
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
                var message = $"{currentUser.Name ?? currentUser.UserName} commented on your post: \"{post.Title}\"";
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
                avatar = currentUser.Image ?? "default-avatar.png"
            });
        }

        // GET: /post/create
        [Authorize]
        public async Task<IActionResult> Create()
        {
            var tags = await _tagRepository.Tags.ToListAsync();
            ViewBag.Tags = tags ?? new List<Tag>();
            _logger.LogInformation($"Create view loaded. Tags count: {(tags?.Count ?? 0)}");
            return View(new PostCreateViewModel());
        }

        // POST: /post/create
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        [HttpPost]
        public async Task<IActionResult> Create(PostCreateViewModel model, IFormFile? imageFile)
        {
            if (!ModelState.IsValid)
            {
                // Model geçerli değilse, tag listesini tekrar yükleyip formu geri göster
                ViewBag.Tags = await _tagRepository.Tags.ToListAsync();
                return View(model);
            }

            // 1. Yeni Post nesnesini ViewModel'den gelen bilgilerle oluştur
            var newPost = new Post
            {
                Title = model.Title,
                Description = model.Description,
                Content = model.Content,
                Url = model.Url,
                PublishedOn = DateTime.Now,
                IsActive = model.IsActive, // EKSİK OLAN KISIM BURASIYDI!
                                           // O an giriş yapmış olan kullanıcının kimliğini (ID) ata
                UserId = User.FindFirstValue(ClaimTypes.NameIdentifier)
            };

            // 2. Resim dosyası seçilmişse işle ve ismini Post nesnesine ata
            if (imageFile != null)
            {
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png" };
                var extension = Path.GetExtension(imageFile.FileName).ToLowerInvariant();
                if (!allowedExtensions.Contains(extension))
                {
                    ModelState.AddModelError("", "Invalid image format. Only JPG, JPEG, and PNG are allowed.");
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

            // 3. Gelen string[] tag ID'lerini int[] dizisine çevir
            var selectedTagIds = model.SelectedTagIds?.Select(int.Parse).ToArray() ?? Array.Empty<int>();

            // 4. Repository'deki doğru metodu çağırarak hem post'u hem de tag'leri kaydet
            await _postRepository.CreatePost(newPost, selectedTagIds);

            // TempData ile başarılı mesajı gönder (isteğe bağlı ama güzel bir özellik)
            TempData["message"] = "Post has been created successfully.";

            return RedirectToAction("List", "Post");
        }

        // GET: /post/list
        [Authorize]
        public async Task<IActionResult> List(string? search, string? status, int page = 1)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("List action failed: UserId is null or invalid.");
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

            // Set default avatar for users without images
            foreach (var post in posts)
            {
                if (post.User != null)
                {
                    post.User.Image ??= "default-avatar-icon.png";
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

            _logger.LogInformation($"List action loaded for UserId: {userId}, Search: {search}, Status: {status}, Total posts: {totalPosts}");
            return View(model);
        }

        // GET: /post/edit/{url}
        [Authorize]
        public async Task<IActionResult> Edit(string? url)
        {
            if (string.IsNullOrEmpty(url))
            {
                _logger.LogWarning("Edit GET action failed: URL is null or empty.");
                return NotFound();
            }

            var post = await _postRepository.Posts
                .Include(p => p.Tags)
                .Include(p => p.User)
                .FirstOrDefaultAsync(p => p.Url == url);

            if (post == null)
            {
                _logger.LogWarning($"Post not found for URL: {url}");
                return NotFound();
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId) || (post.UserId != userId && !User.IsInRole("Admin")))
            {
                _logger.LogWarning($"Unauthorized access to edit post with URL: {url} by UserId: {userId}");
                return Unauthorized();
            }

            // Set default avatar if user image is not set
            if (post.User != null)
            {
                post.User.Image ??= "default-avatar-icon.png";
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

            _logger.LogInformation($"Edit view loaded for Post URL: {url}, PostId: {post.PostId}, UserId: {userId}, SelectedTagIds: {string.Join(",", model.SelectedTagIds)}, Tags count: {(tags?.Count ?? 0)}");
            return View(model);
        }

        // POST: /post/edit
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(PostCreateViewModel model, IFormFile? imageFile)
        {
            if (!ModelState.IsValid)
            {
                // Eğer model geçerli değilse, tag listesini tekrar doldurup view'ı geri döndür
                ViewBag.Tags = await _tagRepository.Tags.ToListAsync();
                return View(model);
            }

            // 1. Veritabanından güncellenecek olan post'u bul
            var entityToUpdate = await _postRepository.Posts.Include(p => p.Tags).FirstOrDefaultAsync(p => p.PostId == model.PostId);

            if (entityToUpdate == null)
            {
                return NotFound(); // Post bulunamazsa hata ver
            }

            // 2. ViewModel'den gelen tüm bilgileri entity'e tek tek aktar
            entityToUpdate.Title = model.Title;
            entityToUpdate.Description = model.Description;
            entityToUpdate.Content = model.Content;
            entityToUpdate.Url = model.Url;
            entityToUpdate.IsActive = model.IsActive; // EKSİK OLAN EN ÖNEMLİ KISIM BURASIYDI!

            // 3. Resim dosyası seçilmişse işle
            if (imageFile != null)
            {
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png" };
                var extension = Path.GetExtension(imageFile.FileName).ToLowerInvariant();
                if (!allowedExtensions.Contains(extension))
                {
                    ModelState.AddModelError("", "Invalid image format. Only JPG, JPEG, and PNG are allowed.");
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

            return RedirectToAction("List", "Post");
        }

        [Authorize]
        [HttpPost]
        [Route("post/uploadimage")]
        public async Task<IActionResult> UploadImage(IFormFile? file)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("UploadImage action failed: User is not authenticated.");
                return Json(new { success = false, message = "User could not be authenticated." });
            }

            if (file == null || file.Length == 0)
            {
                _logger.LogWarning("UploadImage action failed: No file uploaded.");
                return Json(new { success = false, message = "No file uploaded." });
            }

            if (!IsValidImage(file))
            {
                _logger.LogWarning("UploadImage action failed: Invalid image format.");
                return Json(new { success = false, message = "Invalid image format. Only PNG, JPG, JPEG, and GIF are allowed." });
            }

            try
            {
                var imageName = await SaveImageAsync(file, userId);
                if (imageName == null)
                {
                    _logger.LogError("UploadImage action failed: Image saving returned null.");
                    return Json(new { success = false, message = "An error occurred while uploading the image." });
                }
                _logger.LogInformation($"Image uploaded successfully by UserId: {userId}, Image: {imageName}");
                return Json(new { success = true, location = $"/img/{imageName}" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to upload image by UserId: {userId}");
                return Json(new { success = false, message = $"An error occurred while uploading the image: {ex.Message}" });
            }
        }

        // POST: /post/delete
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete([FromBody] DeletePostRequest? request)
        {
            if (request == null || request.Id <= 0)
            {
                _logger.LogWarning("Delete action failed: Invalid post ID.");
                return Json(new { success = false, message = "Invalid post ID." });
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("Delete action failed: UserId is null or invalid.");
                return Json(new { success = false, message = "User could not be authenticated." });
            }

            var post = await _postRepository.Posts
                .Where(p => p.PostId == request.Id && (p.UserId == userId || User.IsInRole("Admin")))
                .FirstOrDefaultAsync();

            if (post == null)
            {
                _logger.LogWarning($"Delete action failed: Post not found or unauthorized for PostId: {request.Id}, UserId: {userId}");
                return Json(new { success = false, message = "Post not found or you do not have permission to delete it." });
            }

            try
            {
                // Delete associated image if it exists and is not default
                if (!string.IsNullOrEmpty(post.Image) && post.Image != "default.jpg")
                {
                    await DeleteImageAsync(post.Image, userId);
                }

                await _postRepository.DeletePost(request.Id);
                _logger.LogInformation($"Post {request.Id} deleted by UserId: {userId}");
                return Json(new { success = true, message = "Post deleted successfully." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to delete Post {request.Id} by UserId: {userId}");
                return Json(new { success = false, message = "An error occurred while deleting the post." });
            }
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
                _logger.LogInformation($"Image saved: {newFileName} by UserId: {userId}");
                return newFileName;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to save image for UserId: {userId}");
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
                    _logger.LogInformation($"Image deleted: {imageName} by UserId: {userId}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, $"Failed to delete image: {imageName} by UserId: {userId}");
            }
        }

        public class DeletePostRequest
        {
            public int Id { get; set; }
        }
    }
}