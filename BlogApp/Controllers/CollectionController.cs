using System.Security.Claims;
using System.Threading.Tasks;
using BlogApp.Data.Abstract;
using BlogApp.Entity;
using BlogApp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BlogApp.Controllers
{
    [Authorize]
    [Route("collections")]
    public class CollectionController : Controller
    {
        private readonly ICollectionRepository _collectionRepository;
        private readonly IPostRepository _postRepository;

        public CollectionController(ICollectionRepository collectionRepository, IPostRepository postRepository)
        {
            _collectionRepository = collectionRepository;
            _postRepository = postRepository;
        }

        [HttpGet("")]
        [AllowAnonymous]
        public async Task<IActionResult> Index(string sort = "newest", int page = 1)
        {
            const int itemsPerPage = 10;
            var query = _collectionRepository.Collections.Where(c => c.IsOpen);

            query = sort switch
            {
                "popular" => query.OrderByDescending(c => c.Posts.Count),
                "newest" => query.OrderByDescending(c => c.CreatedAt),
                _ => query.OrderByDescending(c => c.UpdatedAt)
            };

            int total = await query.CountAsync();
            var collections = await query
                .Skip((page - 1) * itemsPerPage)
                .Take(itemsPerPage)
                .ToListAsync();

            var model = new CollectionsViewModel
            {
                Collections = collections,
                CurrentPage = page,
                TotalPages = (int)Math.Ceiling((double)total / itemsPerPage),
                Sort = sort
            };

            ViewBag.MetaDescription = "Discover crowdsourced article collections.";
            return View(model);
        }

        [HttpGet("details/{id}")]
        [AllowAnonymous]
        public async Task<IActionResult> Details(int id)
        {
            var collection = await _collectionRepository.GetByIdAsync(id);
            if (collection == null) return NotFound();

            collection.Posts = collection.Posts.OrderByDescending(p => p.PublishedOn).ToList();

            ViewBag.MetaDescription = $"Read articles in the '{collection.Title}' collection.";
            return View(collection);
        }

        [HttpGet("create")]
        public IActionResult Create() => View(new CollectionCreateViewModel());

        [HttpPost("create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CollectionCreateViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var collection = new Collection
            {
                Title = model.Title,
                Description = model.Description,
                CreatorId = userId,
                IsOpen = true
            };

            await _collectionRepository.CreateCollectionAsync(collection);
            return RedirectToAction("Index");
        }

        [HttpPost("toggle-status/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleStatus(int id)
        {
            var collection = await _collectionRepository.GetByIdAsync(id);
            if (collection == null)
            {
                return NotFound();
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (collection.CreatorId != userId)
            {
                return Forbid();
            }

            collection.IsOpen = !collection.IsOpen;
            collection.UpdatedAt = DateTime.Now;
            await _collectionRepository.UpdateCollectionAsync(collection);
            return RedirectToAction("Index");
        }


    }
}