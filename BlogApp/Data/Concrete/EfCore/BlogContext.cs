using BlogApp.Entity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace BlogApp.Data.Concrete.EfCore
{
    public class BlogContext : IdentityDbContext<User>
    {


        public DbSet<Post> Posts { get; set; }
        public DbSet<Comment> Comments { get; set; }
        public DbSet<Tag> Tags { get; set; }
        public DbSet<Follow> Follows { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<Like> Likes { get; set; }
        public DbSet<Message> Messages { get; set; }
        public DbSet<Collection> Collections { get; set; }
        public BlogContext(DbContextOptions<BlogContext> options) : base(options)
        {
        }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<Message>()
      .HasOne(m => m.Sender)
      .WithMany(u => u.SentMessages)
      .HasForeignKey(m => m.SenderId)
      .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Message>()
                .HasOne(m => m.Receiver)
                .WithMany(u => u.ReceivedMessages)
                .HasForeignKey(m => m.ReceiverId)
                .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Like>()
      .HasKey(l => new { l.PostId, l.UserId });

            modelBuilder.Entity<Like>()
                .HasOne(l => l.Post)
                .WithMany(p => p.Likes)
                .HasForeignKey(l => l.PostId);

            modelBuilder.Entity<Like>()
                .HasOne(l => l.User)
                .WithMany(u => u.Likes)
                .HasForeignKey(l => l.UserId);
            modelBuilder.Entity<Follow>()
                  .HasKey(f => new { f.FollowerId, f.FollowingId });

            modelBuilder.Entity<Follow>()
 .HasOne(f => f.Follower)
 .WithMany(u => u.Following)
 .HasForeignKey(f => f.FollowerId)
 .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Comment>()
                .HasOne(c => c.Post)
                .WithMany(p => p.Comments)
                .HasForeignKey(c => c.PostId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Follow>()
   .HasOne(f => f.Following)
   .WithMany(u => u.Followers)
   .HasForeignKey(f => f.FollowingId)
   .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Post>()
    .HasMany(p => p.Tags)
    .WithMany(t => t.Posts)
    .UsingEntity<Dictionary<string, object>>(
        "PostTag",
        pt => pt.HasOne<Tag>().WithMany().HasForeignKey("TagId"),
        pt => pt.HasOne<Post>().WithMany().HasForeignKey("PostId"));
            modelBuilder.Entity<Post>()
                    .HasMany(p => p.Collections)
                    .WithMany(c => c.Posts)
                    .UsingEntity<Dictionary<string, object>>(
                        "PostCollections",
                        j => j.HasOne<Collection>().WithMany().HasForeignKey("CollectionId"),
                        j => j.HasOne<Post>().WithMany().HasForeignKey("PostId"),
                        j =>
                        {
                            j.HasKey("PostId", "CollectionId");
                            j.ToTable("PostCollections");
                        });

            modelBuilder.Entity<Post>()
                .HasOne(p => p.User)
                .WithMany(u => u.Posts)
                .HasForeignKey(p => p.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Comment>()
                .HasOne(c => c.User)
                .WithMany(u => u.Comments)
                .HasForeignKey(c => c.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }

}