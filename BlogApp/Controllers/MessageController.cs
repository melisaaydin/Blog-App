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
        private readonly ILogger<MessageController> _logger;

        public MessageController(UserManager<User> userManager, BlogContext context, ILogger<MessageController> logger)
        {
            _userManager = userManager;
            _context = context;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("Index action: Current user ID is null.");
                return BadRequest(new { message = "User not authenticated." });
            }
            _logger.LogInformation($"Index action called for user ID: {userId}");

            var messages = await _context.Messages
                .Include(m => m.Sender)
                .Include(m => m.Receiver)
                .Where(m => m.ReceiverId == userId || m.SenderId == userId)
                .OrderByDescending(m => m.SentAt)
                .ToListAsync();

            _logger.LogInformation($"Found {messages.Count} messages for user ID: {userId}");

            var conversations = messages
                .GroupBy(m => m.ConversationId)
                .Select(g => g.First())
                .Select(m => new ConversationViewModel
                {
                    LastMessage = m,
                    OtherUser = m.SenderId == userId ? m.Receiver : m.Sender
                })
                .Where(c => c.OtherUser != null && !string.IsNullOrEmpty(c.OtherUser.UserName) && c.OtherUser.UserName != User.Identity?.Name)
                .ToList();

            _logger.LogInformation($"Found {conversations.Count} conversations.");
            foreach (var convo in conversations)
            {
                _logger.LogInformation($"Conversation with {convo.OtherUser?.UserName ?? "null"}, Last message: {convo.LastMessage?.Content ?? "null"}");
            }

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
                .Where(u => mutualFollowIds.Contains(u.Id) && !string.IsNullOrEmpty(u.UserName) && u.UserName != User.Identity.Name)
                .ToListAsync();

            _logger.LogInformation($"Found {contacts.Count} mutual contacts.");

            var model = new InboxViewModel
            {
                Conversations = conversations,
                Contacts = contacts
            };

            return View(model);
        }

        [HttpGet]
        [Route("Message/Chat/{username?}")]
        public async Task<IActionResult> Chat(string? username)
        {
            _logger.LogInformation($"Chat GET action called with username: {username ?? "null"}");
            if (string.IsNullOrEmpty(username))
            {
                _logger.LogWarning("Username is null or empty.");
                return BadRequest(new { message = "Username is required." });
            }

            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(currentUserId))
            {
                _logger.LogWarning("Current user ID is null.");
                return BadRequest(new { message = "User not authenticated." });
            }

            var currentUser = await _userManager.FindByIdAsync(currentUserId);
            if (currentUser == null || currentUser.UserName == username)
            {
                _logger.LogWarning($"Invalid username: {username}. User cannot message themselves.");
                return BadRequest(new { message = "You cannot message yourself." });
            }

            var otherUser = await _userManager.Users.FirstOrDefaultAsync(u => u.NormalizedUserName == _userManager.NormalizeName(username));
            if (otherUser == null)
            {
                _logger.LogWarning($"User not found for username: {username}");
                return NotFound(new { message = $"User not found for username: {username}" });
            }

            var ids = new[] { currentUserId, otherUser.Id }.OrderBy(id => id).ToList();
            var conversationId = $"{ids[0]}_{ids[1]}";
            _logger.LogInformation($"Conversation ID: {conversationId}");

            var messages = await _context.Messages
                .Where(m => m.ConversationId == conversationId)
                .OrderBy(m => m.SentAt)
                .Include(m => m.Sender)
                .Include(m => m.Receiver)
                .ToListAsync();

            _logger.LogInformation($"Found {messages.Count} messages for conversation ID: {conversationId}");

            var unreadMessages = messages
                .Where(m => m.ReceiverId == currentUserId && !m.IsRead)
                .ToList();

            if (unreadMessages.Any())
            {
                unreadMessages.ForEach(m => m.IsRead = true);
                await _context.SaveChangesAsync();
                _logger.LogInformation($"Marked {unreadMessages.Count} messages as read.");
            }

            var model = new ChatViewModel
            {
                OtherUser = otherUser,
                Messages = messages
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Chat(ChatViewModel model)
        {
            _logger.LogInformation($"POST Chat action called with receiver username: {model.OtherUser?.UserName}");
            var receiverUserName = model.OtherUser?.UserName;
            if (string.IsNullOrEmpty(receiverUserName))
            {
                _logger.LogWarning("Receiver username is missing.");
                return BadRequest(new { message = "Receiver username is required." });
            }

            if (string.IsNullOrWhiteSpace(model.NewMessageContent))
            {
                _logger.LogWarning("Message content is empty.");
                return RedirectToAction("Chat", new { username = receiverUserName });
            }

            var senderId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var receiver = await _userManager.FindByNameAsync(receiverUserName);

            if (receiver == null || senderId == null)
            {
                _logger.LogWarning($"Receiver or sender not found. Receiver: {receiverUserName}, Sender ID: {senderId}");
                return BadRequest(new { message = "Invalid sender or receiver." });
            }

            if (senderId == receiver.Id)
            {
                _logger.LogWarning($"User {senderId} attempted to send a message to themselves.");
                return BadRequest(new { message = "You cannot send a message to yourself." });
            }

            var ids = new[] { senderId, receiver.Id }.OrderBy(id => id).ToList();
            var conversationId = $"{ids[0]}_{ids[1]}";

            var message = new Message
            {
                Content = model.NewMessageContent,
                SenderId = senderId,
                ReceiverId = receiver.Id,
                SentAt = DateTime.Now,
                IsRead = false,
                ConversationId = conversationId
            };

            _context.Messages.Add(message);
            await _context.SaveChangesAsync();
            _logger.LogInformation($"Message sent from {senderId} to {receiver.Id}: {model.NewMessageContent}");
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                var messages = await _context.Messages
                    .Where(m => m.ConversationId == conversationId)
                    .OrderBy(m => m.SentAt)
                    .Include(m => m.Sender)
                    .Include(m => m.Receiver)
                    .ToListAsync();

                var modelForPartial = new ChatViewModel
                {
                    OtherUser = receiver,
                    Messages = messages
                };

                return PartialView("Chat", modelForPartial);
            }
            return RedirectToAction("Chat", new { username = receiver.UserName });
        }
    }
}