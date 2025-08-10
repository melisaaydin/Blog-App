namespace BlogApp.Entity
{
    public class Like
    {
        public int PostId { get; set; }
        public Post Post { get; set; } = null!;
        public string UserId { get; set; } = null!;
        public User User { get; set; } = null!;
    }
}