using BlogApp.Data.Abstract;
using BlogApp.Data.Concrete.EfCore;
using BlogApp.Entity;
using SQLitePCL;

namespace BlogApp.Data.Concrete
{
    public class EfUserRepository : IUserRepository
    {
        private BlogContext _context;
        public EfUserRepository(BlogContext context)
        {
            _context = context;
        }
        public IQueryable<User> Users => _context.Users;

    }
}