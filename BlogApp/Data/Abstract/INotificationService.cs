namespace BlogApp.Data.Abstract
{
    public interface INotificationService
    {
        Task CreateNotificationAsync(string userId, string message, string linkUrl);
    }
}