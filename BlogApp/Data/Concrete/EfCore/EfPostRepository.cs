using BlogApp.Data.Abstract;
using BlogApp.Data.Concrete.EfCore;
using BlogApp.Entity;
using Microsoft.EntityFrameworkCore;

namespace BlogApp.Data.Concrete
{
    public class EfPostRepository : IPostRepository
    {
        private readonly BlogContext _context;
        private readonly ILogger<EfPostRepository> _logger;

        public EfPostRepository(BlogContext context, ILogger<EfPostRepository> logger)
        {
            _context = context;
            _logger = logger;
        }

        public IQueryable<Post> Posts => _context.Posts;

        public async Task CreatePost(Post post)
        {
            _context.Posts.Add(post);
            await _context.SaveChangesAsync();
            _logger.LogInformation($"Post created with ID: {post.PostId}");
        }

        public async Task CreatePost(Post post, int[] tagIds)
        {
            try
            {
                if (tagIds != null && tagIds.Length > 0)
                {
                    var tags = await _context.Tags
                        .Where(t => tagIds.Contains(t.TagId))
                        .ToListAsync();
                    post.Tags = tags;
                }

                _context.Posts.Add(post);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Post {post.PostId} created successfully with tags: {string.Join(",", tagIds ?? new int[0])}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Post creation failed. PostId: {post.PostId}, Tags: {string.Join(",", tagIds ?? new int[0])}");
                throw;
            }
        }

        public void EditPost(Post post)
        {
            var entity = _context.Posts.FirstOrDefault(i => i.PostId == post.PostId);
            if (entity != null)
            {
                entity.Title = post.Title;
                entity.Description = post.Description;
                entity.Content = post.Content;
                entity.Url = post.Url;
                entity.Image = post.Image;
                entity.IsActive = post.IsActive;

                _context.SaveChanges();
                _logger.LogInformation($"Post {post.PostId} updated without tags");
            }
            else
            {
                _logger.LogWarning($"Post not found for ID: {post.PostId}");
            }
        }

        public async Task EditPost(Post post, int[] tagIds)
        {
            var entity = await _context.Posts
                .Include(p => p.Tags)
                .FirstOrDefaultAsync(p => p.PostId == post.PostId);

            if (entity == null)
            {
                _logger.LogWarning($"Post not found for ID: {post.PostId}");
                return;
            }

            entity.Title = post.Title;
            entity.Description = post.Description;
            entity.Content = post.Content;
            entity.Url = post.Url;
            entity.Image = post.Image;
            entity.IsActive = post.IsActive;

            if (tagIds != null && tagIds.Length > 0)
            {
                var tags = await _context.Tags
                    .Where(t => tagIds.Contains(t.TagId))
                    .ToListAsync();

                entity.Tags.Clear();
                foreach (var tag in tags)
                {
                    entity.Tags.Add(tag);
                }

                _logger.LogInformation($"Updated tags for Post {post.PostId}: {string.Join(",", tagIds)}");
            }
            else
            {
                entity.Tags.Clear();
                _logger.LogInformation($"Cleared all tags for Post {post.PostId}");
            }

            await _context.SaveChangesAsync();
            _logger.LogInformation($"Post {post.PostId} updated successfully");
        }

        public async Task DeletePost(int id)
        {
            var post = await _context.Posts
                .Include(p => p.Tags)
                .FirstOrDefaultAsync(p => p.PostId == id);

            if (post != null)
            {
                post.Tags.Clear(); // ilişkiyi temizle
                _context.Posts.Remove(post);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Post {id} deleted");
            }
            else
            {
                _logger.LogWarning($"Post not found for deletion, ID: {id}");
            }
        }
        public async Task UpdatePost(Post post)
        {
            _context.Update(post);
            await _context.SaveChangesAsync();
        }
    }
}
