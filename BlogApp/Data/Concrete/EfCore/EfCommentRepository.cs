using BlogApp.Data.Abstract;
using BlogApp.Data.Concrete.EfCore;
using BlogApp.Entity;
using Microsoft.EntityFrameworkCore;

namespace BlogApp.Data.Concrete
{
    public class EfCommentRepository : ICommentRepository
    {
        private readonly BlogContext _context;

        public EfCommentRepository(BlogContext context)
        {
            _context = context;
        }

        public IQueryable<Comment> Comments => _context.Comments;

        public async Task CreateComment(Comment comment)
        {
            _context.Comments.Add(comment);
            await _context.SaveChangesAsync();
        }


    }
}