namespace BlogApp.Entity
{
    public class Follow
    {
        public string FollowerId { get; set; } = null!;
        public User Follower { get; set; } = null!;

        public string FollowingId { get; set; } = null!;
        public User Following { get; set; } = null!;
    }
}