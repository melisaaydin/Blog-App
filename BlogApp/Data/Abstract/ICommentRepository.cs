using BlogApp.Entity;

namespace BlogApp.Data.Abstract
{
    public interface ICommentRepository
    {
        IQueryable<Comment> Comments { get; }
        Task CreateComment(Comment comment);
        Task<Comment?> GetCommentById(int id);
    }
}