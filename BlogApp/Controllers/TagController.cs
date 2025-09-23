using BlogApp.Data.Abstract;
using BlogApp.Entity;
using BlogApp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text.RegularExpressions;

namespace BlogApp.Controllers
{
    [Authorize]
    public class TagController : Controller
    {
        private readonly ITagRepository _tagRepository;
        private readonly IPostRepository _postRepository;
        private readonly ILogger<TagController> _logger;

        public TagController(ITagRepository tagRepository, IPostRepository postRepository, ILogger<TagController> logger)
        {
            _tagRepository = tagRepository;
            _postRepository = postRepository;
            _logger = logger;
        }

        [AllowAnonymous]
        public async Task<IActionResult> Index(string sort = "newest", int page = 1)
        {
            _logger.LogInformation($"Tag Index action called with sort: {sort}, page: {page}");
            int pageSize = 9;
            var tagsQuery = _tagRepository.Tags.AsQueryable();

            tagsQuery = sort switch
            {
                "popular" => tagsQuery.OrderByDescending(t => t.Posts.Count),
                "updated" => tagsQuery.OrderByDescending(t => t.Posts.Max(p => p.PublishedOn)),
                _ => tagsQuery.OrderByDescending(t => t.TagId)
            };

            var totalTags = await tagsQuery.CountAsync();
            var totalPages = (int)Math.Ceiling(totalTags / (double)pageSize);
            page = Math.Max(1, Math.Min(page, totalPages));

            var tags = await tagsQuery
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Include(t => t.Posts)
                .ToListAsync();

            var model = new TagsViewModel
            {
                Tags = tags,
                Sort = sort,
                CurrentPage = page,
                TotalPages = totalPages
            };

            return View(model);
        }

        public IActionResult Create()
        {
            _logger.LogInformation("Tag Create GET action called");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(TagCreateViewModel model)
        {
            _logger.LogInformation($"Tag Create POST action called with Text: {model.Text}");
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Invalid model state for tag creation.");
                return View(model);
            }

            var url = Regex.Replace(model.Text.ToLower(), @"[^a-z0-9]+", "-").Trim('-');
            if (string.IsNullOrEmpty(url))
            {
                ModelState.AddModelError("Text", "Tag name cannot result in an empty URL.");
                _logger.LogWarning("Tag creation failed: Generated URL is empty.");
                return View(model);
            }

            if (await _tagRepository.Tags.AnyAsync(t => t.Url == url))
            {
                ModelState.AddModelError("Text", "A tag with this name or URL already exists.");
                _logger.LogWarning($"Tag creation failed: URL '{url}' already exists.");
                return View(model);
            }

            var tag = new Tag
            {
                Text = model.Text,
                Url = url,
                CreatorId = User.FindFirstValue(ClaimTypes.NameIdentifier)
            };

            await _tagRepository.AddAsync(tag);
            _logger.LogInformation($"Tag created successfully: {model.Text}, URL: {url}");

            TempData["ToastMessage"] = "Tag created successfully!";
            TempData["ToastType"] = "success";
            return RedirectToAction("Index");
        }

        [AllowAnonymous]
        public async Task<IActionResult> Details(string url)
        {
            _logger.LogInformation($"Tag Details action called for URL: {url}");
            if (string.IsNullOrEmpty(url))
            {
                _logger.LogWarning("Tag URL is null or empty.");
                return NotFound();
            }

            var tag = await _tagRepository.Tags
                .Include(t => t.Posts)
                .ThenInclude(p => p.User)
                .Include(t => t.Creator)
                .FirstOrDefaultAsync(t => t.Url == url);

            if (tag == null)
            {
                _logger.LogWarning($"Tag not found for URL: {url}");
                return NotFound();
            }

            return View(tag);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            _logger.LogInformation($"Tag Delete action called for TagId: {id}");
            var tag = await _tagRepository.Tags
                .Include(t => t.Posts)
                .FirstOrDefaultAsync(t => t.TagId == id);

            if (tag == null)
            {
                _logger.LogWarning($"Tag not found for ID: {id}");
                return NotFound();
            }

            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (tag.CreatorId != currentUserId)
            {
                _logger.LogWarning($"User {currentUserId} attempted to delete tag {id} but is not the owner.");
                return Forbid();
            }

            foreach (var post in tag.Posts)
            {
                post.Tags.Remove(tag);
            }

            _tagRepository.Delete(tag);
            await _tagRepository.SaveChangesAsync();
            _logger.LogInformation($"Tag deleted successfully: {tag.Text}, ID: {id}");

            TempData["ToastMessage"] = "Tag deleted successfully!";
            TempData["ToastType"] = "success";
            return RedirectToAction("Index");
        }
    }
}