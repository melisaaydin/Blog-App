using BlogApp.Entity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

namespace BlogApp.Data.Concrete.EfCore
{
    public static class SeedData
    {
        public static async Task FillTheTestInfo(IApplicationBuilder app)
        {
            using var scope = app.ApplicationServices.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<BlogContext>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

            logger.LogInformation("Database seeding process started.");

            if (context.Database.GetPendingMigrations().Any())
            {
                logger.LogInformation("Applying migrations...");
                await context.Database.MigrateAsync();
            }

            string[] roleNames = { "Admin", "User" };
            foreach (var roleName in roleNames)
            {
                if (!await roleManager.RoleExistsAsync(roleName))
                {
                    await roleManager.CreateAsync(new IdentityRole(roleName));
                }
            }

            User? adminUser = await userManager.FindByEmailAsync("melisaaydin@gmail.com");
            if (adminUser == null)
            {
                logger.LogInformation("Admin user not found, creating new one...");
                adminUser = new User { UserName = "melisaaydin", Name = "Melisa Aydın", Email = "melisaaydin@gmail.com", EmailConfirmed = true };
                var result = await userManager.CreateAsync(adminUser, "Password123!");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(adminUser, "Admin");
                    await userManager.AddToRoleAsync(adminUser, "User");
                }
                else
                {
                    logger.LogError("Admin user creation failed: {Errors}", string.Join(", ", result.Errors.Select(e => e.Description)));
                    adminUser = null;
                }
            }

            User? normalUser = await userManager.FindByEmailAsync("mehmetcivan@gmail.com");
            if (normalUser == null)
            {
                logger.LogInformation("Normal user not found, creating new one...");
                normalUser = new User { UserName = "mehmetcivan", Name = "Mehmet Civan", Email = "mehmetcivan@gmail.com", EmailConfirmed = true };
                var result = await userManager.CreateAsync(normalUser, "Password123!");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(normalUser, "User");
                }
                else
                {
                    normalUser = null;
                }
            }

            if (!context.Tags.Any())
            {
                if (adminUser != null)
                {
                    context.Tags.AddRange(
                        new Tag { Text = "Skin Care", Url = Regex.Replace("Skin Care".ToLower(), @"[^a-z0-9]+", "-").Trim('-'), CreatorId = adminUser.Id },
                        new Tag { Text = "Beauty Routine", Url = Regex.Replace("Beauty Routine".ToLower(), @"[^a-z0-9]+", "-").Trim('-'), CreatorId = adminUser.Id },
                        new Tag { Text = "Healthy Skin", Url = Regex.Replace("Healthy Skin".ToLower(), @"[^a-z0-9]+", "-").Trim('-'), CreatorId = adminUser.Id },
                        new Tag { Text = "Self-Care", Url = Regex.Replace("Self-Care".ToLower(), @"[^a-z0-9]+", "-").Trim('-'), CreatorId = adminUser.Id },
                        new Tag { Text = "Web Development", Url = Regex.Replace("Web Development".ToLower(), @"[^a-z0-9]+", "-").Trim('-'), CreatorId = adminUser.Id },
                        new Tag { Text = "Technology", Url = Regex.Replace("Technology".ToLower(), @"[^a-z0-9]+", "-").Trim('-'), CreatorId = adminUser.Id },
                        new Tag { Text = "Lifestyle", Url = Regex.Replace("Lifestyle".ToLower(), @"[^a-z0-9]+", "-").Trim('-'), CreatorId = adminUser.Id }
                    );
                    await context.SaveChangesAsync();
                }
            }

            if (!context.Posts.Any())
            {
                if (adminUser != null && normalUser != null)
                {
                    logger.LogInformation("Users are available, creating posts...");
                    context.Posts.AddRange(
                       new Post
                       {
                           Title = "Retinol Usage",
                           Description = "Learn how to use it effectively.",
                           Content = "...",
                           Url = "retinol-usage",
                           IsActive = true,
                           PublishedOn = DateTime.Now.AddDays(-5),
                           Image = "retinol.jpeg",
                           UserId = adminUser.Id,
                           Tags = await context.Tags.Where(t => t.Url == "skin-care").ToListAsync()
                       },
                       new Post
                       {
                           Title = "Healthy Life",
                           Description = "Tips for a healthier lifestyle.",
                           Content = "...",
                           Url = "healthy-life",
                           IsActive = true,
                           PublishedOn = DateTime.Now.AddDays(-15),
                           Image = "3.jpeg",
                           UserId = normalUser.Id,
                           Tags = await context.Tags.Where(t => t.Url == "self-care").ToListAsync()
                       }
                   );
                    await context.SaveChangesAsync();
                    logger.LogInformation("Posts created successfully.");
                }
                else
                {
                    logger.LogError("Could not create posts because one or more seed users are null after creation attempt.");
                }
            }

            logger.LogInformation("Database seeding process finished.");
        }
    }
}