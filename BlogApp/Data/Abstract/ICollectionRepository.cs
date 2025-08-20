using BlogApp.Entity;

namespace BlogApp.Data.Abstract
{
    public interface ICollectionRepository
    {
        IQueryable<Collection> Collections { get; }
        Task CreateCollectionAsync(Collection collection);
        Task UpdateCollectionAsync(Collection collection);
        Task DeleteCollectionAsync(int id);
        Task<Collection?> GetByIdAsync(int id);
        Task AddPostToCollectionAsync(int postId, int collectionId);
        Task RemovePostFromCollectionAsync(int postId, int collectionId);
        Task<List<Collection>> GetCollectionsByPostIdAsync(int postId);
    }
}