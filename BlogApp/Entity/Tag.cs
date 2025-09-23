using System;
using System.Collections.Generic;

namespace BlogApp.Entity;

public class Tag
{
    public int TagId { get; set; }
    public string? Text { get; set; }
    public string Url { get; set; } = null!;
    public string? CreatorId { get; set; }
    public User? Creator { get; set; }
    public List<Post> Posts { get; set; } = new List<Post>();
}