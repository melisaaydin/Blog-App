using BlogApp.Data.Abstract;
using BlogApp.Entity;
using Microsoft.EntityFrameworkCore;

namespace BlogApp.Data.Concrete.EfCore
{
    public class EfCollectionRepository : ICollectionRepository
    {
        private readonly BlogContext _context;
        private readonly ILogger<EfCollectionRepository> _logger;

        public EfCollectionRepository(BlogContext context, ILogger<EfCollectionRepository> logger)
        {
            _context = context;
            _logger = logger;
        }

        public IQueryable<Collection> Collections => _context.Collections.Include(c => c.Creator).Include(c => c.Posts);

        public async Task CreateCollectionAsync(Collection collection)
        {
            _context.Collections.Add(collection);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateCollectionAsync(Collection collection)
        {
            _context.Collections.Update(collection);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteCollectionAsync(int id)
        {
            var collection = await _context.Collections.FindAsync(id);
            if (collection != null)
            {
                _context.Collections.Remove(collection);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<Collection?> GetByIdAsync(int id)
        {
            return await _context.Collections
                .Include(c => c.Posts).ThenInclude(p => p.User)
                .FirstOrDefaultAsync(c => c.Id == id);
        }

        public async Task AddPostToCollectionAsync(int postId, int collectionId)
        {
            var collection = await _context.Collections.FindAsync(collectionId);
            var post = await _context.Posts.FindAsync(postId);

            if (collection == null)
            {
                _logger.LogWarning("Collection not found: {CollectionId}", collectionId);
                return;
            }
            if (post == null)
            {
                _logger.LogWarning("Post not found: {PostId}", postId);
                return;
            }
            if (collection.Posts.Any(p => p.PostId == postId))
            {
                _logger.LogWarning("Post {PostId} already exists in collection {CollectionId}", postId, collectionId);
                return;
            }

            collection.Posts.Add(post);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Post {PostId} added to collection {CollectionId}", postId, collectionId);
        }

        public async Task RemovePostFromCollectionAsync(int postId, int collectionId)
        {
            var post = await _context.Posts.FindAsync(postId);
            var collection = await _context.Collections.FindAsync(collectionId);
            if (post != null && collection != null)
            {
                collection.Posts.Remove(post);
                await _context.SaveChangesAsync();
            }
        }
        public async Task<List<Collection>> GetCollectionsByPostIdAsync(int postId)
        {
            var collections = await _context.Collections
                .Include(c => c.Posts)
                .Where(c => c.Posts.Any(p => p.PostId == postId))
                .ToListAsync();
            _logger.LogInformation("Post {PostId} için {Count} koleksiyon bulundu", postId, collections.Count);
            return collections;
        }
    }
}