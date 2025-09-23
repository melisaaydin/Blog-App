using System.Net;
using System.Net.Mail;
using BlogApp.Data.Abstract;
using BlogApp.Data.Concrete;
using BlogApp.Data.Concrete.EfCore;
using BlogApp.Entity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;

namespace BlogApp
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            try
            {
                var builder = WebApplication.CreateBuilder(args);

                builder.Services.AddControllersWithViews();
                builder.Services.AddSession(options =>
                {
                    options.IdleTimeout = TimeSpan.FromMinutes(30);
                    options.Cookie.HttpOnly = true;
                    options.Cookie.IsEssential = true;
                });
                builder.Services.AddLogging(logging =>
                {
                    logging.AddConsole();
                    logging.AddDebug();
                });
                builder.Services.AddDbContext<BlogContext>(options =>
                {
                    var connectionString = builder.Configuration.GetConnectionString("sql_connection");
                    options.UseSqlite(connectionString);
                });

                builder.Services.AddIdentity<User, IdentityRole>(options =>
                {
                    options.Password.RequiredLength = 6;
                    options.Password.RequireNonAlphanumeric = false;
                    options.Password.RequireDigit = false;
                    options.User.RequireUniqueEmail = true;
                    options.SignIn.RequireConfirmedAccount = true;
                })
                .AddEntityFrameworkStores<BlogContext>()
                .AddDefaultUI()
                .AddDefaultTokenProviders();

                builder.Services.Configure<DataProtectionTokenProviderOptions>(options =>
                {
                    options.TokenLifespan = TimeSpan.FromHours(3);
                });

                builder.Services.AddAntiforgery();
                builder.Services.AddScoped<IPostRepository, EfPostRepository>();
                builder.Services.AddScoped<ITagRepository, EfTagRepository>();
                builder.Services.AddScoped<ICommentRepository, EfCommentRepository>();
                builder.Services.AddScoped<IUserRepository, EfUserRepository>();
                builder.Services.AddScoped<INotificationService, EfNotificationService>();
                builder.Services.AddScoped<IEmailSender, EmailSender>();
                builder.Services.AddScoped<ICollectionRepository, EfCollectionRepository>();

                var app = builder.Build();
                app.Logger.LogInformation("Application is starting...");

                await SeedDatabaseAsync(app);

                if (!app.Environment.IsDevelopment())
                {
                    app.UseExceptionHandler("/Home/Error");
                    app.UseHsts();
                }
                if (app.Environment.IsDevelopment())
                {
                    app.UseDeveloperExceptionPage();
                }
                app.UseStaticFiles();
                app.UseRouting();
                app.UseSession();
                app.UseAntiforgery();
                app.UseAuthentication();
                app.UseAuthorization();

                app.MapControllerRoute(
                            name: "message_chat",
                            pattern: "Message/Chat/{username}",
                            defaults: new { controller = "Message", action = "Chat" }
                        );
                app.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Home}/{action=Index}/{id?}"
                );
                app.MapControllerRoute(
                    name: "post_details",
                    pattern: "posts/details/{url}",
                    defaults: new { controller = "Post", action = "Details" }
                );
                app.MapControllerRoute(
                    name: "post_by_tag",
                    pattern: "posts/tag/{url}",
                    defaults: new { controller = "Post", action = "Index" }
                );
                app.MapControllerRoute(
                    name: "user_profile",
                    pattern: "profile/{username}",
                    defaults: new { controller = "Users", action = "Profile" }
                );
                app.MapControllerRoute(
                    name: "admin_post_status_edit",
                    pattern: "Admin/EditPostStatus/{url}",
                    defaults: new { controller = "Admin", action = "EditPostStatus" }
                );

                app.MapControllerRoute(
                    name: "collection_details",
                    pattern: "collections/details/{id}",
                    defaults: new { controller = "Collection", action = "Details" }
                );
                app.MapControllerRoute(
                                name: "tag_index",
                                pattern: "tags",
                                defaults: new { controller = "Tag", action = "Index" }
                            );
                app.MapControllerRoute(
                    name: "tag_create",
                    pattern: "tags/create",
                    defaults: new { controller = "Tag", action = "Create" }
                );
                app.MapControllerRoute(
                    name: "tag_details",
                    pattern: "tags/details/{url}",
                    defaults: new { controller = "Tag", action = "Details" }
                );
                app.MapControllerRoute(
          name: "tag_delete",
          pattern: "tags/delete/{id}",
          defaults: new { controller = "Tag", action = "Delete" });
                app.MapGet("/", context =>
                {
                    context.Response.Redirect("/posts");
                    return Task.CompletedTask;
                });

                app.MapControllers();

                app.Logger.LogInformation("Application is running...");
                await app.RunAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Application failed to start: {ex.Message}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");
                throw;
            }
        }

        static async Task SeedDatabaseAsync(IApplicationBuilder app)
        {
            using var scope = app.ApplicationServices.CreateScope();
            var services = scope.ServiceProvider;
            var logger = services.GetRequiredService<ILogger<Program>>();
            try
            {
                logger.LogInformation("Starting database seeding...");
                await SeedData.FillTheTestInfo(app);
                logger.LogInformation("Database seeding completed successfully.");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Veritabanı seed işlemi sırasında hata oluştu: {Message}", ex.Message);
                logger.LogError("Stack Trace: {StackTrace}", ex.StackTrace);
            }
        }
    }

    public class EmailSender : IEmailSender
    {
        private readonly IConfiguration _config;

        public EmailSender(IConfiguration config)
        {
            _config = config;
        }

        public async Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            var smtpServer = _config["EmailSettings:SmtpServer"] ?? throw new InvalidOperationException("EmailSettings:SmtpServer is not configured.");
            var smtpPort = int.Parse(_config["EmailSettings:SmtpPort"] ?? "587");
            var smtpUsername = _config["EmailSettings:SmtpUsername"] ?? throw new InvalidOperationException("EmailSettings:SmtpUsername is not configured.");
            var smtpPassword = _config["EmailSettings:SmtpPassword"] ?? throw new InvalidOperationException("EmailSettings:SmtpPassword is not configured.");
            var fromEmail = _config["EmailSettings:FromEmail"] ?? throw new InvalidOperationException("EmailSettings:FromEmail is not configured.");
            var fromName = _config["EmailSettings:FromName"] ?? "BlogApp";

            using var client = new SmtpClient(smtpServer, smtpPort)
            {
                Credentials = new NetworkCredential(smtpUsername, smtpPassword),
                EnableSsl = true
            };

            var mailMessage = new MailMessage
            {
                From = new MailAddress(fromEmail, fromName),
                Subject = subject,
                Body = htmlMessage,
                IsBodyHtml = true
            };
            mailMessage.To.Add(email);
            await client.SendMailAsync(mailMessage);
        }
    }
}