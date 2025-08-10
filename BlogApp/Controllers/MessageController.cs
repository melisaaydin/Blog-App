using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using BlogApp.Data.Concrete.EfCore;
using BlogApp.Entity;
using BlogApp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BlogApp.Controllers
{
    [Authorize]
    public class MessageController : Controller
    {
        private readonly UserManager<User> _userManager;
        private readonly BlogContext _context;

        public MessageController(UserManager<User> userManager, BlogContext context)
        {
            _userManager = userManager;
            _context = context;
        }

        // GET: /Message/Index (Gelen Kutusu) 
        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var messages = await _context.Messages
                .Include(m => m.Sender)
                .Include(m => m.Receiver)
                .Where(m => m.ReceiverId == userId || m.SenderId == userId)
                .OrderByDescending(m => m.SentAt)
                .ToListAsync();

            var conversations = messages
                .GroupBy(m => m.ConversationId)
                .Select(g => g.First())
                .Select(m => new ConversationViewModel
                {
                    LastMessage = m,
                    OtherUser = m.SenderId == userId ? m.Receiver : m.Sender
                })
                .ToList();

            var followingIds = await _context.Follows
                .Where(f => f.FollowerId == userId)
                .Select(f => f.FollowingId)
                .ToListAsync();

            var followerIds = await _context.Follows
                .Where(f => f.FollowingId == userId)
                .Select(f => f.FollowerId)
                .ToListAsync();

            var mutualFollowIds = followingIds.Intersect(followerIds).ToList();

            var contacts = await _userManager.Users
                .Where(u => mutualFollowIds.Contains(u.Id))
                .ToListAsync();


            var model = new InboxViewModel
            {
                Conversations = conversations,
                Contacts = contacts
            };

            return View(model);
        }

        // GET: /Message/Chat/{username} 
        [HttpGet]
        public async Task<IActionResult> Chat(string? username)
        {
            if (string.IsNullOrEmpty(username))
            {
                return BadRequest("Username is required.");
            }

            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var otherUser = await _userManager.FindByNameAsync(username);

            if (otherUser == null || currentUserId == null)
            {
                return NotFound();
            }

            var ids = new[] { currentUserId, otherUser.Id }.OrderBy(id => id).ToList();
            var conversationId = $"{ids[0]}_{ids[1]}";

            var messages = await _context.Messages
                .Where(m => m.ConversationId == conversationId)
                .OrderBy(m => m.SentAt)
                .Include(m => m.Sender)
                .Include(m => m.Receiver)
                .ToListAsync();

            var model = new ChatViewModel
            {
                OtherUser = otherUser,
                Messages = messages
            };

            return View(model);
        }

        // POST: /Message/Chat 
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Chat(ChatViewModel model)
        {

            var receiverUserName = model.OtherUser?.UserName;
            if (string.IsNullOrEmpty(receiverUserName))
            {
                return BadRequest("Receiver username is missing.");
            }

            if (string.IsNullOrWhiteSpace(model.NewMessageContent))
            {
                return RedirectToAction("Chat", new { username = receiverUserName });
            }

            var senderId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var receiver = await _userManager.FindByNameAsync(receiverUserName);

            if (receiver == null || senderId == null)
            {
                return BadRequest();
            }

            var ids = new[] { senderId, receiver.Id }.OrderBy(id => id).ToList();
            var conversationId = $"{ids[0]}_{ids[1]}";

            var message = new Message
            {
                Content = model.NewMessageContent,
                SenderId = senderId,
                ReceiverId = receiver.Id,
                SentAt = DateTime.Now,
                ConversationId = conversationId
            };

            _context.Messages.Add(message);
            await _context.SaveChangesAsync();

            return RedirectToAction("Chat", new { username = receiver.UserName });
        }
    }
}