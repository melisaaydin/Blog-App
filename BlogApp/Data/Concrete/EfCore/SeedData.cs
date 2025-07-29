using BlogApp.Entity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace BlogApp.Data.Concrete.EfCore
{
    public static class SeedData
    {
        public static void FillTheTestInfo(IApplicationBuilder app)
        {
            using var scope = app.ApplicationServices.CreateScope();
            var context = scope.ServiceProvider.GetService<BlogContext>();

            if (context == null)
            {
                throw new InvalidOperationException("BlogContext is not registered in the service provider.");
            }

            // Veritabanı migration'larını uygula
            if (context.Database.GetPendingMigrations().Any())
            {
                context.Database.Migrate();
            }

            // Tag'leri ekle
            if (!context.Tags.Any())
            {
                context.Tags.AddRange(
                    new Tag { Text = "Skin Care", Url = "skin-care", Color = TagColors.primary },
                    new Tag { Text = "Beauty Routine", Url = "beauty-routine", Color = TagColors.secondary },
                    new Tag { Text = "Healthy Skin", Url = "healthy-skin", Color = TagColors.warning },
                    new Tag { Text = "Self-Care", Url = "self-care", Color = TagColors.danger },
                    new Tag { Text = "Django", Url = "django", Color = TagColors.primary },
                    new Tag { Text = "Python", Url = "python", Color = TagColors.success },
                    new Tag { Text = "Web Development", Url = "web-development", Color = TagColors.info },
                    new Tag { Text = "Backend Framework", Url = "backend-framework", Color = TagColors.warning },
                    new Tag { Text = "Retinol", Url = "retinol", Color = TagColors.success },
                    new Tag { Text = "Anti-Aging", Url = "anti-aging", Color = TagColors.danger },
                    new Tag { Text = "Acne Treatment", Url = "acne-treatment", Color = TagColors.info },
                    new Tag { Text = "Skin Products", Url = "skin-products", Color = TagColors.warning },
                    new Tag { Text = "Vitamin A", Url = "vitamin-a", Color = TagColors.success }
                );
                context.SaveChanges();
            }

            // Kullanıcıları ekle
            if (!context.Users.Any())
            {
                context.Users.AddRange(
                    new User { UserName = "mehmetcivan", Name = "Mehmet Civan", Email = "mehmetcivan@gmail.com", Password = "123456", Image = "1.jpg" },
                    new User { UserName = "melisaaydin", Name = "Melisa Aydın", Email = "melisaaydin@gmail.com", Password = "123456", Image = "1.jpg" }
                );
                context.SaveChanges();
            }

            // Post'ları ve PostTag ilişkilerini ekle
            if (!context.Posts.Any())
            {
                var tags = context.Tags.ToList();
                context.Posts.AddRange(
                    new Post
                    {
                        Title = "Skin Care",
                        Description = "Skin Care Information",
                        Content = "To select a skin care routine, we highly recommend booking a facial with a knowledgeable, certified professional. Estheticians have the experience and training to recommend professional treatments that boost your results as well as educated guidance for your home skin care routine. To enhance your knowledge, we’ve compiled the essential steps for a skin care routine as well as additional recommendations tailored to your unique skin type and concerns.",
                        Url = "skin-care",
                        IsActive = true,
                        PublishedOn = DateTime.Now.AddDays(-10),
                        Image = "skin.jpg",
                        UserId = 1,
                        Comments = new List<Comment> { new Comment { Text = "Deneme", PublishedOn = DateTime.Now, UserId = 1 } }
                    },
                    new Post
                    {
                        Title = "Retinol",
                        Description = "Retinol Usage",
                        Content = "Retinol, a form of vitamin A, is a staple in skin creams, lotions, and serums. It has anti-aging properties and can help clear up acne. Products containing retinol are widely available over-the-counter, while stronger retinoids are available by prescription.",
                        Url = "retinol",
                        IsActive = true,
                        PublishedOn = DateTime.Now.AddDays(-8),
                        Image = "retinol.jpeg",
                        UserId = 2
                    },
                    new Post
                    {
                        Title = "Django",
                        Description = "Inform about Django",
                        Content = "Django is a Python framework that makes it easier to create web sites using Python. Django takes care of the difficult stuff so that you can concentrate on building your web applications. Django emphasizes reusability of components, also referred to as DRY (Don't Repeat Yourself), and comes with ready-to-use features like login system, database connection and CRUD operations (Create Read Update Delete).",
                        Url = "django",
                        IsActive = true,
                        PublishedOn = DateTime.Now.AddDays(-20),
                        Image = "2.jpg",
                        UserId = 1
                    },
                    new Post
                    {
                        Title = "Healthy Life",
                        Description = "Talk about Healthy Life",
                        Content = "Getting enough regular physical activity or exercise, eating nourishing foods, and reducing your intake of sugar and alcohol are just some of the recommendations for maintaining a healthy lifestyle. There are many ways to maintain a healthy lifestyle. Tips range from maintaining a moderate weight and eating nourishing foods to getting enough physical activity and quality sleep.",
                        Url = "healthy-life",
                        IsActive = true,
                        PublishedOn = DateTime.Now.AddDays(-60),
                        Image = "3.jpeg",
                        UserId = 2
                    }
                );
                context.SaveChanges();
            }
        }
    }
}