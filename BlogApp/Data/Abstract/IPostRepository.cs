using BlogApp.Entity;

namespace BlogApp.Data.Abstract
{
    public interface IPostRepository
    {
        IQueryable<Post> Posts { get; }
        Task CreatePost(Post post);
        Task CreatePost(Post post, int[] tagIds);
        void EditPost(Post post);
        Task EditPost(Post post, int[] tagIds);
        Task DeletePost(int id);
        Task UpdatePost(Post post);
    }
}