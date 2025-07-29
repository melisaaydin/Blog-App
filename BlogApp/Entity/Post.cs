using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace BlogApp.Entity
{
    public class Post
    {
        public int PostId { get; set; }
        [Required]
        public string? Title { get; set; }
        [Required]
        public string? Description { get; set; }
        [Required]
        public string? Content { get; set; }
        public string? Url { get; set; }
        public string? Image { get; set; }
        public bool IsActive { get; set; }
        public DateTime PublishedOn { get; set; }
        public int UserId { get; set; }
        public User? User { get; set; }
        public List<Comment>? Comments { get; set; }
        public List<Tag>? Tags { get; set; }
    }
}