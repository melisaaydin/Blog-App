using System;
using System.ComponentModel.DataAnnotations;

namespace BlogApp.Entity
{
    public class Comment
    {
        public int CommentId { get; set; }
        [Required]
        public string Text { get; set; } = string.Empty;
        public bool IsDeleted { get; set; }
        public DateTime PublishedOn { get; set; }
        public int PostId { get; set; }
        public Post? Post { get; set; }
        public string UserId { get; set; } = string.Empty;
        public User? User { get; set; }
    }
}