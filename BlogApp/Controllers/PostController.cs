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
        private readonly ICollectionRepository _collectionRepository;

        public PostController(
            IPostRepository postRepository,
            ICommentRepository commentRepository,
            ITagRepository tagRepository,
            ILogger<PostController> logger,
            UserManager<User> userManager,
            INotificationService notificationService,
            BlogContext context,
            ICollectionRepository collectionRepository)
        {
            _postRepository = postRepository;
            _commentRepository = commentRepository;
            _tagRepository = tagRepository;
            _logger = logger;
            _userManager = userManager;
            _notificationService = notificationService;
            _context = context;
            _collectionRepository = collectionRepository;
        }

        // GET: /posts or /posts/tag/{url}
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
                ? "Discover the latest articles on technology, lifestyle, and more at MyBlog."
                : $"Discover articles tagged '{url}' at MyBlog.";
            ViewBag.CanonicalUrl = string.IsNullOrEmpty(url)
                ? $"/posts?page={page}"
                : $"/posts/tag/{url}?page={page}";

            _logger.LogInformation($"Index action loaded. Total posts: {totalPosts}, Page: {page}, Tag: {url}");
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
                return Json(new { success = false, message = "Reply text cannot be empty." });
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Json(new { success = false, message = "User not authenticated." });
            }

            var currentUser = await _userManager.FindByIdAsync(userId);
            if (currentUser == null)
            {
                return Json(new { success = false, message = "User not found." });
            }

            var post = await _postRepository.Posts.FirstOrDefaultAsync(p => p.PostId == PostId && p.IsActive);
            if (post == null)
            {
                return Json(new { success = false, message = "Post not found." });
            }

            var parentComment = await _commentRepository.GetCommentById(ParentCommentId);
            if (parentComment == null)
            {
                return Json(new { success = false, message = "Parent comment not found." });
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
                var message = $"{currentUser.Name ?? currentUser.UserName} replied to your comment on \"{post.Title}\".";
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
                var message = $"{currentUser.Name ?? currentUser.UserName} commented on your post \"{post.Title}\".";
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
            ViewBag.Tags = await _tagRepository.Tags.ToListAsync();
            ViewBag.OpenCollections = await _collectionRepository.Collections.Where(c => c.IsOpen).ToListAsync();  // Open collections for selection
            return View(new PostCreateViewModel());
        }

        [HttpPost("create")]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(PostCreateViewModel model, IFormFile? imageFile)
        {
            _logger.LogInformation("Create action called. SelectedCollectionIds: {Ids}",
                model.SelectedCollectionIds != null ? string.Join(",", model.SelectedCollectionIds) : "null");
            ModelState.Remove("SelectedCollectionIds");
            if (!ModelState.IsValid)
                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("ModelState invalid: {Errors}", string.Join("; ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));
                    ViewBag.Tags = await _tagRepository.Tags.ToListAsync();
                    ViewBag.OpenCollections = await _collectionRepository.Collections.Where(c => c.IsOpen).ToListAsync();
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
                    ModelState.AddModelError("", "Invalid image format. Only JPG, JPEG, and PNG are supported.");
                    ViewBag.Tags = await _tagRepository.Tags.ToListAsync();
                    ViewBag.OpenCollections = await _collectionRepository.Collections.Where(c => c.IsOpen).ToListAsync();
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

            var selectedTagIds = model.SelectedTagIds?.Select(id => int.TryParse(id, out int result) ? result : 0)
                .Where(id => id > 0).ToArray() ?? Array.Empty<int>();
            await _postRepository.CreatePost(newPost, selectedTagIds);

            if (model.SelectedCollectionIds != null && model.SelectedCollectionIds.Any())
            {
                foreach (var collId in model.SelectedCollectionIds)
                {
                    if (int.TryParse(collId, out int collectionId))
                    {
                        try
                        {
                            await _collectionRepository.AddPostToCollectionAsync(newPost.PostId, collectionId);
                            _logger.LogInformation("Post {PostId} added to collection {CollectionId}", newPost.PostId, collectionId);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Failed to add post {PostId} to collection {CollectionId}", newPost.PostId, collectionId);
                            ModelState.AddModelError("", $"Failed to add post to collection ID {collectionId}.");
                        }
                    }
                    else
                    {
                        _logger.LogWarning("Invalid collection ID: {CollectionId}", collId);
                        ModelState.AddModelError("", $"Invalid collection ID: {collId}");
                    }
                }
            }

            if (!ModelState.IsValid)
            {
                ViewBag.Tags = await _tagRepository.Tags.ToListAsync();
                ViewBag.OpenCollections = await _collectionRepository.Collections.Where(c => c.IsOpen).ToListAsync();
                return View(model);
            }

            var admins = await _userManager.GetUsersInRoleAsync("Admin");
            var postCreator = await _userManager.FindByIdAsync(newPost.UserId);
            foreach (var admin in admins)
            {
                if (admin.Id != newPost.UserId)
                {
                    var message = $"{postCreator?.Name ?? "A user"} created a new post titled \"{newPost.Title}\".";
                    var link = "/posts/list";
                    await _notificationService.CreateNotificationAsync(admin.Id, message, link);
                }
            }

            TempData["message"] = "Post created successfully.";
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
                _logger.LogWarning("List action failed: User ID is null or invalid.");
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

            _logger.LogInformation($"List action loaded. User ID: {userId}, Search: {search}, Status: {status}, Total posts: {totalPosts}");
            return View(model);
        }

        [HttpGet("edit/{url}")]
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
                _logger.LogWarning($"Post not found. URL: {url}");
                return NotFound();
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId) || (post.UserId != userId && !User.IsInRole("Admin")))
            {
                _logger.LogWarning($"Unauthorized access: URL: {url}, User ID: {userId}");
                return Unauthorized();
            }

            if (post.User != null)
            {
                post.User.Image ??= "default-avatar.jpg";
            }

            var tags = await _tagRepository.Tags.ToListAsync();
            ViewBag.Tags = tags ?? new List<Tag>();
            ViewBag.OpenCollections = await _collectionRepository.Collections.Where(c => c.IsOpen).ToListAsync();

            // Fetch the collections associated with the post
            var postCollections = await _collectionRepository.GetCollectionsByPostIdAsync(post.PostId);

            var model = new PostCreateViewModel
            {
                PostId = post.PostId,
                Title = post.Title,
                Description = post.Description,
                Content = post.Content,
                Url = post.Url,
                Image = post.Image,
                IsActive = post.IsActive,
                SelectedTagIds = post.Tags?.Select(t => t.TagId.ToString()).ToArray() ?? Array.Empty<string>(),
                SelectedCollectionIds = postCollections?.Select(c => c.Id.ToString()).ToArray() ?? Array.Empty<string>()
            };

            _logger.LogInformation($"Edit view loaded. Post URL: {url}, Post ID: {post.PostId}, User ID: {userId}, Selected Tags: {string.Join(",", model.SelectedTagIds)}, Selected Collections: {string.Join(",", model.SelectedCollectionIds)}, Tag count: {(tags?.Count ?? 0)}");
            return View(model);
        }

        [HttpPost("edit/{url}")]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string url, PostCreateViewModel model, IFormFile? imageFile)
        {
            _logger.LogInformation("Edit action called. PostId: {PostId}, SelectedCollectionIds: {Ids}",
                model.PostId,
                model.SelectedCollectionIds != null ? string.Join(",", model.SelectedCollectionIds) : "null");
            ModelState.Remove("SelectedCollectionIds");
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("ModelState invalid: {Errors}", string.Join("; ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));
                ViewBag.Tags = await _tagRepository.Tags.ToListAsync();
                ViewBag.OpenCollections = await _collectionRepository.Collections.Where(c => c.IsOpen).ToListAsync();
                return View(model);
            }

            var entityToUpdate = await _postRepository.Posts
                .Include(p => p.Tags)
                .FirstOrDefaultAsync(p => p.PostId == model.PostId);

            if (entityToUpdate == null)
            {
                _logger.LogWarning("Post not found: {PostId}", model.PostId);
                return NotFound();
            }

            if (entityToUpdate.Url != url)
            {
                _logger.LogWarning("URL mismatch: Requested URL: {RequestedUrl}, Post URL: {PostUrl}", url, entityToUpdate.Url);
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
                    ModelState.AddModelError("", "Invalid image format. Only JPG, JPEG, and PNG are supported.");
                    ViewBag.Tags = await _tagRepository.Tags.ToListAsync();
                    ViewBag.OpenCollections = await _collectionRepository.Collections.Where(c => c.IsOpen).ToListAsync();
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

            var selectedTagIds = model.SelectedTagIds?.Select(id => int.TryParse(id, out int result) ? result : 0)
                .Where(id => id > 0).ToArray() ?? Array.Empty<int>();
            await _postRepository.EditPost(entityToUpdate, selectedTagIds);

            var existingCollections = await _collectionRepository.GetCollectionsByPostIdAsync(entityToUpdate.PostId);
            var selectedCollectionIds = model.SelectedCollectionIds?.Select(id => int.TryParse(id, out int result) ? result : 0)
                .Where(id => id > 0).ToArray() ?? Array.Empty<int>();
            var collectionsToAdd = selectedCollectionIds.Where(id => !existingCollections.Any(ec => ec.Id == id)).ToList();
            var collectionsToRemove = existingCollections.Where(ec => !selectedCollectionIds.Contains(ec.Id)).Select(ec => ec.Id).ToList();

            foreach (var collId in collectionsToAdd)
            {
                try
                {
                    await _collectionRepository.AddPostToCollectionAsync(entityToUpdate.PostId, collId);
                    _logger.LogInformation("Post {PostId} added to collection {CollectionId}", entityToUpdate.PostId, collId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to add post {PostId} to collection {CollectionId}", entityToUpdate.PostId, collId);
                    ModelState.AddModelError("", $"Failed to add post to collection ID {collId}.");
                }
            }

            foreach (var collId in collectionsToRemove)
            {
                try
                {
                    await _collectionRepository.RemovePostFromCollectionAsync(entityToUpdate.PostId, collId);
                    _logger.LogInformation("Post {PostId} removed from collection {CollectionId}", entityToUpdate.PostId, collId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to remove post {PostId} from collection {CollectionId}", entityToUpdate.PostId, collId);
                    ModelState.AddModelError("", $"Failed to remove post from collection ID {collId}.");
                }
            }

            if (!ModelState.IsValid)
            {
                ViewBag.Tags = await _tagRepository.Tags.ToListAsync();
                ViewBag.OpenCollections = await _collectionRepository.Collections.Where(c => c.IsOpen).ToListAsync();
                return View(model);
            }

            TempData["message"] = "Post updated successfully.";
            return RedirectToAction("List");
        }

        [HttpPost("uploadimage")]
        [Authorize]
        public async Task<IActionResult> UploadImage(IFormFile? file)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("UploadImage action failed: User not authenticated.");
                return Json(new { success = false, message = "User not authenticated." });
            }

            if (file == null || file.Length == 0)
            {
                _logger.LogWarning("UploadImage action failed: No file uploaded.");
                return Json(new { success = false, message = "No file uploaded." });
            }

            if (!IsValidImage(file))
            {
                _logger.LogWarning("UploadImage action failed: Invalid image format.");
                return Json(new { success = false, message = "Invalid image format. Only PNG, JPG, JPEG, and GIF are supported." });
            }

            try
            {
                var imageName = await SaveImageAsync(file, userId);
                if (imageName == null)
                {
                    _logger.LogError("UploadImage action failed: Image could not be saved.");
                    return Json(new { success = false, message = "Error occurred while uploading the image." });
                }
                _logger.LogInformation($"Image uploaded successfully. User ID: {userId}, Image: {imageName}");
                return Json(new { success = true, location = $"/img/{imageName}" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Image upload failed. User ID: {userId}");
                return Json(new { success = false, message = $"Error occurred while uploading the image: {ex.Message}" });
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
                _logger.LogWarning("Delete action failed: Invalid post ID.");
                return Json(new { success = false, message = "Invalid post ID." });
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("Delete action failed: User ID is null or invalid.");
                return Json(new { success = false, message = "User not authenticated." });
            }

            var post = await _postRepository.Posts
                .Where(p => p.PostId == request.Id && (p.UserId == userId || User.IsInRole("Admin")))
                .FirstOrDefaultAsync();

            if (post == null)
            {
                _logger.LogWarning($"Delete action failed: Post not found or unauthorized. Post ID: {request.Id}, User ID: {userId}");
                return Json(new { success = false, message = "Post not found or you don't have permission to delete it." });
            }

            try
            {
                if (!string.IsNullOrEmpty(post.Image) && post.Image != "default.jpg")
                {
                    await DeleteImageAsync(post.Image, userId);
                }

                await _postRepository.DeletePost(request.Id);
                _logger.LogInformation($"Post {request.Id} deleted. User ID: {userId}");
                return Json(new { success = true, message = "Post deleted successfully." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Post deletion failed. Post ID: {request.Id}, User ID: {userId}");
                return Json(new { success = false, message = "Error occurred while deleting the post." });
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
                return Json(new { success = false, message = "User not found." });
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
                    var message = $"{currentUser?.Name ?? "Someone"} liked your post \"{post.Title}\".";
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
                _logger.LogInformation($"Image saved: {newFileName}, User ID: {userId}");
                return newFileName;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Image saving failed. User ID: {userId}");
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
                    _logger.LogInformation($"Image deleted: {imageName}, User ID: {userId}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, $"Image deletion failed: {imageName}, User ID: {userId}");
            }
        }

        public class DeletePostRequest
        {
            public int Id { get; set; }
        }
    }
}