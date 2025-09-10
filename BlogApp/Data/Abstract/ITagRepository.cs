using BlogApp.Entity;

namespace BlogApp.Data.Abstract
{
    public interface ITagRepository
    {
        IQueryable<Tag> Tags { get; }
        void CreateTag(Tag tag);
        Task AddAsync(Tag tag);
        void Delete(Tag tag);
        Task SaveChangesAsync();
    }
}