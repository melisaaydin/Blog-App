using BlogApp.Data.Abstract;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BlogApp.ViewComponents
{
    public class NewPosts : ViewComponent
    {
        private IPostRepository _postRepository;
        public NewPosts(IPostRepository postRepository)
        {
            _postRepository = postRepository;
        }
        public async Task<IViewComponentResult> InvokeAsync()
        {
            var posts = _postRepository
                .Posts
                .Where(p => p.IsActive)
                .OrderByDescending(p => p.PublishedOn)
                .Take(5)
                .Include(p => p.Tags)
                .ToListAsync();

            return View(await posts);
        }
    }
}