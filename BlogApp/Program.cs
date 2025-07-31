using System.Net;
using System.Net.Mail;
using BlogApp.Data.Abstract;
using BlogApp.Data.Concrete;
using BlogApp.Data.Concrete.EfCore;
using BlogApp.Entity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
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
    options.SignIn.RequireConfirmedAccount = false;

})
.AddEntityFrameworkStores<BlogContext>()
.AddDefaultUI()
.AddDefaultTokenProviders();

// Repositories
builder.Services.AddScoped<IPostRepository, EfPostRepository>();
builder.Services.AddScoped<ITagRepository, EfTagRepository>();
builder.Services.AddScoped<ICommentRepository, EfCommentRepository>();
builder.Services.AddScoped<IUserRepository, EfUserRepository>();

// Email Sender
// Bu kısmı kendi IEmailSender implementasyonunuza göre ayarlayın.
// Sizin kodunuzdaki gibi EmailSender sınıfı varsa bu satır doğrudur.
builder.Services.AddScoped<IEmailSender, EmailSender>();


var app = builder.Build();

// Veritabanını ve test verilerini doldur
await SeedDatabaseAsync(app);

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseStaticFiles();
app.UseRouting();
app.UseSession();
// Authentication ve Authorization middleware'lerini doğru sırada ekleyin
app.UseAuthentication();
app.UseAuthorization();

// Route tanımlamaları
app.MapControllerRoute(
    name: "post_details",
    pattern: "posts/details/{url}",
    defaults: new { controller = "Post", action = "Details" }
);
app.MapControllerRoute(
    name: "post_edit",
    pattern: "post/edit/{url}",
    defaults: new { controller = "Post", action = "Edit" }
);
app.MapControllerRoute(
    name: "post_by_tag",
    pattern: "posts/tag/{tag}",
    defaults: new { controller = "Post", action = "Index" }
);
app.MapControllerRoute(
    name: "user_profile",
    pattern: "profile/{username}",
    defaults: new { controller = "Users", action = "Profile" }
);
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}"
);

app.Run();


// Seeding işlemini yapan tek metod
static async Task SeedDatabaseAsync(IApplicationBuilder app)
{
    using var scope = app.ApplicationServices.CreateScope();
    var services = scope.ServiceProvider;
    try
    {
        // Tüm seeding işlemini sadece bu sınıf yapacak
        await SeedData.FillTheTestInfo(app);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred during database seeding.");
    }
}


// IEmailSender implementasyonu (Sizdeki kod)
public class EmailSender : IEmailSender
{
    private readonly IConfiguration _config;
    public EmailSender(IConfiguration config)
    {
        _config = config;
    }
    public async Task SendEmailAsync(string email, string subject, string htmlMessage)
    {
        var smtpServer = _config["EmailSettings:SmtpServer"];
        var smtpPort = int.Parse(_config["EmailSettings:SmtpPort"]);
        var smtpUsername = _config["EmailSettings:SmtpUsername"];
        var smtpPassword = _config["EmailSettings:SmtpPassword"];
        var fromEmail = _config["EmailSettings:FromEmail"];
        var fromName = _config["EmailSettings:FromName"];
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