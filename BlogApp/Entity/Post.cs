using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace BlogApp.Entity
{
    public class Post
    {
        public int PostId { get; set; }
        [Required]
        public string Title { get; set; } = string.Empty;
        [Required]
        public string Description { get; set; } = string.Empty;
        [Required]
        public string Content { get; set; } = string.Empty;
        public string? Url { get; set; }
        public string? Image { get; set; }
        public bool IsActive { get; set; }
        public DateTime PublishedOn { get; set; }
        public string UserId { get; set; } = string.Empty;
        public User? User { get; set; }
        public List<Comment> Comments { get; set; } = new List<Comment>();
        public List<Tag> Tags { get; set; } = new List<Tag>();
    }
}