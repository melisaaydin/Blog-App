using System.Security.Claims;
using System.Text.Json;
using BlogApp.Data.Abstract;
using BlogApp.Entity;
using BlogApp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.IO;
using System.Threading.Tasks;
using System.Linq;

namespace BlogApp.Controllers
{
    public class PostController : Controller
    {
        private readonly IPostRepository _postRepository;
        private readonly ICommentRepository _commentRepository;
        private readonly ITagRepository _tagRepository;
        private readonly ILogger<PostController> _logger;

        public PostController(IPostRepository postRepository, ICommentRepository commentRepository, ITagRepository tagRepository, ILogger<PostController> logger)
        {
            _postRepository = postRepository;
            _commentRepository = commentRepository;
            _tagRepository = tagRepository;
            _logger = logger;
        }

        public async Task<IActionResult> Index(string? tag, int page = 1)
        {
            const int postsPerPage = 4;
            var postsQuery = _postRepository.Posts
                .Include(p => p.Tags)
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

            if (totalPosts == 0)
            {
                _logger.LogInformation("No active posts found for Index action.");
                ViewBag.Message = "No published posts found. Try creating a new post.";
                return View(new PostsViewModel
                {
                    Posts = new List<Post>(),
                    Tag = tag,
                    CurrentPage = page,
                    TotalPages = 0
                });
            }

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

        public async Task<IActionResult> Details(string? url)
        {
            if (string.IsNullOrEmpty(url))
            {
                _logger.LogWarning("Details action failed. URL is null or empty.");
                return NotFound();
            }

            var post = await _postRepository.Posts
                .Include(x => x.User)
                .Include(p => p.Tags)
                .Include(p => p.Comments)
                .ThenInclude(p => p.User)
                .FirstOrDefaultAsync(p => p.IsActive && p.Url == url);

            if (post == null)
            {
                _logger.LogWarning($"Post not found for URL: {url}");
                return NotFound();
            }

            _logger.LogInformation($"Details action loaded for Post URL: {url}, PostId: {post.PostId}, Tags Count: {post.Tags?.Count ?? 0}");
            return View(post);
        }

        [HttpPost]
        public JsonResult AddComment(int PostId, string? Text)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
            {
                _logger.LogWarning("AddComment action failed. User is not authenticated.");
                return Json(new { error = "User could not be authenticated." });
            }

            if (string.IsNullOrEmpty(Text))
            {
                _logger.LogWarning($"AddComment action failed for PostId: {PostId}. Comment text is empty.");
                return Json(new { error = "Comment text is required." });
            }

            var username = User.FindFirstValue(ClaimTypes.Name) ?? "Anonymous";
            var avatar = User.FindFirstValue(ClaimTypes.UserData) ?? "default.jpg";

            var entity = new Comment
            {
                PostId = PostId,
                Text = Text,
                PublishedOn = DateTime.Now,
                UserId = userId
            };

            try
            {
                _commentRepository.CreateComment(entity);
                _logger.LogInformation($"Comment added for PostId: {PostId} by UserId: {userId}");
                return Json(new
                {
                    username,
                    text = entity.Text,
                    publishedOn = entity.PublishedOn,
                    avatar
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to add comment for PostId: {PostId}, UserId: {userId}");
                return Json(new { error = "An error occurred while adding the comment." });
            }
        }

        [Authorize]
        public async Task<IActionResult> Create()
        {
            var tags = await _tagRepository.Tags.ToListAsync();
            ViewBag.Tags = tags ?? new List<Tag>();
            _logger.LogInformation($"Create view loaded. Tags count: {tags?.Count ?? 0}");
            return View(new PostCreateViewModel());
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(PostCreateViewModel model, IFormFile? imageFile, string[]? selectedTags)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
            {
                _logger.LogWarning("Create POST action failed. UserId is null or invalid.");
                return Unauthorized();
            }

            _logger.LogInformation($"Create POST action started. UserId: {userId}, Title: {model.Title}, SelectedTagIds: {string.Join(",", selectedTags ?? Array.Empty<string>())}");

            if (ModelState.ContainsKey("Image"))
            {
                ModelState["Image"].Errors.Clear();
                ModelState["Image"].ValidationState = Microsoft.AspNetCore.Mvc.ModelBinding.ModelValidationState.Valid;
            }

            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                _logger.LogWarning($"Create POST action failed due to invalid model state. UserId: {userId}. Errors: {string.Join(", ", errors)}");
                ViewBag.Tags = await _tagRepository.Tags.ToListAsync() ?? new List<Tag>();
                return View(model);
            }

            var existingPost = await _postRepository.Posts.FirstOrDefaultAsync(p => p.Url == model.Url);
            if (existingPost != null)
            {
                ModelState.AddModelError("Url", "This URL is already in use.");
                ViewBag.Tags = await _tagRepository.Tags.ToListAsync() ?? new List<Tag>();
                return View(model);
            }

            string? imageName = null;
            if (imageFile != null && imageFile.Length > 0)
            {
                var fileName = Path.GetFileNameWithoutExtension(imageFile.FileName);
                var extension = Path.GetExtension(imageFile.FileName);
                imageName = $"{fileName}_{Guid.NewGuid()}{extension}";
                var path = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/img", imageName);
                try
                {
                    using (var stream = new FileStream(path, FileMode.Create))
                    {
                        await imageFile.CopyToAsync(stream);
                    }
                    _logger.LogInformation($"New image saved: {path}");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Failed to save image for UserId: {userId}");
                    ModelState.AddModelError("imageFile", "An error occurred while uploading the image.");
                    ViewBag.Tags = await _tagRepository.Tags.ToListAsync() ?? new List<Tag>();
                    return View(model);
                }
            }

            var post = new Post
            {
                Title = model.Title,
                Description = model.Description,
                Content = model.Content,
                Url = model.Url,
                Image = imageName ?? "default.jpg",
                UserId = userId,
                PublishedOn = DateTime.Now,
                IsActive = User.FindFirstValue(ClaimTypes.Role) == "admin" ? model.IsActive : false
            };

            int[] tagIds = selectedTags?.Select(s => int.TryParse(s, out int id) ? id : 0).Where(id => id != 0).ToArray() ?? Array.Empty<int>();
            _logger.LogInformation($"SelectedTagIds from form: {string.Join(",", selectedTags ?? Array.Empty<string>())}, Converted Tag IDs: {string.Join(",", tagIds)}");

            try
            {
                await _postRepository.CreatePost(post, tagIds);
                _logger.LogInformation($"Post created by user {userId}. PostId: {post.PostId}, Tags: {string.Join(",", tagIds)}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Post creation failed. UserId: {userId}, Tags: {string.Join(",", tagIds)}");
                ModelState.AddModelError("", "An error occurred while creating the post.");
                ViewBag.Tags = await _tagRepository.Tags.ToListAsync() ?? new List<Tag>();
                return View(model);
            }

            Response.Headers["Cache-Control"] = "no-cache, no-store, must-revalidate";
            Response.Headers["Pragma"] = "no-cache";
            Response.Headers["Expires"] = "0";
            return RedirectToAction("List");
        }

        [Authorize]
        public async Task<IActionResult> List(string? search, string? status, int page = 1)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdStr, out int userId))
            {
                _logger.LogWarning("List action failed. UserId is null or invalid.");
                return Unauthorized();
            }

            const int postsPerPage = 6;
            var postsQuery = _postRepository.Posts
                .Include(p => p.Tags)
                .AsQueryable();

            if (!User.IsInRole("admin"))
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

        [Authorize]
        public async Task<IActionResult> Edit(string? url)
        {
            if (string.IsNullOrEmpty(url))
            {
                _logger.LogWarning("Edit GET action failed. URL is null or empty.");
                return NotFound();
            }

            var post = await _postRepository.Posts
                .Include(p => p.Tags)
                .FirstOrDefaultAsync(p => p.Url == url);
            if (post == null)
            {
                _logger.LogWarning($"Post not found for URL: {url}");
                return NotFound();
            }

            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdStr, out int userId))
            {
                _logger.LogWarning($"Edit GET action failed. UserId is null or invalid.");
                return Unauthorized();
            }

            if (post.UserId != userId && !User.IsInRole("admin"))
            {
                _logger.LogWarning($"Unauthorized access to edit post with URL: {url} by UserId: {userId}");
                return Unauthorized();
            }

            var tags = await _tagRepository.Tags.ToListAsync();
            ViewBag.Tags = tags ?? new List<Tag>();
            var model = new PostCreateViewModel
            {
                PostId = post.PostId,
                Title = post.Title ?? string.Empty,
                Description = post.Description,
                Content = post.Content ?? string.Empty,
                Url = post.Url ?? string.Empty,
                Image = post.Image,
                IsActive = post.IsActive,
                SelectedTagIds = post.Tags?.Select(t => t.TagId.ToString()).ToArray() ?? Array.Empty<string>()
            };

            _logger.LogInformation($"Edit view loaded for Post URL: {url}, PostId: {post.PostId}, UserId: {userId}, SelectedTagIds: {string.Join(",", model.SelectedTagIds)}, Tags count: {tags?.Count ?? 0}");
            return View(model);
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(PostCreateViewModel model, IFormFile? imageFile)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
            {
                _logger.LogWarning("Edit POST action failed. UserId is null or invalid.");
                return Unauthorized();
            }

            if (ModelState.ContainsKey("Image"))
            {
                ModelState["Image"].Errors.Clear();
                ModelState["Image"].ValidationState = Microsoft.AspNetCore.Mvc.ModelBinding.ModelValidationState.Valid;
            }

            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                _logger.LogWarning($"Edit POST action failed due to invalid model state. UserId: {userId}, PostId: {model.PostId}. Errors: {string.Join(", ", errors)}");
                ViewBag.Tags = await _tagRepository.Tags.ToListAsync() ?? new List<Tag>();
                return View(model);
            }

            var existingPost = await _postRepository.Posts.FirstOrDefaultAsync(p => p.Url == model.Url && p.PostId != model.PostId);
            if (existingPost != null)
            {
                _logger.LogWarning($"Edit POST action failed. URL '{model.Url}' is already in use by PostId: {existingPost.PostId}");
                ModelState.AddModelError("Url", "This URL is already in use.");
                ViewBag.Tags = await _tagRepository.Tags.ToListAsync() ?? new List<Tag>();
                return View(model);
            }

            var role = User.FindFirstValue(ClaimTypes.Role);
            var postQuery = _postRepository.Posts
                .Include(x => x.Tags)
                .Where(x => x.PostId == model.PostId);

            if (string.IsNullOrEmpty(role) || role != "admin")
            {
                postQuery = postQuery.Where(x => x.UserId == userId);
                _logger.LogInformation($"Non-admin user {userId} is editing their own post {model.PostId}");
            }

            var post = await postQuery.FirstOrDefaultAsync();
            if (post == null)
            {
                _logger.LogWarning($"Post not found for ID: {model.PostId}, UserId: {userId}, Role: {role}");
                return NotFound();
            }

            string? imageName = post.Image;
            if (imageFile != null && imageFile.Length > 0)
            {
                if (!string.IsNullOrEmpty(post.Image) && post.Image != "default.jpg")
                {
                    var oldImagePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/img", post.Image);
                    if (System.IO.File.Exists(oldImagePath))
                    {
                        try
                        {
                            System.IO.File.Delete(oldImagePath);
                            _logger.LogInformation($"Old image deleted: {oldImagePath}");
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning($"Failed to delete old image: {post.Image}. Error: {ex.Message}");
                        }
                    }
                }

                var fileName = Path.GetFileNameWithoutExtension(imageFile.FileName);
                var extension = Path.GetExtension(imageFile.FileName);
                imageName = $"{fileName}_{Guid.NewGuid()}{extension}";
                var path = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/img", imageName);
                try
                {
                    using (var stream = new FileStream(path, FileMode.Create))
                    {
                        await imageFile.CopyToAsync(stream);
                    }
                    _logger.LogInformation($"New image saved: {path}");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Failed to save image for PostId: {model.PostId}, UserId: {userId}");
                    ModelState.AddModelError("imageFile", "An error occurred while uploading the image.");
                    ViewBag.Tags = await _tagRepository.Tags.ToListAsync() ?? new List<Tag>();
                    return View(model);
                }
            }

            post.Title = model.Title;
            post.Description = model.Description;
            post.Content = model.Content;
            post.Url = model.Url;
            post.Image = imageName ?? post.Image;
            if (role == "admin")
            {
                post.IsActive = model.IsActive;
            }

            int[] tagIds = model.SelectedTagIds?
                .Select(s => int.TryParse(s, out int id) ? id : 0)
                .Where(id => id != 0)
                .ToArray() ?? Array.Empty<int>();

            try
            {
                await _postRepository.EditPost(post, tagIds);
                _logger.LogInformation($"Post {model.PostId} successfully updated by UserId: {userId}. Tags: {string.Join(",", tagIds)}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to update Post {model.PostId} with tags: {string.Join(",", tagIds)}");
                ModelState.AddModelError("", "An error occurred while updating the post and tags.");
                ViewBag.Tags = await _tagRepository.Tags.ToListAsync() ?? new List<Tag>();
                return View(model);
            }

            Response.Headers["Cache-Control"] = "no-cache, no-store, must-revalidate";
            Response.Headers["Pragma"] = "no-cache";
            Response.Headers["Expires"] = "0";
            return RedirectToAction("List");
        }

        [HttpPost]
        [Route("post/uploadimage")]
        public async Task<IActionResult> UploadImage(IFormFile? file)
        {
            if (file == null || file.Length == 0)
            {
                _logger.LogWarning("UploadImage action failed. No file uploaded.");
                return Json(new { success = false, message = "No file uploaded." });
            }

            try
            {
                var fileName = Path.GetFileNameWithoutExtension(file.FileName);
                var extension = Path.GetExtension(file.FileName);
                var newFileName = $"{fileName}_{Guid.NewGuid()}{extension}";
                var path = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/img", newFileName);
                using (var stream = new FileStream(path, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }
                _logger.LogInformation($"Image uploaded successfully: {newFileName}");
                return Json(new { success = true, location = $"/img/{newFileName}" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to upload image.");
                return Json(new { success = false, message = "An error occurred while uploading the image: " + ex.Message });
            }
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete([FromBody] DeletePostRequest? request)
        {
            if (request == null || request.Id <= 0)
            {
                _logger.LogWarning("Delete action failed. Invalid post ID.");
                return Json(new { success = false, message = "Invalid post ID." });
            }

            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdStr, out int userId))
            {
                _logger.LogWarning("Delete action failed. UserId is null or invalid.");
                return Json(new { success = false, message = "User could not be authenticated." });
            }

            var post = await _postRepository.Posts
                .Where(p => p.PostId == request.Id && (p.UserId == userId || User.IsInRole("admin")))
                .FirstOrDefaultAsync();

            if (post == null)
            {
                _logger.LogWarning($"Delete action failed. Post not found or unauthorized for PostId: {request.Id}, UserId: {userId}");
                return Json(new { success = false, message = "Post not found or you do not have permission to delete it." });
            }

            try
            {
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

        public class DeletePostRequest
        {
            public int Id { get; set; }
        }
    }
}